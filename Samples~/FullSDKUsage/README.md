# Full SDK Usage 샘플

이 샘플은 Unity에서 제공하는 SDK의 주요 흐름을 “한 번에” 보여줍니다.

- 게스트 로그인
- (선택) 구글 계정 링크/로그인 (ID 토큰 사용)
- 유저 데이터 Save/Load
- 유저 이벤트 전송
- RemoteConfig 구독/새로고침/Get
- Edge Function 호출
- 채팅 채널 입장/전송/이탈

## 실행 전 준비

1. `SupabaseSettings`를 `Resources/SupabaseSettings.asset`에 준비합니다.
   - (예: Unity 메뉴 `TrueSoft/Supabase/Create Settings Asset`으로 생성 후 값 입력)
2. `ExampleSupabaseAllFeatures` 스크립트를 원하는 GameObject에 붙입니다.
   - `runOnStart=true` 이므로 Play 시 자동으로 실행됩니다.
3. Inspector에서 샘플 필드를 설정합니다.
   - `remoteConfigKey`
   - `functionName`
   - `chatChannelId`
4. 샘플에서 구글 링크/로그인을 하고 싶으면 `googleIdTokenForLinkOrSignIn` 값을 넣습니다.

## 주의사항

- 현재 RemoteConfig는 “엄격 모드”로 동작합니다. `value_json`은 항상 객체 루트 JSON (`{...}`) 형태여야 합니다.
- Edge Function의 JWT 검증 설정이 프로젝트 설정에 따라 추가로 조정이 필요할 수 있습니다.
