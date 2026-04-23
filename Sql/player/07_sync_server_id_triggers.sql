-- =============================================================================
-- 플레이어 스키마 — ts_sync_server_id_by_account + BEFORE 트리거
-- 선행: 03–05 테이블
-- =============================================================================

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
    from public.user_profiles p
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

-- user_saves 테이블이 제거되었으므로 해당 트리거 삭제됨.
-- 커스텀 세이브 테이블에는 프로젝트별로 ts_sync_server_id_by_account 트리거를 직접 추가하세요.

drop trigger if exists trg_user_sessions_sync_server_id on public.user_sessions;
create trigger trg_user_sessions_sync_server_id
before insert or update on public.user_sessions
for each row
execute function public.ts_sync_server_id_by_account();