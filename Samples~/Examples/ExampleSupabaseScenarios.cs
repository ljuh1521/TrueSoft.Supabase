using System;
using System.Threading.Tasks;
using UnityEngine;
using Truesoft.Supabase.Unity;

using SupabaseClient = global::Truesoft.Supabase.Unity.Supabase;

namespace Truesoft.SupabaseUnity.Samples
{
    /// <summary>
    /// 샘플: 로그인/데이터/RemoteConfig/Edge Function 예시를 각각 분리해 제공합니다.
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

        [ContextMenu("Run Duplicate Login Info (Console)")]
        public void RunDuplicateLoginInfoExample()
        {
            LogDuplicateLoginHowToTest();
        }

        private async Task<bool> RunLoginExampleAsync()
        {
            var ok = await SupabaseClient.TrySignInAnonymouslyAsync();
            Debug.Log(ok
                ? "[Sample] login example success."
                : "[Sample] login example failed.");
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
                + "기기 A·B(또는 에뮬+실기)에서 같은 계정으로 순서대로 로그인하면, "
                + "먼저 켜 둔 쪽에서 OnDuplicateLoginDetected가 호출됩니다.");
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

        private async Task<bool> RunFunctionExampleAsync()
        {
            var result = await SupabaseClient.TryInvokeFunctionAsync<object>(
                functionName,
                new { ping = true },
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
