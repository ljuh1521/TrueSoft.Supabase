# Basic Setup 샘플

**최소 데모**(약 5분): 게스트 로그인 → 사용자 데이터 저장·불러오기 → 사용자 이벤트 전송.

원격 설정·Edge Functions·채팅은 **Full SDK Usage** 샘플을 가져오세요.

## 실행 전 체크리스트

1. **Settings**: `TrueSoft > Supabase > Create Settings Asset` → `projectUrl`, `publishableKey` 입력  
2. **Resources**: 에셋을 **`Assets/Resources/SupabaseSettings.asset`** 으로 저장 (파일명은 반드시 `SupabaseSettings`)  
3. **Runtime**: Play 시 이 샘플이 `SupabaseRuntime` GameObject를 만들 수 있습니다. 수동으로 넣으려면 `TrueSoft > Supabase > Create Runtime Object In Scene`  
4. Supabase 대시보드에서 **Anonymous sign-ins** 활성화

## 실행 방법

1. `ExampleBootstrap`을 아무 GameObject에 붙입니다.
2. `runOnStart=true`이면 Play 시 자동 실행됩니다.
3. 콘솔: `[BasicSetup] Load 결과: level=..., coins=...`

## 인스펙터

- `level`, `coins`: 저장·불러오기에 쓰는 예시 값입니다.

초기화가 안 되면 콘솔에 출력되는 **`[Supabase 초기화 점검]`** 블록을 따르세요.
