using System;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Data;
using Truesoft.Supabase.Core.Models;

namespace Truesoft.Supabase.Unity
{
    public sealed class UserSavesFacade
    {
        private readonly SupabaseUserDataService _userDataService;
        private readonly Func<SupabaseAuthService> _authProvider;

        public UserSavesFacade(
            SupabaseUserDataService userDataService,
            Func<SupabaseAuthService> authProvider)
        {
            _userDataService = userDataService ?? throw new ArgumentNullException(nameof(userDataService));
            _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
        }

        public async Task<SupabaseResult<bool>> SaveAsync<T>(T data)
        {
            var auth = _authProvider();
            if (auth == null)
                return SupabaseResult<bool>.Fail("auth_service_null");

            var sessionResult = await auth.RefreshSessionAsync(auth.Session?.RefreshToken);
            if (!sessionResult.Success || sessionResult.Data == null)
                return SupabaseResult<bool>.Fail(sessionResult.Error?.Message ?? "auth_not_signed_in");

            var session = sessionResult.Data;
            var userId = session.User?.Id;

            if (string.IsNullOrWhiteSpace(session.AccessToken) || string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<bool>.Fail("auth_not_signed_in");

            return await _userDataService.SaveAsync(
                accessToken: session.AccessToken,
                userId: userId,
                data: data);
        }

        public async Task<SupabaseResult<T>> LoadAsync<T>() where T : class, new()
        {
            var auth = _authProvider();
            if (auth == null)
                return SupabaseResult<T>.Fail("auth_service_null");

            var sessionResult = await auth.RefreshSessionAsync(auth.Session?.RefreshToken);
            if (!sessionResult.Success || sessionResult.Data == null)
                return SupabaseResult<T>.Fail(sessionResult.Error?.Message ?? "auth_not_signed_in");

            var session = sessionResult.Data;
            var userId = session.User?.Id;

            if (string.IsNullOrWhiteSpace(session.AccessToken) || string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<T>.Fail("auth_not_signed_in");

            return await _userDataService.LoadAsync<T>(
                accessToken: session.AccessToken,
                userId: userId);
        }
    }
}

