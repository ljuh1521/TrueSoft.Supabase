# 우편함(Mailbox) 테스트 가이드

## 빠른 시작

### 1. SQL Editor에서 테스트 우편 발송

`MailboxTestData.sql` 파일을 열어 아래 값을 교체한 후 SQL Editor에서 실행:

```sql
'YOUR_ACCOUNT_UUID'::uuid  →  '실제-유저-uuid'::uuid
'YOUR_USER_ID'             →  '실제-user-id'
```

### 2. Unity에서 테스트

1. **씬에 빈 GameObject 생성** → `MailboxTestSample.cs` 스크립트 추가
2. **Inspector 설정**:
   - `Auto Fetch On Start`: 체크 (자동 테스트 실행)
3. **Play 실행** → Console 로그 확인

## 테스트 종류별 시나리오

| 테스트 | SQL 데이터 | Unity 테스트 | 예상 결과 |
|--------|-----------|--------------|----------|
| **단일 보상** | `테스트 1: 골드만` | `TestClaimSingleMail` | 골드 1000 수령 |
| **복합 보상** | `테스트 2: 복합 보상` | `TestClaimSingleMail` | 골드+보석+무기+포션 수령 |
| **텍스트 전용** | `테스트 3: 공지사항` | 읽음 처리만 | 보상 없이 읽음 처리만 가능 |
| **만료 메일** | `테스트 5: 만료됨` | 조회/수령 시도 | 만료로 인해 수령 불가 |
| **빈 보상** | `테스트 6: 빈 배열` | 수령 시도 | no-op으로 성공 (빈 목록 반환) |
| **일괄 수령** | 여러 개 미수령 | `TestClaimAllMails` | 모든 미수령 보상 한 번에 수령 |

## 핸들러 미등록 시 오류 테스트

새로운 `key`를 가진 우편을 발송하고 핸들러를 등록하지 않으면 수령 시 오류 발생:

```sql
-- 핸들러 없는 아이템
INSERT INTO public.mails (...) VALUES (...,
    '[{"key": "unknown_item", "count": 1}]'::jsonb
);
```

Unity에서 수령 시도 시:
```
[Error] mail_item_handler_missing:unknown_item
```

## UI 버튼 연동 예시

```csharp
using Truesoft.Supabase.Unity;
using UnityEngine;
using UnityEngine.UI;

public class MailUI : MonoBehaviour
{
    [SerializeField] private Button claimButton;
    [SerializeField] private Button claimAllButton;
    [SerializeField] private Text statusText;

    private string currentMailId;

    void Start()
    {
        claimButton.onClick.AddListener(async () => {
            var results = await Supabase.TryClaimMailItemsAsync(currentMailId);
            statusText.text = results != null 
                ? $"수령 완료: {results.Count}개 아이템" 
                : "수령 실패";
        });

        claimAllButton.onClick.AddListener(async () => {
            var results = await Supabase.TryClaimAllMailItemsAsync();
            statusText.text = results != null 
                ? $"일괄 수령: {results.Count}개 아이템" 
                : "수령할 보상 없음";
        });
    }
}
```

## 디버깅 팁

### Console 로그 해석

```
[Mailbox] 미읽음: 3개, 미수령 보상: 2개
  - [미수령] 복합 보상 패키지 (발신: 이벤트팀, 만료: 2025-04-20)
  - [수령완료/없음] 골드 보상 (발신: 운영팀, 만료: 2025-04-15)
  - [만료] 만료된 메일 (발신: 운영팀, 만료: 2025-04-09)

[Mailbox] 수령 완료! 4개 아이템:
  - [0] gold: 5000개
  - [1] gem: 100개
  - [2] weapon_001: 1개
  - [3] potion_hp: 5개
```

### 핸들러 등록 확인

```csharp
// 게임 시작 시 핸들러 등록 로그 확인
void Start()
{
    // 정상 등록 시 출력
    // [Mailbox] 핸들러 등록 완료: Gold, Gem, Weapon, Consumable, TestItem
}
```

### 자주 발생하는 오류

| 오류 메시지 | 원인 | 해결 |
|-----------|------|------|
| `auth_not_signed_in` | 로그인 안 됨 | `Supabase.TrySignIn...` 먼저 호출 |
| `mail_item_handler_missing:X` | X 핸들러 미등록 | `MailItemHandlerRegistry.Register(new XHandler())` |
| `mail_expired` | 만료일 지남 | SQL에서 `expires_at` 수정 |
| `cannot_delete_unclaimed` | 미수령 보상 있음 | 먼저 수령 후 삭제 시도 |
| `forbidden_server` | 서버 불일치 | `profiles.server_id` 확인 |

## 고급 테스트

### 특정 조건 필터 테스트

```csharp
// 만료 임박 메일만 조회 (3일 이내)
var urgentMails = mails.Where(m => 
    m.ExpiresAt <= DateTime.UtcNow.AddDays(3) && 
    m.HasUnclaimedItems
).ToList();
```

### 수령 후 이벤트 연동

```csharp
// 핸들러 내부에서 이벤트 발행
public class GemMailHandler : IMailItemHandler
{
    public event Action<int> OnGemAcquired;

    public async Task<SupabaseResult<ClaimResult>> HandleAsync(...)
    {
        OnGemAcquired?.Invoke(count);
        // ...
    }
}
```

## 테스트 정리

테스트 완료 후 정리:

```sql
-- 테스트 데이터 삭제
DELETE FROM public.mails 
WHERE title LIKE '[테스트]%' 
   OR sender_name IN ('GM', '테스트');
```
