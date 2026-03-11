namespace Truesoft.Supabase.Unity
{
    public static class SupabaseUnityBootstrap
    {
        public static SupabaseClient CreateClient(SupabaseSettings settings)
        {
            var options = new SupabaseOptions
            {
                ProjectURL = settings.projectUrl,
                PublishableKey = settings.publishableKey
            };

            var http = new UnitySupabaseHttpClient(settings.timeoutSeconds);
            var json = new UnitySupabaseJsonSerializer();
            var storage = new UnityPlayerPrefsAuthStorage();

            return new SupabaseClient(options, json, http, storage);
        }
    }
}