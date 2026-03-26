-- =============================================================================
-- 서버 기준 현재 시각 (비로그인 포함)
-- Supabase SQL Editor에서 실행. 클라이언트는 POST /rest/v1/rpc/ts_server_now (바디 {})
-- =============================================================================

create or replace function public.ts_server_now()
returns table(server_time timestamptz)
language sql
stable
security definer
set search_path = public
as $$
  select clock_timestamp() as server_time;
$$;

comment on function public.ts_server_now() is 'DB 서버 시각(요청 시점). 클라이언트 시계 대신 이벤트·쿨다운 등에 사용.';

grant execute on function public.ts_server_now() to anon, authenticated;
