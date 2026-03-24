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

- Auth: 게스트 로그인, 구글 ID 토큰 로그인/연동, 세션 복원
- User 데이터: 저장/로드
- User 이벤트: 전송
- RemoteConfig: 구독/새로고침/폴링/캐시 Get
- Edge Functions: 함수 호출
- Chat: 채널 입장/전송/이탈

## 샘플 (Package Manager)

- **Basic Setup**: 최소 데모 (게스트, Save/Load, 이벤트)
- **Full SDK Usage**: RemoteConfig, Edge Functions, Chat 등 전체 흐름