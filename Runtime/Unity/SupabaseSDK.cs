using System;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Data;
using Truesoft.Supabase.Unity.Config;

namespace Truesoft.Supabase.Unity
{
    public static class SupabaseSDK
    {
        private static SupabaseUnityBootstrap _bootstrap;
        private static UserSavesFacade _userSaves;

        public static SupabaseAuthService Auth => _bootstrap?.AuthService;

        public static UserSavesFacade UserSaves
        {
            get
            {
                if (_bootstrap == null)
                    throw new InvalidOperationException("SupabaseSDK is not initialized. Call SupabaseUnityBootstrap.Initialize first.");

                return _userSaves ??= new UserSavesFacade(_bootstrap.UserDataService, () => _bootstrap.AuthService);
            }
        }

        public static void Initialize(SupabaseUnityBootstrap bootstrap)
        {
            _bootstrap = bootstrap ?? throw new ArgumentNullException(nameof(bootstrap));
            _userSaves = null;
        }
    }
}

