using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Data;
using Truesoft.Supabase.Unity.Auth.Anonymous;
using Truesoft.Supabase.Unity.Auth;
using Truesoft.Supabase.Unity.Auth.Google;
using Truesoft.Supabase.Unity.Config;
using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// Unity용 Supabase 정적 진입점. 초기화·인증·유저 데이터·공개 닉네임·Remote Config·Edge Functions·채팅 API를 한 곳에 둡니다.
    /// </summary>
    /// <remarks>
    /// <b>구글 로그인 두 가지</b><br/>
    /// • <see cref="SignInWithGoogleAsync()"/> — Android 네이티브 플러그인으로 계정 선택·ID 토큰 획득까지 포함한 끝단 흐름.<br/>
    /// • <see cref="SignInWithGoogleIdTokenAsync"/> — 이미 가진 Google ID 토큰 문자열만 넘겨 Supabase에만 맞출 때(iOS, 커스텀 OAuth, 테스트 등). 입력 형태가 달라 둘 다 유지합니다.
    /// </remarks>
    public static class SupabaseSDK
    {
        private const string RefreshTokenKey = "Truesoft.Supabase.RefreshToken";

        /// <summary>계정별 로컬 세션 토큰 저장 키 접두어. <c>PlayerPrefs</c> 키는 <c>{접두어}{account_id}</c> 입니다.</summary>
        public const string SessionTokenPlayerPrefsKeyPrefix = "Truesoft.Supabase.SessionToken.";

        private static SupabaseUnityBootstrap _bootstrap;
        private static SupabaseSession _currentSession;
        private static UserSavesFacade _userSaves;
        private static RemoteConfigFacade _remoteConfig;
        private static ServerFunctionsFacade _functions;
        private static readonly Dictionary<string, ChatChannelFacade> _chatChannels = new(StringComparer.Ordinal);
        private static string _initializedProjectUrl;
        private static bool _enableApiResultLogs = true;

        private static float _remoteConfigPollIntervalSeconds = 0f;
        private static float _remoteConfigNextPollAtRealtime = 0f;

        private static bool _duplicateSessionMonitorEnabled = true;
        private static float _duplicateSessionPollSeconds = 15f;
        private static float _duplicateSessionActionCheckCooldownSeconds = 5f;

        /// <summary>
        /// 다른 기기에서 같은 계정으로 로그인해 서버 세션 토큰이 바뀐 경우 호출됩니다(이미 <see cref="ClearSession"/> 후).
        /// UI에서 팝업을 띄우세요.
        /// </summary>
        public static event Action OnDuplicateLoginDetected;

        /// <summary><see cref="Config.SupabaseSettings.enableDuplicateSessionMonitor"/>.</summary>
        public static bool DuplicateSessionMonitorEnabled => _duplicateSessionMonitorEnabled;

        /// <summary><see cref="Config.SupabaseSettings.duplicateSessionPollSeconds"/>.</summary>
        public static float DuplicateSessionPollSeconds => _duplicateSessionPollSeconds;

        /// <summary><see cref="Config.SupabaseSettings.duplicateSessionActionCheckCooldownSeconds"/>.</summary>
        public static float DuplicateSessionActionCheckCooldownSeconds => _duplicateSessionActionCheckCooldownSeconds;

        /// <summary>중복 로그인 감지용 <c>user_sessions</c> REST 서비스. 미초기화 시 null.</summary>
        public static SupabaseUserSessionService UserSessionService => _bootstrap?.UserSessionService;
        public static SupabaseAnonymousRecoveryService AnonymousRecoveryService => _bootstrap?.AnonymousRecoveryService;

        /// <summary>서버 기준 시각 RPC. 로그인 없이 호출 가능합니다.</summary>
        public static SupabaseServerTimeService ServerTimeService => _bootstrap?.ServerTimeService;

        /// <summary>Try* API 결과 로그 접두어. API마다 고정이며 호출자가 넘기지 않습니다.</summary>
        private static class ApiLogTags
        {
            public const string AuthGoogleSettings = "Supabase.Auth.Google.Settings";
            public const string AuthGoogleIdToken = "Supabase.Auth.Google.IdToken";
            public const string AuthAnonymous = "Supabase.Auth.Anonymous";
            public const string AuthGoogleSignOut = "Supabase.Auth.Google.SignOut";
            public const string BootStart = "Supabase.Boot.Start";
            public const string AuthRefreshSession = "Supabase.Auth.RefreshSession";
            public const string UserDataSave = "Supabase.UserData.Save";
            public const string UserDataLoad = "Supabase.UserData.Load";
            public const string EdgeFunctionInvoke = "Supabase.EdgeFunction.Invoke";
            public const string RemoteConfigRefresh = "Supabase.RemoteConfig.Refresh";
            public const string RemoteConfigPoll = "Supabase.RemoteConfig.Poll";
            public const string RemoteConfigGet = "Supabase.RemoteConfig.Get";
            public const string ChatSend = "Supabase.Chat.Send";
            public const string ChatJoin = "Supabase.Chat.Join";
            public const string AuthRestoreSession = "Supabase.Auth.RestoreSession";
            public const string ProfilePublicNicknameGet = "Supabase.Profile.Nickname.Get";
            public const string ProfileMyNicknameSet = "Supabase.Profile.Nickname.Set";
            public const string ProfileNicknameAvailable = "Supabase.Profile.Nickname.Available";
            public const string ProfileSnapshotGet = "Supabase.Profile.Snapshot.Get";
            public const string ProfileWithdrawnAt = "Supabase.Profile.WithdrawnAt";
            public const string ServerTime = "Supabase.Server.Time";
        }

        internal static float RemoteConfigNextPollAtRealtime => _remoteConfigNextPollAtRealtime;

        internal static void ForceRemoteConfigNextPollAt(float realtimeAt)
        {
            _remoteConfigNextPollAtRealtime = realtimeAt;
        }

        internal static void UpdateRemoteConfigPollIntervalSeconds(float intervalSeconds)
        {
            _remoteConfigPollIntervalSeconds = intervalSeconds <= 0f ? 0f : intervalSeconds;
        }

        internal static void ScheduleRemoteConfigNextPollAt(float realtimeAt)
        {
            if (realtimeAt > _remoteConfigNextPollAtRealtime)
                _remoteConfigNextPollAtRealtime = realtimeAt;
        }

        private static void RequestRemoteConfigPollingReset()
        {
            if (_remoteConfigPollIntervalSeconds <= 0f)
                return;

            // 의도치 않게 더 자주 호출되지 않도록, "현재 스케줄보다 더 늦게"만 확장합니다.
            var target = Time.realtimeSinceStartup + _remoteConfigPollIntervalSeconds;
            ScheduleRemoteConfigNextPollAt(target);
        }

        /// <summary><see cref="EnsureInitializedAsync"/> 기본 대기 시간(ms). 씬의 <c>SupabaseRuntime</c> Awake를 기다립니다.</summary>
        public const int DefaultEnsureInitTimeoutMs = 30000;

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

        /// <summary>
        /// 씬의 <c>SupabaseRuntime</c> 등으로 초기화될 때까지 대기한 뒤, 실패 시 <c>Resources/SupabaseSettings</c>로 부트스트랩을 시도합니다.
        /// Unity 메인 스레드에서 호출하는 것을 권장합니다.
        /// </summary>
        public static async Task<bool> EnsureInitializedAsync(int timeoutMs = DefaultEnsureInitTimeoutMs)
        {
            if (IsInitialized)
                return true;

            // SupabaseRuntime(Awake) 쪽 초기화를 잠시 기다립니다.
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!IsInitialized && sw.ElapsedMilliseconds < timeoutMs)
                await Task.Delay(16);

            if (IsInitialized)
                return true;

            return TryBootstrapFromResources();
        }

        /// <summary>
        /// 동기 API에서만 사용. <see cref="EnsureInitializedAsync"/> 없이 호출할 때 Resources에 설정이 있으면 초기화합니다.
        /// </summary>
        private static void EnsureInitializedOrBootstrapSync()
        {
            if (IsInitialized)
                return;
            TryBootstrapFromResources();
        }

        /// <summary>
        /// <c>Resources/SupabaseSettings</c>를 로드해 <see cref="SupabaseUnityBootstrap.Initialize"/>를 호출합니다.
        /// 씬에 <c>SupabaseRuntime</c>이 없을 때의 보조 경로입니다.
        /// </summary>
        private static bool TryBootstrapFromResources()
        {
            if (IsInitialized)
                return true;

            // 씬에 Runtime이 없어도 동작하도록 Resources의 기본 설정으로 직접 부트스트랩합니다.
            var settings = Resources.Load<SupabaseSettings>("SupabaseSettings");
            if (settings == null)
            {
                Debug.LogWarning(
                    "[Supabase] SupabaseSettings를 Resources에서 찾을 수 없습니다. "
                    + "씬에 SupabaseRuntime을 두거나 Resources/SupabaseSettings 에셋을 추가하세요.");
                return false;
            }

            var bootstrap = new SupabaseUnityBootstrap();
            bootstrap.Initialize(settings);
            return IsInitialized;
        }

        /// <summary>인증 서비스. 가능하면 Resources 부트스트랩 후 반환합니다.</summary>
        public static SupabaseAuthService Auth
        {
            get
            {
                EnsureInitializedOrBootstrapSync();
                return _bootstrap?.AuthService;
            }
        }

        /// <summary>이미 가진 Google ID 토큰 문자열로 Supabase에 로그인하고 SDK 세션을 맞춥니다.</summary>
        /// <remarks>
        /// Android 네이티브 <see cref="SignInWithGoogleAsync(bool)"/>와 달리 «토큰 획득»은 호출자 책임입니다(iOS 플러그인, 웹 OAuth, 수동 입력 등).
        /// 익명(게스트) 세션이 있으면 게스트→구글 연동을 위해 identity link를 먼저 시도합니다.
        /// </remarks>
        public static async Task<SupabaseResult<SupabaseSession>> SignInWithGoogleIdTokenAsync(string idToken, bool saveSessionToStorage = true)
        {
            if (!await EnsureInitializedAsync())
                return SupabaseResult<SupabaseSession>.Fail("sdk_not_initialized");

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
                SetSession(result.Data, SupabaseSessionChangeKind.NewSignIn);
                if (saveSessionToStorage)
                    SaveSessionToStorage();
            }

            return result;
        }

        /// <summary>
        /// <c>Resources/SupabaseSettings</c>에 입력한 <c>googleWebClientId</c>로 Android 네이티브 Google 로그인을 수행합니다.
        /// </summary>
        /// <remarks>
        /// 인스펙터에 Web Client ID를 저장해 두고, 게임 코드에서는 인자 없이 호출할 때 쓰는 오버로드입니다.
        /// </remarks>
        public static async Task<SupabaseResult<SupabaseSession>> SignInWithGoogleAsync(bool saveSessionToStorage = true)
        {
            var webClientId = TryGetGoogleWebClientIdFromSettings();
            if (string.IsNullOrWhiteSpace(webClientId))
                return SupabaseResult<SupabaseSession>.Fail("google_web_client_id_empty");

            if (!await EnsureInitializedAsync())
                return SupabaseResult<SupabaseSession>.Fail("sdk_not_initialized");

            if (Auth == null)
                return SupabaseResult<SupabaseSession>.Fail("sdk_not_initialized");

            var bridge = EnsureGoogleLoginBridge();
            var provider = new AndroidGoogleLoginProvider(bridge, webClientId.Trim());
            var googleAuth = new SupabaseGoogleAuthService(provider, Auth, () => _currentSession);

            var tcs = new TaskCompletionSource<SupabaseResult<SupabaseSession>>(TaskCreationOptions.RunContinuationsAsynchronously);

            googleAuth.SignInWithGoogle(
                session =>
                {
                    if (session == null)
                    {
                        tcs.TrySetResult(SupabaseResult<SupabaseSession>.Fail("supabase_session_null"));
                        return;
                    }

                    SetSession(session, SupabaseSessionChangeKind.NewSignIn);
                    if (saveSessionToStorage)
                        SaveSessionToStorage();

                    tcs.TrySetResult(SupabaseResult<SupabaseSession>.Success(session));
                },
                err => tcs.TrySetResult(
                    SupabaseResult<SupabaseSession>.Fail(string.IsNullOrWhiteSpace(err) ? "google_signin_failed" : err)));

            return await tcs.Task;
        }

        /// <summary><see cref="SignInWithGoogleAsync(bool)"/>를 bool 기반으로 호출합니다.</summary>
        public static async Task<bool> TrySignInWithGoogleAsync(bool saveSessionToStorage = true)
        {
            var r = await SignInWithGoogleAsync(saveSessionToStorage);
            return LogAndReturn(ApiLogTags.AuthGoogleSettings, r);
        }

        /// <summary><see cref="SignInWithGoogleIdTokenAsync"/>를 bool 기반으로 호출합니다.</summary>
        public static async Task<bool> TrySignInWithGoogleIdTokenAsync(string idToken, bool saveSessionToStorage = true)
        {
            var r = await SignInWithGoogleIdTokenAsync(idToken, saveSessionToStorage);
            return LogAndReturn(ApiLogTags.AuthGoogleIdToken, r);
        }

        /// <summary><see cref="SignInAnonymouslyAsync"/>를 bool 기반으로 호출합니다.</summary>
        public static async Task<bool> TrySignInAnonymouslyAsync(bool saveSessionToStorage = true)
        {
            var r = await SignInAnonymouslyAsync(saveSessionToStorage);
            return LogAndReturn(ApiLogTags.AuthAnonymous, r);
        }

        /// <summary><see cref="SignOutFromGoogleAsync"/>를 bool 기반으로 호출합니다.</summary>
        public static async Task<bool> TrySignOutFromGoogleAsync()
        {
            var r = await SignOutFromGoogleAsync();
            return LogAndReturn(ApiLogTags.AuthGoogleSignOut, r);
        }

        /// <summary><see cref="StartAsync"/>를 bool 기반으로 호출합니다.</summary>
        public static async Task<bool> TryStartAsync(
            bool restoreSessionFirst = true,
            bool refreshRemoteConfigOnStart = false)
        {
            var ok = await StartAsync(restoreSessionFirst, refreshRemoteConfigOnStart);
            LogApiResult(ApiLogTags.BootStart, ok, ok ? null : "start_failed");
            return ok;
        }

        /// <summary><see cref="RefreshSessionAsync"/>를 bool 기반으로 호출합니다.</summary>
        public static async Task<bool> TryRefreshSessionAsync(string refreshToken, bool saveSessionToStorage = true)
        {
            var r = await RefreshSessionAsync(refreshToken, saveSessionToStorage);
            return LogAndReturn(ApiLogTags.AuthRefreshSession, r);
        }

        /// <summary><see cref="SaveUserDataAsync{T}(T)"/>를 bool 기반으로 호출합니다.</summary>
        public static async Task<bool> TrySaveUserDataAsync<T>(T data)
        {
            var r = await SaveUserDataAsync(data);
            return LogAndReturn(ApiLogTags.UserDataSave, r);
        }

        /// <summary><see cref="LoadUserDataAsync{T}()"/>를 호출하고 성공 시 데이터를 반환, 실패 시 default를 반환합니다.</summary>
        public static async Task<T> TryLoadUserDataAsync<T>(T defaultValue = default) where T : class, new()
        {
            var r = await LoadUserDataAsync<T>();
            return LogAndReturnData(ApiLogTags.UserDataLoad, r, defaultValue);
        }

        /// <summary><see cref="InvokeFunctionAsync{TResponse}(string, object)"/>를 호출하고 성공 시 데이터를 반환, 실패 시 default를 반환합니다.</summary>
        public static async Task<TResponse> TryInvokeFunctionAsync<TResponse>(
            string functionName,
            object requestBody = null,
            TResponse defaultValue = default)
        {
            var r = await InvokeFunctionAsync<TResponse>(functionName, requestBody);
            return LogAndReturnData(ApiLogTags.EdgeFunctionInvoke, r, defaultValue);
        }

        /// <summary><see cref="RefreshRemoteConfigAsync"/>를 bool 기반으로 호출합니다.</summary>
        public static async Task<bool> TryRefreshRemoteConfigAsync()
        {
            var ok = await RefreshRemoteConfigAsync();
            if (ok == false)
            {
                LogApiResult(ApiLogTags.RemoteConfigRefresh, ok, "remote_config_refresh_failed");
                return false;
            }

            // 변경이 없으면 로그를 찍지 않습니다(폴링 성공 로그 스팸 방지).
            if (_remoteConfig != null && _remoteConfig.LastApplyHadChanges)
                LogApiResult(ApiLogTags.RemoteConfigRefresh, true, null);

            return ok;
        }

        /// <summary><see cref="PollRemoteConfigAsync"/>를 bool 기반으로 호출합니다.</summary>
        public static async Task<bool> TryPollRemoteConfigAsync()
        {
            var ok = await PollRemoteConfigAsync();
            if (ok == false)
            {
                LogApiResult(ApiLogTags.RemoteConfigPoll, ok, "remote_config_poll_failed");
                return false;
            }

            // 실제 변경이 있을 때만 success 로그를 찍습니다.
            if (_remoteConfig != null && _remoteConfig.LastApplyHadChanges)
                LogApiResult(ApiLogTags.RemoteConfigPoll, true, null);

            return ok;
        }

        /// <summary>
        /// 온디맨드 RemoteConfig 동기화: 서버에서 즉시 최신 값을 받아 캐시를 갱신합니다.
        /// 호출 직후에는 주기 폴링 타이머를 초기화(의도치 않게 더 자주 호출되지 않도록 다음 폴링을 뒤로 미룸)합니다.
        /// </summary>
        public static async Task<bool> RefreshRemoteConfigOnDemandAsync()
        {
            var ok = await TryRefreshRemoteConfigAsync();
            if (ok)
                RequestRemoteConfigPollingReset();
            return ok;
        }

        /// <summary><see cref="GetRemoteConfigAsync{T}"/>를 호출하고 반환값 유무를 로그로 남깁니다.</summary>
        public static async Task<T> TryGetRemoteConfigAsync<T>(string key, T defaultValue = default, bool pollOnly = false)
        {
            var value = await GetRemoteConfigAsync(key, defaultValue, pollOnly);
            var hasValue = EqualityComparer<T>.Default.Equals(value, defaultValue) == false;
            LogApiResult(ApiLogTags.RemoteConfigGet, hasValue, hasValue ? null : "remote_config_default_returned");
            return value;
        }

        /// <summary><see cref="SendChatMessageAsync(string, string, string)"/>를 bool 기반으로 호출합니다.</summary>
        public static async Task<bool> TrySendChatMessageAsync(
            string channelId,
            string content,
            string displayName = null)
        {
            var ok = await SendChatMessageAsync(channelId, content, displayName);
            LogApiResult(ApiLogTags.ChatSend, ok, ok ? null : "chat_send_failed");
            return ok;
        }

        /// <summary><see cref="JoinChatChannelAsync"/>를 호출하고 채널 join 성공 여부를 로그로 남깁니다.</summary>
        public static async Task<ChatChannelFacade> TryJoinChatChannelAsync(
            string channelId,
            MonoBehaviour pollHost,
            Action<SupabaseChatService.ChatMessageRow> onMessageReceived,
            float pollIntervalSeconds = 1.5f,
            bool loadHistory = true,
            int historyCount = 50)
        {
            var channel = await JoinChatChannelAsync(channelId, pollHost, onMessageReceived, pollIntervalSeconds, loadHistory, historyCount);
            LogApiResult(ApiLogTags.ChatJoin, channel != null, channel != null ? null : "chat_join_failed");
            return channel;
        }

        /// <summary><see cref="RestoreSessionAsync"/>를 bool 기반으로 호출합니다.</summary>
        public static async Task<bool> TryRestoreSessionAsync()
        {
            var ok = await RestoreSessionAsync();
            LogApiResult(ApiLogTags.AuthRestoreSession, ok, ok ? null : "restore_session_failed");
            return ok;
        }

        /// <summary>
        /// Postgres RPC <c>ts_server_now</c>로 서버 시각(UTC)을 가져옵니다. 로그인 세션 없이 anon 키로 호출합니다.
        /// </summary>
        public static async Task<SupabaseResult<DateTime>> GetServerUtcNowAsync()
        {
            if (!await EnsureInitializedAsync())
                return SupabaseResult<DateTime>.Fail("sdk_not_initialized");

            var svc = ServerTimeService;
            if (svc == null)
                return SupabaseResult<DateTime>.Fail("sdk_not_initialized");

            return await svc.GetServerUtcNowAsync();
        }

        /// <summary><see cref="GetServerUtcNowAsync"/>를 값 기반으로 호출합니다.</summary>
        public static async Task<DateTime> TryGetServerUtcNowAsync(DateTime defaultValue = default)
        {
            var r = await GetServerUtcNowAsync();
            return LogAndReturnData(ApiLogTags.ServerTime, r, defaultValue);
        }

        private static bool LogAndReturn<T>(string logTag, SupabaseResult<T> result)
        {
            var ok = result != null && result.IsSuccess;
            LogApiResult(logTag, ok, ok ? null : result?.ErrorMessage);
            return ok;
        }

        private static T LogAndReturnData<T>(string logTag, SupabaseResult<T> result, T defaultValue)
        {
            var ok = result != null && result.IsSuccess;
            LogApiResult(logTag, ok, ok ? null : result?.ErrorMessage);
            return ok ? result.Data : defaultValue;
        }

        private static void LogApiResult(string logTag, bool isSuccess, string message = null)
        {
            if (!_enableApiResultLogs)
                return;

            var prefix = $"[{logTag}]";
            if (isSuccess)
            {
                Debug.Log($"{prefix} success");
                return;
            }

            var detail = string.IsNullOrWhiteSpace(message) ? "unknown_error" : message.Trim();
            Debug.LogError($"{prefix} failed: {detail}");
        }

        /// <summary>
        /// Android 네이티브 Google 계정에서 로그아웃합니다. (Supabase 세션은 그대로이므로 필요하면 <see cref="ClearSession"/> 호출)
        /// </summary>
        public static async Task<SupabaseResult<bool>> SignOutFromGoogleAsync()
        {
            if (!await EnsureInitializedAsync())
                return SupabaseResult<bool>.Fail("sdk_not_initialized");

            var bridge = EnsureGoogleLoginBridge();
            var tcs = new TaskCompletionSource<SupabaseResult<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);

            bridge.SignOut(
                () => tcs.TrySetResult(SupabaseResult<bool>.Success(true)),
                err => tcs.TrySetResult(
                    SupabaseResult<bool>.Fail(string.IsNullOrWhiteSpace(err) ? "google_signout_failed" : err)));

            return await tcs.Task;
        }

        /// <summary>게스트(익명)로 로그인하고 SDK 세션을 자동 설정합니다.</summary>
        /// <remarks>저장된 refresh_token이 있으면 먼저 <see cref="RestoreSessionAsync"/>를 시도해 동일 계정을 이어갑니다(<see cref="Config.SupabaseRuntime"/>의 Restore Session On Start가 꺼져 있어도 동일).</remarks>
        public static async Task<SupabaseResult<SupabaseSession>> SignInAnonymouslyAsync(bool saveSessionToStorage = true)
        {
            if (!await EnsureInitializedAsync())
                return SupabaseResult<SupabaseSession>.Fail("sdk_not_initialized");

            // RestoreSessionOnStart가 꺼져 있어도, 저장된 refresh_token이 있으면 동일 계정을 이어갑니다.
            if (!IsLoggedIn)
                await RestoreSessionAsync();

            // 로컬 토큰이 사라진(재설치/로그아웃) 경우를 위해 서버의 best-effort 복구 토큰을 1회 시도합니다.
            if (!IsLoggedIn)
                _ = await TryRestoreSessionFromAnonymousRecoveryAsync();

            if (IsLoggedIn)
            {
                if (saveSessionToStorage)
                    SaveSessionToStorage();
                return SupabaseResult<SupabaseSession>.Success(_currentSession);
            }

            if (Auth == null)
                return SupabaseResult<SupabaseSession>.Fail("sdk_not_initialized");

            var result = await Auth.SignInAnonymouslyAsync();

            if (result.IsSuccess && result.Data != null)
            {
                SetSession(result.Data, SupabaseSessionChangeKind.NewSignIn);
                if (saveSessionToStorage)
                    SaveSessionToStorage();

                await TryUpsertAnonymousRecoveryTokenAsync(result.Data);
            }

            return result;
        }

        /// <summary>
        /// 초기화 후 로그인 세션이 준비되었는지 확인합니다.
        /// 미로그인 상태면 실패를 반환하며, 자동 익명 로그인은 수행하지 않습니다.
        /// </summary>
        public static async Task<SupabaseResult<SupabaseSession>> EnsureReadySessionAsync()
        {
            if (!await EnsureInitializedAsync())
                return SupabaseResult<SupabaseSession>.Fail("sdk_not_initialized");

            // 이미 로그인되어 있으면 재로그인 없이 현재 세션을 그대로 사용합니다.
            if (IsLoggedIn)
            {
                var singleSessionOk = await SupabaseDuplicateSessionCoordinator.VerifyCurrentSessionForActionAsync();
                if (singleSessionOk == false)
                    return SupabaseResult<SupabaseSession>.Fail("duplicate_login_detected");

                return SupabaseResult<SupabaseSession>.Success(_currentSession);
            }

            return SupabaseResult<SupabaseSession>.Fail("auth_not_signed_in");
        }

        /// <summary>
        /// 앱 시작 시 자주 필요한 준비를 한 번에 수행합니다.
        /// 초기화 → (선택) 저장 세션 복원 → (선택) RemoteConfig 새로고침.
        /// </remarks>
        public static async Task<bool> StartAsync(
            bool restoreSessionFirst = true,
            bool refreshRemoteConfigOnStart = false)
        {
            if (!await EnsureInitializedAsync())
                return false;

            if (restoreSessionFirst && !IsLoggedIn)
                _ = await RestoreSessionAsync();

            if (refreshRemoteConfigOnStart)
                _ = await RefreshRemoteConfigAsync();

            return true;
        }

        /// <summary>refresh_token 문자열로 세션을 갱신하고 SDK에 반영합니다(직접 보유한 토큰용).</summary>
        /// <remarks>
        /// 앱 재실행 시 저장된 토큰 복원은 <see cref="RestoreSessionAsync"/>를 쓰는 편이 일반적입니다.
        /// </remarks>
        public static async Task<SupabaseResult<SupabaseSession>> RefreshSessionAsync(string refreshToken, bool saveSessionToStorage = true)
        {
            if (!await EnsureInitializedAsync())
                return SupabaseResult<SupabaseSession>.Fail("sdk_not_initialized");

            if (Auth == null)
                return SupabaseResult<SupabaseSession>.Fail("sdk_not_initialized");

            var result = await Auth.RefreshSessionAsync(refreshToken);
            if (result.IsSuccess && result.Data != null)
            {
                SetSession(result.Data, SupabaseSessionChangeKind.RestoredOrRefreshed);
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
                EnsureInitializedOrBootstrapSync();
                if (_bootstrap == null)
                    throw new InvalidOperationException("SupabaseSDK is not initialized. Call SupabaseUnityBootstrap.Initialize first.");

                return _userSaves ??= new UserSavesFacade(_bootstrap.UserDataService, () => _currentSession);
            }
        }

        /// <summary>현재 세션으로 유저 데이터 저장.</summary>
        public static async Task<SupabaseResult<bool>> SaveUserDataAsync<T>(T data)
        {
            var ready = await EnsureReadySessionAsync();
            if (!ready.IsSuccess)
                return SupabaseResult<bool>.Fail(ready.ErrorMessage ?? "auth_not_signed_in");

            return await UserSaves.SaveAsync(data);
        }

        /// <summary>현재 세션으로 유저 데이터 로드.</summary>
        public static async Task<SupabaseResult<T>> LoadUserDataAsync<T>() where T : class, new()
        {
            var ready = await EnsureReadySessionAsync();
            if (!ready.IsSuccess)
                return SupabaseResult<T>.Fail(ready.ErrorMessage ?? "auth_not_signed_in");

            return await UserSaves.LoadAsync<T>();
        }

        /// <summary>
        /// 로그인 없이 다른 사용자의 공개 닉네임을 조회합니다. <paramref name="userId"/>는 DB <c>profiles.user_id</c>(OAuth <c>sub</c> 등 안정 id)입니다. 테이블 RLS에서 anon <c>SELECT</c>가 허용되어야 합니다.
        /// </summary>
        public static async Task<SupabaseResult<string>> GetPublicNicknameAsync(string userId)
        {
            if (!await EnsureInitializedAsync())
                return SupabaseResult<string>.Fail("sdk_not_initialized");

            if (_bootstrap?.PublicProfileService == null)
                return SupabaseResult<string>.Fail("sdk_not_initialized");

            return await _bootstrap.PublicProfileService.GetNicknameAsync(userId);
        }

        /// <summary>현재 로그인 사용자의 공개 닉네임을 upsert합니다.</summary>
        public static async Task<SupabaseResult<bool>> UpsertMyNicknameAsync(string nickname)
        {
            var ready = await EnsureReadySessionAsync();
            if (!ready.IsSuccess)
                return SupabaseResult<bool>.Fail(ready.ErrorMessage ?? "auth_not_signed_in");

            if (_bootstrap?.PublicProfileService == null)
                return SupabaseResult<bool>.Fail("sdk_not_initialized");

            return await _bootstrap.PublicProfileService.UpsertMyNicknameAsync(
                _currentSession.AccessToken,
                _currentSession.User.Id,
                _currentSession.User.PlayerUserId,
                nickname);
        }

        /// <inheritdoc cref="GetPublicNicknameAsync"/>
        public static async Task<string> TryGetPublicNicknameAsync(string userId, string defaultValue = "")
        {
            var r = await GetPublicNicknameAsync(userId);
            return LogAndReturnData(ApiLogTags.ProfilePublicNicknameGet, r, defaultValue);
        }

        /// <inheritdoc cref="UpsertMyNicknameAsync"/>
        public static async Task<bool> TrySetMyNicknameAsync(string nickname)
        {
            var r = await UpsertMyNicknameAsync(nickname);
            return LogAndReturn(ApiLogTags.ProfileMyNicknameSet, r);
        }

        /// <summary>
        /// 닉네임이 사용 가능한지 조회합니다. 로그인 후 본인 닉을 유지한 채 검사할 때는 <paramref name="ignoreUserIdForSelf"/>에 현재 Auth 사용자 id(<c>auth.uid()</c>, <c>profiles.account_id</c>)를 넘깁니다.
        /// </summary>
        public static async Task<SupabaseResult<bool>> IsNicknameAvailableAsync(
            string nickname,
            string ignoreUserIdForSelf = null)
        {
            if (!await EnsureInitializedAsync())
                return SupabaseResult<bool>.Fail("sdk_not_initialized");

            if (_bootstrap?.PublicProfileService == null)
                return SupabaseResult<bool>.Fail("sdk_not_initialized");

            return await _bootstrap.PublicProfileService.IsNicknameAvailableAsync(nickname, ignoreUserIdForSelf);
        }

        /// <inheritdoc cref="IsNicknameAvailableAsync"/>
        public static async Task<bool> TryIsNicknameAvailableAsync(string nickname, string ignoreUserIdForSelf = null)
        {
            var r = await IsNicknameAvailableAsync(nickname, ignoreUserIdForSelf);
            if (r == null || !r.IsSuccess)
            {
                LogApiResult(ApiLogTags.ProfileNicknameAvailable, false, r?.ErrorMessage ?? "unknown");
                return false;
            }

            if (!r.Data)
                LogApiResult(ApiLogTags.ProfileNicknameAvailable, false, "nickname_taken");
            else
                LogApiResult(ApiLogTags.ProfileNicknameAvailable, true, null);

            return r.Data;
        }

        /// <summary>공개 프로필(닉네임·탈퇴 시각)을 한 번에 조회합니다. <paramref name="userId"/>는 <c>profiles.user_id</c>(안정 플레이어 id)입니다.</summary>
        public static async Task<SupabaseResult<PublicProfileSnapshot>> GetPublicProfileAsync(string userId)
        {
            if (!await EnsureInitializedAsync())
                return SupabaseResult<PublicProfileSnapshot>.Fail("sdk_not_initialized");

            if (_bootstrap?.PublicProfileService == null)
                return SupabaseResult<PublicProfileSnapshot>.Fail("sdk_not_initialized");

            return await _bootstrap.PublicProfileService.GetProfileAsync(userId);
        }

        /// <inheritdoc cref="GetPublicProfileAsync"/>
        public static async Task<PublicProfileSnapshot> TryGetPublicProfileAsync(string userId)
        {
            var r = await GetPublicProfileAsync(userId);
            if (r == null || !r.IsSuccess)
            {
                LogApiResult(ApiLogTags.ProfileSnapshotGet, false, r?.ErrorMessage ?? "unknown");
                return null;
            }

            LogApiResult(ApiLogTags.ProfileSnapshotGet, true, null);
            return r.Data;
        }

        /// <summary>본인 <c>withdrawn_at</c>을 ISO 8601로 설정합니다(soft 탈퇴 표시).</summary>
        public static async Task<SupabaseResult<bool>> SetMyWithdrawnAtAsync(string withdrawnAtIsoUtc)
        {
            var ready = await EnsureReadySessionAsync();
            if (!ready.IsSuccess)
                return SupabaseResult<bool>.Fail(ready.ErrorMessage ?? "auth_not_signed_in");

            if (_bootstrap?.PublicProfileService == null)
                return SupabaseResult<bool>.Fail("sdk_not_initialized");

            if (string.IsNullOrWhiteSpace(withdrawnAtIsoUtc))
                return SupabaseResult<bool>.Fail("withdrawn_at_empty");

            return await _bootstrap.PublicProfileService.PatchMyWithdrawnAtAsync(
                _currentSession.AccessToken,
                _currentSession.User.Id,
                withdrawnAtIsoUtc);
        }

        /// <summary>본인 <c>withdrawn_at</c>을 비웁니다(SQL NULL).</summary>
        public static async Task<SupabaseResult<bool>> ClearMyWithdrawalAsync()
        {
            var ready = await EnsureReadySessionAsync();
            if (!ready.IsSuccess)
                return SupabaseResult<bool>.Fail(ready.ErrorMessage ?? "auth_not_signed_in");

            if (_bootstrap?.PublicProfileService == null)
                return SupabaseResult<bool>.Fail("sdk_not_initialized");

            return await _bootstrap.PublicProfileService.PatchMyWithdrawnAtAsync(
                _currentSession.AccessToken,
                _currentSession.User.Id,
                withdrawnAtIso: null);
        }

        /// <summary>현재 시각(UTC)으로 soft 탈퇴 시각을 기록합니다.</summary>
        public static Task<SupabaseResult<bool>> MarkMyWithdrawnAsync() =>
            SetMyWithdrawnAtAsync(DateTime.UtcNow.ToString("o"));

        /// <inheritdoc cref="MarkMyWithdrawnAsync"/>
        public static async Task<bool> TryMarkMyWithdrawnAsync()
        {
            var r = await MarkMyWithdrawnAsync();
            return LogAndReturn(ApiLogTags.ProfileWithdrawnAt, r);
        }

        /// <inheritdoc cref="ClearMyWithdrawalAsync"/>
        public static async Task<bool> TryClearMyWithdrawalAsync()
        {
            var r = await ClearMyWithdrawalAsync();
            return LogAndReturn(ApiLogTags.ProfileWithdrawnAt, r);
        }

        /// <inheritdoc cref="SetMyWithdrawnAtAsync"/>
        public static async Task<bool> TrySetMyWithdrawnAtAsync(string withdrawnAtIsoUtc)
        {
            var r = await SetMyWithdrawnAtAsync(withdrawnAtIsoUtc);
            return LogAndReturn(ApiLogTags.ProfileWithdrawnAt, r);
        }

        /// <summary>Remote Config 캐시·구독·서버 동기화 퍼사드.</summary>
        /// <remarks>
        /// 서버 요청 시 액세스 토큰이 있으면 전달됩니다(정책에 따라 익명/로그인 모두 가능한 경우가 많음).
        /// </remarks>
        public static RemoteConfigFacade RemoteConfig
        {
            get
            {
                EnsureInitializedOrBootstrapSync();
                if (_bootstrap == null)
                    throw new InvalidOperationException("SupabaseSDK is not initialized. Call SupabaseUnityBootstrap.Initialize first.");

                return _remoteConfig ??= new RemoteConfigFacade(
                    _bootstrap.RemoteConfigService,
                    () => _currentSession?.AccessToken);
            }
        }

        /// <summary>Remote Config 전체를 서버에서 다시 받아 캐시를 갱신합니다.</summary>
        public static async Task<bool> RefreshRemoteConfigAsync()
        {
            if (!await EnsureInitializedAsync())
                return false;

            return await RemoteConfig.RefreshAllAsync();
        }

        /// <summary>마지막 동기 시각 이후 변경분만 가져와 캐시에 머지합니다(주기적 폴링용).</summary>
        public static async Task<bool> PollRemoteConfigAsync()
        {
            if (!await EnsureInitializedAsync())
                return false;

            return await RemoteConfig.PollAsync();
        }

        /// <summary>로컬 캐시에서 key에 해당하는 값을 읽습니다(네트워크 호출 없음).</summary>
        /// <remarks>
        /// 최신 값이 필요하면 먼저 <see cref="RefreshRemoteConfigAsync"/> 또는 <see cref="GetRemoteConfigAsync{T}"/>를 호출합니다.
        /// </remarks>
        public static T GetRemoteConfig<T>(string key, T defaultValue = default)
        {
            return RemoteConfig.Get(key, defaultValue);
        }

        /// <summary>
        /// Remote Config를 한 번 서버와 맞춘 뒤 특정 key 값을 반환합니다(원라인 조회).
        /// 기본은 전체 새로고침이며, pollOnly=true면 변경분 폴링 후 조회합니다.
        /// </summary>
        public static async Task<T> GetRemoteConfigAsync<T>(string key, T defaultValue = default, bool pollOnly = false)
        {
            if (!await EnsureInitializedAsync())
                return defaultValue;

            if (pollOnly)
                _ = await PollRemoteConfigAsync();
            else
                _ = await RefreshRemoteConfigAsync();

            return GetRemoteConfig(key, defaultValue);
        }

        public static bool TryGetRemoteConfigRaw(string key, out string valueJson)
        {
            return RemoteConfig.TryGetRaw(key, out valueJson);
        }

        /// <summary>특정 key 값이 바뀔 때마다 콜백을 호출합니다(캐시에 객체 루트 JSON이 있을 때).</summary>
        public static void SubscribeRemoteConfig(string key, Action<string> onValueChanged, bool invokeIfCached = true)
        {
            RemoteConfig.Subscribe(key, onValueChanged, invokeIfCached);
        }

        /// <summary><see cref="SubscribeRemoteConfig"/>에서 등록한 콜백을 제거합니다.</summary>
        public static void UnsubscribeRemoteConfig(string key, Action<string> onValueChanged)
        {
            RemoteConfig.Unsubscribe(key, onValueChanged);
        }

        /// <summary>Supabase Edge Functions 호출 퍼사드(로그인 세션의 액세스 토큰 사용).</summary>
        public static ServerFunctionsFacade Functions
        {
            get
            {
                EnsureInitializedOrBootstrapSync();
                if (_bootstrap == null)
                    throw new InvalidOperationException("SupabaseSDK is not initialized. Call SupabaseUnityBootstrap.Initialize first.");

                return _functions ??= new ServerFunctionsFacade(
                    _bootstrap.EdgeFunctionsService,
                    () => _currentSession);
            }
        }

        /// <summary>채널 ID당 하나의 <see cref="ChatChannelFacade"/>를 만들거나 캐시에서 꺼냅니다.</summary>
        /// <remarks>
        /// 로그인 세션이 있어야 전송·히스토리 등이 동작합니다. 미로그인 자동 처리는 <see cref="JoinChatChannelAsync"/> 등을 사용합니다.
        /// </remarks>
        public static ChatChannelFacade OpenChatChannel(string channelId, string displayName = null)
        {
            EnsureInitializedOrBootstrapSync();
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
        public static async Task<bool> SendChatMessageAsync(string channelId, string content, string displayName = null)
        {
            // 채팅 전송도 동일하게 세션이 준비되어 있어야 합니다.
            var ready = await EnsureReadySessionAsync();
            if (!ready.IsSuccess)
                return false;

            var channel = OpenChatChannel(channelId, displayName);
            return await channel.SendAsync(content);
        }

        /// <summary>채널이 현재 SDK 캐시에 열려 있는지 확인.</summary>
        public static bool IsChatChannelOpen(string channelId)
        {
            return GetChatChannel(channelId) != null;
        }

        /// <summary>서버 함수 호출(로그인 세션 필요).</summary>
        public static async Task<SupabaseResult<TResponse>> InvokeFunctionAsync<TResponse>(string functionName, object requestBody = null)
        {
            return await Functions.InvokeAsync<TResponse>(functionName, requestBody, requireAuth: true);
        }

        /// <summary>
        /// 채팅 채널에 입장 + 수신 핸들러 연결 + 코루틴 폴링까지 한 번에 수행합니다(동기 진입, 세션은 미리 있어야 할 수 있음).
        /// </summary>
        /// <remarks>
        /// 로그인 보장이 필요하면 <see cref="JoinChatChannelAsync"/>를 사용합니다.
        /// </remarks>
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
        /// 채팅 채널 join + 이벤트 구독 + 폴링 시작.
        /// 한 줄 사용을 위한 비동기 진입점입니다.
        /// </summary>
        public static async Task<ChatChannelFacade> JoinChatChannelAsync(
            string channelId,
            MonoBehaviour pollHost,
            Action<SupabaseChatService.ChatMessageRow> onMessageReceived,
            float pollIntervalSeconds = 1.5f,
            bool loadHistory = true,
            int historyCount = 50)
        {
            // Join + Polling 시작까지 한 번에 쓰되, 로그인 상태를 먼저 확인합니다.
            var ready = await EnsureReadySessionAsync();
            if (!ready.IsSuccess)
                return null;

            return JoinChatChannel(
                channelId,
                pollHost,
                onMessageReceived,
                pollIntervalSeconds,
                loadHistory,
                historyCount);
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
            SetSession(session, SupabaseSessionChangeKind.RestoredOrRefreshed);
        }

        /// <summary>
        /// 세션을 설정합니다. <paramref name="kind"/>가 <see cref="SupabaseSessionChangeKind.NewSignIn"/>이면 서버에 새 세션 토큰을 등록합니다(중복 로그인 감지).
        /// </summary>
        public static void SetSession(SupabaseSession session, SupabaseSessionChangeKind kind)
        {
            _currentSession = session;
            if (session == null || session.User == null || string.IsNullOrWhiteSpace(session.User.Id))
                return;

            SupabaseDuplicateSessionCoordinator.ScheduleSyncAfterSessionChange(kind);
        }

        /// <summary>로그아웃 시 호출. clearStorage가 true면 PlayerPrefs에 저장된 refresh_token도 삭제합니다.</summary>
        /// <param name="clearStorage">저장된 refresh_token 삭제 여부.</param>
        /// <param name="deleteUserSessionRow">true면 <c>user_sessions</c>에서 본인 행을 삭제합니다. 다른 기기에 의해 세션이 무효화된 경우 false로 두세요.</param>
        public static void ClearSession(bool clearStorage = true, bool deleteUserSessionRow = true)
        {
            var accessToken = _currentSession?.AccessToken;
            var accountId = _currentSession?.User?.Id;

            SupabaseDuplicateSessionCoordinator.StopPolling();

            // 채널 상태는 세션이 끊기면 더 이상 의미가 없으므로 정리
            foreach (var pair in _chatChannels)
            {
                pair.Value?.StopPolling();
            }
            _chatChannels.Clear();

            _currentSession = null;
            if (clearStorage)
                PlayerPrefs.DeleteKey(RefreshTokenKey);

            if (string.IsNullOrWhiteSpace(accountId) == false)
                PlayerPrefs.DeleteKey(SessionTokenPlayerPrefsKeyPrefix + accountId);

            PlayerPrefs.Save();

            if (deleteUserSessionRow
                && string.IsNullOrWhiteSpace(accessToken) == false
                && string.IsNullOrWhiteSpace(accountId) == false)
            {
                var svc = _bootstrap?.UserSessionService;
                if (svc != null)
                    _ = svc.DeleteMySessionRowAsync(accessToken, accountId);
            }
        }

        /// <summary>내부: 다른 기기 로그인으로 서버 토큰이 바뀐 경우 세션을 끊고 이벤트를 올립니다.</summary>
        internal static void RaiseDuplicateLoginDetected()
        {
            ClearSession(clearStorage: true, deleteUserSessionRow: false);
            OnDuplicateLoginDetected?.Invoke();
        }

        /// <summary>현재 세션의 refresh_token을 PlayerPrefs에 저장. 앱 재시작 후 RestoreSessionAsync로 복원할 수 있습니다.</summary>
        public static void SaveSessionToStorage()
        {
            if (_currentSession == null || string.IsNullOrWhiteSpace(_currentSession.RefreshToken))
                return;
            PlayerPrefs.SetString(RefreshTokenKey, _currentSession.RefreshToken);
            PlayerPrefs.Save();
        }

        /// <summary>PlayerPrefs에 저장된 refresh_token으로 세션을 복원합니다.</summary>
        /// <remarks>
        /// <see cref="SupabaseRuntime"/>의 «Restore Session On Start» 또는 로그인 화면에서 호출합니다. <see cref="StartAsync"/>의 첫 단계와 동일한 복원입니다.
        /// </remarks>
        public static async Task<bool> RestoreSessionAsync()
        {
            if (!await EnsureInitializedAsync())
                return false;

            if (_bootstrap?.AuthService == null)
                return false;

            var refreshToken = PlayerPrefs.GetString(RefreshTokenKey, null);

            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var result = await _bootstrap.AuthService.RefreshSessionAsync(refreshToken);
            if (result.IsSuccess && result.Data != null)
            {
                SetSession(result.Data, SupabaseSessionChangeKind.RestoredOrRefreshed);
                return true;
            }

            PlayerPrefs.DeleteKey(RefreshTokenKey);
            return false;
        }

        /// <summary><see cref="SupabaseUnityBootstrap.Initialize"/>에서 호출합니다. 서비스 인스턴스를 묶고 캐시를 초기화합니다.</summary>
        /// <remarks>
        /// 동일 프로젝트 URL로 재초기화되고 이미 로그인 중이면 세션을 유지합니다(Resources 부트스트랩 후 Runtime Awake 등).
        /// </remarks>
        public static void Initialize(SupabaseUnityBootstrap bootstrap)
        {
            _ = bootstrap ?? throw new ArgumentNullException(nameof(bootstrap));

            var newUrl = bootstrap.ProjectUrl ?? string.Empty;

            var sameProject = _bootstrap != null
                && _initializedProjectUrl != null
                && string.Equals(_initializedProjectUrl, newUrl, StringComparison.OrdinalIgnoreCase);

            // 동일 프로젝트 재초기화(예: Resources 부트스트랩 후 Runtime Awake)면 로그인 세션을 유지합니다.
            var preserveSession = sameProject && IsLoggedIn;

            _bootstrap = bootstrap;
            _initializedProjectUrl = newUrl;
            _enableApiResultLogs = bootstrap.EnableApiResultLogs;
            _duplicateSessionMonitorEnabled = bootstrap.EnableDuplicateSessionMonitor;
            _duplicateSessionPollSeconds = bootstrap.DuplicateSessionPollSeconds;
            _duplicateSessionActionCheckCooldownSeconds = bootstrap.DuplicateSessionActionCheckCooldownSeconds;

            if (!preserveSession)
                _currentSession = null;

            _userSaves = null;
            _remoteConfig = null;
            _functions = null;
            _chatChannels.Clear();

            if (preserveSession && IsLoggedIn)
                SupabaseDuplicateSessionCoordinator.ScheduleSyncAfterSessionChange(SupabaseSessionChangeKind.RestoredOrRefreshed);
        }

        /// <summary>
        /// GoogleLoginBridge가 씬에 없으면 생성합니다. (<see cref="Config.SupabaseRuntime"/>와 동일한 이름의 오브젝트)
        /// </summary>
        private static GoogleLoginBridge EnsureGoogleLoginBridge()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<GoogleLoginBridge>();
            if (existing != null)
                return existing;

            var go = new GameObject("TruesoftGoogleLoginBridge");
            UnityEngine.Object.DontDestroyOnLoad(go);
            return go.AddComponent<GoogleLoginBridge>();
        }

        /// <summary><c>Resources/SupabaseSettings</c>에서 Google Web Client ID를 읽습니다.</summary>
        private static string TryGetGoogleWebClientIdFromSettings()
        {
            var settings = Resources.Load<SupabaseSettings>("SupabaseSettings");
            if (settings == null)
                return null;

            return string.IsNullOrWhiteSpace(settings.googleWebClientId) ? null : settings.googleWebClientId.Trim();
        }

        private static bool IsAnonymousSession(SupabaseSession session)
        {
            if (session == null || session.User == null)
                return false;

            if (string.IsNullOrWhiteSpace(session.AccessToken))
                return false;

            return session.User.IsAnonymous;
        }

        private static async Task<bool> TryRestoreSessionFromAnonymousRecoveryAsync()
        {
            var svc = AnonymousRecoveryService;
            if (svc == null)
                return false;

            var fingerprintHash = DeviceFingerprintProvider.TryCreateHashedFingerprint(_initializedProjectUrl);
            if (string.IsNullOrWhiteSpace(fingerprintHash))
                return false;

            var tokenResult = await svc.TryGetRefreshTokenByFingerprintAsync(fingerprintHash);
            if (tokenResult == null || tokenResult.IsSuccess == false || string.IsNullOrWhiteSpace(tokenResult.Data))
                return false;

            var refreshResult = await RefreshSessionAsync(tokenResult.Data, saveSessionToStorage: true);
            return refreshResult != null && refreshResult.IsSuccess && refreshResult.Data != null;
        }

        private static async Task TryUpsertAnonymousRecoveryTokenAsync(SupabaseSession session)
        {
            var svc = AnonymousRecoveryService;
            if (svc == null)
                return;

            if (session == null || session.User == null || string.IsNullOrWhiteSpace(session.RefreshToken))
                return;

            var fingerprintHash = DeviceFingerprintProvider.TryCreateHashedFingerprint(_initializedProjectUrl);
            if (string.IsNullOrWhiteSpace(fingerprintHash))
                return;

            try
            {
                _ = await svc.UpsertRefreshTokenByFingerprintAsync(
                    fingerprintHash,
                    session.RefreshToken,
                    session.User.Id);
            }
            catch
            {
                // best-effort 복구 경로이므로 본 로그인 결과를 깨지 않습니다.
            }
        }
    }
}

