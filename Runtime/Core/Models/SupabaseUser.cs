using System;

namespace Truesoft.Supabase
{
    [Serializable]
    public sealed class SupabaseUser
    {
        public string id;
        public string email;
        public string phone;
    }
}