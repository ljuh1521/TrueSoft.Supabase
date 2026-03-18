using System;
using System.Collections.Generic;
using Truesoft.Supabase.Unity;
using UnityEngine;

namespace Truesoft.Supabase.Unity.Config
{
    /// <summary>
    /// RemoteConfig 값을 ScriptableObject에 자동 적용합니다.
    /// key별 JSON 콜백은 Inspector 대신 코드에서 <see cref="RemoteConfigFacade.Subscribe"/> 를 사용하세요.
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

            foreach (var b in overwriteBindings)
            {
                if (b == null || b.applyOnStart == false)
                    continue;

                ApplyOverwriteBinding(b);
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
