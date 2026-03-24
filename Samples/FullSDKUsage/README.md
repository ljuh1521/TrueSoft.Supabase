# Full SDK Usage 샘플

Unity SDK 주요 흐름을 **한 번에** 확인합니다.

- 게스트 로그인  
- (선택) Google ID 토큰 로그인·연동  
- 사용자 데이터 저장·불러오기  
- 사용자 이벤트 전송  
- 원격 설정: 구독, 새로고침, 캐시에서 값 읽기  
- Edge Functions 호출  
- 채팅: 입장·전송·이탈  

## 실행 전 체크리스트

1. **Settings**: `TrueSoft > Supabase > Create Settings Asset` → `projectUrl`, `publishableKey` 입력  
2. **Resources**: **`Assets/Resources/SupabaseSettings.asset`** (이름 고정: `SupabaseSettings`)  
3. **Runtime**: Play 시 샘플이 `SupabaseRuntime`을 생성할 수 있습니다. 수동: `TrueSoft > Supabase > Create Runtime Object In Scene`  
4. `ExampleSupabaseAllFeatures`를 GameObject에 붙이고 `runOnStart=true` 유지  
5. 인스펙터에서 필요 시 조정: `remoteConfigKey`, `functionName`, `chatChannelId`, (선택) `googleIdTokenForLinkOrSignIn`  

## 주의

- 원격 설정 `value_json`은 **엄격 모드**: 객체 루트 JSON(`{...}`)만 허용됩니다.  
- Edge Functions JWT·권한은 프로젝트 설정에 따라 추가 조정이 필요할 수 있습니다.  

초기화 타임아웃 시 콘솔의 **`[Supabase 초기화 점검]`** 을 확인하세요.
