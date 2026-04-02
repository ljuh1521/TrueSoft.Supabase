-- =============================================================================
-- 플레이어 스키마 — user_saves + ts_ensure_my_user_save_row
-- 선행: 02_profiles.sql (auth_user_server_id)
-- =============================================================================

-- ---------------------------------------------------------------------------
-- user_saves
-- ---------------------------------------------------------------------------
create table if not exists public.user_saves (
  id uuid primary key default gen_random_uuid(),
  user_id text not null,
  account_id uuid unique references auth.users (id) on delete set null,
  server_id uuid references public.game_servers (id) on delete restrict,
  -- 신규 프로젝트: 게임 데이터는 아래 예시처럼 명시 컬럼 + PATCH(변경분만)를 권장. save_data는 샘플/임시용으로만 두어도 됨.
  save_data jsonb not null default '{}'::jsonb,
  level integer,
  coins integer,
  updated_at timestamptz not null default now()
);

alter table public.user_saves add column if not exists user_id text;
alter table public.user_saves add column if not exists account_id uuid;
alter table public.user_saves add column if not exists server_id uuid;
alter table public.user_saves add column if not exists save_data jsonb not null default '{}'::jsonb;
alter table public.user_saves add column if not exists level integer;
alter table public.user_saves add column if not exists coins integer;
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
comment on column public.user_saves.save_data is '선택. 샘플·임시 JSON 저장용. 신규는 명시 컬럼 + PATCH 권장.';
comment on column public.user_saves.level is '예시 스칼라 컬럼(샘플). 게임별로 컬럼을 추가·삭제해 확장.';
comment on column public.user_saves.coins is '예시 스칼라 컬럼(샘플). 게임별로 컬럼을 추가·삭제해 확장.';

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

-- 로그인 직후 user_saves 본인 행을 보장(부분 PATCH 전제).
-- account_id는 auth.uid()로만 결정하며, server_id는 profiles 기준 트리거와 동일하게 기본 서버로 채웁니다.
create or replace function public.ts_ensure_my_user_save_row(p_user_id text default null)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_uid uuid;
  v_stable text;
  v_server_id uuid;
begin
  v_uid := auth.uid();
  if v_uid is null then
    raise exception 'not_authenticated';
  end if;

  v_stable := coalesce(nullif(trim(p_user_id), ''), v_uid::text);
  v_server_id := public.ts_default_server_id();

  insert into public.user_saves (user_id, account_id, server_id, updated_at)
  values (v_stable, v_uid, v_server_id, now())
  on conflict (account_id) do update set
    user_id = excluded.user_id,
    updated_at = excluded.updated_at;
end;
$$;

comment on function public.ts_ensure_my_user_save_row(text) is
  '로그인 직후 본인 user_saves 행 보장(upsert). 부분 PATCH 저장 전제. account_id는 auth.uid().';

grant execute on function public.ts_ensure_my_user_save_row(text) to authenticated;