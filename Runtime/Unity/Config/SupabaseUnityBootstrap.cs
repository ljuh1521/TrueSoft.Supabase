using System;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Data;
using Truesoft.Supabase.Core.Http;
using Truesoft.Supabase.Unity;

namespace Truesoft.Supabase.Unity.Config
{
    public sealed class SupabaseUnityBootstrap
    {
        /// <summary>현재 부트스트랩이 사용 중인 프로젝트 URL (동일 URL로 재초기화될 때 기존 로그인 세션 유지에 사용).</summary>
        public string ProjectUrl { get; private set; }
        public bool EnableApiResultLogs { get; private set; } = true;

        public SupabaseAuthService AuthService { get; private set; }
        public SupabaseUserDataService UserDataService { get; private set; }
        public SupabaseRemoteConfigService RemoteConfigService { get; private set; }
        public SupabaseChatService ChatService { get; private set; }
        public SupabaseEdgeFunctionsService EdgeFunctionsService { get; private set; }

        public void Initialize(SupabaseSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var options = settings.ToOptions();
            ProjectUrl = options.ProjectURL ?? string.Empty;
            EnableApiResultLogs = settings.enableApiResultLogs;

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
    }
}