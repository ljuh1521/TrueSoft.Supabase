# Basic Setup 샘플

이 샘플은 SDK의 핵심 흐름 중 하나인 **게스트 로그인 → 사용자 데이터 Save/Load → (선택) 사용자 이벤트 전송**을 확인합니다.

## 준비 단계

1. `SupabaseSettings`를 `Resources/SupabaseSettings.asset`에 준비합니다.
   - 런타임 오브젝트는 샘플이 자동 생성하며, 설정은 Resources에서 자동 로드됩니다.
2. Supabase Auth에서 **Anonymous sign-ins** 기능을 활성화합니다.

## 실행 방법

1. 샘플 스크립트 `ExampleBootstrap`을 아무 GameObject에 붙입니다.
2. 기본값은 `runOnStart=true`이므로 Play 시 자동 실행됩니다.
3. 콘솔에서 아래 로그를 확인합니다.
   - `[BasicSetup] Load 결과: level=..., coins=...`

## 인스펙터 값

- `Save/Load 예시값`
  - `level`, `coins`: 저장에 사용되는 값입니다.

