using UnityEngine;

namespace Truesoft.Supabase
{
    [CreateAssetMenu(fileName = "SupabaseSettings", menuName = "Truesoft/Supabase Settings")]
    public sealed class SupabaseSettings : ScriptableObject
    {
        [Header("Project")]
        public string ProjectUrl;
        public string PublishableKey;

        [Header("Runtime")]
        public bool InitializeOnAwake = true;
        public bool VerboseLog = true;

        [Header("Session")]
        public bool SaveSessionToPlayerPrefs = true;
        public string SessionPrefsKey = "TRUESOFT_SUPABASE_SESSION";
    }
}