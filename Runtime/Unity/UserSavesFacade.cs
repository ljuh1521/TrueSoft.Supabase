using System;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Data;
using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    public sealed class UserSavesFacade
    {
        private readonly SupabaseUserDataService _userDataService;
        private readonly Func<SupabaseSession> _sessionGetter;

        public UserSavesFacade(SupabaseUserDataService userDataService, Func<SupabaseSession> sessionGetter = null)
        {
            _userDataService = userDataService ?? throw new ArgumentNullException(nameof(userDataService));
            _sessionGetter = sessionGetter;
        }

        /// <summary>현재 SDK 세션으로 저장. 로그인 후 SetSession 되어 있으면 세션 인자 없이 호출 가능.</summary>
        public Task<SupabaseResult<bool>> SaveAsync<T>(T data)
        {
            var session = _sessionGetter?.Invoke();
            return SaveAsync(session, data);
        }

        public async Task<SupabaseResult<bool>> SaveAsync<T>(SupabaseSession session, T data)
        {
            if (session == null)
                return SupabaseResult<bool>.Fail("session_null");

            var accessToken = session.AccessToken;
            var userId = session.User?.Id;

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<bool>.Fail("auth_not_signed_in");
            
            Debug.Log($"Saving data for user {userId}: {data}: {DateTime.UtcNow.ToString("o")}");
            return await _userDataService.SaveAsync(
                accessToken: accessToken,
                userId: userId,
                data: data);
        }

        /// <summary>현재 SDK 세션으로 로드. 로그인 후 SetSession 되어 있으면 세션 인자 없이 호출 가능.</summary>
        public Task<SupabaseResult<T>> LoadAsync<T>() where T : class, new()
        {
            var session = _sessionGetter?.Invoke();
            return LoadAsync<T>(session);
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
