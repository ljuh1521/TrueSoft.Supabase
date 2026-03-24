using System;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Data;
using Truesoft.Supabase.Unity.Config;
using Truesoft.Supabase.Unity;
using UnityEngine;

namespace Truesoft.Supabase.Samples
{
    /// <summary>
    /// 주요 SDK 기능을 한 번에 모두 확인하는 샘플입니다.
    /// </summary>
    public sealed class ExampleSupabaseAllFeatures : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField] private bool runOnStart = true;

        [Header("RemoteConfig")]
        [SerializeField] private string remoteConfigKey = "game_balance";

        [Header("Edge Function")]
        [SerializeField] private string functionName = "gacha";
        [SerializeField] private string functionBannerId = "normal_banner_001";
        [SerializeField] private int functionDrawCount = 5;

        [Header("Chat")]
        [SerializeField] private string chatChannelId = "room-1";
        [SerializeField] private string chatDisplayName = "SampleUser";
        [SerializeField] private string chatMessage = "Hello from FullSDKUsage sample.";

        [Header("Optional Google Link/Sign-In")]
        [Tooltip("값을 입력하면, 게스트 로그인 후 Supabase.SignInWithGoogleIdTokenAsync(token)을 호출합니다.")]
        [SerializeField] private string googleIdTokenForLinkOrSignIn;

        private void Start()
        {
            if (runOnStart)
                _ = RunAllAsync();
        }

        [ContextMenu("전체 SDK 샘플 단계 실행")]
        public void RunAll()
        {
            _ = RunAllAsync();
        }

        private async Task RunAllAsync()
        {
            try
            {
                EnsureRuntimeExists();
                await WaitForSdkInitializedAsync();

                // RemoteConfig: 먼저 구독을 등록해서 refresh/poll 이벤트를 확인합니다.
                Supabase.SubscribeRemoteConfig(remoteConfigKey, OnRemoteConfigChanged, invokeIfCached: true);

                await RequireAuthSessionAsync();

                await DemoSaveLoadAsync();
                await DemoEventsAsync();
                await DemoRemoteConfigAsync();
                await DemoFunctionAsync();
                await DemoChatAsync();

                Debug.Log("[Sample] Full SDK usage flow completed.");
            }
            catch (Exception e)
            {
                Debug.LogError("[Sample] Unexpected exception: " + e.Message);
            }
        }

        private async Task WaitForSdkInitializedAsync()
        {
            const int timeoutMs = 10000;
            var start = DateTime.UtcNow;

            while (!Supabase.IsInitialized)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                {
                    SupabaseUnitySetupHelp.LogInitializationTimeout("FullSDKUsage");
                    throw new Exception("[FullSDKUsage] SDK 초기화 타임아웃.");
                }

                await Task.Yield();
            }
        }

        private static void EnsureRuntimeExists()
        {
            if (Supabase.IsInitialized)
                return;

            var existing = Object.FindFirstObjectByType<SupabaseRuntime>();
            if (existing != null)
                return;

            var go = new GameObject("SupabaseRuntime");
            go.AddComponent<SupabaseRuntime>();
        }

        private async Task RequireAuthSessionAsync()
        {
            if (!Supabase.IsLoggedIn)
            {
                var guest = await Supabase.SignInAnonymouslyAsync();
                if (!guest.IsSuccess)
                    throw new Exception("Guest sign-in failed: " + guest.ErrorMessage);

                Debug.Log("[Sample] Guest sign-in success.");
            }

            if (!string.IsNullOrWhiteSpace(googleIdTokenForLinkOrSignIn))
            {
                var google = await Supabase.SignInWithGoogleIdTokenAsync(googleIdTokenForLinkOrSignIn);
                if (!google.IsSuccess)
                    Debug.LogWarning("[Sample] Google sign-in/link failed: " + google.ErrorMessage);
                else
                    Debug.Log("[Sample] Google sign-in/link success.");
            }
        }

        private async Task DemoSaveLoadAsync()
        {
            var save = new PlayerSaveData
            {
                level = 3,
                coins = 250,
                stage = "stage_1_2",
                updatedAtIso = DateTime.UtcNow.ToString("o")
            };

            var saveResult = await Supabase.SaveUserDataAsync(save);
            if (!saveResult.IsSuccess)
                throw new Exception("SaveUserData failed: " + saveResult.ErrorMessage);

            var loadResult = await Supabase.LoadUserDataAsync<PlayerSaveData>();
            if (!loadResult.IsSuccess)
                throw new Exception("LoadUserData failed: " + loadResult.ErrorMessage);

            var data = loadResult.Data;
            Debug.Log($"[Sample] Save/Load ok. level={data.level}, coins={data.coins}, stage={data.stage}");
        }

        private async Task DemoEventsAsync()
        {
            var eventA = await Supabase.SendUserEventAsync("sample_session_started");
            if (!eventA.IsSuccess)
                Debug.LogWarning("[Sample] SendUserEvent(no payload) failed: " + eventA.ErrorMessage);

            var payload = new SessionEventPayload
            {
                source = "full_sample",
                timestampIso = DateTime.UtcNow.ToString("o"),
                success = true
            };

            var eventB = await Supabase.SendUserEventAsync("sample_session_payload", payload);
            if (!eventB.IsSuccess)
                Debug.LogWarning("[Sample] SendUserEvent(payload) failed: " + eventB.ErrorMessage);
        }

        private async Task DemoRemoteConfigAsync()
        {
            var refreshed = await Supabase.RefreshRemoteConfigAsync();
            Debug.Log("[Sample] RemoteConfig refresh: " + refreshed);

            var polled = await Supabase.PollRemoteConfigAsync();
            Debug.Log("[Sample] RemoteConfig poll: " + polled);

            if (Supabase.TryGetRemoteConfigRaw(remoteConfigKey, out var raw))
                Debug.Log("[Sample] RemoteConfig raw: " + raw);

            var typed = Supabase.GetRemoteConfig<BalanceConfig>(remoteConfigKey, default);
            if (typed != null)
                Debug.Log($"[Sample] RemoteConfig typed: maxLevel={typed.maxLevel}, rewardMultiplier={typed.rewardMultiplier}");
        }

        private async Task DemoFunctionAsync()
        {
            var request = new DrawRequest
            {
                bannerId = functionBannerId,
                drawCount = functionDrawCount
            };

            var result = await Supabase.InvokeFunctionAsync<DrawResponse>(functionName, request);
            if (!result.IsSuccess)
            {
                Debug.LogWarning("[Sample] Edge function failed: " + result.ErrorMessage);
                return;
            }

            var response = result.Data;
            if (response == null)
            {
                Debug.LogWarning("[Sample] Edge function response was null.");
                return;
            }

            var rewardCount = response.rewards == null ? 0 : response.rewards.Length;
            Debug.Log($"[Sample] Edge function ok. banner={response.bannerId}, drawCount={response.drawCount}, rewards={rewardCount}");
        }

        private async Task DemoChatAsync()
        {
            Supabase.JoinChatChannel(
                channelId: chatChannelId,
                pollHost: this,
                onMessageReceived: OnChatMessageReceived,
                pollIntervalSeconds: 1.5f,
                loadHistory: true,
                historyCount: 20);

            var sent = await Supabase.SendChatMessageAsync(chatChannelId, chatMessage, chatDisplayName);
            Debug.Log("[Sample] Chat send result: " + sent);
        }

        private void OnRemoteConfigChanged(string json)
        {
            Debug.Log($"[Sample] RemoteConfig changed. key={remoteConfigKey}, json={json}");
        }

        private void OnChatMessageReceived(SupabaseChatService.ChatMessageRow row)
        {
            Debug.Log($"[Sample] Chat[{row.channel_id}] {row.display_name}: {row.content}");
        }

        private void OnDestroy()
        {
            Supabase.UnsubscribeRemoteConfig(remoteConfigKey, OnRemoteConfigChanged);
            Supabase.LeaveChatChannel(chatChannelId, OnChatMessageReceived, stopPollingIfNoListeners: true);
        }

        [Serializable]
        private sealed class PlayerSaveData
        {
            public int level;
            public int coins;
            public string stage;
            public string updatedAtIso;
        }

        [Serializable]
        private sealed class SessionEventPayload
        {
            public string source;
            public string timestampIso;
            public bool success;
        }

        [Serializable]
        private sealed class BalanceConfig
        {
            public int maxLevel;
            public float rewardMultiplier;
        }

        [Serializable]
        private sealed class DrawRequest
        {
            public string bannerId;
            public int drawCount;
        }

        [Serializable]
        private sealed class DrawReward
        {
            public string id;
            public string rarity;
        }

        [Serializable]
        private sealed class DrawResponse
        {
            public string bannerId;
            public int drawCount;
            public DrawReward[] rewards;
            public string serverTime;
        }
    }
}
