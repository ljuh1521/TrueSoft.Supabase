# Examples

하나의 컴포넌트에서 Supabase 주요 기능 예시를 **함수별로 분리**해 제공합니다.

- 로그인 예시
- 로그아웃 예시 (`TrySignOutFullyAsync` — Android면 Google 네이티브 로그아웃 시도 후 Supabase 로그아웃)
- 중복 로그인 감지 예시 (`OnDuplicateLoginDetected` 구독 + 콘솔 안내)
- 저장/불러오기 예시 (프로젝트별 명시 컬럼 + 변경분 PATCH)
- 공개 displayName(`display_names` + Edge Functions) 예시 — SQL 적용 + Edge Functions 배포 후 **Run Public DisplayName Example** 또는 전체 실행 시 포함
- RemoteConfig 조회 예시
- Edge Function 호출 예시
- 서버 샤드 예시 — 로컬 서버 코드(`GetCurrentServerCode` / `SetCurrentServerCode`)와 DB의 `ts_my_server_id` 조회, 선택적으로 `TryTransferMyServerAsync`(RPC `ts_transfer_my_server`). 운영·Retool용 `ts_admin_transfer_user_server`는 루트 README 참고.

## 1. 샘플 가져오기

1. Unity 메뉴 **Window** > **Package Manager**
2. **Truesoft Supabase SDK** 선택
3. **Samples** 섹션에서 **Examples** 옆 **Import** 클릭

Import 후 예시 경로:

```text
Assets/Samples/Truesoft Supabase SDK/<버전>/Examples/
```

## 2. 실행 전 준비

1. **TrueSoft** > **Supabase** > **설정 에셋 만들기**로 `SupabaseSettings` 생성
2. `projectUrl`, `publishableKey` 입력
3. Android 네이티브 Google 로그인을 쓸 경우 `googleWebClientId` 입력
4. 에셋을 **`Assets/Resources/SupabaseSettings.asset`** 으로 저장
5. (선택) 게스트 로그인 흐름을 쓰면 Supabase 대시보드에서 **Anonymous sign-ins** 활성화
6. (선택) 중복 로그인·`user_sessions`를 쓰려면 `Sql/player/05_user_sessions.sql`을 적용하고, `SupabaseSettings`에서 **Enable Duplicate Session Monitor**를 켭니다.
7. (선택) 서버 이주 샘플은 `Sql/player/01_game_servers.sql`·`08_transfer_server.sql`(및 선행 파일)로 `game_servers`·RPC(`ts_my_server_id`, `ts_transfer_my_server`)가 적용된 뒤, **로그인한 상태**에서 **Run Server Shard Example** 또는 키 **N**으로 실행합니다. 다른 월드로 옮기려면 DB에 목표 `server_code` 행을 추가하고 인스펙터에서 **Server Shard Attempt Transfer**를 켭니다.
8. (선택) 저장/불러오기 샘플은 `user_saves`에 `level int`, `coins int`, `updated_at timestamptz` 같은 컬럼이 있어야 합니다. 이 샘플은 `TryPatchUserDataAsync`로 변경분만 PATCH하고, `TryLoadUserDataColumnsAsync(select)`로 필요한 컬럼만 로드합니다.

## 3. 씬에서 실행

1. 테스트 씬을 엽니다.
2. GameObject에 **`ExampleSupabaseScenarios`** 컴포넌트를 붙입니다.
3. 실행 방법
   - 전체 실행: `Run All On Start` 체크 후 Play, 또는 우클릭 **Run All Examples**
   - 개별 실행: 우클릭으로 각 함수 실행
     - **Run Login Example**
     - **Run Save/Load Example**
     - **Run RemoteConfig Example**
     - **Run Function Example**
     - **Run Logout Example** — 로그인된 상태에서만 의미 있음
     - **Run Duplicate Login Info (Console)** — 두 기기에서 같은 계정으로 로그인해 볼 때의 동작 안내
     - **Run Server Shard Example** — 로그인 필요; 키보드 테스트가 켜져 있으면 **N**

## 4. 확인

- Console에 `[Supabase.*]` Try API 로그와 `[Sample] ...` 로그가 출력되면 정상입니다.
- 로그인 필요 예시는 미로그인 상태에서 건너뛰도록 되어 있습니다.
- 중복 로그인은 실제로 **다른 기기(또는 에뮬+실기)** 에서 같은 계정으로 다시 로그인해야 콘솔에 감지 로그가 뜹니다. 인스펙터에서 `subscribeDuplicateLoginOnEnable`(Duplicate login / Logout 섹션)이 켜져 있어야 `OnDuplicateLoginDetected`가 구독됩니다.

## 5. 샘플 삭제

`Assets/Samples/.../Examples` 폴더를 삭제하면 됩니다.
