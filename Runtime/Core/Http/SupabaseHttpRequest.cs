using System.Collections.Generic;

namespace Truesoft.Supabase
{
    public sealed class SupabaseHttpRequest
    {
        public string Method;
        public string Url;
        public string Body;
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
        public int TimeoutSeconds = 30;
    }
}