-- =============================================================================
-- 탈퇴 취소 RPC (service_role 제거, JWT 기반)
-- 기존: withdrawal-cancel-redeem Edge Function이 service_role로 profiles 업데이트
-- 변경: 사용자 JWT(auth.uid())로 직접 본인 withdrawn_at 초기화
-- =============================================================================

create or replace function public.ts_withdrawal_cancel_redeem()
returns jsonb
language plpgsql
security definer
set search_path = public
as $$
declare
  v_account_id uuid;
  v_withdrawn_at timestamptz;
begin
  -- JWT 인증 확인
  if auth.uid() is null then
    return jsonb_build_object('ok', false, 'reason', 'not_authenticated');
  end if;

  v_account_id := auth.uid();

  -- 현재 withdrawn_at 조회
  select withdrawn_at into v_withdrawn_at
  from public.user_profiles
  where account_id = v_account_id;

  -- 유효성 검사
  if v_withdrawn_at is null then
    return jsonb_build_object('ok', false, 'reason', 'withdrawal_not_scheduled');
  end if;

  if v_withdrawn_at <= now() then
    return jsonb_build_object('ok', false, 'reason', 'already_withdrawn');
  end if;

  -- withdrawn_at 초기화 (탈퇴 취소)
  update public.user_profiles
  set withdrawn_at = null
  where account_id = v_account_id;

  return jsonb_build_object('ok', true);
end;
$$;

-- 권한: authenticated 사용자만 호출 가능 (service_role 불필요)
revoke all on function public.ts_withdrawal_cancel_redeem() from public;
grant execute on function public.ts_withdrawal_cancel_redeem() to authenticated;

comment on function public.ts_withdrawal_cancel_redeem() is 
  'JWT 기반 탈퇴 취소. 본인(auth.uid()) withdrawn_at만 초기화. SECURITY DEFINER.';
