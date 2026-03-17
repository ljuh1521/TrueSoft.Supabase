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
        private static UserEventsFacade _userEvents;

        /// <summary>SDK가 초기화되었는지 여부.</summary>
        public static bool IsInitialized => _bootstrap != null;

        /// <summary>인증 서비스. 초기화 후에만 사용하세요.</summary>
        public static SupabaseAuthService Auth => _bootstrap?.AuthService;

        /// <summary>유저 세이브/로드 퍼사드. 초기화 후에만 사용하세요.</summary>
        public static UserSavesFacade UserSaves
        {
            get
            {
                if (_bootstrap == null)
                    throw new InvalidOperationException("SupabaseSDK is not initialized. Call SupabaseUnityBootstrap.Initialize first.");

                return _userSaves ??= new UserSavesFacade(_bootstrap.UserDataService);
            }
        }

        /// <summary>이벤트 전송 퍼사드. 초기화 후에만 사용하세요.</summary>
        public static UserEventsFacade Events
        {
            get
            {
                if (_bootstrap == null)
                    throw new InvalidOperationException("SupabaseSDK is not initialized. Call SupabaseUnityBootstrap.Initialize first.");

                return _userEvents ??= new UserEventsFacade(_bootstrap.UserEventsService);
            }
        }

        public static void Initialize(SupabaseUnityBootstrap bootstrap)
        {
            _bootstrap = bootstrap ?? throw new ArgumentNullException(nameof(bootstrap));
            _userSaves = null;
            _userEvents = null;
        }
    }
}

