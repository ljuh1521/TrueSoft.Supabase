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
        private readonly Func<SupabaseSession> _sessionGetter;

        public SupabaseGoogleAuthService(
            IGoogleLoginProvider googleLoginProvider,
            SupabaseAuthService authService,
            Func<SupabaseSession> sessionGetter = null)
        {
            _googleLoginProvider = googleLoginProvider ?? throw new ArgumentNullException(nameof(googleLoginProvider));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _sessionGetter = sessionGetter;
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

                        // If we already have an anonymous (guest) session, link Google identity to it first.
                        // This enables "guest -> google" conversion flows.
                        var currentSession = _sessionGetter?.Invoke();
                        var isAnonymous = currentSession != null
                                           && currentSession.User != null
                                           && currentSession.User.IsAnonymous
                                           && string.IsNullOrWhiteSpace(currentSession.AccessToken) == false;

                        if (isAnonymous)
                        {
                            try
                            {
                                var linkResult = await _authService.LinkIdentityWithIdTokenAsync(
                                    currentSession.AccessToken,
                                    "google",
                                    googleResult.IdToken);
                                if (linkResult == null)
                                {
                                    // Non-blocking: continue with sign-in flow.
                                }
                            }
                            catch
                            {
                                // 링크가 실패해도, 다음 sign-in은 그대로 진행합니다.
                            }
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