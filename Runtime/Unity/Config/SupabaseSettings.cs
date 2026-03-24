using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    [CreateAssetMenu(fileName = "SupabaseSettings", menuName = "TrueSoft/Supabase Settings")]
    public sealed class SupabaseSettings : ScriptableObject
    {
        public string projectUrl;
        public string publishableKey;

        [Tooltip("Google Cloud OAuth 2.0 Web Client ID. Android 네이티브 Google 로그인(SignInWithGoogleAsync)에 사용합니다.")]
        public string googleWebClientId;

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