-- =============================================================================
-- 내 탈퇴 예약 상태 조회 RPC (게이트 UI용)
-- 로그인 직후 본인(account_id=auth.uid())의 닉네임/예약 시각/남은 시간을 한 번에 반환합니다.
-- =============================================================================

create or replace function public.ts_my_withdrawal_status()
returns table(
  nickname text,
  withdrawn_at timestamptz,
  server_now timestamptz,
  is_scheduled boolean,
  seconds_remaining bigint
)
language sql
security invoker
set search_path = public
as $$
  with now_cte as (
    select clock_timestamp() as n
  ),
  me as (
    select
      p.nickname,
      p.withdrawn_at
    from public.profiles p
    where p.account_id = auth.uid()
    limit 1
  )
  select
    coalesce(me.nickname, '') as nickname,
    me.withdrawn_at as withdrawn_at,
    now_cte.n as server_now,
    (me.withdrawn_at is not null and me.withdrawn_at > now_cte.n) as is_scheduled,
    case
      when me.withdrawn_at is not null and me.withdrawn_at > now_cte.n
        then extract(epoch from (me.withdrawn_at - now_cte.n))::bigint
      else 0::bigint
    end as seconds_remaining
  from now_cte
  left join me on true;
$$;

comment on function public.ts_my_withdrawal_status() is
  '본인 탈퇴 예약 상태(닉네임/예약 시각/서버 시각/예약 여부/남은 초)를 반환한다.';

grant execute on function public.ts_my_withdrawal_status() to authenticated;

