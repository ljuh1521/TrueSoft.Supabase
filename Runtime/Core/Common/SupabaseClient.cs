using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Http;

namespace Truesoft.Supabase
{
    public sealed class SupabaseClient
    {
        public SupabaseAuthService Auth { get; }

        public SupabaseOptions Options { get; }

        public SupabaseClient(SupabaseOptions options, ISupabaseJsonSerializer json, ISupabaseHttpClient http, ISupabaseAuthStorage storage)
        {
            Options = options;

            Auth = new SupabaseAuthService(options.ProjectURL, options.PublishableKey, http, json);
        }
    }
}