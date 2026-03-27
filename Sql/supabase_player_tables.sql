-- =============================================================================
-- 플레이어 데이터: user_id(운영·동일인) + account_id(게임·auth.users.id)
-- README「플레이어 데이터 테이블 구조」「공개 프로필」「§5」와 동일 모델.
-- Supabase SQL Editor에서 실행. 기존 policy 이름이 겹치면 아래 DROP 후 재실행.
-- 마지막 SELECT(Result 탭)로 테이블·RLS·함수·인덱스 반영 여부를 확인한다.
--
-- 재실행·스키마 진화(이 스크립트만으로 현재 의도 상태로 수렴)
-- - 테이블: CREATE IF NOT EXISTS 후, 누락 컬럼은 ADD COLUMN IF NOT EXISTS 로 보강.
-- - 새 컬럼을 파일에 추가할 때: CREATE TABLE 절과 ADD COLUMN IF NOT EXISTS 절을 둘 다 같은 이름·타입으로 갱신할 것.
-- - FK/UNIQUE: 아래 DO 블록의 고정 이름 제약이 없을 때만 추가(이미 있으면 건너뜀). 수동으로 이름을 바꾼 DB와는 중복 정의에 주의.
-- - COMMENT ON, RLS, 정책(DROP IF EXISTS 후 CREATE), 인덱스(IF NOT EXISTS), 함수(OR REPLACE), GRANT 는 재실행 시에도 최종 정의로 맞춤.
-- - 컬럼 타입 변경·데이터가 있는 경우의 NOT NULL 강제 등은 자동으로 하지 않으며, 필요 시 별도 마이그레이션으로 처리.
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

-- 기존 DB에 컬럼만 없을 때 보강(신규 생성 테이블에서는 IF NOT EXISTS 로 무시됨)
alter table public.profiles add column if not exists user_id uuid;
alter table public.profiles add column if not exists account_id uuid;
alter table public.profiles add column if not exists nickname text not null default '';
alter table public.profiles add column if not exists withdrawn_at timestamptz;

do $$
begin
  if not exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'profiles'
      and c.conname = 'profiles_account_id_fkey'
  ) then
    alter table public.profiles
      add constraint profiles_account_id_fkey
      foreign key (account_id) references auth.users (id) on delete set null;
  end if;
end $$;

do $$
begin
  if not exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'profiles'
      and c.conname = 'profiles_account_id_key'
  ) then
    alter table public.profiles
      add constraint profiles_account_id_key unique (account_id);
  end if;
end $$;

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

alter table public.user_saves add column if not exists user_id uuid;
alter table public.user_saves add column if not exists account_id uuid;
alter table public.user_saves add column if not exists save_data jsonb not null default '{}'::jsonb;
alter table public.user_saves add column if not exists updated_at timestamptz not null default now();

do $$
begin
  if not exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'user_saves'
      and c.conname = 'user_saves_account_id_fkey'
  ) then
    alter table public.user_saves
      add constraint user_saves_account_id_fkey
      foreign key (account_id) references auth.users (id) on delete set null;
  end if;
end $$;

do $$
begin
  if not exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'user_saves'
      and c.conname = 'user_saves_account_id_key'
  ) then
    alter table public.user_saves
      add constraint user_saves_account_id_key unique (account_id);
  end if;
end $$;

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

alter table public.user_sessions add column if not exists account_id uuid;
alter table public.user_sessions add column if not exists session_token uuid;
alter table public.user_sessions add column if not exists updated_at timestamptz not null default now();

do $$
begin
  if not exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'user_sessions'
      and c.conname = 'user_sessions_account_id_fkey'
  ) then
    alter table public.user_sessions
      add constraint user_sessions_account_id_fkey
      foreign key (account_id) references auth.users (id) on delete cascade;
  end if;
end $$;

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
-- anonymous_recovery_tokens (device-only best-effort 익명 복구)
-- 앱 재설치/로그아웃으로 로컬 refresh_token이 사라진 경우를 대비해
-- 디바이스 지문 해시 기준으로 refresh_token을 보관합니다.
-- ---------------------------------------------------------------------------
create table if not exists public.anonymous_recovery_tokens (
  fingerprint_hash text primary key,
  refresh_token text not null,
  account_id uuid null references auth.users (id) on delete set null,
  updated_at timestamptz not null default now()
);

alter table public.anonymous_recovery_tokens add column if not exists fingerprint_hash text;
alter table public.anonymous_recovery_tokens add column if not exists refresh_token text;
alter table public.anonymous_recovery_tokens add column if not exists account_id uuid;
alter table public.anonymous_recovery_tokens add column if not exists updated_at timestamptz not null default now();

do $$
begin
  if not exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'anonymous_recovery_tokens'
      and c.conname = 'anonymous_recovery_tokens_account_id_fkey'
  ) then
    alter table public.anonymous_recovery_tokens
      add constraint anonymous_recovery_tokens_account_id_fkey
      foreign key (account_id) references auth.users (id) on delete set null;
  end if;
end $$;

comment on table public.anonymous_recovery_tokens is 'device-only 익명 복구용 refresh_token 저장소(best-effort).';
comment on column public.anonymous_recovery_tokens.fingerprint_hash is '클라이언트가 만든 SHA-256 해시 지문.';
comment on column public.anonymous_recovery_tokens.refresh_token is '복구용 refresh_token.';

alter table public.anonymous_recovery_tokens enable row level security;

-- 정책은 두지 않고 RPC(SECURITY DEFINER)로만 접근합니다.

create or replace function public.ts_anon_recovery_get_refresh_token(p_fingerprint_hash text)
returns table(refresh_token text)
language plpgsql
security definer
set search_path = public
as $$
begin
  if p_fingerprint_hash is null or length(trim(p_fingerprint_hash)) = 0 then
    return;
  end if;

  return query
  select t.refresh_token
  from public.anonymous_recovery_tokens t
  where t.fingerprint_hash = trim(p_fingerprint_hash)
  limit 1;
end;
$$;

create or replace function public.ts_anon_recovery_upsert_refresh_token(
  p_fingerprint_hash text,
  p_refresh_token text,
  p_account_id uuid default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
begin
  if p_fingerprint_hash is null or length(trim(p_fingerprint_hash)) = 0 then
    return;
  end if;

  if p_refresh_token is null or length(trim(p_refresh_token)) = 0 then
    return;
  end if;

  insert into public.anonymous_recovery_tokens (fingerprint_hash, refresh_token, account_id, updated_at)
  values (trim(p_fingerprint_hash), trim(p_refresh_token), p_account_id, now())
  on conflict (fingerprint_hash)
  do update set
    refresh_token = excluded.refresh_token,
    account_id = excluded.account_id,
    updated_at = now();
end;
$$;

grant execute on function public.ts_anon_recovery_get_refresh_token(text) to anon, authenticated;
grant execute on function public.ts_anon_recovery_upsert_refresh_token(text, text, uuid) to anon, authenticated;

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

alter table public.account_closures add column if not exists user_id uuid;
alter table public.account_closures add column if not exists account_id uuid;
alter table public.account_closures add column if not exists closed_at timestamptz not null default now();
alter table public.account_closures add column if not exists note text;

comment on table public.account_closures is '탈퇴 기록. PostgREST는 service role 등으로만 쓰는 것을 권장.';

create index if not exists account_closures_user_id_idx on public.account_closures (user_id);
create unique index if not exists account_closures_user_id_uq on public.account_closures (user_id);
create index if not exists account_closures_account_id_idx on public.account_closures (account_id);
create index if not exists account_closures_closed_at_idx on public.account_closures (closed_at desc);

-- 탈퇴 예약 만료 조회(정리 배치/로그인 가드) 성능용 인덱스
create index if not exists profiles_withdrawn_at_idx
  on public.profiles (withdrawn_at)
  where withdrawn_at is not null;

alter table public.account_closures enable row level security;

-- 정책 없음 → anon/authenticated JWT로는 행 접근 불가. service_role은 RLS 우회.
-- 서버에서 일반 사용자에게 열어줄 경우에만 별도 policy 추가.

-- -----------------------------------------------------------------------------
-- 실행 결과 (SQL Editor Result 탭에서 반영 여부 확인)
-- status: applied = 생성·RLS 반영됨 | rls_off = 테이블만 있고 RLS 미적용 | missing = 없음
-- -----------------------------------------------------------------------------
select
  r.category,
  r.object_name,
  r.status,
  r.detail
from (
  select
    'table+rls'::text as category,
    exp.name as object_name,
    case
      when pub.oid is null then 'missing'
      when not pub.relrowsecurity then 'rls_off'
      else 'applied'
    end as status,
    null::text as detail
  from (
    values
      ('profiles'),
      ('user_saves'),
      ('user_sessions'),
      ('anonymous_recovery_tokens'),
      ('account_closures')
  ) as exp(name)
  left join lateral (
    select c.oid, c.relrowsecurity
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace and n.nspname = 'public'
    where c.relkind = 'r'
      and c.relname = exp.name
    limit 1
  ) pub on true

  union all

  select
    'function'::text,
    exp.name,
    case when pub.oid is not null then 'applied' else 'missing' end,
    case
      when pub.oid is not null then pg_get_function_identity_arguments(pub.oid)
      else null
    end
  from (
    values
      ('ts_anon_recovery_get_refresh_token'),
      ('ts_anon_recovery_upsert_refresh_token')
  ) as exp(name)
  left join lateral (
    select p.oid
    from pg_proc p
    join pg_namespace n on n.oid = p.pronamespace and n.nspname = 'public'
    where p.proname = exp.name
    order by p.oid
    limit 1
  ) pub on true

  union all

  select
    'index'::text,
    'profiles_withdrawn_at_idx'::text,
    case
      when exists (
        select 1
        from pg_indexes i
        where i.schemaname = 'public'
          and i.indexname = 'profiles_withdrawn_at_idx'
      ) then 'applied'
      else 'missing'
    end,
    'on profiles'::text
) r
order by r.category, r.object_name;
