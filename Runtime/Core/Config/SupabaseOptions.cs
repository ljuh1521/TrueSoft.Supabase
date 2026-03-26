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

        /// <summary>공개 프로필(닉네임) REST 테이블 — <c>id</c>(auth UUID), <c>nickname</c> (기본 <c>profiles</c>).</summary>
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
    }
}