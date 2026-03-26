-- =============================================================================
-- 탈퇴 요청 RPC (서버 시각 기준)
-- 클라이언트는 "유예 기간(일)"만 전달하고, 실제 withdrawn_at은 서버에서 계산합니다.
-- =============================================================================

create or replace function public.ts_request_withdrawal(p_delay_days integer)
returns table(scheduled_at timestamptz)
language plpgsql
security invoker
set search_path = public
as $$
declare
  v_delay_days integer;
begin
  if auth.uid() is null then
    raise exception 'not_authenticated';
  end if;

  v_delay_days := greatest(0, coalesce(p_delay_days, 0));

  update public.profiles
     set withdrawn_at = case
       when v_delay_days <= 0 then now()
       else now() + make_interval(days => v_delay_days)
     end
   where account_id = auth.uid()
  returning withdrawn_at into scheduled_at;

  if scheduled_at is null then
    return;
  end if;

  return query select scheduled_at;
end;
$$;

comment on function public.ts_request_withdrawal(integer) is
  '탈퇴 요청 시 withdrawn_at을 서버 시각 기준으로 설정(0일이면 즉시 now, 그 외 now + delay_days)';

grant execute on function public.ts_request_withdrawal(integer) to authenticated;
