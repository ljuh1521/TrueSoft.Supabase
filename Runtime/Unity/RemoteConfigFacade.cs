using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Data;
using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// RemoteConfig 캐시 + 조회 API.
    /// 서버(remote_config 테이블) 변경을 PollAsync로 주기적으로 가져와 적용할 수 있습니다.
    /// </summary>
    public sealed class RemoteConfigFacade
    {
        private readonly SupabaseRemoteConfigService _service;
        private readonly Func<string> _accessTokenGetter;
        private readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
        private readonly Dictionary<string, List<Action<string>>> _keySubscribers = new Dictionary<string, List<Action<string>>>();

        /// <summary>Remote config가 변경되어 캐시가 갱신되면 호출됩니다. 인자는 변경된 key 목록.</summary>
        public event Action<IReadOnlyList<string>> OnChanged;

        public string LastUpdatedAtIso { get; private set; }

        public RemoteConfigFacade(SupabaseRemoteConfigService service, Func<string> accessTokenGetter = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _accessTokenGetter = accessTokenGetter;
        }

        /// <summary>
        /// 특정 key가 서버에서 갱신될 때마다 콜백을 호출합니다. (Inspector UnityEvent 대신 코드 연결용)
        /// </summary>
        /// <param name="invokeIfCached">구독 직후 캐시에 값이 있으면 한 번 즉시 호출합니다.</param>
        public void Subscribe(string key, Action<string> onValueChanged, bool invokeIfCached = true)
        {
            if (string.IsNullOrWhiteSpace(key) || onValueChanged == null)
                return;

            if (!_keySubscribers.TryGetValue(key, out var list))
            {
                list = new List<Action<string>>();
                _keySubscribers[key] = list;
            }

            if (list.Contains(onValueChanged) == false)
                list.Add(onValueChanged);

            if (invokeIfCached && TryGetRaw(key, out var json))
                onValueChanged.Invoke(json);
        }

        public void Unsubscribe(string key, Action<string> onValueChanged)
        {
            if (string.IsNullOrWhiteSpace(key) || onValueChanged == null)
                return;

            if (_keySubscribers.TryGetValue(key, out var list) == false)
                return;

            list.Remove(onValueChanged);
            if (list.Count == 0)
                _keySubscribers.Remove(key);
        }

        public bool TryGetRaw(string key, out string valueJson)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                valueJson = null;
                return false;
            }

            return _cache.TryGetValue(key, out valueJson);
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            if (_cache.TryGetValue(key, out var json) == false || string.IsNullOrWhiteSpace(json))
                return defaultValue;

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>전체 설정을 다시 받아 캐시를 교체합니다.</summary>
        public async Task<bool> RefreshAllAsync()
        {
            var accessToken = _accessTokenGetter?.Invoke();
            var result = await _service.GetAllAsync(accessToken);
            if (result.IsSuccess == false || result.Data == null)
                return false;

            ApplyRows(result.Data, replace: true);
            return true;
        }

        /// <summary>
        /// 마지막 updated_at 이후 변경분만 받아 캐시에 머지합니다.
        /// 변경이 있으면 OnChanged가 호출됩니다.
        /// </summary>
        public async Task<bool> PollAsync()
        {
            var accessToken = _accessTokenGetter?.Invoke();
            var result = await _service.GetChangedSinceAsync(LastUpdatedAtIso, accessToken);
            if (result.IsSuccess == false || result.Data == null)
                return false;

            ApplyRows(result.Data, replace: false);
            return true;
        }

        private void ApplyRows(SupabaseRemoteConfigService.RemoteConfigRow[] rows, bool replace)
        {
            if (rows == null)
                rows = Array.Empty<SupabaseRemoteConfigService.RemoteConfigRow>();

            if (replace)
                _cache.Clear();

            var changedKeys = new List<string>();

            foreach (var row in rows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.key))
                    continue;

                var newValue = row.value_json ?? string.Empty;

                if (_cache.TryGetValue(row.key, out var oldValue))
                {
                    if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
                        continue;
                }

                _cache[row.key] = newValue;
                changedKeys.Add(row.key);

                if (string.IsNullOrWhiteSpace(row.updated_at) == false)
                {
                    // ISO8601은 문자열 비교로도 대체로 증가 정렬이 가능하지만,
                    // 여기서는 가장 최근 값을 단순히 "최대"로 잡습니다.
                    if (string.IsNullOrWhiteSpace(LastUpdatedAtIso) || string.CompareOrdinal(row.updated_at, LastUpdatedAtIso) > 0)
                        LastUpdatedAtIso = row.updated_at;
                }
            }

            if (changedKeys.Count == 0)
                return;

            var notified = new HashSet<string>(StringComparer.Ordinal);
            foreach (var key in changedKeys)
            {
                if (notified.Add(key) == false)
                    continue;
                NotifyKeySubscribers(key);
            }

            OnChanged?.Invoke(changedKeys);
        }

        private void NotifyKeySubscribers(string key)
        {
            if (_keySubscribers.TryGetValue(key, out var list) == false || list.Count == 0)
                return;

            TryGetRaw(key, out var json);
            var snapshot = new List<Action<string>>(list);
            foreach (var cb in snapshot)
            {
                try
                {
                    cb?.Invoke(json ?? string.Empty);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Supabase] RemoteConfig key subscriber error. key={key}, err={e.Message}");
                }
            }
        }
    }
}

