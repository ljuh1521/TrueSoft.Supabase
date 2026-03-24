using System;
using System.Threading.Tasks;
using UnityEngine;
using Truesoft.Supabase.Unity;

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
            // 한 줄 호출: SDK가 초기화 대기/부트스트랩/로그인 상태 처리를 내부에서 수행합니다.
            var signIn = await SupabaseClient.SignInAnonymouslyAsync();
            if (!signIn.IsSuccess)
            {
                Debug.LogError("[BasicSetup] Guest sign-in failed: " + signIn.ErrorMessage);
                return;
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

        [Serializable]
        private sealed class SaveData
        {
            public int level;
            public int coins;
            public string updatedAtIso;
        }
    }
}
