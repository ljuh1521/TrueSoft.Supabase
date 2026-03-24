# Truesoft Supabase SDK

Unity Package Manager로 설치하는 Supabase Auth, REST API, Edge Functions용 SDK입니다.

## 설치

Window > Package Manager > + > Install package from git URL

예시:
https://github.com/your-org/com.truesoft.supabase.git#0.1.0

## 준비

1. 메뉴 **TrueSoft > Supabase > Create Settings Asset** 으로 `SupabaseSettings` 를 만듭니다.
2. `projectUrl`, `publishableKey` 를 입력합니다.
3. 에셋을 **`Assets/Resources/SupabaseSettings.asset`** 으로 저장합니다. (`Resources.Load("SupabaseSettings")` 와 이름이 일치해야 합니다.)
4. 씬에 **`SupabaseRuntime`** 이 있어야 합니다. 메뉴 **TrueSoft > Supabase > Create Runtime Object In Scene** 으로 추가하거나, 샘플 스크립트가 런타임에 생성할 수 있습니다.

## 제공 범위

- **인증**: 게스트 로그인, Google ID 토큰 로그인·연동, 세션 복원
- **사용자 데이터**: 저장·불러오기
- **사용자 이벤트**: 전송
- **원격 설정**: 구독, 새로고침, 폴링, 캐시에서 값 읽기
- **Edge Functions**: 호출
- **채팅**: 채널 입장·전송·이탈

## 샘플 (Package Manager)

- **Basic Setup**: 최소 데모(게스트 로그인, 저장·불러오기, 이벤트 전송)
- **Full SDK Usage**: 인증·사용자 데이터·이벤트·원격 설정·Edge Functions·채팅까지 한 번에 보는 샘플

패키지 루트의 `Samples/BasicSetup`, `Samples/FullSDKUsage` 가 Git에 포함되어 있어야 Import 가 동작합니다.

### 샘플 Import 오류 (`path does not exist`)

1. **원격 저장소 최신 커밋**에 위 `Samples/...` 폴더가 있는지 확인한 뒤, `manifest.json` 의 Git URL 을 해당 브랜치·태그로 맞춥니다.  
2. Unity 에서 **Window > Package Manager** 에서 패키지 선택 후 **Remove** 한 다음 다시 추가하거나, 프로젝트의 `Library/PackageCache/com.truesoft.supabase@...` 폴더를 지운 뒤 Unity 를 다시 열어 캐시를 받아옵니다.