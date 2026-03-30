namespace Truesoft.Supabase
{
    public sealed class SupabaseOptions
    {
        public string ProjectURL;
        public string PublishableKey;
        public int TimeoutSeconds = 30;

        /// <summary>유저 세이브 REST 테이블 (기본 <c>user_saves</c>).</summary>
        public string UserSavesTable = "user_saves";

        /// <summary>Remote Config REST 테이블 (기본 <c>remote_config</c>).</summary>
        public string RemoteConfigTable = "remote_config";

        /// <summary>채팅 메시지 REST 테이블 (기본 <c>chat_messages</c>).</summary>
        public string ChatMessagesTable = "chat_messages";

        /// <summary>공개 프로필 REST 테이블 — <c>id</c>(row PK), <c>user_id</c>, <c>account_id</c>, <c>withdrawn_at</c> (기본 <c>profiles</c>).</summary>
        public string PublicProfilesTable = "profiles";

        /// <summary>중복 로그인 감지용 세션 토큰 테이블 (기본 <c>user_sessions</c>).</summary>
        public string UserSessionsTable = "user_sessions";

        /// <summary>
        /// 행동 시점 중복 로그인 검사 최소 간격(초). 0 이하면 행동마다 검사합니다.
        /// </summary>
        public float DuplicateSessionActionCheckCooldownSeconds = 5f;

        /// <summary>
        /// 탈퇴 요청 시 실제 탈퇴 시각(<c>profiles.withdrawn_at</c>)으로 예약할 유예 기간(일).
        /// </summary>
        public float WithdrawalRequestDelayDays = 7f;

        /// <summary>
        /// 로그인 직후 탈퇴 만료 계정 즉시 삭제 가드 함수 호출 여부.
        /// </summary>
        public bool EnableWithdrawalGuardOnLogin = true;

        /// <summary>
        /// 로그인 직후 호출할 Edge Function 이름 (기본 <c>withdrawal-guard</c>).
        /// </summary>
        public string WithdrawalGuardFunctionName = "withdrawal-guard";

        /// <summary>
        /// Google로 <b>신규</b> 가입으로 판단될 때만 <c>user_metadata.displayName</c>을 익명 기본값(<c>Player_xxxxxxxx</c>)으로 덮어씁니다. 재로그인·게스트→구글 연동은 건드리지 않습니다.
        /// </summary>
        public bool ApplyAnonymousDisplayNameOnNewGoogleSignUp = true;
    }
}