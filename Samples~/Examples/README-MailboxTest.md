# 우편함(Mailbox) 샘플

UPM **Examples** 샘플을 Import한 뒤 `MailboxTestSample` 컴포넌트를 씬에 추가해 사용합니다.

## 1. SQL로 테스트 우편 발송

패키지 소스의 `Sql/samples/MailboxTestData.sql`을 Supabase **SQL Editor**에서 열고, `YOUR_ACCOUNT_UUID` / `YOUR_USER_ID`를 본인 값으로 바꾼 뒤 실행합니다.

(`11_mails.sql` 등 우편 스키마가 이미 적용되어 있어야 합니다.)

- **읽은 메일 일괄 숨김**: DB RPC `ts_delete_read_mails_for_user` — Unity에서는 `Supabase.TryDeleteReadMailsAsync()` (반환: 삭제한 건수, 실패 시 `null`).
- **PostgREST 직접 PATCH 방지(선택)**: `Sql/player/11_mails_client_hardening.sql` — 적용 전 주석의 체크리스트를 읽을 것.

## 2. Unity

1. Package Manager → **Truesoft Supabase SDK** → Samples → **Examples** → **Import**
2. 씬에 GameObject 추가 → **`MailboxTestSample`** 붙이기
3. **로그인(또는 저장된 세션 복원)** 이 끝난 뒤에만 우편 API가 동작합니다. SDK의 `Supabase.TryStartAsync` 등으로 세션이 잡힌 상태에서 테스트하세요.
4. Play 후 테스트: Inspector에서 **`Auto Fetch On Start`** 를 켜면(세션이 있을 때만) 시작 시 전체 흐름이 돌고, 끈 상태면 컴포넌트 우클릭 → **`Test: 전체 흐름`** / **`Test: 목록 조회`** 등을 사용합니다.
5. **무보상 우편 읽음(상세 시 DB 읽음)**: `MailboxTestData.sql`에 공지(`items` NULL) 또는 빈 배열 메일이 있어야 합니다. 로그인 후 **`Test: 무보상 상세 → 읽음 확인`** — 콘솔에 목록 시점 `읽음`과 상세 후 `읽음=true`가 나오는지 봅니다. 임의 ID로 보려면 `testMailId`에 UUID 넣고 **`Test: testMailId 상세만`**.

## 3. 핸들러

`items`의 `key`와 동일한 `IMailItemHandler`를 `MailItemHandlerRegistry`에 등록해야 수령이 진행됩니다. 샘플에 `gold`, `gem`, `weapon_001`, `potion_hp`, `test_item` 핸들러가 포함되어 있습니다.

## 정리용 SQL

```sql
DELETE FROM public.mails
WHERE title LIKE '[테스트]%'
   OR sender_name IN ('GM', '테스트');
```
