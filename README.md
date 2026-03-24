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

## 샘플

Package Manager의 **Import Samples**로 사용할 수 있는 샘플을 제공합니다.

- **Basic Setup**: 최소 데모 (게스트 로그인, 저장/불러오기, 이벤트)
- **Full SDK Usage**: 인증, 원격 설정, 함수 호출 등 전체 흐름

샘플은 `Samples~` 폴더에 있어 기본 컴파일 대상에서 제외되며, Import한 뒤에만 프로젝트에 복사됩니다.