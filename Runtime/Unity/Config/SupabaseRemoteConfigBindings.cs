using System;
using System.Collections.Generic;
using Truesoft.Supabase.Unity;
using UnityEngine;

namespace Truesoft.Supabase.Unity.Config
{
    /// <summary>
    /// RemoteConfig 값을 ScriptableObject에 자동 적용합니다.
    /// overwriteBindings에 등록된 key를 구독하여 value_json 변경 시 ScriptableObject에 덮어씁니다.
    /// </summary>
    public sealed class SupabaseRemoteConfigBindings : MonoBehaviour
    {
        [Serializable]
        public sealed class OverwriteBinding
        {
            public string key;
            public ScriptableObject target;
            public bool applyOnStart = true;
        }

        [Header("Overwrite ScriptableObjects")]
        [SerializeField] private List<OverwriteBinding> overwriteBindings = new List<OverwriteBinding>();

        private readonly Dictionary<string, Action<string>> _subscriptions = new Dictionary<string, Action<string>>(StringComparer.Ordinal);

        private void OnEnable()
        {
            if (!Supabase.IsInitialized)
            {
                Debug.LogWarning("[Supabase] RemoteConfigBindings enabled before SDK init.");
                return;
            }

            // key별로 subscribe (applyOnStart는 invokeIfCached로 처리)
            for (var i = 0; i < overwriteBindings.Count; i++)
            {
                var binding = overwriteBindings[i];
                if (binding == null || string.IsNullOrWhiteSpace(binding.key) || binding.target == null)
                    continue;

                var key = binding.key.Trim();
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                // closure 안전: 로컬 복사본 사용
                var localBinding = binding;
                Action<string> handler = _ => ApplyOverwriteBinding(localBinding);

                _subscriptions[key] = handler;
                Supabase.SubscribeRemoteConfig(key, handler, invokeIfCached: localBinding.applyOnStart);
            }
        }

        private void OnDisable()
        {
            if (!Supabase.IsInitialized)
                return;

            foreach (var pair in _subscriptions)
                Supabase.UnsubscribeRemoteConfig(pair.Key, pair.Value);

            _subscriptions.Clear();
        }

        private void Start()
        {
            // applyOnStart는 SubscribeRemoteConfig(..., invokeIfCached:true)에서 처리
        }

        private void ApplyOverwriteBinding(OverwriteBinding binding)
        {
            if (binding.target == null || string.IsNullOrWhiteSpace(binding.key))
                return;

            if (Supabase.TryGetRemoteConfigRaw(binding.key, out var json) == false || string.IsNullOrWhiteSpace(json))
                return;

            try
            {
                JsonUtility.FromJsonOverwrite(json, binding.target);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Supabase] RemoteConfig overwrite failed. key={binding.key}, target={binding.target.name}, err={e.Message}");
            }
        }
    }
}
