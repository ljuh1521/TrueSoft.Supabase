using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Common;
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
        private static ServerFunctionsFacade _functions;
        private static readonly Dictionary<string, ChatChannelFacade> _chatChannels = new(StringComparer.Ordinal);

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

        /// <summary>Google ID Token으로 로그인하고 SDK 세션을 자동 설정합니다.</summary>
        public static async Task<SupabaseResult<SupabaseSession>> SignInWithGoogleIdTokenAsync(string idToken, bool saveSessionToStorage = true)
        {
            if (Auth == null)
                return SupabaseResult<SupabaseSession>.Fail("sdk_not_initialized");

            // 익명(게스트) 세션에서 Google idToken을 받으면, 먼저 identity link을 시도한 뒤 로그인합니다.
            // 이를 통해 "게스트 -> 구글 연동" UX를 지원합니다.
            if (IsAnonymousSession(_currentSession))
            {
                try
                {
                    var linkResult = await Auth.LinkIdentityWithIdTokenAsync(
                        _currentSession.AccessToken,
                        "google",
                        idToken);

                    if (linkResult.IsSuccess == false)
                        UnityEngine.Debug.LogWarning("[Supabase] anonymous->google link failed: " + linkResult.ErrorMessage);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning("[Supabase] anonymous->google link exception: " + e.Message);
                }
            }

            var result = await Auth.SignInWithGoogleIdTokenAsync(idToken);
            if (result.IsSuccess && result.Data != null)
            {
                SetSession(result.Data);
                if (saveSessionToStorage)
                    SaveSessionToStorage();
            }

            return result;
        }

        /// <summary>게스트(익명)로 로그인하고 SDK 세션을 자동 설정합니다.</summary>
        public static async Task<SupabaseResult<SupabaseSession>> SignInAnonymouslyAsync(bool saveSessionToStorage = true)
        {
            if (Auth == null)
                return SupabaseResult<SupabaseSession>.Fail("sdk_not_initialized");

            var result = await Auth.SignInAnonymouslyAsync();
            if (result.IsSuccess && result.Data != null)
            {
                SetSession(result.Data);
                if (saveSessionToStorage)
                    SaveSessionToStorage();
            }

            return result;
        }

        /// <summary>refresh_token으로 세션을 갱신하고 SDK 세션을 자동 설정합니다.</summary>
        public static async Task<SupabaseResult<SupabaseSession>> RefreshSessionAsync(string refreshToken, bool saveSessionToStorage = true)
        {
            if (Auth == null)
                return SupabaseResult<SupabaseSession>.Fail("sdk_not_initialized");

            var result = await Auth.RefreshSessionAsync(refreshToken);
            if (result.IsSuccess && result.Data != null)
            {
                SetSession(result.Data);
                if (saveSessionToStorage)
                    SaveSessionToStorage();
            }

            return result;
        }

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

        /// <summary>현재 세션으로 유저 데이터 저장.</summary>
        public static Task<SupabaseResult<bool>> SaveUserDataAsync<T>(T data)
        {
            return UserSaves.SaveAsync(data);
        }

        /// <summary>현재 세션으로 유저 데이터 로드.</summary>
        public static Task<SupabaseResult<T>> LoadUserDataAsync<T>() where T : class, new()
        {
            return UserSaves.LoadAsync<T>();
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

        /// <summary>현재 세션으로 이벤트 전송 (payload 없음).</summary>
        public static Task<SupabaseResult<bool>> SendUserEventAsync(string eventType)
        {
            return Events.SendAsync(eventType);
        }

        /// <summary>현재 세션으로 이벤트+payload 전송.</summary>
        public static Task<SupabaseResult<bool>> SendUserEventAsync<T>(string eventType, T payload)
        {
            return Events.SendAsync(eventType, payload);
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

        /// <summary>RemoteConfig 전체 새로고침.</summary>
        public static Task<bool> RefreshRemoteConfigAsync()
        {
            return RemoteConfig.RefreshAllAsync();
        }

        /// <summary>RemoteConfig 변경분 폴링.</summary>
        public static Task<bool> PollRemoteConfigAsync()
        {
            return RemoteConfig.PollAsync();
        }

        public static T GetRemoteConfig<T>(string key, T defaultValue = default)
        {
            return RemoteConfig.Get(key, defaultValue);
        }

        public static bool TryGetRemoteConfigRaw(string key, out string valueJson)
        {
            return RemoteConfig.TryGetRaw(key, out valueJson);
        }

        public static void SubscribeRemoteConfig(string key, Action<string> onValueChanged, bool invokeIfCached = true)
        {
            RemoteConfig.Subscribe(key, onValueChanged, invokeIfCached);
        }

        public static void UnsubscribeRemoteConfig(string key, Action<string> onValueChanged)
        {
            RemoteConfig.Unsubscribe(key, onValueChanged);
        }

        /// <summary>서버 함수(Edge Functions) 호출 퍼사드.</summary>
        public static ServerFunctionsFacade Functions
        {
            get
            {
                if (_bootstrap == null)
                    throw new InvalidOperationException("SupabaseSDK is not initialized. Call SupabaseUnityBootstrap.Initialize first.");

                return _functions ??= new ServerFunctionsFacade(
                    _bootstrap.EdgeFunctionsService,
                    () => _currentSession);
            }
        }

        /// <summary>같은 channel_id 유저끼리 채팅. 로그인 세션 필요. 채널 단위로 Facade를 캐시합니다.</summary>
        public static ChatChannelFacade OpenChatChannel(string channelId, string displayName = null)
        {
            if (_bootstrap == null)
                throw new InvalidOperationException("SupabaseSDK is not initialized.");

            if (string.IsNullOrWhiteSpace(channelId))
                throw new ArgumentException("channelId is empty", nameof(channelId));

            channelId = channelId.Trim();

            if (_chatChannels.TryGetValue(channelId, out var existing))
                return existing;

            var facade = new ChatChannelFacade(
                _bootstrap.ChatService,
                () => _currentSession,
                channelId,
                displayName);

            _chatChannels[channelId] = facade;
            return facade;
        }

        /// <summary>
        /// 채팅 메시지 한 건 전송 (인스턴스를 직접 들고 있지 않아도 됨).
        /// UI에서 채널 접속/폴링 여부는 여전히 OpenChatChannel/StartPolling으로 직접 관리하세요.
        /// </summary>
        public static Task<bool> SendChatMessageAsync(string channelId, string content, string displayName = null)
        {
            var channel = OpenChatChannel(channelId, displayName);
            return channel.SendAsync(content);
        }

        /// <summary>채널이 현재 SDK 캐시에 열려 있는지 확인.</summary>
        public static bool IsChatChannelOpen(string channelId)
        {
            return GetChatChannel(channelId) != null;
        }

        /// <summary>서버 함수 호출(로그인 세션 필요).</summary>
        public static Task<SupabaseResult<TResponse>> InvokeFunctionAsync<TResponse>(string functionName, object requestBody = null)
        {
            return Functions.InvokeAsync<TResponse>(functionName, requestBody, requireAuth: true);
        }

        /// <summary>
        /// 채팅 채널에 join + 이벤트 구독 + 폴링 시작까지 한 번에 수행합니다.
        /// 반환값은 캐시에 저장된 Facade이지만, 호출 측에서 들고 있을 필요는 없습니다.
        /// </summary>
        public static ChatChannelFacade JoinChatChannel(
            string channelId,
            MonoBehaviour pollHost,
            Action<SupabaseChatService.ChatMessageRow> onMessageReceived,
            float pollIntervalSeconds = 1.5f,
            bool loadHistory = true,
            int historyCount = 50)
        {
            if (pollHost == null)
                throw new ArgumentNullException(nameof(pollHost));

            var channel = OpenChatChannel(channelId);

            if (onMessageReceived != null)
                channel.OnMessageReceived += onMessageReceived;

            if (loadHistory)
                pollHost.StartCoroutine(LoadHistoryRoutine(channel, historyCount));

            channel.StartPolling(pollHost, pollIntervalSeconds);
            return channel;
        }

        /// <summary>
        /// JoinChatChannel로 구독한 채널에서 빠져나옵니다.
        /// onMessageReceived를 넘기면 해당 핸들러만 제거하고, stopPollingIfNoListeners가 true면 더 이상 리스너가 없을 때 폴링을 멈춥니다.
        /// </summary>
        public static void LeaveChatChannel(
            string channelId,
            Action<SupabaseChatService.ChatMessageRow> onMessageReceived = null,
            bool stopPollingIfNoListeners = true)
        {
            var channel = GetChatChannel(channelId);
            if (channel == null)
                return;

            if (onMessageReceived != null)
                channel.OnMessageReceived -= onMessageReceived;

            if (stopPollingIfNoListeners)
            {
                // event에 남은 구독자가 있는지 확인할 수 없으므로, 호출 측에서 명시적으로 CloseChatChannel을 부르지 않는 한
                // 여기서는 단순히 StopPolling만 맡깁니다.
                channel.StopPolling();
            }
        }

        private static System.Collections.IEnumerator LoadHistoryRoutine(ChatChannelFacade channel, int count)
        {
            var task = channel.LoadHistoryAsync(count);
            yield return new UnityEngine.WaitUntil(() => task.IsCompleted);
        }

        /// <summary>현재 캐시에 열린 채팅 채널이 있으면 반환합니다. 없으면 null.</summary>
        public static ChatChannelFacade GetChatChannel(string channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId))
                return null;

            _chatChannels.TryGetValue(channelId.Trim(), out var facade);
            return facade;
        }

        /// <summary>채팅 채널 캐시에서 제거합니다. (예: 세션 변경, 완전 종료 시)</summary>
        public static void CloseChatChannel(string channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId))
                return;

            channelId = channelId.Trim();
            if (_chatChannels.TryGetValue(channelId, out var facade))
            {
                facade.StopPolling();
                _chatChannels.Remove(channelId);
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
            // 채널 상태는 세션이 끊기면 더 이상 의미가 없으므로 정리
            foreach (var pair in _chatChannels)
            {
                pair.Value?.StopPolling();
            }
            _chatChannels.Clear();

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
            _functions = null;
            _chatChannels.Clear();
        }

        private static bool IsAnonymousSession(SupabaseSession session)
        {
            if (session == null || session.User == null)
                return false;

            if (string.IsNullOrWhiteSpace(session.AccessToken))
                return false;

            return session.User.IsAnonymous;
        }
    }
}

