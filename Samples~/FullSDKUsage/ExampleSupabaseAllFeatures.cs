using System;
using System.Threading.Tasks;
using UnityEngine;
using Truesoft.Supabase.Unity;
using Truesoft.Supabase.Unity.Config;

using SupabaseClient = global::Truesoft.Supabase.Unity.Supabase;

namespace Truesoft.SupabaseUnity.Samples
{
    /// <summary>
    /// 전체 흐름 샘플(간단판): 인증, 데이터, 이벤트, RemoteConfig, 함수 호출.
    /// </summary>
    public sealed class ExampleSupabaseAllFeatures : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private string remoteConfigKey = "game_balance";
        [SerializeField] private string functionName = "gacha";

        private void Start()
        {
            if (runOnStart)
                _ = RunAllAsync();
        }

        [ContextMenu("Run Full SDK Usage")]
        public void RunAll()
        {
            _ = RunAllAsync();
        }

        private async Task RunAllAsync()
        {
            EnsureRuntimeExists();
            await WaitInitializedAsync();
            if (!SupabaseClient.IsInitialized)
                return;

            if (!SupabaseClient.IsLoggedIn)
            {
                var signIn = await SupabaseClient.SignInAnonymouslyAsync();
                if (!signIn.IsSuccess)
                {
                    Debug.LogError("[FullSDKUsage] Guest sign-in failed: " + signIn.ErrorMessage);
                    return;
                }
            }

            _ = await SupabaseClient.SendUserEventAsync("full_sample_started");
            _ = await SupabaseClient.RefreshRemoteConfigAsync();
            _ = await SupabaseClient.PollRemoteConfigAsync();
            SupabaseClient.TryGetRemoteConfigRaw(remoteConfigKey, out var raw);
            Debug.Log("[FullSDKUsage] RemoteConfig raw: " + raw);

            var fn = await SupabaseClient.InvokeFunctionAsync<object>(functionName, new { ping = true });
            if (!fn.IsSuccess)
                Debug.LogWarning("[FullSDKUsage] Function failed: " + fn.ErrorMessage);

            Debug.Log("[FullSDKUsage] done.");
        }

        private static async Task WaitInitializedAsync()
        {
            const int timeoutMs = 10000;
            var start = DateTime.UtcNow;

            while (!SupabaseClient.IsInitialized)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                {
                    SupabaseUnitySetupHelp.LogInitializationTimeout("FullSDKUsage");
                    return;
                }

                await Task.Yield();
            }
        }

        private static void EnsureRuntimeExists()
        {
            if (SupabaseClient.IsInitialized)
                return;

            var existing = UnityEngine.Object.FindFirstObjectByType<SupabaseRuntime>();
            if (existing != null)
                return;

            var go = new GameObject("SupabaseRuntime");
            go.AddComponent<SupabaseRuntime>();
        }
    }
}
