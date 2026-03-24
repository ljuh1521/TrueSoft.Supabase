using System;
using System.Threading.Tasks;
using UnityEngine;
using Truesoft.Supabase.Unity;
using Truesoft.Supabase.Unity.Config;

using SupabaseClient = global::Truesoft.Supabase.Unity.Supabase;

namespace Truesoft.SupabaseUnity.Samples
{
    /// <summary>
    /// 최소 실행 샘플: 게스트 로그인 + 저장/불러오기 + 이벤트 전송.
    /// </summary>
    public sealed class ExampleBootstrap : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private int level = 1;
        [SerializeField] private int coins = 100;

        private void Start()
        {
            if (runOnStart)
                _ = RunAsync();
        }

        [ContextMenu("Run Basic Setup")]
        public void Run()
        {
            _ = RunAsync();
        }

        private async Task RunAsync()
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
                    Debug.LogError("[BasicSetup] Guest sign-in failed: " + signIn.ErrorMessage);
                    return;
                }
            }

            var save = new SaveData
            {
                level = level,
                coins = coins,
                updatedAtIso = DateTime.UtcNow.ToString("o")
            };

            var saveRes = await SupabaseClient.SaveUserDataAsync(save);
            if (!saveRes.IsSuccess)
            {
                Debug.LogError("[BasicSetup] SaveUserData failed: " + saveRes.ErrorMessage);
                return;
            }

            var loadRes = await SupabaseClient.LoadUserDataAsync<SaveData>();
            if (!loadRes.IsSuccess)
            {
                Debug.LogError("[BasicSetup] LoadUserData failed: " + loadRes.ErrorMessage);
                return;
            }

            _ = await SupabaseClient.SendUserEventAsync("basic_setup_done");
            Debug.Log($"[BasicSetup] done. level={loadRes.Data.level}, coins={loadRes.Data.coins}");
        }

        private static async Task WaitInitializedAsync()
        {
            const int timeoutMs = 10000;
            var start = DateTime.UtcNow;

            while (!SupabaseClient.IsInitialized)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                {
                    SupabaseUnitySetupHelp.LogInitializationTimeout("BasicSetup");
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

        [Serializable]
        private sealed class SaveData
        {
            public int level;
            public int coins;
            public string updatedAtIso;
        }
    }
}
