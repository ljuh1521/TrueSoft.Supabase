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
            // 한 줄 호출: 초기화/로그인 실패는 SDK가 로그를 남기고 false를 반환합니다.
            if (!await SupabaseClient.TrySignInAnonymouslyAsync())
                return;

            var save = new SaveData
            {
                level = level,
                coins = coins,
                updatedAtIso = DateTime.UtcNow.ToString("o")
            };

            if (!await SupabaseClient.TrySaveUserDataAsync(save))
                return;

            var loaded = await SupabaseClient.TryLoadUserDataAsync<SaveData>();
            if (loaded == null)
                return;

            _ = await SupabaseClient.TrySendUserEventAsync("basic_setup_done");
            Debug.Log($"[BasicSetup] done. level={loaded.level}, coins={loaded.coins}");
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
