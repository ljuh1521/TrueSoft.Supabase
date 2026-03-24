# Basic Setup 샘플

게스트 로그인 → 사용자 데이터 저장·불러오기 → 사용자 이벤트 전송까지의 **최소 데모**입니다.  
(`EnsureInitializedAsync` / `SignInAnonymouslyAsync`가 초기화·로그인 여부를 SDK 쪽에서 처리합니다.)

## 1. 샘플 가져오기

1. Unity 메뉴 **Window** > **Package Manager**
2. 왼쪽 목록에서 **Truesoft Supabase SDK** 선택
3. 패키지 상세 영역에서 **Samples** 섹션을 펼침
4. **Basic Setup** 옆 **Import** 클릭

Import 후 스크립트는 예시로 다음 경로에 생깁니다.

```text
Assets/Samples/Truesoft Supabase SDK/<버전>/Basic Setup/
```

(표시 이름·버전은 프로젝트에 따라 다를 수 있습니다.)

## 2. 실행 전 준비

1. **TrueSoft** > **Supabase** > **Create Settings Asset** 으로 `SupabaseSettings` 생성
2. `projectUrl`, `publishableKey` 입력
3. 에셋을 **`Assets/Resources/SupabaseSettings.asset`** 으로 저장 (파일명 `SupabaseSettings`)
4. Supabase 대시보드 **Authentication** → **Anonymous sign-ins** 활성화
5. (선택) **TrueSoft** > **Supabase** > **Create Runtime Object In Scene** 으로 `SupabaseRuntime` 배치  
   ※ 이 샘플은 `Resources/SupabaseSettings`가 있으면 SDK가 내부에서 초기화/로그인을 처리하므로, 기본 흐름은 런타임 오브젝트 없이도 동작합니다.

## 3. 씬에서 실행

1. 테스트용 씬을 엽니다.
2. **Hierarchy**에서 빈 GameObject 생성 (이름은 자유)
3. **Add Component** → `ExampleBootstrap` 추가
4. **Play**  
   - `Run On Start`가 켜져 있으면 자동 실행됩니다.
5. **수동 실행**  
   - `ExampleBootstrap` 컴포넌트 우클릭 → **Run Basic Setup**

## 4. 인스펙터

| 필드 | 설명 |
|------|------|
| Run On Start | Play 시 자동 실행 여부 |
| Level / Coins | 저장·불러오기에 쓰는 예시 값 |

## 5. 확인

- Console에 `[Supabase.*]` Try API 로그와 샘플 완료용 `[BasicSetup] done.` 로그가 보이면 정상입니다.
- 실패 시 **`[Supabase 초기화 점검]`** 메시지를 따라 설정을 확인하세요.

## 6. 샘플 삭제

`Assets/Samples/.../Basic Setup` 폴더를 지우면 이 샘플만 제거됩니다.
