using System;

namespace Truesoft.Supabase
{
    [Serializable]
    public sealed class SupabaseSession
    {
        public string access_token;

        public string refresh_token;

        public int expires_in;

        public string token_type;

        public SupabaseUser user;

        public DateTime created_at = DateTime.UtcNow;

        public bool IsExpired()
        {
            return DateTime.UtcNow >= created_at.AddSeconds(expires_in - 60);
        }
    }
}