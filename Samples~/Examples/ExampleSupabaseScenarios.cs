using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Truesoft.Supabase.Unity;

using SupabaseClient = global::Truesoft.Supabase.Unity.Supabase;

namespace Truesoft.SupabaseUnity.Samples
{
    /// <summary>
    /// 샘플: 서버 시각·로그인/데이터·서버 샤드(조회/이주)·RemoteConfig/Edge Function 예시를 각각 분리해 제공합니다.
    /// </summary>
    public sealed class ExampleSupabaseScenarios : MonoBehaviour
    {
        [Header("실행")]
        [SerializeField] private bool runAllOnStart = false;

        [Header("세이브 데모")]
        [SerializeField] private int level = 1;
        [SerializeField] private int coins = 100;

        [Header("RemoteConfig")]
        [SerializeField] private string remoteConfigKey = "game_balance";

        [Header("Edge")]
        [SerializeField] private string functionName = "gacha";

        [Header("표시 이름")]
        [SerializeField] private string demoDisplayName = "SamplePlayer";

        [Header("서버 샤드")]
        [Tooltip("이주 목표 server_code.")]
        [SerializeField] private string serverShardTransferTargetCode = "GLOBAL";

        [Tooltip("시작 시 이주까지 시도.")]
        [SerializeField] private bool serverShardAttemptTransfer = false;

        [Tooltip("시작 시 로컬 서버 코드 덮어쓰기. 비우면 유지.")]
        [SerializeField] private string serverShardOptionalSetLocalCode = "";

        [Header("중복 로그인")]
        [Tooltip("OnDuplicateLoginDetected 구독.")]
        [SerializeField] private bool subscribeDuplicateLoginOnEnable = true;

        [Header("키보드 테스트")]
        [Tooltip("키 입력으로 샘플 API 호출.")]
        [SerializeField] private bool enableKeyboardTest = true;

        [Tooltip("익명 로그인")]
        [SerializeField] private KeyCode keyLoginAnonymous = KeyCode.Q;

        [Tooltip("구글 로그인")]
        [SerializeField] private KeyCode keyLoginGoogle = KeyCode.I;

        [Tooltip("구글 연동")]
        [SerializeField] private KeyCode keyLinkGoogle = KeyCode.P;

        [Tooltip("통합 로그아웃")]
        [SerializeField] private KeyCode keyLogout = KeyCode.W;

        [Tooltip("공개 displayName")]
        [SerializeField] private KeyCode keySetDisplayName = KeyCode.E;

        [Tooltip("세이브·로드 (정적 API·생성기 스타일)")]
        [SerializeField] private KeyCode keySaveLoad = KeyCode.R;

        [Tooltip("RemoteConfig 새로고침·조회")]
        [SerializeField] private KeyCode keyRemoteConfig = KeyCode.T;

        [Tooltip("RemoteConfig 즉시 동기화")]
        [SerializeField] private KeyCode keyRemoteConfigOnDemand = KeyCode.U;

        [Tooltip("Edge 함수 호출")]
        [SerializeField] private KeyCode keyInvokeFunction = KeyCode.Y;

        [Tooltip("중복 로그인 안내 로그")]
        [SerializeField] private KeyCode keyDuplicateLoginInfo = KeyCode.L;

        [Tooltip("서버 시각")]
        [SerializeField] private KeyCode keyServerTime = KeyCode.H;

        [Tooltip("탈퇴 요청")]
        [SerializeField] private KeyCode keyRequestWithdrawal = KeyCode.J;

        [Tooltip("탈퇴 상태 조회")]
        [SerializeField] private KeyCode keyWithdrawalStatus = KeyCode.K;

        [Tooltip("탈퇴 예약 취소")]
        [SerializeField] private KeyCode keyWithdrawalCancel = KeyCode.C;

        [Tooltip("서버 샤드 조회·이주")]
        [SerializeField] private KeyCode keyServerShard = KeyCode.N;

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
            else if (Input.GetKeyDown(keyLinkGoogle))
                _ = RunAsyncGuarded(RunGoogleLinkExampleAsync);
            else if (Input.GetKeyDown(keyLogout))
                _ = RunAsyncGuarded(RunLogoutExampleAsync);
            else if (Input.GetKeyDown(keySetDisplayName))
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
            else if (Input.GetKeyDown(keyServerShard))
                _ = RunAsyncGuarded(RunServerShardExampleAsync);
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

        [ContextMenu("Run Google Link Example")]
        public void RunGoogleLinkExample()
        {
            _ = RunGoogleLinkExampleAsync();
        }

        [ContextMenu("Run Save/Load Example (static user save)")]
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

        [ContextMenu("Run Public DisplayName Example")]
        public void RunPublicNicknameExample()
        {
            _ = RunPublicNicknameExampleAsync();
        }

        [ContextMenu("Run Logout Example")]
        public void RunLogoutExample()
        {
            _ = RunLogoutExampleAsync();
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

        [ContextMenu("Run Server Shard Example (my server + optional transfer)")]
        public void RunServerShardExample()
        {
            _ = RunServerShardExampleAsync();
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

        private async Task<bool> RunGoogleLinkExampleAsync()
        {
            if (!SupabaseClient.IsLoggedIn || SupabaseClient.Session?.User == null || !SupabaseClient.Session.User.IsAnonymous)
            {
                Debug.LogWarning("[Sample] google link example skipped: anonymous session required.");
                return false;
            }

            var beforeId = SupabaseClient.Session.User.Id;
            var ok = await SupabaseClient.TryLinkGoogleToCurrentAnonymousAsync();
            if (!ok || !SupabaseClient.IsLoggedIn || SupabaseClient.Session?.User == null)
            {
                Debug.LogWarning("[Sample] google link example failed (이미 사용 중인 Google이면 연동 불가).");
                return false;
            }

            var after = SupabaseClient.Session.User;
            var sameId = string.Equals(beforeId, after.Id, StringComparison.OrdinalIgnoreCase);
            var converted = !after.IsAnonymous;
            Debug.Log(
                "[Sample] google link example result. "
                + $"same_auth_user_id={sameId}, is_anonymous_after={after.IsAnonymous}");
            return sameId && converted;
        }

        private async Task<bool> RunSaveLoadExampleAsync()
        {
            if (!SupabaseClient.IsLoggedIn)
            {
                Debug.LogWarning("[Sample] save/load example skipped: sign in first.");
                return false;
            }

            // user_saves: level int, coins int, updated_at timestamptz — OpenAPI 생성기 static 출력과 동일 패턴은 SampleStaticUserSave 참고.
            var initialLoadOk = await SampleStaticUserSave.TryLoadAsync();
            if (!initialLoadOk)
                Debug.Log("[Sample] static save: 첫 로드 실패·무응답(행 없음)일 수 있음. 이후 PATCH로 행이 생기면 정상입니다.");

            SampleStaticUserSave.Level = level;
            SampleStaticUserSave.Coins = coins;

            if (!await SampleStaticUserSave.TryFlushNowAsync())
            {
                Debug.LogWarning("[Sample] save/load example failed at TryFlushNowAsync (정적 세이브 즉시 전송).");
                return false;
            }

            if (!await SampleStaticUserSave.TryLoadAsync())
            {
                Debug.LogWarning("[Sample] save/load example failed at reload after flush.");
                return false;
            }

            Debug.Log(
                $"[Sample] save/load (static) success. level={SampleStaticUserSave.Level}, coins={SampleStaticUserSave.Coins}, updated_at={SampleStaticUserSave.UpdatedAt}");
            return true;
        }

        private async Task<bool> RunPublicNicknameExampleAsync()
        {
            if (!SupabaseClient.IsLoggedIn || SupabaseClient.Session?.User == null)
            {
                Debug.LogWarning("[Sample] displayName example skipped: sign in first.");
                return false;
            }

            var accountId = SupabaseClient.Session.User.Id;
            var playerUserId = SupabaseClient.Session.User.PlayerUserId;
            if (!await SupabaseClient.TryIsDisplayNameAvailableAsync(demoDisplayName))
            {
                Debug.LogWarning("[Sample] displayName example: name already taken (or check failed).");
                return false;
            }

            if (!await SupabaseClient.TrySetMyDisplayNameAsync(demoDisplayName))
            {
                Debug.LogWarning("[Sample] displayName example failed at set (display_names 테이블·RLS·유니크 인덱스·Edge Functions 배포 확인).");
                return false;
            }

            var readBack = await SupabaseClient.TryGetPublicDisplayNameAsync(playerUserId, defaultValue: "");
            Debug.Log(readBack == demoDisplayName
                ? $"[Sample] displayName example success: '{readBack}'"
                : $"[Sample] displayName example: set ok but read '{readBack}' (expected '{demoDisplayName}').");
            return readBack == demoDisplayName;
        }

        /// <summary>
        /// 통합 로그아웃: Android에서는 <c>TrySignOutFullyAsync</c>가 Google 네이티브 로그아웃을 시도한 뒤 Supabase <c>SignOutAsync</c>와 동일하게 처리합니다.
        /// 익명이면 로컬 refresh 삭제 전 복구용 upsert가 수행됩니다.
        /// </summary>
        private async Task<bool> RunLogoutExampleAsync()
        {
            if (!SupabaseClient.IsLoggedIn)
            {
                Debug.LogWarning("[Sample] logout example skipped: not signed in.");
                return false;
            }

            await SupabaseClient.TrySignOutFullyAsync();
            Debug.Log("[Sample] logout example: TrySignOutFullyAsync 완료.");
            return true;
        }

        private static void LogDuplicateLoginHowToTest()
        {
            Debug.Log(
                "[Sample] 중복 로그인 테스트: Sql/player/05_user_sessions.sql 적용 후, "
                + "SupabaseSettings에서 enableDuplicateSessionMonitor를 켠 뒤 "
                + "기기 A·B(또는 에뮬+실기)에서 같은 계정(익명 또는 구글)으로 순서대로 로그인하면, "
                + "먼저 켜 둔 쪽에서 OnDuplicateLoginDetected가 호출됩니다.");
        }

        /// <summary>
        /// 로컬 <see cref="SupabaseClient.GetCurrentServerCode"/>와 RPC <c>ts_my_server_id</c> 결과를 비교하고,
        /// 인스펙터에서 허용한 경우 <c>ts_transfer_my_server</c>(<see cref="SupabaseClient.TryTransferMyServerAsync"/>)를 호출합니다.
        /// Retool·Secret 키 이주는 README의 <c>ts_admin_transfer_user_server</c>를 참고하세요.
        /// </summary>
        private async Task<bool> RunServerShardExampleAsync()
        {
            if (!await SupabaseClient.EnsureInitializedAsync())
            {
                Debug.LogWarning("[Sample] server shard skipped: SDK not initialized.");
                return false;
            }

            if (!SupabaseClient.IsLoggedIn)
            {
                Debug.LogWarning("[Sample] server shard skipped: sign in first (anonymous or Google).");
                return false;
            }

            if (string.IsNullOrWhiteSpace(serverShardOptionalSetLocalCode) == false)
            {
                SupabaseClient.SetCurrentServerCode(serverShardOptionalSetLocalCode.Trim());
                Debug.Log("[Sample] server shard: applied local server code from inspector: " + serverShardOptionalSetLocalCode.Trim());
            }

            var localCode = SupabaseClient.GetCurrentServerCode();
            var db = await SupabaseClient.GetMyServerInfoAsync();
            if (db == null || !db.IsSuccess)
            {
                var hint = string.Equals(db?.ErrorMessage, "my_server_not_found", StringComparison.Ordinal)
                    ? "profiles에 account_id=본인 행이 없을 때 흔함. TryStartAsync(restoreSessionFirst:true)로 복원하면 SDK가 프로필 upsert를 수행합니다. Q로 익명 로그인해도 됩니다. Console의 [Supabase] ensure profile row failed 유무·RLS를 확인하세요."
                    : "Sql/player/08_transfer_server.sql 등 적용·ts_my_server_id·로그인·프로필 행 확인.";
                Debug.LogWarning("[Sample] server shard: ts_my_server_id failed — " + (db?.ErrorMessage ?? "null") + ". " + hint);
                return false;
            }

            Debug.Log(
                "[Sample] server shard: local_selected_code=" + localCode
                + ", db_server_code=" + db.Data.ServerCode
                + ", db_server_id=" + db.Data.ServerId);

            if (!serverShardAttemptTransfer)
            {
                Debug.Log("[Sample] server shard: transfer skipped (enable Server Shard Attempt Transfer in inspector to call TryTransferMyServerAsync).");
                return true;
            }

            var target = serverShardTransferTargetCode?.Trim();
            if (string.IsNullOrEmpty(target))
            {
                Debug.LogWarning("[Sample] server shard: transfer skipped — serverShardTransferTargetCode is empty.");
                return false;
            }

            var moved = await SupabaseClient.TryTransferMyServerAsync(target, "sample_ExampleSupabaseScenarios");
            Debug.Log(moved
                ? "[Sample] server shard: TryTransferMyServerAsync ok. local prefs updated to target on success."
                : "[Sample] server shard: TryTransferMyServerAsync failed (target missing, allow_transfers=false, or display_name_taken_in_target_server 등).");
            return moved;
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

                Debug.Log($"[Sample] cached gate status. displayName={cached.DisplayName}, withdrawn_at={cached.WithdrawnAtIso}, remain_sec={cached.SecondsRemaining}");
                return true;
            }

            var status = await SupabaseClient.TryGetMyWithdrawalStatusAsync();
            if (status == null)
            {
                Debug.LogWarning("[Sample] withdrawal status failed.");
                return false;
            }

            Debug.Log(
                $"[Sample] withdrawal status. displayName={status.DisplayName}, is_scheduled={status.IsScheduled}, withdrawn_at={status.WithdrawnAtIso}, remain_sec={status.SecondsRemaining}, server_now={status.ServerNowIso}");
            return true;
        }

        private async Task<bool> RunWithdrawalCancelRedeemExampleAsync()
        {
            // B 방식 샘플:
            // • cancel_token은 탈퇴 예약 계정으로 로그인할 때 게이트에서 발급·저장됨(신청한 기기에만 묶이지 않음).
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
    }
}
