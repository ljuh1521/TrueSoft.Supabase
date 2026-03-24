# Truesoft Supabase SDK

Unity Package Manager로 설치하는 Supabase Auth, REST API, Edge Functions용 SDK입니다.

## 설치

Window > Package Manager > + > Install package from git URL

예시:
https://github.com/your-org/com.truesoft.supabase.git#0.1.0

## 준비

`SupabaseSettings`와 `SupabaseRuntime`의 역할은 다음처럼 구분합니다.

- **SupabaseSettings (공통 설정값)**: 프로젝트 URL, publishable key, Google Web Client ID, 기본 로그/타임아웃 같은 정적 값을 정의합니다.
- **SupabaseRuntime (씬 실행 정책)**: 시작 시 세션 복원 여부, RemoteConfig 첫 로드/폴링 주기 같은 런타임 동작 시점을 제어합니다.

1. 메뉴 **TrueSoft > Supabase > Create Settings Asset** 으로 `SupabaseSettings` 를 만듭니다.
2. `projectUrl`, `publishableKey` 를 입력합니다. (Android 네이티브 Google 로그인을 쓰면 `googleWebClientId` 도 입력)
3. API 결과 로그를 제어하려면 `enableApiResultLogs`를 설정합니다. (`true`면 Try API별 고정 태그(예: `[Supabase.UserData.Save]`)로 성공/실패 로그 출력. 호출자가 태그를 넘기지 않습니다.)
4. 에셋을 **`Assets/Resources/SupabaseSettings.asset`** 으로 저장합니다. (`Resources.Load("SupabaseSettings")` 와 이름이 일치해야 합니다.)
5. `SupabaseRuntime`은 선택 사항입니다.  
   - 인증/데이터/함수/채팅의 기본 비동기 API는 SDK 내부에서 초기화를 대기하고, 필요 시 `Resources/SupabaseSettings`로 자동 부트스트랩합니다.  
   - 자동 세션 복원/RemoteConfig 주기 폴링까지 씬 라이프사이클로 관리하려면 `SupabaseRuntime` 배치를 권장합니다.

## 제공 범위

- **초기화/세션 준비**: `Supabase.TryStartAsync()`를 기본 진입점으로 사용합니다. 이 단계는 초기화/세션 복원만 담당하며 자동 익명 로그인은 수행하지 않습니다.
- **인증**: `TrySignInAnonymouslyAsync`, `TrySignInWithGoogleAsync`, `TrySignInWithGoogleIdTokenAsync`, `TryRestoreSessionAsync`
- **사용자 데이터**: `TrySaveUserDataAsync`, `TryLoadUserDataAsync`
- **원격 설정**: 구독, `TryRefreshRemoteConfigAsync`, `TryPollRemoteConfigAsync`, `TryGetRemoteConfigAsync`, 캐시 조회
- **Edge Functions**: `TryInvokeFunctionAsync`
- **채팅**: `TryJoinChatChannelAsync`, `TrySendChatMessageAsync`, 채널 이탈

## 샘플

Package Manager의 **Samples** 탭에서 **Import**로 프로젝트에 복사해 사용합니다.

| 샘플 | 내용 |
|------|------|
| **Examples** | 로그인, 데이터 저장/불러오기, RemoteConfig, Edge Function을 기능별 함수로 분리한 데모 |

샘플 소스는 패키지 안의 `Samples~`에만 있고, **Import 전에는 프로젝트에 컴파일되지 않습니다.** Import 후에는 `Assets/Samples/<패키지 표시 이름>/<버전>/` 아래에 복사됩니다.

### 샘플 사용 방법

1. **패키지 설치**  
   `Window` > `Package Manager`에서 `Truesoft Supabase SDK`를 선택합니다.

2. **샘플 가져오기**  
   패키지 상세 패널에서 **Samples** 목록을 펼칩니다.  
   - **Examples** 옆 **Import**를 누릅니다.

3. **실행 전 준비** (루트 README의 **준비** 섹션과 동일)  
   - `SupabaseSettings` 생성 후 `projectUrl`, `publishableKey` 입력  
   - `Assets/Resources/SupabaseSettings.asset` 경로·이름 유지  
   - Supabase 대시보드에서 **Anonymous sign-ins** 활성화 (Basic/Full 게스트 로그인 사용 시)

4. **씬 실행**  
   - 빈 씬 또는 테스트용 씬을 엽니다.  
   - Hierarchy에 빈 GameObject를 만들고 `ExampleSupabaseScenarios` 컴포넌트를 붙입니다.  
   - **Play** 시 `runOnStart`가 켜져 있으면 자동 실행됩니다.  
   - 수동 실행 시 인스펙터에서 컴포넌트 우클릭 → **Run All Examples** 또는 기능별 실행 메뉴를 사용합니다.

5. **결과 확인**  
   - Console에 `[Supabase.*]` 형태의 Try API 로그와 샘플 스크립트의 `[Sample]` 로그가 나오면 성공입니다.  
   - 초기화가 안 되면 Console의 **`[Supabase 초기화 점검]`** 을 따릅니다.

6. **샘플 제거**  
   - `Assets/Samples/Truesoft Supabase SDK/0.1.0/` (버전은 다를 수 있음) 폴더를 삭제하거나, Import한 샘플 폴더만 삭제합니다.

각 샘플 폴더 안의 `README.md`에도 동일한 내용을 요약해 두었습니다.