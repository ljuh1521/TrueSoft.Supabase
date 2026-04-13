# 우편함(Mailbox) 샘플

UPM **Examples** 샘플을 Import한 뒤 `MailboxTestSample` 컴포넌트를 씬에 추가해 사용합니다.

## 1. SQL로 테스트 우편 발송

패키지 소스의 `Sql/samples/MailboxTestData.sql`을 Supabase **SQL Editor**에서 열고, `YOUR_ACCOUNT_UUID` / `YOUR_USER_ID`를 본인 값으로 바꾼 뒤 실행합니다.

(`11_mails.sql` 등 우편 스키마가 이미 적용되어 있어야 합니다.)

## 2. Unity

1. Package Manager → **Truesoft Supabase SDK** → Samples → **Examples** → **Import**
2. 씬에 GameObject 추가 → **`MailboxTestSample`** 붙이기
3. Play (기본으로 `Auto Fetch On Start`가 켜져 있으면 자동 테스트)

## 3. 핸들러

`items`의 `key`와 동일한 `IMailItemHandler`를 `MailItemHandlerRegistry`에 등록해야 수령이 진행됩니다. 샘플에 `gold`, `gem`, `weapon_001`, `potion_hp`, `test_item` 핸들러가 포함되어 있습니다.

## 정리용 SQL

```sql
DELETE FROM public.mails
WHERE title LIKE '[테스트]%'
   OR sender_name IN ('GM', '테스트');
```
