# Full SDK Usage 샘플

이벤트 전송, RemoteConfig 새로고침·폴링, Edge Function 호출 등 **여러 기능을 한 흐름**으로 확인하는 샘플입니다.

## 1. 샘플 가져오기

1. Unity 메뉴 **Window** > **Package Manager**
2. **Truesoft Supabase SDK** 선택
3. **Samples** 섹션에서 **Full SDK Usage** 옆 **Import** 클릭

Import 후 예시 경로:

```text
Assets/Samples/Truesoft Supabase SDK/<버전>/Full SDK Usage/
```

## 2. 실행 전 준비

1. **TrueSoft** > **Supabase** > **Create Settings Asset** → `projectUrl`, `publishableKey` 입력
2. **`Assets/Resources/SupabaseSettings.asset`** 로 저장 (이름 `SupabaseSettings`)
3. **Anonymous sign-ins** 활성화 (게스트 로그인 사용 시)
4. (선택) **Create Runtime Object In Scene** 으로 `SupabaseRuntime` 배치  
   ※ 없으면 Play 시 샘플이 생성할 수 있습니다.

## 3. 씬에서 실행

1. 테스트 씬을 엽니다.
2. GameObject에 **`ExampleSupabaseAllFeatures`** 컴포넌트를 붙입니다.
3. **Play**  
   - `Run On Start`가 켜져 있으면 자동 실행됩니다.
4. **수동 실행**  
   - 컴포넌트 우클릭 → **Run Full SDK Usage**

## 4. 인스펙터 (예시)

| 필드 | 설명 |
|------|------|
| Run On Start | Play 시 자동 실행 |
| Remote Config Key | RemoteConfig 조회에 쓰는 키 |
| Function Name | 호출할 Edge Function 이름 (프로젝트에 맞게 수정) |

Edge Function이 없거나 이름이 다르면 해당 단계에서 경고 로그만 나올 수 있습니다. 서버에 맞게 `functionName`을 바꾸세요.

## 5. 확인

- Console에 `[FullSDKUsage]` 로그가 보이면 정상입니다.
- 초기화 실패 시 **`[Supabase 초기화 점검]`** 을 확인하세요.

## 6. 샘플 삭제

`Assets/Samples/.../Full SDK Usage` 폴더를 삭제하면 됩니다.
