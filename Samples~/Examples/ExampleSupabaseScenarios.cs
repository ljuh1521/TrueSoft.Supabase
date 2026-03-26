using System;
using System.Threading.Tasks;
using UnityEngine;
using Truesoft.Supabase.Unity;

using SupabaseClient = global::Truesoft.Supabase.Unity.Supabase;

namespace Truesoft.SupabaseUnity.Samples
{
    /// <summary>
    /// 샘플: 서버 시각·로그인/데이터/RemoteConfig/Edge Function 예시를 각각 분리해 제공합니다.
    /// </summary>
    public sealed class ExampleSupabaseScenarios : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField] private bool runAllOnStart = false;

        [Header("Save Data")]
        [SerializeField] private int level = 1;
        [SerializeField] private int coins = 100;

        [Header("Remote Config")]
        [SerializeField] private string remoteConfigKey = "game_balance";

        [Header("Edge Function")]
        [SerializeField] private string functionName = "gacha";

        [Header("Public nickname (profiles 테이블 + RLS 필요)")]
        [SerializeField] private string demoNickname = "SamplePlayer";

        [Header("Duplicate login / Logout (user_sessions + Sql 참고)")]
        [Tooltip("켜면 OnEnable에서 OnDuplicateLoginDetected를 구독합니다. 다른 기기에서 같은 계정으로 로그인했을 때(이미 ClearSession 후) 호출됩니다.")]
        [SerializeField] private bool subscribeDuplicateLoginOnEnable = true;

        [Header("Keyboard Test (간단 키보드로 Try API 호출)")]
        [Tooltip("켜면 Update에서 키보드 입력(Q/W/E/R/T/Y 등)을 감지해 샘플 함수를 실행합니다.")]
        [SerializeField] private bool enableKeyboardTest = true;

        [Tooltip("Q: 익명 로그인 시도")]
        [SerializeField] private KeyCode keyLoginAnonymous = KeyCode.Q;

        [Tooltip("I: 구글 로그인(중복 로그인 테스트용)")]
        [SerializeField] private KeyCode keyLoginGoogle = KeyCode.I;

        [Tooltip("W: 로그아웃(ClearSession)")]
        [SerializeField] private KeyCode keyLogout = KeyCode.W;

        [Tooltip("O: 구글 로그아웃(TrySignOutFromGoogleAsync) + ClearSession")]
        [SerializeField] private KeyCode keyLogoutGoogle = KeyCode.O;

        [Tooltip("E: 공개 닉네임(demoNickname) 설정")]
        [SerializeField] private KeyCode keySetNickname = KeyCode.E;

        [Tooltip("R: 세이브/불러오기")]
        [SerializeField] private KeyCode keySaveLoad = KeyCode.R;

        [Tooltip("T: RemoteConfig refresh + 조회")]
        [SerializeField] private KeyCode keyRemoteConfig = KeyCode.T;

        [Tooltip("U: RemoteConfig on-demand 동기화(즉시 갱신 + 캐시 반영)")]
        [SerializeField] private KeyCode keyRemoteConfigOnDemand = KeyCode.U;

        [Tooltip("Y: Edge function 호출")]
        [SerializeField] private KeyCode keyInvokeFunction = KeyCode.Y;

        [Tooltip("L: 중복 로그인 테스트 방법 안내 출력")]
        [SerializeField] private KeyCode keyDuplicateLoginInfo = KeyCode.L;

        [Tooltip("H: 서버 시각(ts_server_now, Sql/supabase_server_time.sql). 로그인 불필요.")]
        [SerializeField] private KeyCode keyServerTime = KeyCode.H;

        private bool _keyboardBusy;

        private void OnEnable()
        {
            if (subscribeDuplicateLoginOnEnable)
                SupabaseClient.OnDuplicateLoginDetected += HandleDuplicateLoginDetected;
        }

        private void OnDisable()
        {
            SupabaseClient.OnDuplicateLoginDetected -= HandleDuplicateLoginDetected;
        }

        private void HandleDuplicateLoginDetected()
        {
            Debug.Log(
                "[Sample] OnDuplicateLoginDetected: 다른 기기에서 같은 계정으로 로그인했습니다. "
                + "이미 Supabase 세션은 정리되었으므로 로그인 화면으로 보내거나 팝업만 띄우면 됩니다.");
        }

        private void Start()
        {
            if (runAllOnStart)
                _ = RunAllExamplesAsync();
        }

        private void Update()
        {
            if (!enableKeyboardTest)
                return;

            if (_keyboardBusy)
                return;

            if (Input.GetKeyDown(keyLoginAnonymous))
                _ = RunAsyncGuarded(RunLoginExampleAsync);
            else if (Input.GetKeyDown(keyLoginGoogle))
                _ = RunAsyncGuarded(RunGoogleLoginExampleAsync);
            else if (Input.GetKeyDown(keyLogout))
                _ = RunAsyncGuarded(RunLogoutExampleAsync);
            else if (Input.GetKeyDown(keyLogoutGoogle))
                _ = RunAsyncGuarded(RunGoogleLogoutExampleAsync);
            else if (Input.GetKeyDown(keySetNickname))
                _ = RunAsyncGuarded(RunPublicNicknameExampleAsync);
            else if (Input.GetKeyDown(keySaveLoad))
                _ = RunAsyncGuarded(RunSaveLoadExampleAsync);
            else if (Input.GetKeyDown(keyRemoteConfig))
                _ = RunAsyncGuarded(RunRemoteConfigExampleAsync);
            else if (Input.GetKeyDown(keyRemoteConfigOnDemand))
                _ = RunAsyncGuarded(RunRemoteConfigOnDemandExampleAsync);
            else if (Input.GetKeyDown(keyInvokeFunction))
                _ = RunAsyncGuarded(RunFunctionExampleAsync);
            else if (Input.GetKeyDown(keyDuplicateLoginInfo))
                LogDuplicateLoginHowToTest();
            else if (Input.GetKeyDown(keyServerTime))
                _ = RunAsyncGuarded(RunServerTimeExampleAsync);
        }

        private async Task RunAsyncGuarded(Func<Task<bool>> body)
        {
            try
            {
                _keyboardBusy = true;
                var ok = await body();
                if (ok == false)
                    Debug.LogWarning("[Sample] Keyboard test failed (see previous logs).");
            }
            catch (Exception e)
            {
                Debug.LogError("[Sample] Keyboard test exception: " + e.Message);
            }
            finally
            {
                _keyboardBusy = false;
            }
        }

        private async Task RunAsyncGuarded(Func<Task> body)
        {
            try
            {
                _keyboardBusy = true;
                await body();
            }
            catch (Exception e)
            {
                Debug.LogError("[Sample] Keyboard test exception: " + e.Message);
            }
            finally
            {
                _keyboardBusy = false;
            }
        }

        [ContextMenu("Run All Examples")]
        public void RunAllExamples()
        {
            _ = RunAllExamplesAsync();
        }

        [ContextMenu("Run Login Example")]
        public void RunLoginExample()
        {
            _ = RunLoginExampleAsync();
        }

        [ContextMenu("Run Google Login Example")]
        public void RunGoogleLoginExample()
        {
            _ = RunGoogleLoginExampleAsync();
        }

        [ContextMenu("Run Save/Load Example")]
        public void RunSaveLoadExample()
        {
            _ = RunSaveLoadExampleAsync();
        }

        [ContextMenu("Run RemoteConfig Example")]
        public void RunRemoteConfigExample()
        {
            _ = RunRemoteConfigExampleAsync();
        }

        [ContextMenu("Run RemoteConfig On-Demand Example")]
        public void RunRemoteConfigOnDemandExample()
        {
            _ = RunRemoteConfigOnDemandExampleAsync();
        }

        [ContextMenu("Run Function Example")]
        public void RunFunctionExample()
        {
            _ = RunFunctionExampleAsync();
        }

        [ContextMenu("Run Public Nickname Example")]
        public void RunPublicNicknameExample()
        {
            _ = RunPublicNicknameExampleAsync();
        }

        [ContextMenu("Run Logout Example")]
        public void RunLogoutExample()
        {
            _ = RunLogoutExampleAsync();
        }

        [ContextMenu("Run Google Logout Example")]
        public void RunGoogleLogoutExample()
        {
            _ = RunGoogleLogoutExampleAsync();
        }

        [ContextMenu("Run Duplicate Login Info (Console)")]
        public void RunDuplicateLoginInfoExample()
        {
            LogDuplicateLoginHowToTest();
        }

        [ContextMenu("Run Server Time Example")]
        public void RunServerTimeExample()
        {
            _ = RunServerTimeExampleAsync();
        }

        private async Task<bool> RunLoginExampleAsync()
        {
            var ok = await SupabaseClient.TrySignInAnonymouslyAsync();
            Debug.Log(ok
                ? "[Sample] login example success."
                : "[Sample] login example failed.");
            return ok;
        }

        private async Task<bool> RunGoogleLoginExampleAsync()
        {
            var ok = await SupabaseClient.TrySignInWithGoogleAsync();
            Debug.Log(ok
                ? "[Sample] google login example success."
                : "[Sample] google login example failed.");
            return ok;
        }

        private async Task<bool> RunSaveLoadExampleAsync()
        {
            if (!SupabaseClient.IsLoggedIn)
            {
                Debug.LogWarning("[Sample] save/load example skipped: sign in first.");
                return false;
            }

            var save = new SaveData
            {
                level = level,
                coins = coins,
                updatedAtIso = DateTime.UtcNow.ToString("o")
            };

            if (!await SupabaseClient.TrySaveUserDataAsync(save))
            {
                Debug.LogWarning("[Sample] save/load example failed at save.");
                return false;
            }

            var loaded = await SupabaseClient.TryLoadUserDataAsync<SaveData>();
            if (loaded == null)
            {
                Debug.LogWarning("[Sample] save/load example failed at load.");
                return false;
            }

            Debug.Log($"[Sample] save/load example success. level={loaded.level}, coins={loaded.coins}");
            return true;
        }

        private async Task<bool> RunGoogleLogoutExampleAsync()
        {
            // Supabase 세션은 그대로이므로, 구글 네이티브까지 끊으려면 아래를 호출한 뒤 ClearSession도 같이 수행합니다.
            var ok = await SupabaseClient.TrySignOutFromGoogleAsync();
            SupabaseClient.ClearSession();
            Debug.Log("[Sample] google logout example: SignOutFromGoogle + ClearSession 완료. result=" + ok);
            return ok;
        }

        private async Task<bool> RunPublicNicknameExampleAsync()
        {
            if (!SupabaseClient.IsLoggedIn || SupabaseClient.Session?.User == null)
            {
                Debug.LogWarning("[Sample] nickname example skipped: sign in first.");
                return false;
            }

            var accountId = SupabaseClient.Session.User.Id;
            var playerUserId = SupabaseClient.Session.User.PlayerUserId;
            if (!await SupabaseClient.TryIsNicknameAvailableAsync(demoNickname, ignoreUserIdForSelf: accountId))
            {
                Debug.LogWarning("[Sample] nickname example: nickname already taken (or check failed).");
                return false;
            }

            if (!await SupabaseClient.TrySetMyNicknameAsync(demoNickname))
            {
                Debug.LogWarning("[Sample] nickname example failed at set (profiles 테이블·RLS·유니크 인덱스 확인).");
                return false;
            }

            var readBack = await SupabaseClient.TryGetPublicNicknameAsync(playerUserId, defaultValue: "");
            Debug.Log(readBack == demoNickname
                ? $"[Sample] nickname example success: '{readBack}'"
                : $"[Sample] nickname example: set ok but read '{readBack}' (expected '{demoNickname}').");
            return readBack == demoNickname;
        }

        /// <summary>
        /// Supabase 세션 로그아웃: 저장된 refresh_token 삭제, <c>user_sessions</c> 행 삭제(루트 SQL 적용 시).
        /// Android 네이티브 Google 계정까지 끊으려면 <c>TrySignOutFromGoogleAsync</c> 후 <c>ClearSession</c>을 호출하세요.
        /// </summary>
        private Task<bool> RunLogoutExampleAsync()
        {
            if (!SupabaseClient.IsLoggedIn)
            {
                Debug.LogWarning("[Sample] logout example skipped: not signed in.");
                return Task.FromResult(false);
            }

            SupabaseClient.ClearSession();
            Debug.Log("[Sample] logout example: ClearSession 완료 (refresh 삭제·로컬 세션 토큰 삭제, user_sessions는 deleteUserSessionRow 기본값으로 서버 행 삭제).");
            return Task.FromResult(true);
        }

        private static void LogDuplicateLoginHowToTest()
        {
            Debug.Log(
                "[Sample] 중복 로그인 테스트: Sql/supabase_player_tables.sql의 user_sessions 적용 후, "
                + "SupabaseSettings에서 enableDuplicateSessionMonitor를 켠 뒤 "
                + "기기 A·B(또는 에뮬+실기)에서 같은 계정(익명 또는 구글)으로 순서대로 로그인하면, "
                + "먼저 켜 둔 쪽에서 OnDuplicateLoginDetected가 호출됩니다.");
        }

        /// <summary>
        /// RPC <c>ts_server_now</c>로 DB 서버 시각을 가져옵니다. 로그인 세션 없이 호출 가능합니다.
        /// </summary>
        private async Task<bool> RunServerTimeExampleAsync()
        {
            if (!await SupabaseClient.EnsureInitializedAsync())
            {
                Debug.LogWarning("[Sample] server time skipped: SDK not initialized.");
                return false;
            }

            var r = await SupabaseClient.GetServerUtcNowAsync();
            if (r == null || !r.IsSuccess)
            {
                Debug.LogWarning("[Sample] server time failed: " + (r?.ErrorMessage ?? "null")
                    + " (Sql/supabase_server_time.sql 적용 여부 확인)");
                return false;
            }

            Debug.Log("[Sample] server time (UTC): " + r.Data.ToString("o"));
            return true;
        }

        private async Task<bool> RunRemoteConfigExampleAsync()
        {
            if (!await SupabaseClient.TryRefreshRemoteConfigAsync())
            {
                Debug.LogWarning("[Sample] remote config example failed at refresh.");
                return false;
            }

            _ = await SupabaseClient.TryGetRemoteConfigAsync<object>(remoteConfigKey, defaultValue: null);
            SupabaseClient.TryGetRemoteConfigRaw(remoteConfigKey, out var raw);
            Debug.Log("[Sample] remote config raw: " + raw);
            return true;
        }

        private async Task<bool> RunRemoteConfigOnDemandExampleAsync()
        {
            if (!await SupabaseClient.RefreshRemoteConfigOnDemandAsync())
            {
                Debug.LogWarning("[Sample] remote config on-demand example failed.");
                return false;
            }

            // on-demand로 서버 값을 캐시에 반영한 뒤에는, raw를 바로 읽어오는 편이 네트워크 호출을 줄입니다.
            var has = SupabaseClient.TryGetRemoteConfigRaw(remoteConfigKey, out var raw);
            Debug.Log(has
                ? "[Sample] remote config on-demand raw: " + raw
                : "[Sample] remote config on-demand raw: (null)");
            return has;
        }

        private async Task<bool> RunFunctionExampleAsync()
        {
            var result = await SupabaseClient.TryInvokeFunctionAsync<object>(
                functionName,
                new { bannerId = "asd", drawCount = 4, seed = 15 },
                defaultValue: null);

            var ok = result != null;
            Debug.Log(ok
                ? "[Sample] function example success."
                : "[Sample] function example failed.");
            return ok;
        }

        private async Task RunAllExamplesAsync()
        {
            _ = await SupabaseClient.TryStartAsync(restoreSessionFirst: true, refreshRemoteConfigOnStart: false);

            await RunServerTimeExampleAsync();
            await RunLoginExampleAsync();
            await RunSaveLoadExampleAsync();
            await RunPublicNicknameExampleAsync();
            await RunRemoteConfigExampleAsync();
            await RunFunctionExampleAsync();

            Debug.Log("[Sample] all examples finished.");
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
