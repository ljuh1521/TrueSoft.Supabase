-- =============================================================================
-- profiles INSERT RLS 완화 (한 번만 실행하면 됨)
-- 증상: PostgREST upsert 시 body 에 server_id 가 없으면 WITH CHECK 단계에서 null 로 보여
--       "new row violates row-level security policy for table profiles" (42501)
-- 해결: INSERT 정책에서는 account_id = auth.uid() 만 검증. server_id 는 컬럼 DEFAULT 로 채움.
-- (클라이언트는 game_servers 조회 후 server_id 를 넣도록 보강되어 있음 — 이 SQL 은 DB 정리용)
-- =============================================================================

drop policy if exists "profiles_insert_own" on public.profiles;

create policy "profiles_insert_own"
on public.profiles for insert
with check (
  account_id is not null
  and account_id = auth.uid()
);
