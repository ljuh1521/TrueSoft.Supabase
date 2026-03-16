using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Config;
using Truesoft.Supabase.Core.Data;
using Truesoft.Supabase.Core.Http;
using Truesoft.Supabase.Unity;

namespace Truesoft.Supabase.Unity.Config
{
    public sealed class SupabaseUnityBootstrap
    {
        public SupabaseAuthService AuthService { get; private set; }
        public SupabaseUserDataService UserDataService { get; private set; }

        public void Initialize(SupabaseSettings settings)
        {
            var options = settings.ToOptions();

            var http = new UnitySupabaseHttpClient(options.TimeoutSeconds);
            var json = new UnitySupabaseJsonSerializer();

            AuthService = new SupabaseAuthService(
                options.ProjectURL,
                options.PublishableKey,
                http,
                json);

            UserDataService = new SupabaseUserDataService(
                options.ProjectURL,
                options.PublishableKey,
                http,
                json);

            SupabaseSDK.Initialize(this);
        }
    }
}