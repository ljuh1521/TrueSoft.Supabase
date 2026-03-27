# Withdrawal Edge Functions

현재 구성은 아래 4개 Function입니다.

- `withdrawal-guard`: 로그인 직후 만료 계정 즉시 삭제(하드 삭제)
- `withdrawal-cleanup`: 일일 배치 삭제
- `withdrawal-cancel-issue`: 탈퇴 예약 중 계정에 대해 철회 전용 토큰 발급
- `withdrawal-cancel-redeem`: 철회 토큰 검증 후 `profiles.withdrawn_at` 해제

권장 스케줄(한국 트래픽 저점): **KST 05:00 = UTC 20:00**

템플릿 파일:
- `Sql/edge-functions/withdrawal-guard/index.ts`
- `Sql/edge-functions/withdrawal-cleanup/index.ts`
- `Sql/edge-functions/withdrawal-cancel-issue/index.ts`
- `Sql/edge-functions/withdrawal-cancel-redeem/index.ts`

---

## 환경변수(Secrets)

필수:
- `SUPABASE_URL`
- `SUPABASE_ANON_KEY`
- `SUPABASE_SERVICE_ROLE_KEY`

철회 토큰(B 방식) 추가:
- `CANCEL_TOKEN_SECRET` (충분히 긴 랜덤 문자열)
- `CANCEL_TOKEN_TTL_SECONDS` (선택, 기본 900초)

---

## 1) `withdrawal-guard` (로그인 직후 1회 호출)

- 입력: `Authorization: Bearer <access_token>`
- 동작:
  - 본인 `profiles.withdrawn_at` 조회
  - `withdrawn_at <= now`이면 `account_closures` 기록 후 `auth.admin.deleteUser`
  - 만료 전(미래) 또는 예약 없음이면 `deleted: false`

---

## 2) `withdrawal-cleanup` (매일 배치 실행)

- 동작:
  - `withdrawn_at <= now` 계정을 배치로 조회
  - `account_closures` upsert 후 `auth.admin.deleteUser`

---

## 3) `withdrawal-cancel-issue` (철회 토큰 발급)

- 입력: `Authorization: Bearer <access_token>`
- 발급 조건:
  - `profiles.withdrawn_at > now` (탈퇴 예약 진행 중)일 때만 발급
- 출력:
  - `cancel_token`
  - `expires_at` (UTC ISO)

토큰은 HMAC-SHA256(`CANCEL_TOKEN_SECRET`)으로 서명된 짧은 TTL 토큰입니다.

---

## 4) `withdrawal-cancel-redeem` (철회 토큰 사용)

- 입력(JSON): `{ "cancel_token": "..." }`
- 동작:
  - 토큰 검증(서명/만료/타입)
  - `profiles`에서 `account_id = token.sub`의 `withdrawn_at`를 `NULL`로 업데이트
- 출력(JSON): `{ "ok": true }`

---

## 배포 예시

```bash
supabase functions deploy withdrawal-guard
supabase functions deploy withdrawal-cleanup
supabase functions deploy withdrawal-cancel-issue
supabase functions deploy withdrawal-cancel-redeem
```

## 스케줄 예시

- Scheduled Function (cleanup): `0 20 * * *` (UTC 20:00 = KST 05:00)

## B 방식 UX 권장 순서

1. 로그인 성공
2. `ts_my_withdrawal_status` 조회(닉네임/예약/남은 초)
3. 예약 중이면 본편 진입 차단
4. `withdrawal-cancel-issue` 호출 후 `cancel_token` 로컬 저장
5. `ClearSession`으로 로그아웃
6. UI에서 철회 선택 시 `withdrawal-cancel-redeem` 호출
7. 성공 후 일반 로그인 재진입

