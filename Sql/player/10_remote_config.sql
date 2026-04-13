-- =============================================================================
-- 플레이어 스키마 — remote_config (Retool / 클라이언트 원격 설정)
-- 선행: 없음 (독립 테이블)
-- =============================================================================
-- 
-- 설계: 1키 = 1설정묶음(JSON 클러스터링) = 1폴링주기
-- 관련 설정은 하나의 키에 JSON 객체로 묶어 관리합니다.
-- 예: key="gameplay_v1", value_json={"stamina":{...},"battle":{...}}
-- poll_interval_seconds는 키 단위로 설정됩니다.

-- 기존 테이블/컬럼 마이그레이션용 (category 제거)
alter table if exists public.remote_config drop column if exists category;

create table if not exists public.remote_config (
  key text primary key,
  value_json text not null,  -- JSON 객체 루트 ({...}) 필수 (컬럼이 jsonb여도 클라이언트 SDK가 객체/문자열 응답 모두 처리)
  updated_at timestamptz not null default now(),
  version int not null default 1,
  enabled boolean not null default true,
  description text,
  poll_interval_seconds int not null default 300,  -- 키 단위 폴링 주기 (초, 0=폴링 안함)
  requires_auth boolean not null default false,
  client_version_min text,
  client_version_max text,
  max_stale_seconds int not null default 300  -- 캐시 유효 시간 (초)
);

alter table public.remote_config add column if not exists value_json text;
alter table public.remote_config add column if not exists updated_at timestamptz not null default now();
alter table public.remote_config add column if not exists version int not null default 1;
alter table public.remote_config add column if not exists enabled boolean not null default true;
-- category 컬럼 제거됨
alter table public.remote_config add column if not exists description text;
alter table public.remote_config add column if not exists poll_interval_seconds int not null default 300;
alter table public.remote_config add column if not exists requires_auth boolean not null default false;
alter table public.remote_config add column if not exists client_version_min text;
alter table public.remote_config add column if not exists client_version_max text;
alter table public.remote_config add column if not exists max_stale_seconds int not null default 300;

-- updated_at 자동 갱신 (Retool UPDATE 시에도 일관되게 갱신)
create or replace function public.ts_remote_config_set_updated_at()
returns trigger
language plpgsql
as $$
begin
  new.updated_at := now();
  return new;
end;
$$;

drop trigger if exists tr_remote_config_set_updated_at on public.remote_config;
create trigger tr_remote_config_set_updated_at
  before update on public.remote_config
  for each row
  execute function public.ts_remote_config_set_updated_at();

alter table public.remote_config enable row level security;

-- anon: 읽기만 (로그인 없이 클라이언트 조회)
drop policy if exists remote_config_select_anon on public.remote_config;
create policy remote_config_select_anon
  on public.remote_config
  for select
  to anon
  using (true);

-- authenticated: 읽기 (로그인 사용자)
drop policy if exists remote_config_select_authenticated on public.remote_config;
create policy remote_config_select_authenticated
  on public.remote_config
  for select
  to authenticated
  using (true);

-- 쓰기: service_role 전용 (Retool 등은 Service Role 키 사용 권장)
drop policy if exists remote_config_all_service_role on public.remote_config;
create policy remote_config_all_service_role
  on public.remote_config
  for all
  to service_role
  using (true)
  with check (true);
