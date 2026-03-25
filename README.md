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

## REST 테이블 이름 (프로젝트마다 테이블명이 다를 때)

PostgREST로 접근하는 **테이블 이름**은 프로젝트마다 다를 수 있으므로, `SupabaseSettings`에서 바꿀 수 있습니다.

- **User Saves** (`TrySaveUserDataAsync` / `TryLoadUserDataAsync`): 필드 `userSavesTable` (비우면 기본값 `user_saves`)
- **Remote Config**: `remoteConfigTable` (기본 `remote_config`)
- **채팅**: `chatMessagesTable` (기본 `chat_messages`)
- **공개 프로필**: `publicProfilesTable` (기본 `profiles`) — `TryGetPublicNicknameAsync`, `TrySetMyNicknameAsync`

코드에서 `SupabaseOptions`의 `UserSavesTable`, `RemoteConfigTable`, `ChatMessagesTable`, `PublicProfilesTable`도 같은 역할을 합니다. Unity 에셋 경로로 쓸 때는 `SupabaseSettings.ToOptions()`가 비어 있는 테이블 필드를 기본 이름으로 채웁니다. `SupabaseOptions`만 직접 만들 때는 빈 문자열을 넣지 말고, 필드 기본값을 두거나 유효한 이름을 지정하세요. 스키마가 `public`이 아니면 `schema.table` 형식으로 지정할 수 있습니다. 잘못된 문자(`..`, `/`, `\` 등)는 초기화 시 검증되어 예외가 납니다.

### 아직 “고정”인 부분 (하드코딩이 없다는 뜻은 아님)

테이블 **이름**만 설정 가능합니다. 다음은 Supabase 표준이거나 이 SDK가 전제로 두는 스키마입니다.

| 구분 | 고정에 가까운 내용 |
|------|-------------------|
| **URL 경로** | `…/auth/v1/…`, `…/rest/v1/{테이블}`, `…/functions/v1/{함수명}` — Supabase API 규격 (프로젝트 URL·함수 이름은 설정/인자로 바뀜) |
| **User Saves** | 컬럼 `user_id`, `save_data`, `updated_at`, upsert 시 `on_conflict=user_id` — DB를 이 형태에 맞추거나 SDK를 확장해야 함 |
| **Remote Config** | 조회 컬럼 `key`, `value_json`, `updated_at`, `version` |
| **채팅** | 컬럼 `id`, `channel_id`, `user_id`, `display_name`, `content`, `created_at` |
| **공개 프로필** | 컬럼 `id`(auth UUID, PK), `nickname`, 선택 `withdrawn_at`(soft 탈퇴 시각). 조회는 anon, 수정은 JWT + RLS(본인만) |

즉, **REST 대상 테이블명**은 유연하고, **각 기능이 쓰는 컬럼·쿼리 형태**는 아직 코드에 박혀 있습니다. 다른 스키마를 쓰려면 해당 서비스를 감싼 별도 레이어나 포크가 필요합니다.

### 공개 프로필·닉네임·탈퇴 표시 (`profiles`)

**탈퇴(비활성) 상태를 어디에 둘까?** 게임에서 “이 유저는 탈퇴했다”를 **다른 클라이언트가 조회**해야 하면 `profiles` 같은 **공개 프로필 테이블**에 두는 편이 맞습니다. `auth.users`만 건드리면 anon으로는 확인이 어렵고, 닉네임·아바타와 같은 **공개 메타**와 한곳에 모이므로 유지보수도 쉽습니다. 완전 삭제(hard delete)와 `auth` 계정 삭제는 별도 정책(Edge Function·관리자 API 등)으로 처리하는 경우가 많고, 앱에서는 **`withdrawn_at`이 비어 있지 않으면 탈퇴 처리**처럼 쓰면 됩니다.

조회는 **로그인 없이** publishable key만으로 되게 하려면 `SELECT`를 공개하고, 쓰기는 본인만 허용하는 RLS가 필요합니다.

```sql
create table if not exists public.profiles (
  id uuid primary key references auth.users (id) on delete cascade,
  nickname text not null default '',
  withdrawn_at timestamptz null
);

alter table public.profiles enable row level security;

create policy "profiles_select_public"
on public.profiles for select
using (true);

create policy "profiles_insert_own"
on public.profiles for insert
with check (auth.uid() = id);

create policy "profiles_update_own"
on public.profiles for update
using (auth.uid() = id);

-- 닉네임 중복 방지(빈 문자열 제외·대소문자 무시). 클라이언트 TryIsNicknameAvailableAsync와 함께 쓰는 것을 권장합니다.
create unique index if not exists profiles_nickname_unique
on public.profiles (lower(trim(nickname)))
where trim(nickname) <> '';
```

기존 테이블에만 `nickname`이 있다면:

```sql
alter table public.profiles add column if not exists withdrawn_at timestamptz null;
```

**Unity API 요약**

- 중복 확인: `TryIsNicknameAvailableAsync("후보닉")` — 가입·로그인 후 **본인 닉을 바꿀 때**는 `TryIsNicknameAvailableAsync("후보닉", ignoreUserIdForSelf: Supabase.Session.User.Id)`처럼 자기 id를 넘겨 같은 닉을 허용합니다.
- 최초/수정 저장: `TrySetMyNicknameAsync` 또는 별칭 `TryUpdateMyNicknameAsync`(동일 upsert).
- 프로필 한 번에 조회: `TryGetPublicProfileAsync(userId)` → `Nickname`, `IsWithdrawn`, `WithdrawnAtIso`.
- 탈퇴 표시: `TryMarkMyWithdrawnAsync()`(UTC 시각 기록) / 해제: `TryClearMyWithdrawalAsync()` / 임의 시각: `TrySetMyWithdrawnAtAsync(iso8601)`.
- 닉네임만: `TryGetPublicNicknameAsync`는 그대로 사용 가능합니다.

닉네임 길이는 클라이언트에서 최대 64자로 잘립니다. DB 유니크 인덱스는 `lower(trim(...))` 기준이므로, **저장되는 문자열과 중복 검사**가 가능한 한 같은 규칙(공백·대소문자)을 맞추는 것이 좋습니다.

## 제공 범위

- **초기화/세션 준비**: `Supabase.TryStartAsync()`를 기본 진입점으로 사용합니다. 이 단계는 초기화/세션 복원만 담당하며 자동 익명 로그인은 수행하지 않습니다.
- **인증**: `TrySignInAnonymouslyAsync`, `TrySignInWithGoogleAsync`, `TrySignInWithGoogleIdTokenAsync`, `TryRestoreSessionAsync`
- **사용자 데이터**: `TrySaveUserDataAsync`, `TryLoadUserDataAsync`
- **공개 프로필**: `TryGetPublicProfileAsync`, `TryIsNicknameAvailableAsync`, `TrySetMyNicknameAsync` / `TryUpdateMyNicknameAsync`, `TryMarkMyWithdrawnAsync`, `TryClearMyWithdrawalAsync` (위 SQL·RLS·선택 유니크 인덱스)
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