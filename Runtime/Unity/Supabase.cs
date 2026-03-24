using System;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Common;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// 유니티 전역에서 Supabase SDK에 쉽게 접근하기 위한 정적 진입점.
    /// 로그인 후 Supabase.SetSession(session) 한 번만 하면, SaveUserDataAsync / LoadUserDataAsync / SendUserEventAsync 등을 세션 인자 없이 호출할 수 있습니다.
    /// </summary>
    public static class Supabase
    {
        /// <summary>SDK가 초기화되었는지 여부.</summary>
        public static bool IsInitialized => SupabaseSDK.IsInitialized;

        /// <summary>현재 로그인된 세션.</summary>
        public static SupabaseSession Session => SupabaseSDK.Session;

        /// <summary>현재 로그인 여부.</summary>
        public static bool IsLoggedIn => SupabaseSDK.IsLoggedIn;

        /// <summary>Google ID Token으로 로그인하고 SDK 세션을 자동 설정.</summary>
        public static Task<SupabaseResult<SupabaseSession>> SignInWithGoogleIdTokenAsync(
            string idToken,
            bool saveSessionToStorage = true) =>
            SupabaseSDK.SignInWithGoogleIdTokenAsync(idToken, saveSessionToStorage);

        /// <summary>게스트(익명)로 가입하고 SDK 세션을 자동 설정.</summary>
        public static Task<SupabaseResult<SupabaseSession>> SignInAnonymouslyAsync(
            bool saveSessionToStorage = true) =>
            SupabaseSDK.SignInAnonymouslyAsync(saveSessionToStorage);

        /// <summary>refresh_token으로 세션 갱신 후 SDK 세션 자동 설정.</summary>
        public static Task<SupabaseResult<SupabaseSession>> RefreshSessionAsync(
            string refreshToken,
            bool saveSessionToStorage = true) =>
            SupabaseSDK.RefreshSessionAsync(refreshToken, saveSessionToStorage);

        /// <summary>현재 세션으로 유저 데이터 저장 (정적 호출용).</summary>
        public static Task<SupabaseResult<bool>> SaveUserDataAsync<T>(T data) =>
            SupabaseSDK.SaveUserDataAsync(data);

        /// <summary>현재 세션으로 유저 데이터 로드 (정적 호출용).</summary>
        public static Task<SupabaseResult<T>> LoadUserDataAsync<T>() where T : class, new() =>
            SupabaseSDK.LoadUserDataAsync<T>();

        /// <summary>현재 세션으로 이벤트 전송 (payload 없음).</summary>
        public static Task<SupabaseResult<bool>> SendUserEventAsync(string eventType) =>
            SupabaseSDK.SendUserEventAsync(eventType);

        /// <summary>현재 세션으로 이벤트+payload 전송.</summary>
        public static Task<SupabaseResult<bool>> SendUserEventAsync<T>(string eventType, T payload) =>
            SupabaseSDK.SendUserEventAsync(eventType, payload);

        /// <summary>특정 key가 갱신될 때마다 콜백 (코드 연결, 실제 JSON 문자열 전달).</summary>
        public static void SubscribeRemoteConfig(string key, Action<string> onValueChanged, bool invokeIfCached = true) =>
            SupabaseSDK.SubscribeRemoteConfig(key, onValueChanged, invokeIfCached);

        public static void UnsubscribeRemoteConfig(string key, Action<string> onValueChanged) =>
            SupabaseSDK.UnsubscribeRemoteConfig(key, onValueChanged);

        /// <summary>RemoteConfig 전체 새로고침.</summary>
        public static Task<bool> RefreshRemoteConfigAsync() => SupabaseSDK.RefreshRemoteConfigAsync();

        /// <summary>RemoteConfig 변경분 폴링.</summary>
        public static Task<bool> PollRemoteConfigAsync() => SupabaseSDK.PollRemoteConfigAsync();

        public static T GetRemoteConfig<T>(string key, T defaultValue = default) =>
            SupabaseSDK.GetRemoteConfig(key, defaultValue);

        public static bool TryGetRemoteConfigRaw(string key, out string valueJson) =>
            SupabaseSDK.TryGetRemoteConfigRaw(key, out valueJson);

        /// <summary>채팅 메시지 전송 (채널 인스턴스를 직접 들고 있지 않아도 됨).</summary>
        public static Task<bool> SendChatMessageAsync(string channelId, string content, string displayName = null) =>
            SupabaseSDK.SendChatMessageAsync(channelId, content, displayName);

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
        /// JoinChatChannel로 구독한 채널에서 빠져나옵니다.
        /// 예: Supabase.LeaveChatChannel(\"room-1\", OnChatMessage);
        /// </summary>
        public static void LeaveChatChannel(
            string channelId,
            Action<Core.Data.SupabaseChatService.ChatMessageRow> onMessageReceived = null,
            bool stopPollingIfNoListeners = true) =>
            SupabaseSDK.LeaveChatChannel(channelId, onMessageReceived, stopPollingIfNoListeners);

        /// <summary>
        /// 로그인 세션으로 서버 함수 호출.
        /// 예: var result = await Supabase.InvokeFunctionAsync<DrawResult>(\"gacha-draw\", payload);
        /// </summary>
        public static Task<SupabaseResult<TResponse>> InvokeFunctionAsync<TResponse>(
            string functionName,
            object requestBody = null) =>
            SupabaseSDK.InvokeFunctionAsync<TResponse>(functionName, requestBody);

        /// <summary>로그인 성공 시 세션을 SDK에 설정. 이후 Save/Load/Events는 세션 인자 없이 사용 가능.</summary>
        public static void SetSession(SupabaseSession session) => SupabaseSDK.SetSession(session);

        /// <summary>로그아웃 시 호출. clearStorage가 true면 저장된 refresh_token도 삭제.</summary>
        public static void ClearSession(bool clearStorage = true) => SupabaseSDK.ClearSession(clearStorage);

        /// <summary>현재 세션을 기기에 저장. 앱 재시작 후 RestoreSessionAsync로 복원 가능.</summary>
        public static void SaveSessionToStorage() => SupabaseSDK.SaveSessionToStorage();

        /// <summary>저장된 refresh_token으로 세션 복원. Runner의 'Restore Session On Start' 또는 로그인 화면에서 호출.</summary>
        public static Task<bool> RestoreSessionAsync() => SupabaseSDK.RestoreSessionAsync();
    }
}
