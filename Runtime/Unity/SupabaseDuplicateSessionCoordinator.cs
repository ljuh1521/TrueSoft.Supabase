using System;
using System.Collections;
using Truesoft.Supabase.Core.Auth;
using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// <c>user_sessions</c>의 <c>session_token</c>을 등록·폴링해 다른 기기에서 같은 계정으로 로그인했을 때 이 기기에서 세션을 끊습니다.
    /// </summary>
    internal sealed class SupabaseDuplicateSessionCoordinator : MonoBehaviour
    {
        private static SupabaseDuplicateSessionCoordinator _instance;
        private Coroutine _pollRoutine;
        private Coroutine _syncRoutine;

        internal static void EnsureExists()
        {
            if (_instance != null)
                return;

            var go = new GameObject("TruesoftSupabaseDuplicateSession");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SupabaseDuplicateSessionCoordinator>();
        }

        internal static void StopPolling()
        {
            if (_instance == null)
                return;

            if (_instance._pollRoutine != null)
            {
                _instance.StopCoroutine(_instance._pollRoutine);
                _instance._pollRoutine = null;
            }
        }

        internal static void ScheduleSyncAfterSessionChange(SupabaseSessionChangeKind kind)
        {
            EnsureExists();
            if (_instance._syncRoutine != null)
                _instance.StopCoroutine(_instance._syncRoutine);

            StopPolling();
            _instance._syncRoutine = _instance.StartCoroutine(_instance.RunSyncAfterChangeRoutine(kind));
        }

        private IEnumerator RunSyncAfterChangeRoutine(SupabaseSessionChangeKind kind)
        {
            try
            {
                if (SupabaseSDK.DuplicateSessionMonitorEnabled == false)
                    yield break;

                var svc = SupabaseSDK.UserSessionService;
                if (svc == null)
                    yield break;

                if (SupabaseSDK.IsLoggedIn == false || SupabaseSDK.Session == null || SupabaseSDK.Session.User == null)
                    yield break;

                var accountId = SupabaseSDK.Session.User.Id;
                var accessToken = SupabaseSDK.Session.AccessToken;
                if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(accessToken))
                    yield break;

                var getTask = svc.GetSessionTokenAsync(accessToken, accountId);
                yield return new WaitUntil(() => getTask.IsCompleted);

                var serverResult = getTask.Result;
                if (serverResult == null || !serverResult.IsSuccess)
                    yield break;

                var serverToken = serverResult.Data;

                if (kind == SupabaseSessionChangeKind.NewSignIn)
                {
                    var newToken = Guid.NewGuid().ToString("D");
                    var upsertTask = svc.UpsertSessionTokenAsync(accessToken, accountId, newToken);
                    yield return new WaitUntil(() => upsertTask.IsCompleted);
                    if (upsertTask.Result == null || !upsertTask.Result.IsSuccess)
                        yield break;

                    PlayerPrefs.SetString(SupabaseSDK.SessionTokenPlayerPrefsKeyPrefix + accountId, newToken);
                    PlayerPrefs.Save();
                    StartPollingIfNeeded(accountId);
                    yield break;
                }

                var localKey = SupabaseSDK.SessionTokenPlayerPrefsKeyPrefix + accountId;
                var localToken = PlayerPrefs.GetString(localKey, "");

                if (string.IsNullOrWhiteSpace(serverToken))
                {
                    if (string.IsNullOrWhiteSpace(localToken))
                    {
                        var fresh = Guid.NewGuid().ToString("D");
                        var t = svc.UpsertSessionTokenAsync(accessToken, accountId, fresh);
                        yield return new WaitUntil(() => t.IsCompleted);
                        if (t.Result == null || !t.Result.IsSuccess)
                            yield break;

                        PlayerPrefs.SetString(localKey, fresh);
                        PlayerPrefs.Save();
                    }
                    else
                    {
                        var t = svc.UpsertSessionTokenAsync(accessToken, accountId, localToken.Trim());
                        yield return new WaitUntil(() => t.IsCompleted);
                    }

                    StartPollingIfNeeded(accountId);
                    yield break;
                }

                if (string.IsNullOrWhiteSpace(localToken))
                {
                    PlayerPrefs.SetString(localKey, serverToken.Trim());
                    PlayerPrefs.Save();
                    StartPollingIfNeeded(accountId);
                    yield break;
                }

                if (!string.Equals(localToken.Trim(), serverToken.Trim(), StringComparison.Ordinal))
                {
                    SupabaseSDK.RaiseDuplicateLoginDetected();
                    yield break;
                }

                StartPollingIfNeeded(accountId);
            }
            finally
            {
                _syncRoutine = null;
            }
        }

        private void StartPollingIfNeeded(string accountId)
        {
            var interval = SupabaseSDK.DuplicateSessionPollSeconds;
            if (interval <= 0f)
                return;

            if (_pollRoutine != null)
            {
                StopCoroutine(_pollRoutine);
                _pollRoutine = null;
            }

            _pollRoutine = StartCoroutine(PollLoop(accountId, interval));
        }

        private IEnumerator PollLoop(string accountId, float intervalSeconds)
        {
            while (SupabaseSDK.IsLoggedIn && SupabaseSDK.Session != null)
            {
                yield return new WaitForSeconds(intervalSeconds);

                if (SupabaseSDK.DuplicateSessionMonitorEnabled == false)
                    yield break;

                var svc = SupabaseSDK.UserSessionService;
                if (svc == null)
                    yield break;

                if (SupabaseSDK.Session?.User == null)
                    yield break;

                var uid = SupabaseSDK.Session.User.Id;
                var token = SupabaseSDK.Session.AccessToken;
                if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(token))
                    yield break;

                var t = svc.GetSessionTokenAsync(token, uid);
                yield return new WaitUntil(() => t.IsCompleted);

                if (!t.Result.IsSuccess)
                    continue;

                var server = t.Result.Data;
                var key = SupabaseSDK.SessionTokenPlayerPrefsKeyPrefix + accountId;
                var local = PlayerPrefs.GetString(key, "");

                if (string.IsNullOrWhiteSpace(server))
                    continue;

                if (string.IsNullOrWhiteSpace(local))
                {
                    PlayerPrefs.SetString(key, server.Trim());
                    PlayerPrefs.Save();
                    continue;
                }

                if (!string.Equals(local.Trim(), server.Trim(), StringComparison.Ordinal))
                {
                    SupabaseSDK.RaiseDuplicateLoginDetected();
                    yield break;
                }
            }

            _pollRoutine = null;
        }
    }
}
