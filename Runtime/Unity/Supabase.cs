using System;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Unity.Config;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// 유니티 전역에서 Supabase SDK에 쉽게 접근하기 위한 정적 진입점.
    /// 로그인 후 Supabase.SetSession(session) 한 번만 하면, SaveAsync(data) / LoadAsync&lt;T&gt;() / Events.SendAsync(eventType) 등 세션 없이 호출 가능.
    /// </summary>
    public static class Supabase
    {
        /// <summary>SDK가 초기화되었는지 여부.</summary>
        public static bool IsInitialized => SupabaseSDK.IsInitialized;

        /// <summary>현재 로그인된 세션.</summary>
        public static SupabaseSession Session => SupabaseSDK.Session;

        /// <summary>현재 로그인 여부.</summary>
        public static bool IsLoggedIn => SupabaseSDK.IsLoggedIn;

        /// <summary>인증 서비스 (로그인, 리프레시 등).</summary>
        public static SupabaseAuthService Auth => SupabaseSDK.Auth;

        /// <summary>유저 데이터 저장/불러오기 퍼사드.</summary>
        public static UserSavesFacade UserSaves => SupabaseSDK.UserSaves;

        /// <summary>이벤트 전송 퍼사드 (서버 권한 패턴용).</summary>
        public static UserEventsFacade Events => SupabaseSDK.Events;

        /// <summary>RemoteConfig 퍼사드.</summary>
        public static RemoteConfigFacade RemoteConfig => SupabaseSDK.RemoteConfig;

        /// <summary>특정 key가 갱신될 때마다 콜백 (코드 연결, 실제 JSON 문자열 전달).</summary>
        public static void SubscribeRemoteConfig(string key, Action<string> onValueChanged, bool invokeIfCached = true) =>
            SupabaseSDK.RemoteConfig.Subscribe(key, onValueChanged, invokeIfCached);

        public static void UnsubscribeRemoteConfig(string key, Action<string> onValueChanged) =>
            SupabaseSDK.RemoteConfig.Unsubscribe(key, onValueChanged);

        /// <summary>채팅 채널 열기 (동일 channel_id 참여자와 대화). 로그인 후 호출.</summary>
        public static ChatChannelFacade OpenChatChannel(string channelId, string displayName = null) =>
            SupabaseSDK.OpenChatChannel(channelId, displayName);

        /// <summary>채팅 메시지 전송 (채널 인스턴스를 직접 들고 있지 않아도 됨).</summary>
        public static Task<bool> SendChatMessageAsync(string channelId, string content, string displayName = null) =>
            SupabaseSDK.SendChatMessageAsync(channelId, content, displayName);

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
