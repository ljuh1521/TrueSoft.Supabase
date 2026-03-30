using System;

namespace Truesoft.Supabase.Core.Auth
{
    [Serializable]
    public sealed class SupabaseSession
    {
        public string access_token;
        public string refresh_token;
        public string token_type;
        public int expires_in;
        public long expires_at;
        public SupabaseUser user;

        /// <summary>
        /// Auth 응답 파싱 직후만 사용. Google OAuth로 <b>방금 생성된</b> 계정으로 추정되면 true(서버 JSON에는 없음).
        /// </summary>
        public bool likely_brand_new_google_signup;

        public string AccessToken => access_token;
        public string RefreshToken => refresh_token;
        public string TokenType => token_type;
        public int ExpiresIn => expires_in;
        public long ExpiresAt => expires_at;
        public SupabaseUser User => user;
    }

    [Serializable]
    public sealed class SupabaseUser
    {
        /// <summary>Supabase Auth <c>auth.users.id</c> (JWT <c>sub</c>).</summary>
        public string id;

        public string email;
        public bool is_anonymous;

        /// <summary>
        /// DB <c>profiles.user_id</c> / <c>user_saves.user_id</c>에 넣을 안정 플레이어 id.
        /// OAuth면 응답의 <c>identities[0].identity_data.sub</c>로 채우고, 없으면 <see cref="id"/>와 동일하게 둡니다.
        /// </summary>
        public string player_user_id;

        public string Id => id;
        public string Email => email;
        public bool IsAnonymous => is_anonymous;

        /// <summary><see cref="player_user_id"/>가 비어 있으면 <see cref="id"/>를 반환합니다.</summary>
        public string PlayerUserId =>
            string.IsNullOrWhiteSpace(player_user_id) ? id : player_user_id.Trim();
    }
}