using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Http;

namespace Truesoft.Supabase.Core.Data
{
    /// <summary>
    /// Supabase user_events 테이블로 이벤트를 전송합니다.
    /// 서버에서 이벤트를 검증·가공해 상태를 갱신하는 서버 권한 패턴에 적합합니다.
    /// 필요한 테이블: user_events (user_id, event_type, payload jsonb, created_at), RLS로 본인 행만 INSERT 허용 권장.
    /// </summary>
    public sealed class SupabaseUserEventsService
    {
        private readonly string _supabaseUrl;
        private readonly string _publishableKey;
        private readonly ISupabaseHttpClient _httpClient;
        private readonly ISupabaseJsonSerializer _jsonSerializer;

        public SupabaseUserEventsService(
            string supabaseUrl,
            string publishableKey,
            ISupabaseHttpClient httpClient,
            ISupabaseJsonSerializer jsonSerializer)
        {
            _supabaseUrl = supabaseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(supabaseUrl));
            _publishableKey = publishableKey ?? throw new ArgumentNullException(nameof(publishableKey));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        }

        /// <summary>
        /// 이벤트 한 건을 전송합니다.
        /// </summary>
        /// <param name="accessToken">유저 액세스 토큰</param>
        /// <param name="userId">유저 ID</param>
        /// <param name="eventType">이벤트 종류 (예: "level_cleared", "score_earned")</param>
        /// <param name="payload">이벤트 페이로드 (직렬화 가능한 객체). 없으면 null.</param>
        public async Task<SupabaseResult<bool>> SendAsync<T>(
            string accessToken,
            string userId,
            string eventType,
            T payload)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<bool>.Fail("user_id_empty");

            if (string.IsNullOrWhiteSpace(eventType))
                return SupabaseResult<bool>.Fail("event_type_empty");

            var url = $"{_supabaseUrl}/rest/v1/user_events";

            var row = new EventRowRequest<T>
            {
                user_id = userId,
                event_type = eventType,
                payload = payload,
                created_at = DateTime.UtcNow.ToString("o")
            };

            var singleJson = _jsonSerializer.ToJson(row);
            var bodyJson = "[" + singleJson + "]";

            var response = await _httpClient.SendAsync(
                method: "POST",
                url: url,
                jsonBody: bodyJson,
                headers: CreateAuthHeaders(accessToken, "return=minimal"));

            if (response == null)
                return SupabaseResult<bool>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<bool>.Fail(response.ErrorMessage ?? response.Body ?? "event_send_failed");

            return SupabaseResult<bool>.Success(true);
        }

        private Dictionary<string, string> CreateAuthHeaders(string accessToken, string prefer = null)
        {
            var headers = new Dictionary<string, string>
            {
                { "apikey", _publishableKey },
                { "Authorization", "Bearer " + accessToken },
                { "Content-Type", "application/json" }
            };

            if (string.IsNullOrEmpty(prefer) == false)
                headers["Prefer"] = prefer;

            return headers;
        }

        [Serializable]
        private sealed class EventRowRequest<T>
        {
            public string user_id;
            public string event_type;
            public T payload;
            public string created_at;
        }
    }
}
