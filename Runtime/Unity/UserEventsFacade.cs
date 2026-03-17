using System;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Data;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// 세션 기반으로 이벤트 전송 API를 노출합니다.
    /// 서버에서 이벤트를 검증·가공하는 패턴에 사용하세요.
    /// </summary>
    public sealed class UserEventsFacade
    {
        private readonly SupabaseUserEventsService _eventsService;

        public UserEventsFacade(SupabaseUserEventsService eventsService)
        {
            _eventsService = eventsService ?? throw new ArgumentNullException(nameof(eventsService));
        }

        /// <summary>
        /// 페이로드 없이 이벤트만 전송합니다.
        /// </summary>
        public Task<SupabaseResult<bool>> SendAsync(SupabaseSession session, string eventType)
        {
            return SendAsync(session, eventType, (object)null);
        }

        /// <summary>
        /// 이벤트와 페이로드를 전송합니다. T는 [Serializable]이고 public 필드만 직렬화됩니다.
        /// </summary>
        public async Task<SupabaseResult<bool>> SendAsync<T>(SupabaseSession session, string eventType, T payload)
        {
            if (session == null)
                return SupabaseResult<bool>.Fail("session_null");

            var accessToken = session.AccessToken;
            var userId = session.User?.Id;

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<bool>.Fail("auth_not_signed_in");

            return await _eventsService.SendAsync(accessToken, userId, eventType, payload);
        }
    }
}
