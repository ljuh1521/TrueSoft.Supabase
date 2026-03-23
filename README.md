# Truesoft Supabase SDK

Unity Package Manager로 설치하는 Supabase Auth, REST API, Edge Functions용 SDK입니다.

## 설치

Window > Package Manager > + > Install package from git URL

예시:
https://github.com/your-org/com.truesoft.supabase.git#0.1.0

## 준비

1. `TrueSoft/Supabase/Create Settings Asset` 메뉴로 `SupabaseSettings` 에셋을 생성합니다.
2. `SupabaseSettings`에 `projectUrl`과 `publishableKey`를 입력합니다.
3. `TrueSoft/Supabase/Create Runtime Object In Scene` 메뉴로 씬에 `SupabaseRuntime` 오브젝트를 생성합니다.

## 제공 범위

- Auth: 게스트 로그인, 구글 ID 토큰 로그인/연동, 세션 복원
- User 데이터: 저장/로드
- User 이벤트: 전송
- RemoteConfig: 구독/새로고침/폴링/캐시 Get
- Edge Functions: 함수 호출
- Chat: 채널 입장/전송/이탈




의존성 항목 추가
    implementation "androidx.credentials:credentials:1.6.0-rc02"
    implementation "androidx.credentials:credentials-play-services-auth:1.6.0-rc02"
    implementation "com.google.android.libraries.identity.googleid:googleid:1.1.1"
    implementation "org.jetbrains.kotlinx:kotlinx-coroutines-android:1.8.1"