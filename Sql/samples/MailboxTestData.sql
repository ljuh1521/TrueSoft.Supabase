-- =============================================================================
-- 우편함 테스트 데이터
-- 이 SQL을 SQL Editor에서 실행하여 테스트용 우편을 발송합니다.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. 테스트 대상 유저 찾기 (실제 uuid와 user_id로 교체해서 사용)
-- -----------------------------------------------------------------------------
-- SELECT account_id, user_id, display_name
-- FROM public.profiles p
-- JOIN public.display_names dn ON p.account_id = dn.account_id
-- LIMIT 5;

-- 아래 INSERT문들의 'YOUR_ACCOUNT_UUID'와 'YOUR_USER_ID'를 실제 값으로 교체하세요.

-- -----------------------------------------------------------------------------
-- 테스트 1: 단일 보상 (골드만)
-- -----------------------------------------------------------------------------
INSERT INTO public.mails (account_id, user_id, sender_type, sender_name, title, content, expires_at, items)
VALUES (
    'YOUR_ACCOUNT_UUID'::uuid,  -- ★ 교체 필요
    'YOUR_USER_ID',              -- ★ 교체 필요
    'system',
    '운영팀',
    '[테스트] 골드 보상',
    '테스트용 골드 1000을 지급합니다.',
    now() + interval '7 days',
    '[{"key": "gold", "count": 1000}]'::jsonb
);

-- -----------------------------------------------------------------------------
-- 테스트 2: 복합 보상 (골드 + 보석 + 무기)
-- -----------------------------------------------------------------------------
INSERT INTO public.mails (account_id, user_id, sender_type, sender_name, title, content, expires_at, items)
VALUES (
    'YOUR_ACCOUNT_UUID'::uuid,
    'YOUR_USER_ID',
    'event',
    '이벤트팀',
    '[테스트] 복합 보상 패키지',
    '출석 이벤트 보상입니다. 다양한 아이템을 확인해보세요!',
    now() + interval '14 days',
    '[
        {"key": "gold", "count": 5000},
        {"key": "gem", "count": 100},
        {"key": "weapon_001", "count": 1},
        {"key": "potion_hp", "count": 5}
    ]'::jsonb
);

-- -----------------------------------------------------------------------------
-- 테스트 3: 보상 없는 텍스트 전용 메일 (공지)
-- -----------------------------------------------------------------------------
INSERT INTO public.mails (account_id, user_id, sender_type, sender_name, title, content, expires_at, items)
VALUES (
    'YOUR_ACCOUNT_UUID'::uuid,
    'YOUR_USER_ID',
    'notice',
    '시스템',
    '[테스트] 서버 점검 안내',
    '내일 새벽 3시부터 2시간 동안 서버 점검이 진행됩니다.\n\n점검 시간: 03:00 ~ 05:00\n영향: 로그인 및 게임플레이 불가',
    now() + interval '3 days',
    NULL  -- 보상 없음
);

-- -----------------------------------------------------------------------------
-- 테스트 4: 테스트용 커스텀 아이템
-- -----------------------------------------------------------------------------
INSERT INTO public.mails (account_id, user_id, sender_type, sender_name, title, content, expires_at, items)
VALUES (
    'YOUR_ACCOUNT_UUID'::uuid,
    'YOUR_USER_ID',
    'gm',
    'GM',
    '[테스트] GM 특별 보상',
    '테스트를 위해 GM이 직접 발송한 특별 보상입니다.',
    now() + interval '30 days',
    '[
        {"key": "test_item", "count": 10},
        {"key": "gold", "count": 9999}
    ]'::jsonb
);

-- -----------------------------------------------------------------------------
-- 테스트 5: 이미 만료된 메일 (만료 정리 테스트용)
-- -----------------------------------------------------------------------------
INSERT INTO public.mails (account_id, user_id, sender_type, sender_name, title, content, expires_at, items)
VALUES (
    'YOUR_ACCOUNT_UUID'::uuid,
    'YOUR_USER_ID',
    'system',
    '운영팀',
    '[테스트] 만료된 메일',
    '이 메일은 이미 만료되어 수령 불가능해야 합니다.',
    now() - interval '1 day',  -- 어제 만료됨
    '[{"key": "expired_test", "count": 1}]'::jsonb
);

-- -----------------------------------------------------------------------------
-- 테스트 6: 빈 보상 배열 (수령 no-op 테스트)
-- -----------------------------------------------------------------------------
INSERT INTO public.mails (account_id, user_id, sender_type, sender_name, title, content, expires_at, items)
VALUES (
    'YOUR_ACCOUNT_UUID'::uuid,
    'YOUR_USER_ID',
    'system',
    '운영팀',
    '[테스트] 보상 없는 메일 (빈 배열)',
    '이 메일은 보상이 없습니다. 수령 시 no-op으로 성공해야 합니다.',
    now() + interval '7 days',
    '[]'::jsonb  -- 빈 배열
);

-- -----------------------------------------------------------------------------
-- 발송 후 확인 쿼리
-- -----------------------------------------------------------------------------
-- SELECT * FROM public.mails WHERE account_id = 'YOUR_ACCOUNT_UUID'::uuid ORDER BY created_at DESC;
