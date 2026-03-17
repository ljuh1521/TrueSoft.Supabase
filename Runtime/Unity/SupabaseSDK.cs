using System;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Data;
using Truesoft.Supabase.Unity.Config;
using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    public static class SupabaseSDK
    {
        private const string RefreshTokenKey = "Truesoft.Supabase.RefreshToken";

        private static SupabaseUnityBootstrap _bootstrap;
        private static SupabaseSession _currentSession;
        private static UserSavesFacade _userSaves;
        private static UserEventsFacade _userEvents;
        private static RemoteConfigFacade _remoteConfig;

        /// <summary>SDK가 초기화되었는지 여부.</summary>
        public static bool IsInitialized => _bootstrap != null;

        /// <summary>현재 로그인된 세션. 로그인 후 SetSession으로 설정하세요.</summary>
        public static SupabaseSession Session => _currentSession;

        /// <summary>현재 로그인 여부 (세션이 있고 유효한 토큰이 있는지).</summary>
        public static bool IsLoggedIn =>
            _currentSession != null
            && string.IsNullOrWhiteSpace(_currentSession.AccessToken) == false
            && _currentSession.User != null
            && string.IsNullOrWhiteSpace(_currentSession.User.Id) == false;

        /// <summary>인증 서비스. 초기화 후에만 사용하세요.</summary>
        public static SupabaseAuthService Auth => _bootstrap?.AuthService;

        /// <summary>유저 세이브/로드 퍼사드. 초기화 후에만 사용하세요.</summary>
        public static UserSavesFacade UserSaves
        {
            get
            {
                if (_bootstrap == null)
                    throw new InvalidOperationException("SupabaseSDK is not initialized. Call SupabaseUnityBootstrap.Initialize first.");

                return _userSaves ??= new UserSavesFacade(_bootstrap.UserDataService, () => _currentSession);
            }
        }

        /// <summary>이벤트 전송 퍼사드. 초기화 후에만 사용하세요.</summary>
        public static UserEventsFacade Events
        {
            get
            {
                if (_bootstrap == null)
                    throw new InvalidOperationException("SupabaseSDK is not initialized. Call SupabaseUnityBootstrap.Initialize first.");

                return _userEvents ??= new UserEventsFacade(_bootstrap.UserEventsService, () => _currentSession);
            }
        }

        /// <summary>RemoteConfig 퍼사드. 초기화 후에만 사용하세요.</summary>
        public static RemoteConfigFacade RemoteConfig
        {
            get
            {
                if (_bootstrap == null)
                    throw new InvalidOperationException("SupabaseSDK is not initialized. Call SupabaseUnityBootstrap.Initialize first.");

                return _remoteConfig ??= new RemoteConfigFacade(
                    _bootstrap.RemoteConfigService,
                    () => _currentSession?.AccessToken);
            }
        }

        /// <summary>로그인 성공 시 세션을 SDK에 설정하세요. 이후 SaveAsync/LoadAsync/Events는 세션 없이 호출 가능.</summary>
        public static void SetSession(SupabaseSession session)
        {
            _currentSession = session;
        }

        /// <summary>로그아웃 시 호출. clearStorage가 true면 PlayerPrefs에 저장된 refresh_token도 삭제합니다.</summary>
        public static void ClearSession(bool clearStorage = true)
        {
            _currentSession = null;
            if (clearStorage)
                PlayerPrefs.DeleteKey(RefreshTokenKey);
        }

        /// <summary>현재 세션의 refresh_token을 PlayerPrefs에 저장. 앱 재시작 후 RestoreSessionAsync로 복원할 수 있습니다.</summary>
        public static void SaveSessionToStorage()
        {
            if (_currentSession == null || string.IsNullOrWhiteSpace(_currentSession.RefreshToken))
                return;
            PlayerPrefs.SetString(RefreshTokenKey, _currentSession.RefreshToken);
            PlayerPrefs.Save();
        }

        /// <summary>PlayerPrefs에 저장된 refresh_token으로 세션을 복원합니다. Runner의 'Restore Session On Start' 또는 로그인 화면에서 호출하세요.</summary>
        public static async Task<bool> RestoreSessionAsync()
        {
            if (_bootstrap?.AuthService == null)
                return false;

            var refreshToken = PlayerPrefs.GetString(RefreshTokenKey, null);
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var result = await _bootstrap.AuthService.RefreshSessionAsync(refreshToken);
            if (result.IsSuccess && result.Data != null)
            {
                _currentSession = result.Data;
                return true;
            }

            PlayerPrefs.DeleteKey(RefreshTokenKey);
            return false;
        }

        public static void Initialize(SupabaseUnityBootstrap bootstrap)
        {
            _bootstrap = bootstrap ?? throw new ArgumentNullException(nameof(bootstrap));
            _currentSession = null;
            _userSaves = null;
            _userEvents = null;
            _remoteConfig = null;
        }
    }
}

