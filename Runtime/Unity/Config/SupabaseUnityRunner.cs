using UnityEngine;

namespace Truesoft.Supabase.Unity.Config
{
    /// <summary>
    /// 씬에 하나 배치하여 Supabase SDK를 자동으로 초기화합니다.
    /// Inspector에서 Supabase Settings 에셋을 할당하면 Awake 시점에 초기화됩니다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SupabaseUnityRunner : MonoBehaviour
    {
        [SerializeField] private SupabaseSettings settings;

        [Tooltip("체크 시 다른 씬으로 넘어가도 이 오브젝트가 유지됩니다.")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        private void Awake()
        {
            if (settings == null)
            {
                Debug.LogWarning("[Supabase] SupabaseSettings가 할당되지 않았습니다. Inspector에서 TrueSoft > Supabase Settings 에셋을 지정하세요.");
                return;
            }

            var bootstrap = new SupabaseUnityBootstrap();
            bootstrap.Initialize(settings);

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }
    }
}
