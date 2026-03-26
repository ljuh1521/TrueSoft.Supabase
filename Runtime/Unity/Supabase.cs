using System;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Data;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// 게임 코드에서 쓰기 위한 정적 진입점입니다. 실제 구현은 <see cref="SupabaseSDK"/>에 있습니다.
    /// </summary>
    /// <remarks>
    /// • 구글: <see cref="TrySignInWithGoogleAsync(bool)"/>는 Android 네이티브 전체 플로우(설정의 Web Client ID), <see cref="TrySignInWithGoogleIdTokenAsync"/>는 ID 토큰 문자열만 넘길 때.<br/>
    /// • <see cref="TryStartAsync"/>는 초기화·(선택)세션 복원·(선택)RC를 한 번에 수행합니다.<br/>
    /// • 공개 프로필: <see cref="TryGetPublicProfileAsync"/>, 닉네임 <see cref="TryIsNicknameAvailableAsync"/> → <see cref="TrySetMyNicknameAsync"/>(수정 동일), 탈퇴 표시 <see cref="TryMarkMyWithdrawnAsync"/> 등 (DB <c>profiles</c>, README).<br/>
    /// • Try API들은 <c>SupabaseSettings.enableApiResultLogs</c>에 따라 API별 고정 태그로 성공/실패 로그를 자동 출력합니다.
    /// </remarks>
    public static class Supabase
    {
        /// <summary>SDK가 초기화되었는지 여부.</summary>
        public static bool IsInitialized => SupabaseSDK.IsInitialized;

        /// <summary>현재 로그인된 세션.</summary>
        public static SupabaseSession Session => SupabaseSDK.Session;

        /// <summary>현재 로그인 여부.</summary>
        public static bool IsLoggedIn => SupabaseSDK.IsLoggedIn;

        /// <summary>
        /// 씬의 SupabaseRuntime 초기화를 잠시 대기한 뒤, 필요 시 Resources의 SupabaseSettings로 부트스트랩합니다.
        /// 대부분의 API가 내부에서 호출하므로, 게임 코드에서는 생략해도 됩니다.
        /// </summary>
        public static Task<bool> EnsureInitializedAsync(int timeoutMs = SupabaseSDK.DefaultEnsureInitTimeoutMs) =>
            SupabaseSDK.EnsureInitializedAsync(timeoutMs);

        /// <summary>이미 가진 Google ID 토큰으로 Supabase 세션을 맞춥니다(iOS·커스텀 OAuth·테스트 등).</summary>
        internal static Task<SupabaseResult<SupabaseSession>> SignInWithGoogleIdTokenAsync(
            string idToken,
            bool saveSessionToStorage = true) =>
            SupabaseSDK.SignInWithGoogleIdTokenAsync(idToken, saveSessionToStorage);

        /// <summary>
        /// Android 네이티브 Google 로그인 후 Supabase 세션까지 한 번에 처리합니다.
        /// <c>SupabaseSettings.googleWebClientId</c>(Resources)를 사용합니다.
        /// </summary>
        internal static Task<SupabaseResult<SupabaseSession>> SignInWithGoogleAsync(bool saveSessionToStorage = true) =>
            SupabaseSDK.SignInWithGoogleAsync(saveSessionToStorage);

        /// <summary>Android 네이티브 Google 계정 로그아웃 (Supabase 세션은 유지).</summary>
        internal static Task<SupabaseResult<bool>> SignOutFromGoogleAsync() =>
            SupabaseSDK.SignOutFromGoogleAsync();

        /// <summary>게스트(익명)로 가입하고 SDK 세션을 자동 설정.</summary>
        internal static Task<SupabaseResult<SupabaseSession>> SignInAnonymouslyAsync(
            bool saveSessionToStorage = true) =>
            SupabaseSDK.SignInAnonymouslyAsync(saveSessionToStorage);

        /// <inheritdoc cref="SupabaseSDK.TrySignInWithGoogleAsync(bool)"/>
        public static Task<bool> TrySignInWithGoogleAsync(bool saveSessionToStorage = true) =>
            SupabaseSDK.TrySignInWithGoogleAsync(saveSessionToStorage);

        /// <inheritdoc cref="SupabaseSDK.TrySignInWithGoogleIdTokenAsync(string, bool)"/>
        public static Task<bool> TrySignInWithGoogleIdTokenAsync(string idToken, bool saveSessionToStorage = true) =>
            SupabaseSDK.TrySignInWithGoogleIdTokenAsync(idToken, saveSessionToStorage);

        /// <inheritdoc cref="SupabaseSDK.TrySignInAnonymouslyAsync(bool)"/>
        public static Task<bool> TrySignInAnonymouslyAsync(bool saveSessionToStorage = true) =>
            SupabaseSDK.TrySignInAnonymouslyAsync(saveSessionToStorage);

        /// <inheritdoc cref="SupabaseSDK.TryStartAsync(bool, bool)"/>
        public static Task<bool> TryStartAsync(
            bool restoreSessionFirst = true,
            bool refreshRemoteConfigOnStart = false) =>
            SupabaseSDK.TryStartAsync(restoreSessionFirst, refreshRemoteConfigOnStart);

        /// <inheritdoc cref="SupabaseSDK.TrySignOutFromGoogleAsync"/>
        public static Task<bool> TrySignOutFromGoogleAsync() =>
            SupabaseSDK.TrySignOutFromGoogleAsync();

        /// <inheritdoc cref="SupabaseSDK.TryRefreshSessionAsync"/>
        public static Task<bool> TryRefreshSessionAsync(string refreshToken, bool saveSessionToStorage = true) =>
            SupabaseSDK.TryRefreshSessionAsync(refreshToken, saveSessionToStorage);

        /// <summary>초기화 + 로그인 세션을 확인합니다. 미로그인이면 실패를 반환합니다.</summary>
        internal static Task<SupabaseResult<SupabaseSession>> EnsureReadySessionAsync() =>
            SupabaseSDK.EnsureReadySessionAsync();

        /// <summary>
        /// 앱 시작 시 자주 필요한 준비를 한 번에 수행합니다.
        /// 초기화 -> (선택) 저장 세션 복원 -> (선택) RemoteConfig 새로고침.
        /// </summary>
        public static Task<bool> StartAsync(
            bool restoreSessionFirst = true,
            bool refreshRemoteConfigOnStart = false) =>
            SupabaseSDK.StartAsync(restoreSessionFirst, refreshRemoteConfigOnStart);

        /// <summary>refresh_token으로 세션 갱신 후 SDK 세션 자동 설정.</summary>
        internal static Task<SupabaseResult<SupabaseSession>> RefreshSessionAsync(
            string refreshToken,
            bool saveSessionToStorage = true) =>
            SupabaseSDK.RefreshSessionAsync(refreshToken, saveSessionToStorage);

        /// <summary>현재 세션으로 유저 데이터 저장 (내부 Result API).</summary>
        internal static Task<SupabaseResult<bool>> SaveUserDataAsync<T>(T data) =>
            SupabaseSDK.SaveUserDataAsync(data);

        /// <summary>현재 세션으로 유저 데이터 로드 (내부 Result API).</summary>
        internal static Task<SupabaseResult<T>> LoadUserDataAsync<T>() where T : class, new() =>
            SupabaseSDK.LoadUserDataAsync<T>();

        /// <inheritdoc cref="SupabaseSDK.TrySaveUserDataAsync{T}(T)"/>
        public static Task<bool> TrySaveUserDataAsync<T>(T data) =>
            SupabaseSDK.TrySaveUserDataAsync(data);

        /// <inheritdoc cref="SupabaseSDK.TryLoadUserDataAsync{T}(T)"/>
        public static Task<T> TryLoadUserDataAsync<T>(T defaultValue = default) where T : class, new() =>
            SupabaseSDK.TryLoadUserDataAsync(defaultValue);

        /// <summary>다른 사용자 공개 닉네임 조회 (내부 Result API).</summary>
        internal static Task<SupabaseResult<string>> GetPublicNicknameAsync(string userId) =>
            SupabaseSDK.GetPublicNicknameAsync(userId);

        /// <summary>현재 사용자 공개 닉네임 저장 (내부 Result API).</summary>
        internal static Task<SupabaseResult<bool>> UpsertMyNicknameAsync(string nickname) =>
            SupabaseSDK.UpsertMyNicknameAsync(nickname);

        /// <inheritdoc cref="SupabaseSDK.TryGetPublicNicknameAsync(string, string)"/>
        public static Task<string> TryGetPublicNicknameAsync(string userId, string defaultValue = "") =>
            SupabaseSDK.TryGetPublicNicknameAsync(userId, defaultValue);

        /// <inheritdoc cref="SupabaseSDK.TrySetMyNicknameAsync"/>
        public static Task<bool> TrySetMyNicknameAsync(string nickname) =>
            SupabaseSDK.TrySetMyNicknameAsync(nickname);

        /// <summary>닉네임 수정. <see cref="TrySetMyNicknameAsync"/>와 동일(upsert).</summary>
        public static Task<bool> TryUpdateMyNicknameAsync(string nickname) =>
            SupabaseSDK.TrySetMyNicknameAsync(nickname);

        /// <inheritdoc cref="SupabaseSDK.TryIsNicknameAvailableAsync"/>
        public static Task<bool> TryIsNicknameAvailableAsync(string nickname, string ignoreUserIdForSelf = null) =>
            SupabaseSDK.TryIsNicknameAvailableAsync(nickname, ignoreUserIdForSelf);

        /// <summary>닉네임 사용 가능 여부 (내부 Result API).</summary>
        internal static Task<SupabaseResult<bool>> IsNicknameAvailableAsync(string nickname, string ignoreUserIdForSelf = null) =>
            SupabaseSDK.IsNicknameAvailableAsync(nickname, ignoreUserIdForSelf);

        /// <inheritdoc cref="SupabaseSDK.TryGetPublicProfileAsync"/>
        public static Task<PublicProfileSnapshot> TryGetPublicProfileAsync(string userId) =>
            SupabaseSDK.TryGetPublicProfileAsync(userId);

        /// <summary>공개 프로필 조회 (내부 Result API).</summary>
        internal static Task<SupabaseResult<PublicProfileSnapshot>> GetPublicProfileAsync(string userId) =>
            SupabaseSDK.GetPublicProfileAsync(userId);

        /// <inheritdoc cref="SupabaseSDK.TryMarkMyWithdrawnAsync"/>
        public static Task<bool> TryMarkMyWithdrawnAsync() =>
            SupabaseSDK.TryMarkMyWithdrawnAsync();

        /// <inheritdoc cref="SupabaseSDK.TryClearMyWithdrawalAsync"/>
        public static Task<bool> TryClearMyWithdrawalAsync() =>
            SupabaseSDK.TryClearMyWithdrawalAsync();

        /// <inheritdoc cref="SupabaseSDK.TrySetMyWithdrawnAtAsync"/>
        public static Task<bool> TrySetMyWithdrawnAtAsync(string withdrawnAtIsoUtc) =>
            SupabaseSDK.TrySetMyWithdrawnAtAsync(withdrawnAtIsoUtc);

        /// <summary>특정 key가 갱신될 때마다 콜백 (코드 연결, 실제 JSON 문자열 전달).</summary>
        public static void SubscribeRemoteConfig(string key, Action<string> onValueChanged, bool invokeIfCached = true) =>
            SupabaseSDK.SubscribeRemoteConfig(key, onValueChanged, invokeIfCached);

        public static void UnsubscribeRemoteConfig(string key, Action<string> onValueChanged) =>
            SupabaseSDK.UnsubscribeRemoteConfig(key, onValueChanged);

        /// <summary>RemoteConfig 전체 새로고침 (내부 API).</summary>
        internal static Task<bool> RefreshRemoteConfigAsync() => SupabaseSDK.RefreshRemoteConfigAsync();

        /// <summary>RemoteConfig 변경분 폴링 (내부 API).</summary>
        internal static Task<bool> PollRemoteConfigAsync() => SupabaseSDK.PollRemoteConfigAsync();

        public static T GetRemoteConfig<T>(string key, T defaultValue = default) =>
            SupabaseSDK.GetRemoteConfig(key, defaultValue);

        /// <summary>RemoteConfig를 한 번 갱신/폴링 후 key 값을 읽는 내부 API.</summary>
        internal static Task<T> GetRemoteConfigAsync<T>(string key, T defaultValue = default, bool pollOnly = false) =>
            SupabaseSDK.GetRemoteConfigAsync(key, defaultValue, pollOnly);

        /// <inheritdoc cref="SupabaseSDK.TryRefreshRemoteConfigAsync"/>
        public static Task<bool> TryRefreshRemoteConfigAsync() =>
            SupabaseSDK.TryRefreshRemoteConfigAsync();

        /// <inheritdoc cref="SupabaseSDK.TryPollRemoteConfigAsync"/>
        public static Task<bool> TryPollRemoteConfigAsync() =>
            SupabaseSDK.TryPollRemoteConfigAsync();

        /// <summary>
        /// RemoteConfig를 온디맨드 방식으로 즉시 동기화합니다(서버에서 다시 가져와 캐시 갱신).
        /// 호출 직후에는 다음 주기 폴링을 뒤로 미뤄 의도치 않은 잦은 호출을 방지합니다.
        /// </summary>
        public static Task<bool> RefreshRemoteConfigOnDemandAsync() =>
            SupabaseSDK.RefreshRemoteConfigOnDemandAsync();

        /// <inheritdoc cref="SupabaseSDK.TryGetRemoteConfigAsync{T}(string, T, bool)"/>
        public static Task<T> TryGetRemoteConfigAsync<T>(string key, T defaultValue = default, bool pollOnly = false) =>
            SupabaseSDK.TryGetRemoteConfigAsync(key, defaultValue, pollOnly);

        public static bool TryGetRemoteConfigRaw(string key, out string valueJson) =>
            SupabaseSDK.TryGetRemoteConfigRaw(key, out valueJson);

        /// <summary>채팅 메시지 전송 (내부 API).</summary>
        internal static Task<bool> SendChatMessageAsync(string channelId, string content, string displayName = null) =>
            SupabaseSDK.SendChatMessageAsync(channelId, content, displayName);

        /// <inheritdoc cref="SupabaseSDK.TrySendChatMessageAsync(string, string, string)"/>
        public static Task<bool> TrySendChatMessageAsync(
            string channelId,
            string content,
            string displayName = null) =>
            SupabaseSDK.TrySendChatMessageAsync(channelId, content, displayName);

        /// <summary>채널이 현재 SDK 캐시에 열려 있는지 확인.</summary>
        public static bool IsChatChannelOpen(string channelId) => SupabaseSDK.IsChatChannelOpen(channelId);

        /// <summary>
        /// 채널 join + 이벤트 구독 + 폴링 시작을 한 번에 수행합니다.
        /// 예: Supabase.JoinChatChannel(\"room-1\", this, OnChatMessage);
        /// </summary>
        public static ChatChannelFacade JoinChatChannel(
            string channelId,
            UnityEngine.MonoBehaviour pollHost,
            Action<Core.Data.SupabaseChatService.ChatMessageRow> onMessageReceived,
            float pollIntervalSeconds = 1.5f,
            bool loadHistory = true,
            int historyCount = 50) =>
            SupabaseSDK.JoinChatChannel(channelId, pollHost, onMessageReceived, pollIntervalSeconds, loadHistory, historyCount);

        /// <summary>
        /// 채널 join + 이벤트 구독 + 폴링 시작.
        /// 예: await Supabase.JoinChatChannelAsync("room-1", this, OnChatMessage);
        /// </summary>
        public static Task<ChatChannelFacade> JoinChatChannelAsync(
            string channelId,
            UnityEngine.MonoBehaviour pollHost,
            Action<Core.Data.SupabaseChatService.ChatMessageRow> onMessageReceived,
            float pollIntervalSeconds = 1.5f,
            bool loadHistory = true,
            int historyCount = 50) =>
            SupabaseSDK.JoinChatChannelAsync(
                channelId,
                pollHost,
                onMessageReceived,
                pollIntervalSeconds,
                loadHistory,
                historyCount);

        /// <inheritdoc cref="SupabaseSDK.TryJoinChatChannelAsync"/>
        public static Task<ChatChannelFacade> TryJoinChatChannelAsync(
            string channelId,
            UnityEngine.MonoBehaviour pollHost,
            Action<Core.Data.SupabaseChatService.ChatMessageRow> onMessageReceived,
            float pollIntervalSeconds = 1.5f,
            bool loadHistory = true,
            int historyCount = 50) =>
            SupabaseSDK.TryJoinChatChannelAsync(
                channelId,
                pollHost,
                onMessageReceived,
                pollIntervalSeconds,
                loadHistory,
                historyCount);

        /// <summary>
        /// JoinChatChannel로 구독한 채널에서 빠져나옵니다.
        /// 예: Supabase.LeaveChatChannel(\"room-1\", OnChatMessage);
        /// </summary>
        public static void LeaveChatChannel(
            string channelId,
            Action<Core.Data.SupabaseChatService.ChatMessageRow> onMessageReceived = null,
            bool stopPollingIfNoListeners = true) =>
            SupabaseSDK.LeaveChatChannel(channelId, onMessageReceived, stopPollingIfNoListeners);

        /// <summary>서버 함수 호출 (내부 Result API, 로그인 세션 필요).</summary>
        internal static Task<SupabaseResult<TResponse>> InvokeFunctionAsync<TResponse>(
            string functionName,
            object requestBody = null) =>
            SupabaseSDK.InvokeFunctionAsync<TResponse>(functionName, requestBody);

        /// <inheritdoc cref="SupabaseSDK.TryInvokeFunctionAsync{TResponse}(string, object, TResponse)"/>
        public static Task<TResponse> TryInvokeFunctionAsync<TResponse>(
            string functionName,
            object requestBody = null,
            TResponse defaultValue = default) =>
            SupabaseSDK.TryInvokeFunctionAsync(functionName, requestBody, defaultValue);

        /// <summary>로그인 성공 시 세션을 SDK에 설정. 이후 Save/Load API는 세션 인자 없이 사용 가능.</summary>
        public static void SetSession(SupabaseSession session) => SupabaseSDK.SetSession(session);

        /// <inheritdoc cref="SupabaseSDK.SetSession(SupabaseSession, SupabaseSessionChangeKind)"/>
        public static void SetSession(SupabaseSession session, SupabaseSessionChangeKind kind) =>
            SupabaseSDK.SetSession(session, kind);

        /// <summary>다른 기기에서 같은 계정으로 로그인해 이 기기 세션이 무효화된 경우(이미 로그아웃 처리 후). UI 팝업에 구독하세요.</summary>
        public static event Action OnDuplicateLoginDetected
        {
            add => SupabaseSDK.OnDuplicateLoginDetected += value;
            remove => SupabaseSDK.OnDuplicateLoginDetected -= value;
        }

        /// <summary>로그아웃 시 호출. clearStorage가 true면 저장된 refresh_token도 삭제.</summary>
        public static void ClearSession(bool clearStorage = true) => SupabaseSDK.ClearSession(clearStorage);

        /// <inheritdoc cref="SupabaseSDK.ClearSession(bool, bool)"/>
        public static void ClearSession(bool clearStorage, bool deleteUserSessionRow) =>
            SupabaseSDK.ClearSession(clearStorage, deleteUserSessionRow);

        /// <summary>현재 세션을 기기에 저장. 앱 재시작 후 RestoreSessionAsync로 복원 가능.</summary>
        public static void SaveSessionToStorage() => SupabaseSDK.SaveSessionToStorage();

        /// <summary>저장된 refresh_token으로 세션 복원 (내부 API).</summary>
        internal static Task<bool> RestoreSessionAsync() => SupabaseSDK.RestoreSessionAsync();

        /// <inheritdoc cref="SupabaseSDK.TryRestoreSessionAsync"/>
        public static Task<bool> TryRestoreSessionAsync() => SupabaseSDK.TryRestoreSessionAsync();

        /// <summary>서버 기준 현재 시각(UTC). 로그인 없이 호출 가능합니다. SQL: <c>Sql/supabase_server_time.sql</c>.</summary>
        public static Task<SupabaseResult<DateTime>> GetServerUtcNowAsync() => SupabaseSDK.GetServerUtcNowAsync();

        /// <inheritdoc cref="SupabaseSDK.TryGetServerUtcNowAsync"/>
        public static Task<DateTime> TryGetServerUtcNowAsync(DateTime defaultValue = default) =>
            SupabaseSDK.TryGetServerUtcNowAsync(defaultValue);
    }
}
