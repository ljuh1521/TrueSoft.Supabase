using System;
using UnityEngine;

namespace Truesoft.Supabase.Unity.Config
{
    /// <summary>
    /// <see cref="SupabaseRuntime"/>에서 RemoteConfig 카테고리별 폴링 주기를 DB 값 대신 덮어쓸 때 사용합니다.
    /// </summary>
    [Serializable]
    public sealed class RemoteConfigCategoryPollOverrideEntry
    {
        [Tooltip("remote_config.category 값과 동일해야 합니다.")]
        public string category;

        [Tooltip("-1 = DB의 poll_interval_seconds 사용. 0 = 해당 카테고리 백그라운드 폴링 끔. 0 초과 = 초 단위 간격.")]
        public float overrideIntervalSeconds = -1f;
    }
}
