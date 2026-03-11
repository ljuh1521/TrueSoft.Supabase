using UnityEngine;

namespace Truesoft.Supabase.Unity.Auth.Google
{
    public static class GoogleLoginBridgeInstaller
    {
        private const string BridgeObjectName = "TruesoftGoogleLoginBridge";

        public static GoogleLoginBridge GetOrCreate()
        {
            var existing = Object.FindFirstObjectByType<GoogleLoginBridge>();
            if (existing != null)
                return existing;

            var go = new GameObject(BridgeObjectName);
            Object.DontDestroyOnLoad(go);

            return go.AddComponent<GoogleLoginBridge>();
        }
    }
}