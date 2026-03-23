using System;
using System.Collections;
using Truesoft.Supabase.Unity;
using Truesoft.Supabase.Unity.Auth.Google;
using UnityEngine;

namespace Truesoft.Supabase.Unity.Config
{
    /// <summary>
    /// Supabase SDK 통합 런타임 컴포넌트.
    /// - SDK 초기화
    /// - 세션 자동 복원
    /// - RemoteConfig 첫 로드 + 폴링
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("TrueSoft/Supabase/Supabase Runtime")]
    public sealed class SupabaseRuntime : MonoBehaviour
    {
        private static SupabaseRuntime _instance;

        [Header("Initialization")]
        [SerializeField] private SupabaseSettings settings;

        [Tooltip("체크 시 다른 씬으로 넘어가도 이 오브젝트가 유지됩니다.")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Tooltip("체크 시 플레이 시작 시 저장된 refresh_token으로 자동 복원을 시도합니다.")]
        [SerializeField] private bool restoreSessionOnStart = true;

        [Header("Remote Config")]
        [SerializeField] private bool enableRemoteConfig = true;

        [Tooltip("시작 시 RemoteConfig 전체를 1회 가져옵니다.")]
        [SerializeField] private bool refreshAllOnStart = true;

        [Tooltip("RemoteConfig 폴링 주기(초). 0 이하면 폴링하지 않습니다.")]
        [SerializeField] private float pollIntervalSeconds = 10f;

        private Coroutine _lifecycleRoutine;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[Supabase] Duplicate SupabaseRuntime detected. Destroying duplicate object.");
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (settings == null)
            {
                settings = Resources.Load<SupabaseSettings>("SupabaseSettings");
            }

            if (settings == null)
            {
                Debug.LogWarning("[Supabase] SupabaseSettings is not assigned.");
                return;
            }

            var bootstrap = new SupabaseUnityBootstrap();
            bootstrap.Initialize(settings);

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            EnsureGoogleLoginBridge();
        }

        private void OnEnable()
        {
            if (_lifecycleRoutine == null)
                _lifecycleRoutine = StartCoroutine(RunLifecycle());
        }

        private void OnDisable()
        {
            if (_lifecycleRoutine != null)
            {
                StopCoroutine(_lifecycleRoutine);
                _lifecycleRoutine = null;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private IEnumerator RunLifecycle()
        {
            while (!Supabase.IsInitialized)
                yield return null;

            if (restoreSessionOnStart)
            {
                var restoreTask = Supabase.RestoreSessionAsync();
                yield return new WaitUntil(() => restoreTask.IsCompleted);
            }

            if (!enableRemoteConfig)
                yield break;

            if (refreshAllOnStart)
            {
                var refreshTask = Supabase.RefreshRemoteConfigAsync();
                yield return new WaitUntil(() => refreshTask.IsCompleted);
            }

            if (pollIntervalSeconds <= 0f)
                yield break;

            while (true)
            {
                var pollTask = Supabase.PollRemoteConfigAsync();
                yield return new WaitUntil(() => pollTask.IsCompleted);
                yield return new WaitForSeconds(pollIntervalSeconds);
            }
        }

        private void EnsureGoogleLoginBridge()
        {
            // Unity scene에 별도 컴포넌트를 붙이지 않아도 Google 로그인 브릿지가 항상 존재하도록 보장합니다.
            var existing = FindFirstObjectByType<GoogleLoginBridge>();
            if (existing != null)
                return;

            var go = new GameObject("TruesoftGoogleLoginBridge");
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(go);

            go.AddComponent<GoogleLoginBridge>();
        }
    }
}
