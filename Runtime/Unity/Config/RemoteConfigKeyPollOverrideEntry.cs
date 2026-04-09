using System;
using UnityEngine;

namespace Truesoft.Supabase.Unity.Config
{
    /// <summary>
    /// <see cref="SupabaseRuntime"/>에서 RemoteConfig 키별 폴링 주기를 DB 값 대신 덮어쓸 때 사용합니다.
    /// 설계: 1키 = 1폴링주기 (category 없음)
    /// </summary>
    [Serializable]
    public sealed class RemoteConfigKeyPollOverrideEntry
    {
        [Tooltip("remote_config.key 값과 동일해야 합니다.")]
        public string key;

        [Tooltip("-1 = DB의 poll_interval_seconds 사용. 0 = 해당 키 백그라운드 폴링 끔. 0 초과 = 초 단위 간격.")]
        public float overrideIntervalSeconds = -1f;
    }
}
