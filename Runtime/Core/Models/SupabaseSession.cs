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
        public string id;
        public string email;

        public string Id => id;
        public string Email => email;
    }
}