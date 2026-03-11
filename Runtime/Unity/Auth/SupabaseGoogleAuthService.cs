using System;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Unity.Auth.Google;

namespace Truesoft.Supabase.Unity.Auth
{
    public sealed class SupabaseGoogleAuthService
    {
        private readonly IGoogleLoginProvider _googleLoginProvider;
        private readonly SupabaseAuthService _authService;

        public SupabaseGoogleAuthService(
            IGoogleLoginProvider googleLoginProvider,
            SupabaseAuthService authService)
        {
            _googleLoginProvider = googleLoginProvider ?? throw new ArgumentNullException(nameof(googleLoginProvider));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public void SignInWithGoogle(
            Action<SupabaseSession> onSuccess,
            Action<string> onError)
        {
            _googleLoginProvider.SignIn(
                async googleResult =>
                {
                    try
                    {
                        if (googleResult == null)
                        {
                            onError?.Invoke("google_result_null");
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(googleResult.IdToken))
                        {
                            onError?.Invoke("google_id_token_empty");
                            return;
                        }

                        var authResult = await _authService.SignInWithGoogleIdTokenAsync(googleResult.IdToken);

                        if (authResult == null)
                        {
                            onError?.Invoke("supabase_auth_result_null");
                            return;
                        }

                        if (authResult.IsSuccess == false)
                        {
                            onError?.Invoke(authResult.ErrorMessage ?? "supabase_google_signin_failed");
                            return;
                        }

                        if (authResult.Data == null)
                        {
                            onError?.Invoke("supabase_session_null");
                            return;
                        }

                        onSuccess?.Invoke(authResult.Data);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke("supabase_google_signin_exception:" + e.Message);
                    }
                },
                onError);
        }

        public void SignOutGoogle(Action onComplete, Action<string> onError)
        {
            _googleLoginProvider.SignOut(onComplete, onError);
        }
    }
}