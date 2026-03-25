-- =============================================================================
-- 플레이어 데이터: user_id(운영·동일인) + account_id(게임·auth.users.id)
-- README「플레이어 데이터 테이블 구조」「공개 프로필」「§5」와 동일 모델.
-- Supabase SQL Editor에서 실행. 기존 policy 이름이 겹치면 아래 DROP 후 재실행.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- profiles
-- ---------------------------------------------------------------------------
create table if not exists public.profiles (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null,
  account_id uuid unique references auth.users (id) on delete set null,
  nickname text not null default '',
  withdrawn_at timestamptz null
);

comment on table public.profiles is '공개 프로필. 게임 RLS는 account_id. 운영 조회는 user_id.';
comment on column public.profiles.user_id is '플레이어 고유 id (동일 Google 등이면 재가입 후에도 동일 값 가능).';
comment on column public.profiles.account_id is 'auth.users.id. 탈퇴 시 NULL. 게임 조회·수정 기준.';

create index if not exists profiles_user_id_idx on public.profiles (user_id);

alter table public.profiles enable row level security;

drop policy if exists "profiles_select_public" on public.profiles;
drop policy if exists "profiles_insert_own" on public.profiles;
drop policy if exists "profiles_update_own" on public.profiles;

create policy "profiles_select_public"
on public.profiles for select
using (true);

create policy "profiles_insert_own"
on public.profiles for insert
with check (account_id is not null and account_id = auth.uid());

create policy "profiles_update_own"
on public.profiles for update
using (account_id is not null and account_id = auth.uid());

create unique index if not exists profiles_nickname_unique
on public.profiles (lower(trim(nickname)))
where trim(nickname) <> '';

-- ---------------------------------------------------------------------------
-- user_saves
-- ---------------------------------------------------------------------------
create table if not exists public.user_saves (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null,
  account_id uuid unique references auth.users (id) on delete set null,
  save_data jsonb not null default '{}'::jsonb,
  updated_at timestamptz not null default now()
);

comment on table public.user_saves is '유저 세이브. 게임은 account_id만 RLS.';
comment on column public.user_saves.user_id is '플레이어 고유 id (운영·감사용 묶음).';
comment on column public.user_saves.account_id is 'auth.users.id. 탈퇴 시 NULL.';

create index if not exists user_saves_user_id_idx on public.user_saves (user_id);
create index if not exists user_saves_account_id_idx on public.user_saves (account_id)
where account_id is not null;

alter table public.user_saves enable row level security;

drop policy if exists "user_saves_select_own" on public.user_saves;
drop policy if exists "user_saves_insert_own" on public.user_saves;
drop policy if exists "user_saves_update_own" on public.user_saves;

create policy "user_saves_select_own"
on public.user_saves for select
using (account_id = auth.uid());

create policy "user_saves_insert_own"
on public.user_saves for insert
with check (account_id = auth.uid());

create policy "user_saves_update_own"
on public.user_saves for update
using (account_id = auth.uid());

-- ---------------------------------------------------------------------------
-- user_sessions (중복 로그인 감지 — 계정당 하나의 활성 세션 토큰)
-- SDK가 로그인 시 새 토큰을 upsert하고, 다른 기기에서 로그인하면 토큰이 바뀌어 이전 기기에서 감지합니다.
-- ---------------------------------------------------------------------------
create table if not exists public.user_sessions (
  account_id uuid primary key references auth.users (id) on delete cascade,
  session_token uuid not null,
  updated_at timestamptz not null default now()
);

comment on table public.user_sessions is '기기별 세션 식별. 최신 로그인이 이 행의 session_token을 덮어씀.';
comment on column public.user_sessions.session_token is '클라이언트가 생성한 UUID. 다른 기기에서 로그인하면 값이 바뀜.';

alter table public.user_sessions enable row level security;

drop policy if exists "user_sessions_select_own" on public.user_sessions;
drop policy if exists "user_sessions_insert_own" on public.user_sessions;
drop policy if exists "user_sessions_update_own" on public.user_sessions;
drop policy if exists "user_sessions_delete_own" on public.user_sessions;

create policy "user_sessions_select_own"
on public.user_sessions for select
using (account_id = auth.uid());

create policy "user_sessions_insert_own"
on public.user_sessions for insert
with check (account_id = auth.uid());

create policy "user_sessions_update_own"
on public.user_sessions for update
using (account_id = auth.uid());

create policy "user_sessions_delete_own"
on public.user_sessions for delete
using (account_id = auth.uid());

-- ---------------------------------------------------------------------------
-- account_closures (탈퇴 이력 예시 — 클라이언트 직접 접근 없음 가정)
-- ---------------------------------------------------------------------------
create table if not exists public.account_closures (
  id bigint generated always as identity primary key,
  user_id uuid not null,
  account_id uuid null,
  closed_at timestamptz not null default now(),
  note text null
);

comment on table public.account_closures is '탈퇴 기록. PostgREST는 service role 등으로만 쓰는 것을 권장.';

create index if not exists account_closures_user_id_idx on public.account_closures (user_id);

alter table public.account_closures enable row level security;

-- 정책 없음 → anon/authenticated JWT로는 행 접근 불가. service_role은 RLS 우회.
-- 서버에서 일반 사용자에게 열어줄 경우에만 별도 policy 추가.
