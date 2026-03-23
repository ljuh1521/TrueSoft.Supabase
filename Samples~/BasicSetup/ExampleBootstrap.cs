using System;
using System.Threading.Tasks;
using UnityEngine;
using Truesoft.Supabase.Unity;
using Truesoft.Supabase.Unity.Config;

namespace Truesoft.Supabase.Samples
{
    /// <summary>
    /// SDK 핵심 흐름(게스트 로그인 + Save/Load)을 확인하는 간단 샘플입니다.
    /// 씬에 `SupabaseRuntime` 오브젝트가 있어야 합니다.
    /// </summary>
    public sealed class ExampleBootstrap : MonoBehaviour
    {
        [Header("Save/Load 예시값")]
        [SerializeField] private int level = 1;
        [SerializeField] private int coins = 100;

        [Header("실행")]
        [SerializeField] private bool runOnStart = true;

        private void Start()
        {
            if (!runOnStart)
                return;

            Run();
        }

        [ContextMenu("게스트 로그인 -> Save/Load 실행")]
        public void Run()
        {
            _ = RunAsync();
        }

        private async Task RunAsync()
        {
            EnsureRuntimeExists();
            await WaitForSdkInitializedAsync();
            if (!Supabase.IsInitialized)
                return;

            if (!Supabase.IsLoggedIn)
            {
                var guest = await Supabase.SignInAnonymouslyAsync();
                if (!guest.IsSuccess)
                {
                    Debug.LogError("[BasicSetup] 게스트 로그인 실패: " + guest.ErrorMessage);
                    return;
                }
            }

            var save = new BasicSaveData
            {
                level = level,
                coins = coins,
                updatedAtIso = DateTime.UtcNow.ToString("o")
            };

            var saveRes = await Supabase.SaveUserDataAsync(save);
            if (!saveRes.IsSuccess)
            {
                Debug.LogError("[BasicSetup] SaveUserData 실패: " + saveRes.ErrorMessage);
                return;
            }

            var loadRes = await Supabase.LoadUserDataAsync<BasicSaveData>();
            if (!loadRes.IsSuccess)
            {
                Debug.LogError("[BasicSetup] LoadUserData 실패: " + loadRes.ErrorMessage);
                return;
            }

            Debug.Log($"[BasicSetup] Load 결과: level={loadRes.Data.level}, coins={loadRes.Data.coins}");

            var evtRes = await Supabase.SendUserEventAsync("basic_setup_done");
            if (!evtRes.IsSuccess)
                Debug.LogWarning("[BasicSetup] SendUserEvent 실패: " + evtRes.ErrorMessage);
        }

        private static async Task WaitForSdkInitializedAsync()
        {
            const int timeoutMs = 10000;
            var start = DateTime.UtcNow;

            while (!Supabase.IsInitialized)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                {
                    Debug.LogError("[BasicSetup] SDK 초기화가 완료되지 않았습니다. 'Resources/SupabaseSettings.asset'이 존재하는지 확인하세요.");
                    return;
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

        [Serializable]
        public sealed class BasicSaveData
        {
            public int level;
            public int coins;
            public string updatedAtIso;
        }
    }
}