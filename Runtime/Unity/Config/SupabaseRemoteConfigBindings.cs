using System;
using System.Collections.Generic;
using Truesoft.Supabase.Unity;
using UnityEngine;
using UnityEngine.Events;

namespace Truesoft.Supabase.Unity.Config
{
    /// <summary>
    /// RemoteConfig 값을 프로젝트 오브젝트에 자동으로 적용하는 바인딩 컴포넌트.
    /// - key + ScriptableObject를 연결해두면 value_json을 JsonUtility.FromJsonOverwrite로 덮어씁니다.
    /// - key + UnityEvent(string)을 연결하면 raw json을 전달받아 직접 처리할 수 있습니다.
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

        [Serializable]
        public sealed class RawJsonEventBinding
        {
            public string key;
            public bool invokeOnStart = true;
            public UnityEvent<string> onChanged;
        }

        [Header("Overwrite ScriptableObjects")]
        [SerializeField] private List<OverwriteBinding> overwriteBindings = new List<OverwriteBinding>();

        [Header("Raw JSON Events")]
        [SerializeField] private List<RawJsonEventBinding> rawJsonEventBindings = new List<RawJsonEventBinding>();

        private void OnEnable()
        {
            if (!Supabase.IsInitialized)
            {
                Debug.LogWarning("[Supabase] RemoteConfigBindings enabled before SDK init.");
                return;
            }

            Supabase.RemoteConfig.OnChanged += HandleChanged;
        }

        private void OnDisable()
        {
            if (!Supabase.IsInitialized)
                return;

            Supabase.RemoteConfig.OnChanged -= HandleChanged;
        }

        private void Start()
        {
            if (!Supabase.IsInitialized)
                return;

            // RefreshAllOnStart는 Runner에서 담당하는게 일반적이지만,
            // 여기서는 캐시에 값이 이미 들어왔다고 가정하고 applyOnStart만 처리합니다.
            ApplyAllStartBindings();
        }

        private void ApplyAllStartBindings()
        {
            foreach (var b in overwriteBindings)
            {
                if (b == null || b.applyOnStart == false)
                    continue;

                ApplyOverwriteBinding(b);
            }

            foreach (var b in rawJsonEventBindings)
            {
                if (b == null || b.invokeOnStart == false)
                    continue;

                InvokeRawJsonBinding(b);
            }
        }

        private void HandleChanged(IReadOnlyList<string> keys)
        {
            if (keys == null || keys.Count == 0)
                return;

            for (var i = 0; i < overwriteBindings.Count; i++)
            {
                var b = overwriteBindings[i];
                if (b == null || string.IsNullOrWhiteSpace(b.key))
                    continue;

                if (Contains(keys, b.key))
                    ApplyOverwriteBinding(b);
            }

            for (var i = 0; i < rawJsonEventBindings.Count; i++)
            {
                var b = rawJsonEventBindings[i];
                if (b == null || string.IsNullOrWhiteSpace(b.key))
                    continue;

                if (Contains(keys, b.key))
                    InvokeRawJsonBinding(b);
            }
        }

        private void ApplyOverwriteBinding(OverwriteBinding binding)
        {
            if (binding.target == null || string.IsNullOrWhiteSpace(binding.key))
                return;

            if (Supabase.RemoteConfig.TryGetRaw(binding.key, out var json) == false || string.IsNullOrWhiteSpace(json))
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

        private void InvokeRawJsonBinding(RawJsonEventBinding binding)
        {
            if (binding.onChanged == null || string.IsNullOrWhiteSpace(binding.key))
                return;

            if (Supabase.RemoteConfig.TryGetRaw(binding.key, out var json) == false)
                json = null;

            binding.onChanged.Invoke(json);
        }

        private static bool Contains(IReadOnlyList<string> keys, string key)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                if (string.Equals(keys[i], key, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}

