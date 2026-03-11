namespace Truesoft.Supabase.Unity
{
    public static class SupabaseUnityBootstrap
    {
        public static SupabaseClient CreateClient(SupabaseSettings settings)
        {
            var options = settings.ToOptions();
            var json = new UnitySupabaseJsonSerializer();
            var http = new UnitySupabaseHttpClient();
            var storage = new UnityPlayerPrefsAuthStorage();

            return new SupabaseClient(options, json, http, storage);
        }
    }
}