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
    }
}