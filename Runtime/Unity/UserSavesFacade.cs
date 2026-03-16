using System;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Data;

namespace Truesoft.Supabase.Unity
{
    public sealed class UserSavesFacade
    {
        private readonly SupabaseUserDataService _userDataService;

        public UserSavesFacade(SupabaseUserDataService userDataService)
        {
            _userDataService = userDataService ?? throw new ArgumentNullException(nameof(userDataService));
        }

        public async Task<SupabaseResult<bool>> SaveAsync<T>(SupabaseSession session, T data)
        {
            if (session == null)
                return SupabaseResult<bool>.Fail("session_null");

            var accessToken = session.AccessToken;
            var userId = session.User?.Id;

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<bool>.Fail("auth_not_signed_in");

            return await _userDataService.SaveAsync(
                accessToken: accessToken,
                userId: userId,
                data: data);
        }

        public async Task<SupabaseResult<T>> LoadAsync<T>(SupabaseSession session) where T : class, new()
        {
            if (session == null)
                return SupabaseResult<T>.Fail("session_null");

            var accessToken = session.AccessToken;
            var userId = session.User?.Id;

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<T>.Fail("auth_not_signed_in");

            return await _userDataService.LoadAsync<T>(
                accessToken: accessToken,
                userId: userId);
        }
    }
}
