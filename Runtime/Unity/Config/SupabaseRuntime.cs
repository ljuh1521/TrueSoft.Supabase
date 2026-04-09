using System;
using System.Collections;
using System.Collections.Generic;
using Truesoft.Supabase.Unity;
using Truesoft.Supabase.Unity.Auth.Google;
using UnityEngine;
using UnityEngine.Serialization;

namespace Truesoft.Supabase.Unity.Config
{
    /// <summary>
    /// Supabase SDK의 "씬 실행 정책"을 제어하는 런타임 컴포넌트입니다.
    /// - 초기화 시점
    /// - 앱 시작 자동 로그인 시도
    /// - RemoteConfig: Cold Start(시작 시 fetch 없음), 카테고리별 백그라운드 폴링
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
        [Tooltip("런타임 동기화 사용. Cold Start: 시작 시 RemoteConfig를 가져오지 않습니다.")]
        [SerializeField] private bool enableRemoteConfig = true;

        [FormerlySerializedAs("pollIntervalSeconds")]
        [Tooltip("TryRefreshRemoteConfigAsync / RefreshRemoteConfigOnDemandAsync 호출 후 카테고리 폴링 시각을 이 시간(초)만큼 뒤로 미룹니다. 0 이하면 SDK에서 60초로 처리합니다.")]
        [SerializeField] private float remoteConfigOnDemandPushbackSeconds = 60f;

        [Tooltip("카테고리별 폴링 주기 오버라이드. 비우면 DB remote_config.poll_interval_seconds만 사용.")]
        [SerializeField] private List<RemoteConfigCategoryPollOverrideEntry> remoteConfigCategoryPollOverrides = new List<RemoteConfigCategoryPollOverrideEntry>();

        [Header("UserSave 자동 저장")]
        [Tooltip("정적 세이브 자동 동기화 사용.")]
        [SerializeField] private bool enableUserSaveAutoSync = true;

        [Tooltip("자동 저장 쿨타임(초).")]
        [SerializeField] private float userSaveAutoSyncCooldownSeconds = 1f;

        private Coroutine _lifecycleRoutine;
        private bool _remoteConfigPollSettingsApplied;

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

            Supabase.ConfigureUserSaveAutoSyncCooldown(userSaveAutoSyncCooldownSeconds);

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

        private void Update()
        {
            if (enableUserSaveAutoSync && Supabase.IsInitialized)
                SupabaseSDK.TickUserSaveAutoSync(Time.realtimeSinceStartup);

            if (!enableRemoteConfig || !Supabase.IsInitialized)
                return;

            EnsureRemoteConfigPollSettingsApplied();
            SupabaseSDK.TickRemoteConfigCategoryPolls(Time.realtimeSinceStartup);
        }

        private void EnsureRemoteConfigPollSettingsApplied()
        {
            if (_remoteConfigPollSettingsApplied)
                return;

            _remoteConfigPollSettingsApplied = true;
            var pushback = remoteConfigOnDemandPushbackSeconds <= 0f ? 60f : remoteConfigOnDemandPushbackSeconds;
            SupabaseSDK.UpdateRemoteConfigPollIntervalSeconds(pushback);
            SupabaseSDK.ApplyRemoteConfigCategoryPollOverrides(remoteConfigCategoryPollOverrides);
        }

        private void OnApplicationPause(bool pause)
        {
            if (!pause || !enableUserSaveAutoSync)
                return;

            Supabase.RequestImmediateUserSaveStaticFlushAll();
        }

        private void OnApplicationQuit()
        {
            if (!enableUserSaveAutoSync)
                return;

            Supabase.RequestImmediateUserSaveStaticFlushAll();
        }

        private IEnumerator RunLifecycle()
        {
            while (!Supabase.IsInitialized)
                yield return null;

            var autoLoginTask = Supabase.TryAutoLoginOnStartAsync();
            yield return new WaitUntil(() => autoLoginTask.IsCompleted);

            // RemoteConfig: Cold Start — 시작 시 fetch 없음. 폴링은 Update에서 TickRemoteConfigCategoryPolls.
        }

        private void EnsureGoogleLoginBridge()
        {
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
