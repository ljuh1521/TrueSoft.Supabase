using System;
using System.Collections;
using Truesoft.Supabase.Unity;
using Truesoft.Supabase.Unity.Auth.Google;
using UnityEngine;

namespace Truesoft.Supabase.Unity.Config
{
    /// <summary>
    /// Supabase SDK의 "씬 실행 정책"을 제어하는 런타임 컴포넌트입니다.
    /// - 초기화 시점
    /// - 세션 자동 복원 여부
    /// - RemoteConfig 첫 로드/폴링 주기
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("TrueSoft/Supabase/Supabase Runtime")]
    public sealed class SupabaseRuntime : MonoBehaviour
    {
        private static SupabaseRuntime _instance;

        [Header("Configuration Source (설정값 소스)")]
        [Tooltip("프로젝트 공통 설정값 에셋. 비워두면 Resources/SupabaseSettings를 자동으로 찾습니다.")]
        [SerializeField] private SupabaseSettings settings;

        [Header("Scene Lifecycle Policy (씬 실행 정책)")]
        [Tooltip("체크 시 이 런타임 오브젝트를 DontDestroyOnLoad로 유지합니다.")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Tooltip("체크 시 시작 시점에 저장된 refresh_token으로 세션 복원을 시도합니다.")]
        [SerializeField] private bool restoreSessionOnStart = true;

        [Header("RemoteConfig Runtime Policy (런타임 정책)")]
        [Tooltip("RemoteConfig 런타임 동기화 루틴 사용 여부입니다.")]
        [SerializeField] private bool enableRemoteConfig = true;

        [Tooltip("체크 시 시작 시점에 RemoteConfig 전체를 1회 새로고침합니다.")]
        [SerializeField] private bool refreshAllOnStart = true;

        [Tooltip("RemoteConfig 폴링 주기(초). 0 이하이면 주기 폴링을 하지 않습니다.")]
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
                Debug.LogWarning(
                    "[Supabase] SupabaseSettings를 찾을 수 없습니다(인스펙터 미할당 또는 Resources 로드 실패).\n"
                    + SupabaseUnitySetupHelp.InitializationChecklistKo);
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
                var restoreTask = Supabase.TryRestoreSessionAsync();
                yield return new WaitUntil(() => restoreTask.IsCompleted);
            }

            if (!enableRemoteConfig)
                yield break;

            if (refreshAllOnStart)
            {
                var refreshTask = Supabase.TryRefreshRemoteConfigAsync();
                yield return new WaitUntil(() => refreshTask.IsCompleted);
            }

            if (pollIntervalSeconds <= 0f)
                yield break;

            while (true)
            {
                var pollTask = Supabase.TryPollRemoteConfigAsync();
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
