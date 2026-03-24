# Examples

하나의 컴포넌트에서 Supabase 주요 기능 예시를 **함수별로 분리**해 제공합니다.

- 로그인 예시
- 저장/불러오기 예시
- 사용자 이벤트 전송 예시
- RemoteConfig 조회 예시
- Edge Function 호출 예시

## 1. 샘플 가져오기

1. Unity 메뉴 **Window** > **Package Manager**
2. **Truesoft Supabase SDK** 선택
3. **Samples** 섹션에서 **Examples** 옆 **Import** 클릭

Import 후 예시 경로:

```text
Assets/Samples/Truesoft Supabase SDK/<버전>/Examples/
```

## 2. 실행 전 준비

1. **TrueSoft** > **Supabase** > **Create Settings Asset**으로 `SupabaseSettings` 생성
2. `projectUrl`, `publishableKey` 입력
3. Android 네이티브 Google 로그인을 쓸 경우 `googleWebClientId` 입력
4. 에셋을 **`Assets/Resources/SupabaseSettings.asset`** 으로 저장
5. (선택) 게스트 로그인 흐름을 쓰면 Supabase 대시보드에서 **Anonymous sign-ins** 활성화

## 3. 씬에서 실행

1. 테스트 씬을 엽니다.
2. GameObject에 **`ExampleSupabaseScenarios`** 컴포넌트를 붙입니다.
3. 실행 방법
   - 전체 실행: `Run All On Start` 체크 후 Play, 또는 우클릭 **Run All Examples**
   - 개별 실행: 우클릭으로 각 함수 실행
     - **Run Login Example**
     - **Run Save/Load Example**
     - **Run Event Example**
     - **Run RemoteConfig Example**
     - **Run Function Example**

## 4. 확인

- Console에 `[Supabase.*]` Try API 로그와 `[Sample] ...` 로그가 출력되면 정상입니다.
- 로그인 필요 예시는 미로그인 상태에서 건너뛰도록 되어 있습니다.

## 5. 샘플 삭제

`Assets/Samples/.../Examples` 폴더를 삭제하면 됩니다.
