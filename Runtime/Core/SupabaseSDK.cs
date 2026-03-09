using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Truesoft.Supabase
{
    public static class SupabaseSDK
    {
        public static bool IsInitialized { get; private set; }
        public static SupabaseSettings Settings { get; private set; }

        public static SupabaseAuthService Auth { get; private set; }
        public static SupabaseDatabaseService Database { get; private set; }
        public static SupabaseFunctionsService Functions { get; private set; }

        public static async Task InitializeAsync(SupabaseSettings settings)
        {
            if (IsInitialized)
                return;

            if (settings == null)
                throw new Exception("SupabaseSettings is null.");

            if (string.IsNullOrWhiteSpace(settings.ProjectUrl))
                throw new Exception("SupabaseSettings.ProjectUrl is empty.");

            if (string.IsNullOrWhiteSpace(settings.AnonKey))
                throw new Exception("SupabaseSettings.AnonKey is empty.");

            Settings = settings;

            var http = new SupabaseHttp(settings);
            Auth = new SupabaseAuthService(settings, http);
            Database = new SupabaseDatabaseService(settings, http, Auth);
            Functions = new SupabaseFunctionsService(settings, http, Auth);

            IsInitialized = true;

            if (settings.VerboseLog)
                Debug.Log("[Truesoft.Supabase] Initialized.");
            
            await Task.CompletedTask;
        }
    }
}