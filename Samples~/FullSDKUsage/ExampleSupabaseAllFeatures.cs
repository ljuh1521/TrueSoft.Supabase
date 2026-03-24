using System.Threading.Tasks;
using UnityEngine;
using Truesoft.Supabase.Unity;

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
            // 원라인 시작 준비: 초기화 + (선택)세션복원 + 자동익명로그인 + RemoteConfig 새로고침
            if (!await SupabaseClient.StartAsync(restoreSessionFirst: true, autoSignInIfNeeded: true, refreshRemoteConfigOnStart: true))
            {
                Debug.LogError("[FullSDKUsage] SDK start failed.");
                return;
            }

            _ = await SupabaseClient.SendUserEventAsync("full_sample_started");
            _ = await SupabaseClient.GetRemoteConfigAsync<object>(remoteConfigKey, defaultValue: null);
            SupabaseClient.TryGetRemoteConfigRaw(remoteConfigKey, out var raw);
            Debug.Log("[FullSDKUsage] RemoteConfig raw: " + raw);

            var fn = await SupabaseClient.InvokeFunctionAsync<object>(functionName, new { ping = true });
            if (!fn.IsSuccess)
                Debug.LogWarning("[FullSDKUsage] Function failed: " + fn.ErrorMessage);

            Debug.Log("[FullSDKUsage] done.");
        }
    }
}
