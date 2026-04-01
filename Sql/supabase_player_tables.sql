-- =============================================================================
-- 플레이어 데이터: user_id(운영·동일인) + account_id(게임·auth.users.id)
-- README「플레이어 데이터 테이블 구조」「공개 프로필」「§5」와 동일 모델.
-- Supabase SQL Editor에서 실행. 마지막 SELECT(Result 탭)로 테이블·RLS·함수·인덱스 반영 여부를 확인한다.
--
-- ■ 한 번에 재실행(이미 테이블·정책이 있어도 됨)
--   이 파일 전체를 통째로 실행하면 최종 스키마·RLS·함수·GRANT 로 수렴하도록 설계했다.
--
-- 재실행·스키마 진화
-- - 테이블: CREATE IF NOT EXISTS 후, 누락 컬럼은 ADD COLUMN IF NOT EXISTS 로 보강.
-- - 새 컬럼을 파일에 추가할 때: CREATE TABLE 절과 ADD COLUMN IF NOT EXISTS 절을 둘 다 같은 이름·타입으로 갱신할 것.
-- - FK/UNIQUE: 아래 DO 블록의 고정 이름 제약이 없을 때만 추가(이미 있으면 건너뜀). 수동으로 이름을 바꾼 DB와는 중복 정의에 주의.
-- - COMMENT ON, RLS, 정책(DROP IF EXISTS 후 CREATE), 인덱스(IF NOT EXISTS), 함수(OR REPLACE), GRANT 는 재실행 시에도 최종 정의로 맞춤.
-- - server_id NOT NULL 은 NULL 채운 뒤, 컬럼이 아직 nullable 일 때만 적용(재실행 시 중복 ALTER 방지). 남는 NULL 이 있으면 NOTICE 후 건너뜀.
-- - auth 스키마 트리거는 권한·환경에 따라 실패할 수 있어 NOTICE 로 건너뛸 수 있음.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- game_servers (서버/월드 마스터)
-- ---------------------------------------------------------------------------
create table if not exists public.game_servers (
  id uuid primary key default gen_random_uuid(),
  server_code text not null,
  display_name text not null,
  allow_new_signups boolean not null default true,
  allow_transfers boolean not null default true,
  created_at timestamptz not null default now()
);

alter table public.game_servers add column if not exists id uuid;
alter table public.game_servers add column if not exists server_code text;
alter table public.game_servers add column if not exists display_name text;
alter table public.game_servers add column if not exists allow_new_signups boolean not null default true;
alter table public.game_servers add column if not exists allow_transfers boolean not null default true;
alter table public.game_servers add column if not exists created_at timestamptz not null default now();

do $$
begin
  if not exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'game_servers'
      and c.conname = 'game_servers_server_code_key'
  ) then
    alter table public.game_servers
      add constraint game_servers_server_code_key unique (server_code);
  end if;
end $$;

insert into public.game_servers (server_code, display_name)
select 'GLOBAL', 'Global'
where not exists (
  select 1 from public.game_servers where server_code = 'GLOBAL'
);

create or replace function public.ts_default_server_id()
returns uuid
language sql
stable
security definer
set search_path = public
as $$
  select gs.id
  from public.game_servers gs
  order by
    case when gs.server_code = 'GLOBAL' then 0 else 1 end,
    gs.created_at,
    gs.id
  limit 1;
$$;

comment on function public.ts_default_server_id() is
  '기본 game_servers 행 id. 클라이언트 프로필 upsert 시 server_id 채움·RLS 호환용으로 authenticated 에서 호출 가능.';

grant execute on function public.ts_default_server_id() to anon, authenticated;

comment on table public.game_servers is '게임 서버(월드) 마스터.';
comment on column public.game_servers.server_code is '클라이언트에서 선택/표시하는 고유 코드(예: GLOBAL, KR1).';

alter table public.game_servers enable row level security;
drop policy if exists "game_servers_select_public" on public.game_servers;
create policy "game_servers_select_public"
on public.game_servers for select
using (true);

-- ---------------------------------------------------------------------------
-- profiles
-- ---------------------------------------------------------------------------
create table if not exists public.profiles (
  id uuid primary key default gen_random_uuid(),
  user_id text not null,
  account_id uuid unique references auth.users (id) on delete set null,
  server_id uuid references public.game_servers (id) on delete restrict,
  withdrawn_at timestamptz null
);

-- 기존 DB에 컬럼만 없을 때 보강(신규 생성 테이블에서는 IF NOT EXISTS 로 무시됨)
alter table public.profiles add column if not exists user_id text;
alter table public.profiles add column if not exists account_id uuid;
alter table public.profiles add column if not exists server_id uuid;
alter table public.profiles add column if not exists withdrawn_at timestamptz;

update public.profiles p
set server_id = public.ts_default_server_id()
where p.server_id is null;

alter table public.profiles
  alter column server_id set default public.ts_default_server_id();

do $$
begin
  if exists (
    select 1
    from information_schema.columns
    where table_schema = 'public'
      and table_name = 'profiles'
      and column_name = 'server_id'
      and is_nullable = 'YES'
  ) then
    alter table public.profiles
      alter column server_id set not null;
  end if;
exception
  when others then
    raise notice 'profiles.server_id SET NOT NULL skipped: %', sqlerrm;
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

-- Google 등 OAuth subject(sub)는 UUID 형식이 아닐 수 있어 user_id는 text로 통일합니다.
do $$
begin
  if exists (
    select 1 from information_schema.columns
    where table_schema = 'public' and table_name = 'profiles'
      and column_name = 'user_id' and udt_name = 'uuid'
  ) then
    alter table public.profiles alter column user_id type text using user_id::text;
  end if;
end $$;

comment on table public.profiles is '공개 프로필. 게임 RLS는 account_id. 운영 조회는 user_id.';
comment on column public.profiles.user_id is '플레이어 고유 id (동일 Google 등이면 재가입 후에도 동일 값 가능).';
comment on column public.profiles.account_id is 'auth.users.id. 탈퇴 시 NULL. 게임 조회·수정 기준.';
comment on column public.profiles.server_id is '플레이어가 속한 서버 id (public.game_servers.id).';

create index if not exists profiles_user_id_idx on public.profiles (user_id);
create index if not exists profiles_server_id_idx on public.profiles (server_id);

do $$
begin
  if not exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'profiles'
      and c.conname = 'profiles_server_id_fkey'
  ) then
    alter table public.profiles
      add constraint profiles_server_id_fkey
      foreign key (server_id) references public.game_servers (id) on delete restrict;
  end if;
end $$;

create or replace function public.auth_user_server_id()
returns uuid
language sql
stable
security definer
set search_path = public
as $$
  select p.server_id
  from public.profiles p
  where p.account_id = auth.uid()
    and p.account_id is not null
  limit 1;
$$;

create or replace function public.ts_my_server_id()
returns table(server_id uuid, server_code text)
language sql
stable
security definer
set search_path = public
as $$
  select p.server_id, gs.server_code
  from public.profiles p
  join public.game_servers gs on gs.id = p.server_id
  where p.account_id = auth.uid()
    and p.account_id is not null
  limit 1;
$$;

alter table public.profiles enable row level security;

drop policy if exists "profiles_select_public" on public.profiles;
drop policy if exists "profiles_insert_own" on public.profiles;
drop policy if exists "profiles_update_own" on public.profiles;

create policy "profiles_select_public"
on public.profiles for select
using (
  auth.uid() is not null
  and server_id is not null
  and server_id = public.auth_user_server_id()
);

-- server_id 는 DEFAULT(ts_default_server_id()) 로 채워지나, PostgREST JSON upsert 시 RLS WITH CHECK 가
-- 기본값 적용 전에 평가되면 server_id 가 null 로 보여 42501 이 날 수 있음 → insert 정책에서는 account_id 만 검증.
create policy "profiles_insert_own"
on public.profiles for insert
with check (
  account_id is not null
  and account_id = auth.uid()
);

create policy "profiles_update_own"
on public.profiles for update
using (account_id is not null and account_id = auth.uid())
with check (server_id is not null);

-- PostgREST upsert(merge-duplicates) UPDATE 분기에서 기존 server_id가 NULL이면 WITH CHECK(server_id is not null)가 계속 실패(42501→403).
-- INSERT 시에도 JSON에 server_id가 없을 때 RLS/기본값 평가 순서에 따라 NULL로 남는 경우가 있어 BEFORE에서 보강.
create or replace function public.ts_profiles_coalesce_server_id()
returns trigger
language plpgsql
security invoker
set search_path = public
as $$
begin
  if new.server_id is null then
    new.server_id := public.ts_default_server_id();
  end if;
  return new;
end;
$$;

comment on function public.ts_profiles_coalesce_server_id() is
  'profiles 행 INSERT·UPDATE 직전 server_id가 NULL이면 ts_default_server_id()로 채움. ensure-profile upsert·RLS 호환.';

drop trigger if exists trg_profiles_coalesce_server_id on public.profiles;
create trigger trg_profiles_coalesce_server_id
before insert or update on public.profiles
for each row
execute function public.ts_profiles_coalesce_server_id();

-- 클라이언트 ensure-profile: PostgREST upsert만으로는 RLS/병합 순서에 따라 42501이 남을 수 있어 RPC로 통일.
-- account_id는 항상 auth.uid()만 사용(클라이언트 조작 불가). user_id는 p_user_id 또는 uid 문자열.
create or replace function public.ts_ensure_my_profile(p_user_id text default null)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_uid uuid;
  v_stable text;
  v_server uuid;
begin
  v_uid := auth.uid();
  if v_uid is null then
    raise exception 'not_authenticated';
  end if;

  v_stable := coalesce(nullif(trim(p_user_id), ''), v_uid::text);
  v_server := public.ts_default_server_id();

  insert into public.profiles (user_id, account_id, withdrawn_at, server_id)
  values (v_stable, v_uid, null, v_server)
  on conflict (account_id) do update set
    user_id = excluded.user_id,
    withdrawn_at = excluded.withdrawn_at,
    server_id = coalesce(profiles.server_id, excluded.server_id);
end;
$$;

comment on function public.ts_ensure_my_profile(text) is
  '로그인 직후 본인 profiles 행 보장(upsert). SECURITY DEFINER. SDK EnsureMyProfileRowAsync 가 호출.';

grant execute on function public.ts_ensure_my_profile(text) to authenticated;

-- nickname은 auth.user_metadata.displayName으로 이동했으므로 profiles에 두지 않습니다.

-- ---------------------------------------------------------------------------
-- display_names (닉네임 유니크/조회용)
-- - 닉네임 원본은 Auth user metadata(displayName)가 소스이며,
--   DB에서는 유니크 강제/가벼운 공개 조회를 위해 별도 테이블로 관리합니다.
-- ---------------------------------------------------------------------------
create table if not exists public.display_names (
  account_id uuid primary key references auth.users (id) on delete cascade,
  user_id text not null,
  server_id uuid references public.game_servers (id) on delete restrict,
  display_name text not null,
  updated_at timestamptz not null default now()
);

alter table public.display_names add column if not exists account_id uuid;
alter table public.display_names add column if not exists user_id text;
alter table public.display_names add column if not exists server_id uuid;
alter table public.display_names add column if not exists display_name text;
alter table public.display_names add column if not exists updated_at timestamptz not null default now();

update public.display_names d
set server_id = coalesce(p.server_id, public.ts_default_server_id())
from public.profiles p
where p.account_id = d.account_id
  and d.server_id is null;

update public.display_names d
set server_id = public.ts_default_server_id()
where d.server_id is null;

do $$
begin
  if exists (
    select 1
    from information_schema.columns
    where table_schema = 'public'
      and table_name = 'display_names'
      and column_name = 'server_id'
      and is_nullable = 'YES'
  ) then
    alter table public.display_names
      alter column server_id set not null;
  end if;
exception
  when others then
    raise notice 'display_names.server_id SET NOT NULL skipped: %', sqlerrm;
end $$;

do $$
begin
  if not exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'display_names'
      and c.conname = 'display_names_server_id_fkey'
  ) then
    alter table public.display_names
      add constraint display_names_server_id_fkey
      foreign key (server_id) references public.game_servers (id) on delete restrict;
  end if;
end $$;

do $$
begin
  if exists (
    select 1 from information_schema.columns
    where table_schema = 'public' and table_name = 'display_names'
      and column_name = 'user_id' and udt_name = 'uuid'
  ) then
    alter table public.display_names alter column user_id type text using user_id::text;
  end if;
end $$;

comment on table public.display_names is '닉네임 유니크/공개 조회용. 실제 표시 이름은 auth.user_metadata.displayName이 소스.';
comment on column public.display_names.account_id is 'auth.users.id (RLS: auth.uid()).';
comment on column public.display_names.user_id is '플레이어 안정 id (profiles.user_id와 동일 값).';
comment on column public.display_names.server_id is '표시 이름이 속한 서버 id.';
comment on column public.display_names.display_name is '표시용 닉네임(원문). 유니크 인덱스는 lower(trim(...)) 기준.';

create index if not exists display_names_user_id_idx on public.display_names (user_id);
create index if not exists display_names_server_id_idx on public.display_names (server_id);

alter table public.display_names enable row level security;

drop policy if exists "display_names_select_public" on public.display_names;
drop policy if exists "display_names_insert_own" on public.display_names;
drop policy if exists "display_names_update_own" on public.display_names;

create policy "display_names_select_public"
on public.display_names for select
using (
  auth.uid() is not null
  and server_id = public.auth_user_server_id()
);

create policy "display_names_insert_own"
on public.display_names for insert
with check (
  account_id is not null
  and account_id = auth.uid()
  and server_id is not null
  and server_id = public.auth_user_server_id()
);

create policy "display_names_update_own"
on public.display_names for update
using (account_id is not null and account_id = auth.uid())
with check (
  server_id is not null
  and server_id = public.auth_user_server_id()
);

create unique index if not exists display_names_display_name_unique
on public.display_names (server_id, lower(trim(display_name)))
where trim(display_name) <> '';

-- ---------------------------------------------------------------------------
-- user_saves
-- ---------------------------------------------------------------------------
create table if not exists public.user_saves (
  id uuid primary key default gen_random_uuid(),
  user_id text not null,
  account_id uuid unique references auth.users (id) on delete set null,
  server_id uuid references public.game_servers (id) on delete restrict,
  save_data jsonb not null default '{}'::jsonb,
  updated_at timestamptz not null default now()
);

alter table public.user_saves add column if not exists user_id text;
alter table public.user_saves add column if not exists account_id uuid;
alter table public.user_saves add column if not exists server_id uuid;
alter table public.user_saves add column if not exists save_data jsonb not null default '{}'::jsonb;
alter table public.user_saves add column if not exists updated_at timestamptz not null default now();

update public.user_saves u
set server_id = coalesce(p.server_id, public.ts_default_server_id())
from public.profiles p
where p.account_id = u.account_id
  and u.server_id is null;

update public.user_saves u
set server_id = public.ts_default_server_id()
where u.server_id is null;

do $$
begin
  if exists (
    select 1
    from information_schema.columns
    where table_schema = 'public'
      and table_name = 'user_saves'
      and column_name = 'server_id'
      and is_nullable = 'YES'
  ) then
    alter table public.user_saves
      alter column server_id set not null;
  end if;
exception
  when others then
    raise notice 'user_saves.server_id SET NOT NULL skipped: %', sqlerrm;
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
      and c.conname = 'user_saves_server_id_fkey'
  ) then
    alter table public.user_saves
      add constraint user_saves_server_id_fkey
      foreign key (server_id) references public.game_servers (id) on delete restrict;
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

do $$
begin
  if exists (
    select 1 from information_schema.columns
    where table_schema = 'public' and table_name = 'user_saves'
      and column_name = 'user_id' and udt_name = 'uuid'
  ) then
    alter table public.user_saves alter column user_id type text using user_id::text;
  end if;
end $$;

comment on table public.user_saves is '유저 세이브. 게임은 account_id만 RLS.';
comment on column public.user_saves.user_id is '플레이어 고유 id (운영·감사용 묶음).';
comment on column public.user_saves.account_id is 'auth.users.id. 탈퇴 시 NULL.';
comment on column public.user_saves.server_id is '세이브가 속한 서버 id.';

create index if not exists user_saves_user_id_idx on public.user_saves (user_id);
create index if not exists user_saves_account_id_idx on public.user_saves (account_id)
where account_id is not null;
create index if not exists user_saves_server_id_idx on public.user_saves (server_id);

alter table public.user_saves enable row level security;

drop policy if exists "user_saves_select_own" on public.user_saves;
drop policy if exists "user_saves_insert_own" on public.user_saves;
drop policy if exists "user_saves_update_own" on public.user_saves;

create policy "user_saves_select_own"
on public.user_saves for select
using (account_id = auth.uid());

create policy "user_saves_insert_own"
on public.user_saves for insert
with check (
  account_id = auth.uid()
  and server_id is not null
  and server_id = public.auth_user_server_id()
);

create policy "user_saves_update_own"
on public.user_saves for update
using (account_id = auth.uid())
with check (
  server_id is not null
  and server_id = public.auth_user_server_id()
);

-- ---------------------------------------------------------------------------
-- user_sessions (중복 로그인 감지 — 계정당 하나의 활성 세션 토큰)
-- SDK가 로그인 시 새 토큰을 upsert하고, 다른 기기에서 로그인하면 토큰이 바뀌어 이전 기기에서 감지합니다.
-- ---------------------------------------------------------------------------
create table if not exists public.user_sessions (
  account_id uuid primary key references auth.users (id) on delete cascade,
  server_id uuid references public.game_servers (id) on delete restrict,
  session_token uuid not null,
  updated_at timestamptz not null default now()
);

alter table public.user_sessions add column if not exists account_id uuid;
alter table public.user_sessions add column if not exists server_id uuid;
alter table public.user_sessions add column if not exists session_token uuid;
alter table public.user_sessions add column if not exists updated_at timestamptz not null default now();

update public.user_sessions s
set server_id = coalesce(p.server_id, public.ts_default_server_id())
from public.profiles p
where p.account_id = s.account_id
  and s.server_id is null;

update public.user_sessions s
set server_id = public.ts_default_server_id()
where s.server_id is null;

do $$
begin
  if exists (
    select 1
    from information_schema.columns
    where table_schema = 'public'
      and table_name = 'user_sessions'
      and column_name = 'server_id'
      and is_nullable = 'YES'
  ) then
    alter table public.user_sessions
      alter column server_id set not null;
  end if;
exception
  when others then
    raise notice 'user_sessions.server_id SET NOT NULL skipped: %', sqlerrm;
end $$;

do $$
begin
  if not exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'user_sessions'
      and c.conname = 'user_sessions_server_id_fkey'
  ) then
    alter table public.user_sessions
      add constraint user_sessions_server_id_fkey
      foreign key (server_id) references public.game_servers (id) on delete restrict;
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
      and t.relname = 'user_sessions'
      and c.conname = 'user_sessions_account_id_fkey'
  ) then
    alter table public.user_sessions
      add constraint user_sessions_account_id_fkey
      foreign key (account_id) references auth.users (id) on delete cascade;
  end if;
end $$;

comment on table public.user_sessions is '기기별 세션 식별. 최신 로그인이 이 행의 session_token을 덮어씀.';
comment on column public.user_sessions.server_id is '세션 토큰이 속한 서버 id.';
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
with check (
  account_id = auth.uid()
  and server_id is not null
  and server_id = public.auth_user_server_id()
);

create policy "user_sessions_update_own"
on public.user_sessions for update
using (account_id = auth.uid())
with check (
  server_id is not null
  and server_id = public.auth_user_server_id()
);

create policy "user_sessions_delete_own"
on public.user_sessions for delete
using (account_id = auth.uid());

-- ---------------------------------------------------------------------------
-- anonymous_recovery_tokens (device-only best-effort 익명 복구)
-- 앱 재설치/로그아웃으로 로컬 refresh_token이 사라진 경우를 대비해
-- 디바이스 지문 해시 기준으로 refresh_token을 보관합니다.
-- ---------------------------------------------------------------------------
create table if not exists public.anonymous_recovery_tokens (
  fingerprint_hash text not null,
  server_id uuid not null references public.game_servers (id) on delete restrict,
  refresh_token text not null,
  account_id uuid null references auth.users (id) on delete set null,
  updated_at timestamptz not null default now(),
  primary key (fingerprint_hash, server_id)
);

alter table public.anonymous_recovery_tokens add column if not exists fingerprint_hash text;
alter table public.anonymous_recovery_tokens add column if not exists server_id uuid;
alter table public.anonymous_recovery_tokens add column if not exists refresh_token text;
alter table public.anonymous_recovery_tokens add column if not exists account_id uuid;
alter table public.anonymous_recovery_tokens add column if not exists updated_at timestamptz not null default now();

update public.anonymous_recovery_tokens t
set server_id = coalesce(p.server_id, public.ts_default_server_id())
from public.profiles p
where p.account_id = t.account_id
  and t.server_id is null;

update public.anonymous_recovery_tokens t
set server_id = public.ts_default_server_id()
where t.server_id is null;

do $$
begin
  if exists (
    select 1
    from information_schema.columns
    where table_schema = 'public'
      and table_name = 'anonymous_recovery_tokens'
      and column_name = 'server_id'
      and is_nullable = 'YES'
  ) then
    alter table public.anonymous_recovery_tokens
      alter column server_id set not null;
  end if;
exception
  when others then
    raise notice 'anonymous_recovery_tokens.server_id SET NOT NULL skipped: %', sqlerrm;
end $$;

do $$
begin
  if exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'anonymous_recovery_tokens'
      and c.contype = 'p'
      and c.conname <> 'anonymous_recovery_tokens_pkey'
  ) then
    -- no-op: custom PK name 환경은 건드리지 않습니다.
    null;
  end if;
end $$;

do $$
begin
  if exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'anonymous_recovery_tokens'
      and c.contype = 'p'
      and pg_get_constraintdef(c.oid) not ilike '%(fingerprint_hash, server_id)%'
  ) then
    alter table public.anonymous_recovery_tokens
      drop constraint if exists anonymous_recovery_tokens_pkey;
  end if;

  if not exists (
    select 1
    from pg_constraint c
    join pg_class t on c.conrelid = t.oid
    join pg_namespace n on t.relnamespace = n.oid
    where n.nspname = 'public'
      and t.relname = 'anonymous_recovery_tokens'
      and c.contype = 'p'
      and pg_get_constraintdef(c.oid) ilike '%(fingerprint_hash, server_id)%'
  ) then
    alter table public.anonymous_recovery_tokens
      add constraint anonymous_recovery_tokens_pkey primary key (fingerprint_hash, server_id);
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
      and t.relname = 'anonymous_recovery_tokens'
      and c.conname = 'anonymous_recovery_tokens_account_id_fkey'
  ) then
    alter table public.anonymous_recovery_tokens
      add constraint anonymous_recovery_tokens_account_id_fkey
      foreign key (account_id) references auth.users (id) on delete set null;
  end if;
end $$;

comment on table public.anonymous_recovery_tokens is
  'device-only 익명 복구용 refresh_token 저장소(best-effort). 탈퇴 요청(ts_request_withdrawal→ts_delete_my_anon_recovery_tokens), auth.users 삭제/익명해제, auth.identities에 비익명 provider 추가 시 해당 account 행 자동 삭제.';
comment on column public.anonymous_recovery_tokens.fingerprint_hash is '클라이언트가 만든 SHA-256 해시 지문.';
comment on column public.anonymous_recovery_tokens.server_id is '복구 토큰이 속한 서버 id.';
comment on column public.anonymous_recovery_tokens.refresh_token is '복구용 refresh_token.';

create index if not exists anonymous_recovery_tokens_account_id_idx
on public.anonymous_recovery_tokens (account_id)
where account_id is not null;

create index if not exists anonymous_recovery_tokens_server_id_idx
on public.anonymous_recovery_tokens (server_id);

alter table public.anonymous_recovery_tokens enable row level security;

-- 정책은 두지 않고 RPC(SECURITY DEFINER)로만 접근합니다.

create or replace function public.ts_anon_recovery_get_refresh_token(
  p_fingerprint_hash text,
  p_server_code text default null
)
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
  with target_server as (
    select gs.id
    from public.game_servers gs
    where
      case
        when p_server_code is null or length(trim(p_server_code)) = 0 then gs.id = public.ts_default_server_id()
        else gs.server_code = trim(p_server_code)
      end
    limit 1
  )
  select t.refresh_token
  from public.anonymous_recovery_tokens t
  join target_server s on s.id = t.server_id
  where t.fingerprint_hash = trim(p_fingerprint_hash)
  limit 1;
end;
$$;

create or replace function public.ts_anon_recovery_upsert_refresh_token(
  p_fingerprint_hash text,
  p_refresh_token text,
  p_account_id uuid default null,
  p_server_code text default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_server_id uuid;
begin
  if p_fingerprint_hash is null or length(trim(p_fingerprint_hash)) = 0 then
    return;
  end if;

  if p_refresh_token is null or length(trim(p_refresh_token)) = 0 then
    return;
  end if;

  if p_account_id is not null then
    select p.server_id into v_server_id
    from public.profiles p
    where p.account_id = p_account_id
    limit 1;
  end if;

  if v_server_id is null and p_server_code is not null and length(trim(p_server_code)) > 0 then
    select gs.id into v_server_id
    from public.game_servers gs
    where gs.server_code = trim(p_server_code)
    limit 1;
  end if;

  if v_server_id is null then
    v_server_id := public.ts_default_server_id();
  end if;

  insert into public.anonymous_recovery_tokens (fingerprint_hash, server_id, refresh_token, account_id, updated_at)
  values (trim(p_fingerprint_hash), v_server_id, trim(p_refresh_token), p_account_id, now())
  on conflict (fingerprint_hash, server_id)
  do update set
    refresh_token = excluded.refresh_token,
    account_id = excluded.account_id,
    updated_at = now();
end;
$$;

create or replace function public.ts_anon_recovery_delete_by_fingerprint(
  p_fingerprint_hash text,
  p_server_code text default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_server_id uuid;
begin
  if p_fingerprint_hash is null or length(trim(p_fingerprint_hash)) = 0 then
    return;
  end if;

  if p_server_code is not null and length(trim(p_server_code)) > 0 then
    select gs.id into v_server_id
    from public.game_servers gs
    where gs.server_code = trim(p_server_code)
    limit 1;
  else
    v_server_id := public.ts_default_server_id();
  end if;

  if v_server_id is null then
    return;
  end if;

  delete from public.anonymous_recovery_tokens
  where fingerprint_hash = trim(p_fingerprint_hash)
    and server_id = v_server_id;
end;
$$;

grant execute on function public.ts_anon_recovery_get_refresh_token(text, text) to anon, authenticated;
grant execute on function public.ts_anon_recovery_upsert_refresh_token(text, text, uuid, text) to anon, authenticated;
grant execute on function public.ts_anon_recovery_delete_by_fingerprint(text, text) to anon, authenticated;

-- 본인 account_id(auth.uid())에 매달린 익명 복구 행만 삭제. 탈퇴 RPC(ts_request_withdrawal) 등에서 호출.
create or replace function public.ts_delete_my_anon_recovery_tokens()
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_id uuid := auth.uid();
begin
  if v_id is null then
    return;
  end if;

  delete from public.anonymous_recovery_tokens
  where account_id = v_id;
end;
$$;

comment on function public.ts_delete_my_anon_recovery_tokens() is
  '현재 JWT 사용자에 대한 anonymous_recovery_tokens 행 삭제(로그아웃 정리·탈퇴 요청 등).';

grant execute on function public.ts_delete_my_anon_recovery_tokens() to authenticated;

-- 트리거 전용: 임의 account_id(검증은 호출부). PostgREST에 노출하지 않음.
create or replace function public._ts_delete_anon_recovery_tokens_by_account_id(p_account_id uuid)
returns void
language plpgsql
security definer
set search_path = public
as $$
begin
  if p_account_id is null then
    return;
  end if;

  delete from public.anonymous_recovery_tokens
  where account_id = p_account_id;
end;
$$;

revoke all on function public._ts_delete_anon_recovery_tokens_by_account_id(uuid) from public;
revoke all on function public._ts_delete_anon_recovery_tokens_by_account_id(uuid) from anon, authenticated;

-- auth.users: 익명→소셜 등 연동 시 is_anonymous true→false, 또는 계정 하드 삭제 시 복구 토큰 제거
create or replace function public.ts_auth_users_anon_recovery_cleanup()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  if tg_op = 'DELETE' then
    perform public._ts_delete_anon_recovery_tokens_by_account_id(old.id);
    return old;
  end if;

  if tg_op = 'UPDATE' then
    if coalesce(old.is_anonymous, false) = true
      and coalesce(new.is_anonymous, false) = false then
      perform public._ts_delete_anon_recovery_tokens_by_account_id(new.id);
    end if;
    return new;
  end if;

  return new;
end;
$$;

do $$
begin
  if exists (
    select 1
    from information_schema.columns c
    where c.table_schema = 'auth'
      and c.table_name = 'users'
      and c.column_name = 'is_anonymous'
  ) then
    drop trigger if exists trg_auth_users_anon_recovery_cleanup_u on auth.users;
    create trigger trg_auth_users_anon_recovery_cleanup_u
      after update of is_anonymous on auth.users
      for each row
      execute function public.ts_auth_users_anon_recovery_cleanup();
  end if;
exception
  when others then
    raise notice 'trg_auth_users_anon_recovery_cleanup_u: skipped — %', sqlerrm;
end $$;

do $$
begin
  drop trigger if exists trg_auth_users_anon_recovery_cleanup_d on auth.users;
  create trigger trg_auth_users_anon_recovery_cleanup_d
    after delete on auth.users
    for each row
    execute function public.ts_auth_users_anon_recovery_cleanup();
exception
  when others then
    raise notice 'trg_auth_users_anon_recovery_cleanup_d: skipped — %', sqlerrm;
end $$;

-- 익명 계정에 Google 등 두 번째 identity 가 붙을 때(INSERT)에도 정리. is_anonymous 갱신 타이밍과 무관하게 동작.
create or replace function public.ts_auth_identities_anon_recovery_cleanup()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  if new.user_id is not null then
    perform public._ts_delete_anon_recovery_tokens_by_account_id(new.user_id);
  end if;
  return new;
end;
$$;

do $$
begin
  if exists (
    select 1
    from information_schema.tables t
    where t.table_schema = 'auth'
      and t.table_name = 'identities'
  )
  and exists (
    select 1
    from information_schema.columns c
    where c.table_schema = 'auth'
      and c.table_name = 'identities'
      and c.column_name = 'provider'
  )
  and exists (
    select 1
    from information_schema.columns c
    where c.table_schema = 'auth'
      and c.table_name = 'identities'
      and c.column_name = 'user_id'
  ) then
    drop trigger if exists trg_auth_identities_anon_recovery_cleanup on auth.identities;
    create trigger trg_auth_identities_anon_recovery_cleanup
      after insert on auth.identities
      for each row
      when (new.provider is distinct from 'anonymous')
      execute function public.ts_auth_identities_anon_recovery_cleanup();
  end if;
exception
  when others then
    raise notice 'trg_auth_identities_anon_recovery_cleanup: skipped — %', sqlerrm;
end $$;

-- ---------------------------------------------------------------------------
-- sync triggers (profiles.server_id를 파생 테이블에 강제 반영)
-- ---------------------------------------------------------------------------
create or replace function public.ts_sync_server_id_by_account()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
declare
  v_server_id uuid;
begin
  if new.account_id is not null then
    select p.server_id into v_server_id
    from public.profiles p
    where p.account_id = new.account_id
    limit 1;
  end if;

  if v_server_id is null then
    v_server_id := public.ts_default_server_id();
  end if;

  new.server_id := v_server_id;
  return new;
end;
$$;

drop trigger if exists trg_display_names_sync_server_id on public.display_names;
create trigger trg_display_names_sync_server_id
before insert or update on public.display_names
for each row
execute function public.ts_sync_server_id_by_account();

drop trigger if exists trg_user_saves_sync_server_id on public.user_saves;
create trigger trg_user_saves_sync_server_id
before insert or update on public.user_saves
for each row
execute function public.ts_sync_server_id_by_account();

drop trigger if exists trg_user_sessions_sync_server_id on public.user_sessions;
create trigger trg_user_sessions_sync_server_id
before insert or update on public.user_sessions
for each row
execute function public.ts_sync_server_id_by_account();

-- ---------------------------------------------------------------------------
-- user transfer API (유저 자가 이주 + Retool/service_role 관리 이주)
-- ---------------------------------------------------------------------------
-- 코어: 계정 UUID 기준 단일 트랜잭션 이주. PostgREST에 노출하지 않음(권한 회수).
create or replace function public._ts_transfer_user_server_core(
  p_account_id uuid,
  p_target_server_code text
)
returns table(ok boolean, reason text, target_server_id uuid)
language plpgsql
security definer
set search_path = public
as $$
declare
  v_current_server_id uuid;
  v_target_server_id uuid;
  v_target_allow_transfers boolean;
  v_target_allow_new_signups boolean;
begin
  if p_account_id is null then
    return query select false, 'account_id_required'::text, null::uuid;
    return;
  end if;

  if p_target_server_code is null or length(trim(p_target_server_code)) = 0 then
    return query select false, 'target_server_code_empty'::text, null::uuid;
    return;
  end if;

  select gs.id, gs.allow_transfers, gs.allow_new_signups
    into v_target_server_id, v_target_allow_transfers, v_target_allow_new_signups
  from public.game_servers gs
  where gs.server_code = trim(p_target_server_code)
  limit 1;

  if v_target_server_id is null then
    return query select false, 'target_server_not_found'::text, null::uuid;
    return;
  end if;

  if v_target_allow_transfers is false then
    return query select false, 'target_server_transfer_blocked'::text, null::uuid;
    return;
  end if;

  select p.server_id into v_current_server_id
  from public.profiles p
  where p.account_id = p_account_id
  limit 1;

  if v_current_server_id is null then
    return query select false, 'profile_not_found'::text, null::uuid;
    return;
  end if;

  if v_current_server_id = v_target_server_id then
    return query select true, null::text, v_target_server_id;
    return;
  end if;

  if exists (
    select 1
    from public.display_names d
    where d.account_id = p_account_id
      and d.server_id = coalesce(v_current_server_id, d.server_id)
      and exists (
        select 1
        from public.display_names x
        where x.server_id = v_target_server_id
          and lower(trim(x.display_name)) = lower(trim(d.display_name))
          and x.account_id <> p_account_id
      )
  ) then
    return query select false, 'display_name_taken_in_target_server'::text, null::uuid;
    return;
  end if;

  update public.profiles
  set server_id = v_target_server_id
  where account_id = p_account_id;

  update public.display_names
  set server_id = v_target_server_id
  where account_id = p_account_id;

  update public.user_saves
  set server_id = v_target_server_id
  where account_id = p_account_id;

  update public.user_sessions
  set server_id = v_target_server_id
  where account_id = p_account_id;

  update public.anonymous_recovery_tokens
  set server_id = v_target_server_id
  where account_id = p_account_id;

  return query select true, null::text, v_target_server_id;
end;
$$;

comment on function public._ts_transfer_user_server_core(uuid, text) is
  '내부용: profiles·display_names·user_saves·user_sessions·anonymous_recovery_tokens 의 server_id 일괄 이주.';

revoke all on function public._ts_transfer_user_server_core(uuid, text) from public;
revoke all on function public._ts_transfer_user_server_core(uuid, text) from anon, authenticated;

-- 로그인 유저: auth.uid()만 이주 대상 (클라이언트·자가 이주)
create or replace function public.ts_transfer_my_server(
  p_target_server_code text,
  p_reason text default null
)
returns table(ok boolean, reason text, target_server_id uuid)
language plpgsql
security definer
set search_path = public
as $$
declare
  v_account_id uuid := auth.uid();
begin
  if v_account_id is null then
    return query select false, 'auth_required'::text, null::uuid;
    return;
  end if;

  return query
  select c.ok, c.reason, c.target_server_id
  from public._ts_transfer_user_server_core(v_account_id, p_target_server_code) as c;
end;
$$;

-- Retool·백오피스: service_role JWT만 허용. p_account_id = auth.users.id
create or replace function public.ts_admin_transfer_user_server(
  p_account_id uuid,
  p_target_server_code text,
  p_reason text default null
)
returns table(ok boolean, reason text, target_server_id uuid)
language plpgsql
security definer
set search_path = public
as $$
begin
  if coalesce(auth.jwt() ->> 'role', '') <> 'service_role' then
    return query select false, 'forbidden_not_service_role'::text, null::uuid;
    return;
  end if;

  return query
  select c.ok, c.reason, c.target_server_id
  from public._ts_transfer_user_server_core(p_account_id, p_target_server_code) as c;
end;
$$;

comment on function public.ts_admin_transfer_user_server(uuid, text, text) is
  '운영 전용: 임의 계정 서버 이주. PostgREST는 service_role 키로만 호출할 것.';

grant execute on function public.auth_user_server_id() to authenticated;
grant execute on function public.ts_my_server_id() to authenticated;
grant execute on function public.ts_transfer_my_server(text, text) to authenticated;

revoke all on function public.ts_admin_transfer_user_server(uuid, text, text) from public;
revoke all on function public.ts_admin_transfer_user_server(uuid, text, text) from anon, authenticated;
grant execute on function public.ts_admin_transfer_user_server(uuid, text, text) to service_role;


-- ---------------------------------------------------------------------------
-- account_closures (탈퇴 이력 예시 — 클라이언트 직접 접근 없음 가정)
-- ---------------------------------------------------------------------------
create table if not exists public.account_closures (
  id bigint generated always as identity primary key,
  user_id text not null,
  account_id uuid null,
  closed_at timestamptz not null default now(),
  note text null
);

alter table public.account_closures add column if not exists user_id text;
alter table public.account_closures add column if not exists account_id uuid;
alter table public.account_closures add column if not exists closed_at timestamptz not null default now();
alter table public.account_closures add column if not exists note text;

do $$
begin
  if exists (
    select 1 from information_schema.columns
    where table_schema = 'public' and table_name = 'account_closures'
      and column_name = 'user_id' and udt_name = 'uuid'
  ) then
    alter table public.account_closures alter column user_id type text using user_id::text;
  end if;
end $$;

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
      ('game_servers'),
      ('user_saves'),
      ('display_names'),
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
      ('ts_anon_recovery_upsert_refresh_token'),
      ('ts_anon_recovery_delete_by_fingerprint'),
      ('ts_delete_my_anon_recovery_tokens'),
      ('auth_user_server_id'),
      ('ts_default_server_id'),
      ('ts_profiles_coalesce_server_id'),
      ('ts_ensure_my_profile'),
      ('ts_my_server_id'),
      ('ts_transfer_my_server'),
      ('ts_admin_transfer_user_server'),
      ('_ts_transfer_user_server_core')
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
