using System.Collections;
using Truesoft.Supabase.Unity;
using UnityEngine;

namespace Truesoft.Supabase.Unity.Config
{
    /// <summary>
    /// RemoteConfig를 주기적으로 폴링하여 변경점을 감지/적용합니다.
    /// 씬에 하나 배치하거나 SupabaseUnityRunner와 같은 오브젝트에 추가해서 사용하세요.
    /// </summary>
    public sealed class SupabaseRemoteConfigRunner : MonoBehaviour
    {
        [Tooltip("시작 시 전체 RemoteConfig를 1회 로드합니다.")]
        [SerializeField] private bool refreshAllOnStart = true;

        [Tooltip("폴링 주기(초). 0 이하면 폴링하지 않습니다.")]
        [SerializeField] private float pollIntervalSeconds = 10f;

        private Coroutine _routine;

        private void OnEnable()
        {
            if (_routine == null)
                _routine = StartCoroutine(Run());
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Supabase.RefreshRemoteConfigAsync();
            }
        }

        private IEnumerator Run()
        {
            // SDK 초기화 대기
            while (!Supabase.IsInitialized)
                yield return null;

            if (refreshAllOnStart)
            {
                var t = Supabase.RefreshRemoteConfigAsync();
                yield return new WaitUntil(() => t.IsCompleted);
            }

            if (pollIntervalSeconds <= 0f)
                yield break;

            while (true)
            {
                var t = Supabase.PollRemoteConfigAsync();
                yield return new WaitUntil(() => t.IsCompleted);
                yield return new WaitForSeconds(pollIntervalSeconds);
            }
        }
    }
}

