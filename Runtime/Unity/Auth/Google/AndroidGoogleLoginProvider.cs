using System;

namespace Truesoft.Supabase.Unity.Auth.Google
{
    public sealed class AndroidGoogleLoginProvider : IGoogleLoginProvider
    {
        private readonly GoogleLoginBridge _bridge;
        private readonly string _webClientId;

        public AndroidGoogleLoginProvider(GoogleLoginBridge bridge, string webClientId)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _webClientId = webClientId;
        }

        public void SignIn(Action<GoogleLoginResult> onSuccess, Action<string> onError)
        {
            _bridge.SignIn(_webClientId, onSuccess, onError);
        }

        public void SignOut(Action onComplete, Action<string> onError)
        {
            _bridge.SignOut(onComplete, onError);
        }
    }
}