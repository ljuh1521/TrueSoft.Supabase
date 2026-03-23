using System;
using System.Threading.Tasks;
using UnityEngine;
using Truesoft.Supabase.Unity;

namespace Truesoft.Supabase.Samples
{
    /// <summary>
    /// RemoteConfig 값을 코드에서 구독/파싱하는 예시입니다.
    /// (value_json은 엄격 모드로 객체 루트(JSON이 '{'로 시작)여야 합니다.)
    /// </summary>
    public sealed class ExampleProfileApi : MonoBehaviour
    {
        [Header("RemoteConfig 키")]
        [SerializeField] private string remoteKey = "game_balance";

        private Action<string> _handler;

        private async void Start()
        {
            _handler = OnRemoteConfigChanged;
            await WaitForSdkInitializedAsync();
            if (!Supabase.IsInitialized)
                return;

            Supabase.SubscribeRemoteConfig(remoteKey, _handler, invokeIfCached: true);
            Debug.Log($"[BasicSetup] RemoteConfig 구독 시작: {remoteKey}");
        }

        private async Task WaitForSdkInitializedAsync()
        {
            const int timeoutMs = 10000;
            var start = DateTime.UtcNow;

            while (!Supabase.IsInitialized)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                    break;

                await Task.Yield();
            }
        }

        private void OnRemoteConfigChanged(string json)
        {
            var cfg = JsonUtility.FromJson<BalanceConfig>(json);
            Debug.Log($"[BasicSetup] RemoteConfig 갱신: maxLevel={cfg.maxLevel}, rewardMultiplier={cfg.rewardMultiplier}");
        }

        private void OnDestroy()
        {
            if (!Supabase.IsInitialized)
                return;

            Supabase.UnsubscribeRemoteConfig(remoteKey, _handler);
        }

        [Serializable]
        private sealed class BalanceConfig
        {
            public int maxLevel;
            public float rewardMultiplier;
        }
    }
}