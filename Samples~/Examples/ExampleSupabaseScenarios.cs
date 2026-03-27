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

        [Tooltip("J: 탈퇴 요청(설정 유예일, 서버 계산)")]
        [SerializeField] private KeyCode keyRequestWithdrawal = KeyCode.J;

        [Tooltip("K: 내 탈퇴 게이트 상태 조회(닉네임/예약/남은 시간)")]
        [SerializeField] private KeyCode keyWithdrawalStatus = KeyCode.K;

        [Tooltip("C: 저장된(또는 로그인 시 발급한) 철회 토큰으로 탈퇴 예약 해제")]
        [SerializeField] private KeyCode keyWithdrawalCancel = KeyCode.C;

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
            else if (Input.GetKeyDown(keyRequestWithdrawal))
                _ = RunAsyncGuarded(RunWithdrawalRequestExampleAsync);
            else if (Input.GetKeyDown(keyWithdrawalStatus))
                _ = RunAsyncGuarded(RunWithdrawalStatusExampleAsync);
            else if (Input.GetKeyDown(keyWithdrawalCancel))
                _ = RunAsyncGuarded(RunWithdrawalCancelRedeemExampleAsync);
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

        [ContextMenu("Run Withdrawal Request Example")]
        public void RunWithdrawalRequestExample()
        {
            _ = RunWithdrawalRequestExampleAsync();
        }

        [ContextMenu("Run Withdrawal Status Example")]
        public void RunWithdrawalStatusExample()
        {
            _ = RunWithdrawalStatusExampleAsync();
        }

        [ContextMenu("Run Withdrawal Cancel Redeem Example")]
        public void RunWithdrawalCancelRedeemExample()
        {
            _ = RunWithdrawalCancelRedeemExampleAsync();
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
        /// 익명 계정은 <c>TrySignOutAsync</c>로 로그아웃해 동일 기기에서 다음 익명 로그인 시 같은 auth 계정으로 이어지게 할 수 있습니다.
        /// Android 네이티브 Google 계정까지 끊으려면 <c>TrySignOutFromGoogleAsync</c> 후 <c>ClearSession</c>을 호출하세요.
        /// </summary>
        private async Task<bool> RunLogoutExampleAsync()
        {
            if (!SupabaseClient.IsLoggedIn)
            {
                Debug.LogWarning("[Sample] logout example skipped: not signed in.");
                return false;
            }

            await SupabaseClient.TrySignOutAsync();
            Debug.Log("[Sample] logout example: TrySignOutAsync 완료 (익명이면 복구용 refresh upsert 후 refresh 삭제·user_sessions 정리).");
            return true;
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

        /// <summary>
        /// 설정된 유예 기간(<c>SupabaseSettings.withdrawalRequestDelayDays</c>)으로 탈퇴를 요청합니다.
        /// 실제 withdrawn_at 계산은 서버 RPC(<c>ts_request_withdrawal</c>)가 처리합니다.
        /// </summary>
        private async Task<bool> RunWithdrawalRequestExampleAsync()
        {
            if (!SupabaseClient.IsLoggedIn)
            {
                Debug.LogWarning("[Sample] withdrawal request skipped: sign in first.");
                return false;
            }

            var ok = await SupabaseClient.TryRequestMyWithdrawalAsync();
            Debug.Log(ok
                ? "[Sample] withdrawal request success. 서버가 유예 기간 기준으로 withdrawn_at을 예약했고, 앱은 즉시 로그아웃 처리했습니다(이후 수동 로그인 UX)."
                : "[Sample] withdrawal request failed. Sql/supabase_withdrawal_request.sql 적용 및 profiles/RLS를 확인하세요.");
            return ok;
        }

        private async Task<bool> RunWithdrawalStatusExampleAsync()
        {
            if (!SupabaseClient.IsLoggedIn)
            {
                var cached = SupabaseClient.GetStoredWithdrawalGateStatus();
                if (cached == null || string.IsNullOrWhiteSpace(cached.WithdrawnAtIso))
                {
                    Debug.LogWarning("[Sample] withdrawal status skipped: sign in first (or no cached gate status).");
                    return false;
                }

                Debug.Log($"[Sample] cached gate status. nickname={cached.Nickname}, withdrawn_at={cached.WithdrawnAtIso}, remain_sec={cached.SecondsRemaining}");
                return true;
            }

            var status = await SupabaseClient.TryGetMyWithdrawalStatusAsync();
            if (status == null)
            {
                Debug.LogWarning("[Sample] withdrawal status failed.");
                return false;
            }

            Debug.Log(
                $"[Sample] withdrawal status. nickname={status.Nickname}, is_scheduled={status.IsScheduled}, withdrawn_at={status.WithdrawnAtIso}, remain_sec={status.SecondsRemaining}, server_now={status.ServerNowIso}");
            return true;
        }

        private async Task<bool> RunWithdrawalCancelRedeemExampleAsync()
        {
            // B 방식 샘플:
            // • TryRequestMyWithdrawalAsync 직후에는 SDK가 로그아웃 전에 cancel_token을 저장하므로, 미로그인 상태에서도 Redeem 가능.
            // 1) 로그인 상태면 issue로 토큰 발급 후 세션 정리
            // 2) 저장된 토큰으로 redeem
            if (SupabaseClient.IsLoggedIn)
            {
                var token = await SupabaseClient.TryRequestWithdrawalCancelTokenAsync(defaultValue: null);
                if (string.IsNullOrWhiteSpace(token))
                {
                    Debug.LogWarning("[Sample] withdrawal cancel issue failed (예약 중 계정인지 확인).");
                    return false;
                }

                SupabaseClient.ClearSession();
                Debug.Log("[Sample] withdrawal cancel token issued, session cleared. proceeding redeem...");
            }

            var ok = await SupabaseClient.TryRedeemWithdrawalCancelAsync();
            Debug.Log(ok
                ? "[Sample] withdrawal cancel redeem success. now sign in again."
                : "[Sample] withdrawal cancel redeem failed. token missing/expired or server not deployed.");
            return ok;
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
            await RunWithdrawalRequestExampleAsync();
            await RunWithdrawalStatusExampleAsync();
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
