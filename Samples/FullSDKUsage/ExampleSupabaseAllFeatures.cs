using System;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Data;
using Truesoft.Supabase.Unity.Config;
using Truesoft.Supabase.Unity;
using UnityEngine;

namespace Truesoft.SupabaseUnity.Samples
{
    /// <summary>
    /// 주요 SDK 기능을 한 번에 모두 확인하는 샘플입니다.
    /// </summary>
    public sealed class ExampleSupabaseAllFeatures : MonoBehaviour
    {
        [Header("실행")]
        [SerializeField] private bool runOnStart = true;

        [Header("원격 설정")]
        [SerializeField] private string remoteConfigKey = "game_balance";

        [Header("Edge Functions")]
        [SerializeField] private string functionName = "gacha";
        [SerializeField] private string functionBannerId = "normal_banner_001";
        [SerializeField] private int functionDrawCount = 5;

        [Header("채팅")]
        [SerializeField] private string chatChannelId = "room-1";
        [SerializeField] private string chatDisplayName = "SampleUser";
        [SerializeField] private string chatMessage = "Hello from FullSDKUsage sample.";

        [Header("Google 연동(선택)")]
        [Tooltip("값을 넣으면 게스트 로그인 뒤 Google ID 토큰으로 로그인·연동(SignInWithGoogleIdTokenAsync)을 호출합니다.")]
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

                // 원격 설정: 먼저 구독을 등록해 새로고침·폴링 이벤트를 확인합니다.
                Supabase.SubscribeRemoteConfig(remoteConfigKey, OnRemoteConfigChanged, invokeIfCached: true);

                await RequireAuthSessionAsync();

                await DemoSaveLoadAsync();
                await DemoEventsAsync();
                await DemoRemoteConfigAsync();
                await DemoFunctionAsync();
                await DemoChatAsync();

                Debug.Log("[Sample] 전체 샘플 흐름 완료.");
            }
            catch (Exception e)
            {
                Debug.LogError("[Sample] 예외: " + e.Message);
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
                    throw new Exception("게스트 로그인 실패: " + guest.ErrorMessage);

                Debug.Log("[Sample] 게스트 로그인 성공.");
            }

            if (!string.IsNullOrWhiteSpace(googleIdTokenForLinkOrSignIn))
            {
                var google = await Supabase.SignInWithGoogleIdTokenAsync(googleIdTokenForLinkOrSignIn);
                if (!google.IsSuccess)
                    Debug.LogWarning("[Sample] Google 로그인·연동 실패: " + google.ErrorMessage);
                else
                    Debug.Log("[Sample] Google 로그인·연동 성공.");
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
                throw new Exception("사용자 데이터 저장 실패: " + saveResult.ErrorMessage);

            var loadResult = await Supabase.LoadUserDataAsync<PlayerSaveData>();
            if (!loadResult.IsSuccess)
                throw new Exception("사용자 데이터 불러오기 실패: " + loadResult.ErrorMessage);

            var data = loadResult.Data;
            Debug.Log($"[Sample] 저장·불러오기 완료. level={data.level}, coins={data.coins}, stage={data.stage}");
        }

        private async Task DemoEventsAsync()
        {
            var eventA = await Supabase.SendUserEventAsync("sample_session_started");
            if (!eventA.IsSuccess)
                Debug.LogWarning("[Sample] 이벤트 전송(페이로드 없음) 실패: " + eventA.ErrorMessage);

            var payload = new SessionEventPayload
            {
                source = "full_sample",
                timestampIso = DateTime.UtcNow.ToString("o"),
                success = true
            };

            var eventB = await Supabase.SendUserEventAsync("sample_session_payload", payload);
            if (!eventB.IsSuccess)
                Debug.LogWarning("[Sample] 이벤트 전송(페이로드 있음) 실패: " + eventB.ErrorMessage);
        }

        private async Task DemoRemoteConfigAsync()
        {
            var refreshed = await Supabase.RefreshRemoteConfigAsync();
            Debug.Log("[Sample] 원격 설정 새로고침: " + refreshed);

            var polled = await Supabase.PollRemoteConfigAsync();
            Debug.Log("[Sample] 원격 설정 폴링: " + polled);

            if (Supabase.TryGetRemoteConfigRaw(remoteConfigKey, out var raw))
                Debug.Log("[Sample] 원격 설정 원문 JSON: " + raw);

            var typed = Supabase.GetRemoteConfig<BalanceConfig>(remoteConfigKey, default);
            if (typed != null)
                Debug.Log($"[Sample] 원격 설정 파싱값: maxLevel={typed.maxLevel}, rewardMultiplier={typed.rewardMultiplier}");
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
                Debug.LogWarning("[Sample] Edge Functions 호출 실패: " + result.ErrorMessage);
                return;
            }

            var response = result.Data;
            if (response == null)
            {
                Debug.LogWarning("[Sample] Edge Functions 응답이 null입니다.");
                return;
            }

            var rewardCount = response.rewards == null ? 0 : response.rewards.Length;
            Debug.Log($"[Sample] Edge Functions 호출 성공. banner={response.bannerId}, drawCount={response.drawCount}, rewards={rewardCount}");
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
            Debug.Log("[Sample] 채팅 전송 결과: " + sent);
        }

        private void OnRemoteConfigChanged(string json)
        {
            Debug.Log($"[Sample] 원격 설정 변경. key={remoteConfigKey}, json={json}");
        }

        private void OnChatMessageReceived(SupabaseChatService.ChatMessageRow row)
        {
            Debug.Log($"[Sample] 채팅[{row.channel_id}] {row.display_name}: {row.content}");
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
