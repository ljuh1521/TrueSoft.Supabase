using System;

namespace Truesoft.Supabase.Unity.Auth.Google
{
    public interface IGoogleLoginProvider
    {
        void SignIn(Action<GoogleLoginResult> onSuccess, Action<string> onError);
        void SignOut(Action onComplete, Action<string> onError);
    }
}