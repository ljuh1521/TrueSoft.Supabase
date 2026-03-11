using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    [CreateAssetMenu(fileName = "SupabaseSettings", menuName = "TrueSoft/Supabase Settings")]
    public sealed class SupabaseSettings : ScriptableObject
    {
        public string projectUrl;
        public string publishableKey;
        public int timeoutSeconds = 30;

        public SupabaseOptions ToOptions()
        {
            return new SupabaseOptions
            {
                ProjectURL = projectUrl,
                PublishableKey = publishableKey,
                TimeoutSeconds = timeoutSeconds
            };
        }
    }
}