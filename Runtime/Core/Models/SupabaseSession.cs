using System;

namespace Truesoft.Supabase
{
    [Serializable]
    public sealed class SupabaseSession
    {
        public string access_token;
        public string token_type;
        public int expires_in;
        public string refresh_token;
        public SupabaseUser user;
    }
}