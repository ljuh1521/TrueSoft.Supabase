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
    /// - 앱 시작 자동 로그인 시도
    /// - RemoteConfig 첫 로드/폴링 주기
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("TrueSoft/Supabase/Supabase 런타임")]
    public sealed class SupabaseRuntime : MonoBehaviour
    {
        private static SupabaseRuntime _instance;

        [Header("설정")]
        [Tooltip("SupabaseSettings. 비우면 Resources에서 로드.")]
        [SerializeField] private SupabaseSettings settings;

        [Header("씬")]
        [Tooltip("DontDestroyOnLoad로 유지.")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("RemoteConfig")]
        [Tooltip("런타임 동기화 사용.")]
        [SerializeField] private bool enableRemoteConfig = true;

        [Tooltip("시작 시 전체 1회 새로고침.")]
        [SerializeField] private bool refreshAllOnStart = true;

        [Tooltip("폴링 주기(초). 0 이하면 폴링 안 함.")]
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

            // 자동 로그인 정책:
            // - 이전 계정이 로그아웃 상태거나(refresh_token 제거 + 자동로그인 차단 플래그)
            // - 이전 계정 정보(refresh token)가 없으면
            // => 아무 동작도 하지 않습니다.
            var autoLoginTask = Supabase.TryAutoLoginOnStartAsync();
            yield return new WaitUntil(() => autoLoginTask.IsCompleted);

            if (!enableRemoteConfig)
                yield break;

            if (refreshAllOnStart)
            {
                var refreshTask = Supabase.TryRefreshRemoteConfigAsync();
                yield return new WaitUntil(() => refreshTask.IsCompleted);
            }

            if (pollIntervalSeconds <= 0f)
                yield break;

            // 주기 동기화 스케줄을 등록하고, 온디맨드 호출이 있으면 next poll 시점을 뒤로 미룹니다.
            SupabaseSDK.UpdateRemoteConfigPollIntervalSeconds(pollIntervalSeconds);
            SupabaseSDK.ForceRemoteConfigNextPollAt(Time.realtimeSinceStartup);

            while (true)
            {
                while (Time.realtimeSinceStartup < SupabaseSDK.RemoteConfigNextPollAtRealtime)
                    yield return null;

                var pollTask = Supabase.TryPollRemoteConfigAsync();
                yield return new WaitUntil(() => pollTask.IsCompleted);

                // 다음 폴링 시점을 설정합니다(더 늦어진 스케줄이 있으면 보존).
                SupabaseSDK.ScheduleRemoteConfigNextPollAt(Time.realtimeSinceStartup + pollIntervalSeconds);
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
