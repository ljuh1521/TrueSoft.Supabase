using UnityEngine;

namespace Truesoft.Supabase
{
    public sealed class SupabaseRunner : MonoBehaviour
    {
        [SerializeField] private SupabaseSettings settings;

        private async void Awake()
        {
            if (settings == null)
            {
                Debug.LogError("[Truesoft.Supabase] Settings is null.");
                return;
            }

            if (!settings.InitializeOnAwake)
                return;

            await SupabaseSDK.InitializeAsync(settings);
        }
    }
}