using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Data;

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

        /// <summary>
        /// 로그인 직후 본인 <c>user_saves</c> 행이 존재하도록 보장합니다.
        /// DB RPC: <c>ts_ensure_my_user_save_row</c>.
        /// </summary>
        public Task<SupabaseResult<bool>> EnsureMyRowAsync()
        {
            var session = _sessionGetter?.Invoke();
            return EnsureMyRowAsync(session);
        }

        public async Task<SupabaseResult<bool>> EnsureMyRowAsync(SupabaseSession session)
        {
            if (session == null)
                return SupabaseResult<bool>.Fail("session_null");

            var accessToken = session.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("auth_not_signed_in");

            return await _userDataService.EnsureMyRowAsync(accessToken, session.User?.PlayerUserId);
        }

        /// <summary>
        /// 변경된 값만 부분 저장(PATCH)합니다. <paramref name="patch"/>에는 변경된 필드만 넣는 것을 권장합니다.
        /// </summary>
        public Task<SupabaseResult<bool>> PatchAsync(
            Dictionary<string, object> patch,
            bool ensureRowFirst = true,
            bool setUpdatedAtIsoUtc = true)
        {
            var session = _sessionGetter?.Invoke();
            return PatchAsync(session, patch, ensureRowFirst, setUpdatedAtIsoUtc);
        }

        public async Task<SupabaseResult<bool>> PatchAsync(
            SupabaseSession session,
            Dictionary<string, object> patch,
            bool ensureRowFirst = true,
            bool setUpdatedAtIsoUtc = true)
        {
            if (session == null)
                return SupabaseResult<bool>.Fail("session_null");

            var accessToken = session.AccessToken;
            var userId = session.User?.Id;

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<bool>.Fail("auth_not_signed_in");

            return await _userDataService.PatchAsync(
                accessToken: accessToken,
                accountId: userId,
                playerUserId: session.User.PlayerUserId,
                patch: patch,
                ensureRowFirst: ensureRowFirst,
                setUpdatedAtIsoUtc: setUpdatedAtIsoUtc);
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

            return await _userDataService.SaveAsync(
                accessToken: accessToken,
                accountId: userId,
                playerUserId: session.User.PlayerUserId,
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
                accountId: userId);
        }

        /// <summary>
        /// 프로젝트별 명시 컬럼을 select로 지정해 로드합니다.
        /// </summary>
        public Task<SupabaseResult<T>> LoadColumnsAsync<T>(string selectColumnsCsv) where T : class, new()
        {
            var session = _sessionGetter?.Invoke();
            return LoadColumnsAsync<T>(session, selectColumnsCsv);
        }

        public async Task<SupabaseResult<T>> LoadColumnsAsync<T>(SupabaseSession session, string selectColumnsCsv) where T : class, new()
        {
            if (session == null)
                return SupabaseResult<T>.Fail("session_null");

            var accessToken = session.AccessToken;
            var userId = session.User?.Id;

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<T>.Fail("auth_not_signed_in");

            return await _userDataService.LoadColumnsAsync<T>(
                accessToken: accessToken,
                accountId: userId,
                selectColumnsCsv: selectColumnsCsv);
        }

        /// <inheritdoc cref="SupabaseUserDataService.LoadColumnsWithRowStateAsync{T}(string, string, string)"/>
        public Task<SupabaseResult<UserSaveColumnsLoadResult<T>>> LoadColumnsWithRowStateAsync<T>(string selectColumnsCsv)
            where T : class, new()
        {
            var session = _sessionGetter?.Invoke();
            return LoadColumnsWithRowStateAsync<T>(session, selectColumnsCsv);
        }

        public async Task<SupabaseResult<UserSaveColumnsLoadResult<T>>> LoadColumnsWithRowStateAsync<T>(
            SupabaseSession session,
            string selectColumnsCsv) where T : class, new()
        {
            if (session == null)
                return SupabaseResult<UserSaveColumnsLoadResult<T>>.Fail("session_null");

            var accessToken = session.AccessToken;
            var userId = session.User?.Id;

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<UserSaveColumnsLoadResult<T>>.Fail("auth_not_signed_in");

            return await _userDataService.LoadColumnsWithRowStateAsync<T>(
                accessToken: accessToken,
                accountId: userId,
                selectColumnsCsv: selectColumnsCsv);
        }

        /// <summary>
        /// <see cref="UserSaveColumnAttribute"/>로 표시한 컬럼만 모아 로드합니다.
        /// </summary>
        public Task<SupabaseResult<T>> LoadAttributedAsync<T>(bool includeUpdatedAt = true) where T : class, new()
        {
            var session = _sessionGetter?.Invoke();
            return LoadAttributedAsync<T>(session, includeUpdatedAt);
        }

        public async Task<SupabaseResult<T>> LoadAttributedAsync<T>(SupabaseSession session, bool includeUpdatedAt = true) where T : class, new()
        {
            if (session == null)
                return SupabaseResult<T>.Fail("session_null");

            var accessToken = session.AccessToken;
            var userId = session.User?.Id;

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<T>.Fail("auth_not_signed_in");

            return await _userDataService.LoadAttributedAsync<T>(
                accessToken: accessToken,
                accountId: userId,
                includeUpdatedAt: includeUpdatedAt);
        }

        /// <inheritdoc cref="SupabaseUserDataService.LoadAttributedWithRowStateAsync{T}(string, string, bool)"/>
        public Task<SupabaseResult<UserSaveColumnsLoadResult<T>>> LoadAttributedWithRowStateAsync<T>(
            bool includeUpdatedAt = true) where T : class, new()
        {
            var session = _sessionGetter?.Invoke();
            return LoadAttributedWithRowStateAsync<T>(session, includeUpdatedAt);
        }

        public async Task<SupabaseResult<UserSaveColumnsLoadResult<T>>> LoadAttributedWithRowStateAsync<T>(
            SupabaseSession session,
            bool includeUpdatedAt = true) where T : class, new()
        {
            if (session == null)
                return SupabaseResult<UserSaveColumnsLoadResult<T>>.Fail("session_null");

            var accessToken = session.AccessToken;
            var userId = session.User?.Id;

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<UserSaveColumnsLoadResult<T>>.Fail("auth_not_signed_in");

            return await _userDataService.LoadAttributedWithRowStateAsync<T>(
                accessToken: accessToken,
                accountId: userId,
                includeUpdatedAt: includeUpdatedAt);
        }

        /// <summary>
        /// <see cref="UserSaveSchema.BuildPatch{T}(T, T)"/>로 변경분만 PATCH합니다.
        /// </summary>
        public Task<SupabaseResult<bool>> PatchDiffAsync<T>(
            T previous,
            T current,
            bool ensureRowFirst = true,
            bool setUpdatedAtIsoUtc = true)
        {
            var session = _sessionGetter?.Invoke();
            return PatchDiffAsync(session, previous, current, ensureRowFirst, setUpdatedAtIsoUtc);
        }

        public async Task<SupabaseResult<bool>> PatchDiffAsync<T>(
            SupabaseSession session,
            T previous,
            T current,
            bool ensureRowFirst = true,
            bool setUpdatedAtIsoUtc = true)
        {
            if (session == null)
                return SupabaseResult<bool>.Fail("session_null");

            var accessToken = session.AccessToken;
            var userId = session.User?.Id;

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<bool>.Fail("auth_not_signed_in");

            return await _userDataService.PatchDiffAsync(
                accessToken: accessToken,
                accountId: userId,
                playerUserId: session.User.PlayerUserId,
                previous: previous,
                current: current,
                ensureRowFirst: ensureRowFirst,
                setUpdatedAtIsoUtc: setUpdatedAtIsoUtc);
        }
    }
}
