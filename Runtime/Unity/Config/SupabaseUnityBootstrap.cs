using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Data;
using Truesoft.Supabase.Core.Http;
using Truesoft.Supabase.Unity;
using UnityEngine;

namespace Truesoft.Supabase.Unity.Config
{
    public sealed class SupabaseUnityBootstrap
    {
        private static bool _didLogInit;

        public SupabaseAuthService AuthService { get; private set; }
        public SupabaseUserDataService UserDataService { get; private set; }
        public SupabaseUserEventsService UserEventsService { get; private set; }
        public SupabaseRemoteConfigService RemoteConfigService { get; private set; }
        public SupabaseChatService ChatService { get; private set; }
        public SupabaseEdgeFunctionsService EdgeFunctionsService { get; private set; }

        public void Initialize(SupabaseSettings settings)
        {
            var options = settings.ToOptions();

            if (_didLogInit == false)
            {
                _didLogInit = true;
                Debug.Log($"[Supabase] Initialize: projectUrl={options.ProjectURL} publishableKey={MaskKey(options.PublishableKey)}");
            }

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

            UserEventsService = new SupabaseUserEventsService(
                options.ProjectURL,
                options.PublishableKey,
                http,
                json);

            RemoteConfigService = new SupabaseRemoteConfigService(
                options.ProjectURL,
                options.PublishableKey,
                http,
                json);

            ChatService = new SupabaseChatService(
                options.ProjectURL,
                options.PublishableKey,
                http,
                json);

            EdgeFunctionsService = new SupabaseEdgeFunctionsService(
                options.ProjectURL,
                options.PublishableKey,
                http,
                json);

            SupabaseSDK.Initialize(this);
        }

        private static string MaskKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "(empty)";

            key = key.Trim();
            if (key.Length <= 10)
                return key.Substring(0, 2) + "..." + key.Substring(key.Length - 2, 2);

            return key.Substring(0, 6) + "..." + key.Substring(key.Length - 4, 4);
        }
    }
}