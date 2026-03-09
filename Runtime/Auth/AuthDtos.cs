using System;

namespace Truesoft.Supabase
{
    [Serializable]
    public sealed class SignInWithPasswordRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public sealed class RefreshTokenRequest
    {
        public string refresh_token;
    }

    [Serializable]
    public sealed class SupabaseUserDto
    {
        public string id;
        public string email;
    }

    [Serializable]
    public sealed class AuthTokenResponse
    {
        public string access_token;
        public string refresh_token;
        public string token_type;
        public int expires_in;
        public SupabaseUserDto user;
    }

    [Serializable]
    public sealed class AuthSessionDto
    {
        public string access_token;
        public string refresh_token;
        public string token_type;
        public int expires_in;
    }

    [Serializable]
    public sealed class SignUpResponse
    {
        public SupabaseUserDto user;
        public AuthSessionDto session;
    }
}