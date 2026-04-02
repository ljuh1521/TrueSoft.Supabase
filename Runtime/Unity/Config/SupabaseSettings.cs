using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// Supabase 프로젝트의 "공통 설정값"을 담는 에셋입니다.
    /// </summary>
    /// <remarks>
    /// 이 에셋은 프로젝트 전역에서 재사용되는 정적 값(서버 주소, 키, REST 테이블명, 기본 옵션)만 정의합니다.
    /// 씬 실행 정책(자동 복원, 폴링 주기 등)은 <see cref="Config.SupabaseRuntime"/>에서 제어합니다.
    /// 런타임에서는 <c>Resources/SupabaseSettings</c> 이름으로 로드되므로 경로·파일명을 맞춰야 합니다.
    /// <see cref="Supabase.TrySignInWithGoogleAsync(bool)"/> 호출 시 <see cref="googleWebClientId"/>를 읽습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "SupabaseSettings", menuName = "TrueSoft/Supabase/Supabase 설정")]
    public sealed class SupabaseSettings : ScriptableObject
    {
        [Header("Project Values (공통 설정값)")]
        [Tooltip("Supabase 프로젝트 URL (https://xxx.supabase.co 형태).")]
        public string projectUrl;

        [Tooltip("Supabase Publishable API 키.")]
        public string publishableKey;

        [Tooltip("Google Cloud OAuth 2.0 Web Client ID. Android 네이티브 Google 로그인(SignInWithGoogleAsync 무인자)에 사용합니다.")]
        public string googleWebClientId;

        [Header("Default SDK Options (기본 동작값)")]
        [Tooltip("Try API 결과 로그 출력 여부. 켜면 API별 고정 태그(예: Supabase.UserData.Save)로 성공/실패가 Console에 출력됩니다.")]
        public bool enableApiResultLogs = true;

        [Tooltip("HTTP 요청 타임아웃(초).")]
        public int timeoutSeconds = 30;

        [Header("REST Table Names (테이블 이름)")]
        [Tooltip("Save/Load 유저 데이터에 사용하는 테이블 이름. 스키마가 public이 아니면 schema.table 형식으로 지정할 수 있습니다.")]
        public string userSavesTable = "user_saves";

        [Tooltip("유저 세이브 로드 시 기본 select 컬럼 CSV(예: level,coins,updated_at). 비우면 게임 코드가 select를 직접 넘겨야 합니다.")]
        public string userSavesDefaultSelectColumnsCsv = "";

        [Tooltip("Remote Config 행을 읽는 테이블 이름.")]
        public string remoteConfigTable = "remote_config";

        [Tooltip("채팅 메시지를 저장·조회하는 테이블 이름.")]
        public string chatMessagesTable = "chat_messages";

        [Tooltip("공개 프로필 테이블 (권장: id UUID PK, user_id text, account_id uuid, withdrawn_at timestamptz). RLS로 누구나 SELECT, 본인만 INSERT/UPDATE.")]
        public string publicProfilesTable = "profiles";

        [Header("Server Shard (서버 샤드)")]
        [Tooltip("로그인 후 기본으로 묶을 서버 코드(예: GLOBAL, KR1). Sql/supabase_player_tables.sql의 game_servers.server_code와 동일해야 합니다.")]
        public string defaultServerCode = "GLOBAL";

        [Header("Duplicate login (중복 로그인)")]
        [Tooltip("켜면 user_sessions 테이블을 폴링해 다른 기기에서 같은 계정으로 로그인했을 때 이 기기에서 세션을 끊고 OnDuplicateLoginDetected를 호출합니다. Sql/supabase_player_tables.sql에 user_sessions가 있어야 합니다.")]
        public bool enableDuplicateSessionMonitor = true;

        [Tooltip("세션 토큰 폴링 주기(초). 0 이하면 등록 직후 비교만 하고 주기 폴링은 하지 않습니다.")]
        public float duplicateSessionPollSeconds = 0f;

        [Tooltip("행동 시점(저장/전송/함수호출 등) 중복 로그인 검사 최소 간격(초). 권장 5초.")]
        public float duplicateSessionActionCheckCooldownSeconds = 5f;

        [Tooltip("중복 로그인 감지용 테이블 이름 (기본 user_sessions).")]
        public string userSessionsTable = "user_sessions";

        [Header("Withdrawal Request (탈퇴 요청)")]
        [Tooltip("탈퇴 요청 시 실제 탈퇴 시각(withdrawn_at)으로 예약할 유예 기간(일). 예: 7이면 요청 시점 + 7일.")]
        public float withdrawalRequestDelayDays = 7f;

        [Tooltip("로그인 직후 탈퇴 만료 계정 즉시 삭제 가드 함수를 호출할지 여부입니다.")]
        public bool enableWithdrawalGuardOnLogin = true;

        [Tooltip("로그인 직후 호출할 Edge Function 이름(기본 withdrawal-guard).")]
        public string withdrawalGuardFunctionName = "withdrawal-guard";

        [Tooltip("Google로 신규 가입으로 판단될 때만 Auth user_metadata.displayName을 Player_xxxxxxxx 형태로 덮어씁니다. 구글 실명 자동 반영을 막습니다.")]
        public bool applyAnonymousDisplayNameOnNewGoogleSignUp = true;

        public SupabaseOptions ToOptions()
        {
            return new SupabaseOptions
            {
                ProjectURL = projectUrl,
                PublishableKey = publishableKey,
                TimeoutSeconds = timeoutSeconds,
                UserSavesTable = string.IsNullOrWhiteSpace(userSavesTable) ? "user_saves" : userSavesTable.Trim(),
                UserSavesDefaultSelectColumnsCsv = userSavesDefaultSelectColumnsCsv == null ? "" : userSavesDefaultSelectColumnsCsv.Trim(),
                RemoteConfigTable = string.IsNullOrWhiteSpace(remoteConfigTable) ? "remote_config" : remoteConfigTable.Trim(),
                ChatMessagesTable = string.IsNullOrWhiteSpace(chatMessagesTable) ? "chat_messages" : chatMessagesTable.Trim(),
                PublicProfilesTable = string.IsNullOrWhiteSpace(publicProfilesTable) ? "profiles" : publicProfilesTable.Trim(),
                UserSessionsTable = string.IsNullOrWhiteSpace(userSessionsTable) ? "user_sessions" : userSessionsTable.Trim(),
                DefaultServerCode = string.IsNullOrWhiteSpace(defaultServerCode) ? "GLOBAL" : defaultServerCode.Trim(),
                DuplicateSessionActionCheckCooldownSeconds = duplicateSessionActionCheckCooldownSeconds < 0f
                    ? 0f
                    : duplicateSessionActionCheckCooldownSeconds,
                WithdrawalRequestDelayDays = withdrawalRequestDelayDays < 0f
                    ? 0f
                    : withdrawalRequestDelayDays,
                EnableWithdrawalGuardOnLogin = enableWithdrawalGuardOnLogin,
                WithdrawalGuardFunctionName = string.IsNullOrWhiteSpace(withdrawalGuardFunctionName)
                    ? "withdrawal-guard"
                    : withdrawalGuardFunctionName.Trim(),
                ApplyAnonymousDisplayNameOnNewGoogleSignUp = applyAnonymousDisplayNameOnNewGoogleSignUp
            };
        }
    }
}