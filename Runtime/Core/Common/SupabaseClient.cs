namespace Truesoft.Supabase
{
    public sealed class SupabaseClient
    {
        public SupabaseOptions Options { get; }
        public ISupabaseJsonSerializer Json { get; }
        public ISupabaseHttpClient Http { get; }
        public ISupabaseAuthStorage AuthStorage { get; }

        public SupabaseAuthService Auth { get; }

        public SupabaseClient(
            SupabaseOptions options,
            ISupabaseJsonSerializer json,
            ISupabaseHttpClient http,
            ISupabaseAuthStorage authStorage = null)
        {
            options.Validate();

            Options = options;
            Json = json;
            Http = http;
            AuthStorage = authStorage;

            Auth = new SupabaseAuthService(options, json, http, authStorage);
        }
    }
}