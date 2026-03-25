using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Http;

namespace Truesoft.Supabase.Core.Data
{
    /// <summary>
    /// 채널 단위 채팅. 기본 테이블 chat_messages (channel_id, user_id, display_name, content, created_at) + RLS 권장. 테이블명은 생성자에서 변경 가능.
    /// </summary>
    public sealed class SupabaseChatService
    {
        private readonly string _supabaseUrl;
        private readonly string _publishableKey;
        private readonly string _chatMessagesTable;
        private readonly ISupabaseHttpClient _httpClient;
        private readonly ISupabaseJsonSerializer _jsonSerializer;

        public SupabaseChatService(
            string supabaseUrl,
            string publishableKey,
            ISupabaseHttpClient httpClient,
            ISupabaseJsonSerializer jsonSerializer,
            string chatMessagesTable = "chat_messages")
        {
            _supabaseUrl = supabaseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(supabaseUrl));
            _publishableKey = publishableKey ?? throw new ArgumentNullException(nameof(publishableKey));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _chatMessagesTable = SupabaseRestTableRef.Normalize(chatMessagesTable, nameof(chatMessagesTable));
        }

        public async Task<SupabaseResult<bool>> SendAsync(
            string accessToken,
            string channelId,
            string userId,
            string displayName,
            string content)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(channelId))
                return SupabaseResult<bool>.Fail("channel_id_empty");

            if (string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<bool>.Fail("user_id_empty");

            if (string.IsNullOrWhiteSpace(content))
                return SupabaseResult<bool>.Fail("content_empty");

            var url = SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _chatMessagesTable);

            var row = new ChatInsertRow
            {
                channel_id = channelId,
                user_id = userId,
                display_name = displayName ?? string.Empty,
                content = content
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
                return SupabaseResult<bool>.Fail(response.ErrorMessage ?? response.Body ?? "chat_send_failed");

            return SupabaseResult<bool>.Success(true);
        }

        /// <summary>최신 메시지부터 limit개 (시간 역순). 클라이언트에서 오래된 순으로 정렬해 사용.</summary>
        public async Task<SupabaseResult<ChatMessageRow[]>> FetchRecentAsync(
            string accessToken,
            string channelId,
            int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<ChatMessageRow[]>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(channelId))
                return SupabaseResult<ChatMessageRow[]>.Fail("channel_id_empty");

            limit = Math.Clamp(limit, 1, 200);

            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _chatMessagesTable)}" +
                $"?select=id,channel_id,user_id,display_name,content,created_at" +
                $"&channel_id=eq.{Uri.EscapeDataString(channelId)}" +
                $"&order=created_at.desc" +
                $"&limit={limit}";

            return await FetchArrayAsync(accessToken, url);
        }

        /// <summary>해당 시각 이후 새 메시지 (오름차순).</summary>
        public async Task<SupabaseResult<ChatMessageRow[]>> FetchAfterAsync(
            string accessToken,
            string channelId,
            string createdAfterIso)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<ChatMessageRow[]>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(channelId))
                return SupabaseResult<ChatMessageRow[]>.Fail("channel_id_empty");

            if (string.IsNullOrWhiteSpace(createdAfterIso))
                return SupabaseResult<ChatMessageRow[]>.Success(Array.Empty<ChatMessageRow>());

            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _chatMessagesTable)}" +
                $"?select=id,channel_id,user_id,display_name,content,created_at" +
                $"&channel_id=eq.{Uri.EscapeDataString(channelId)}" +
                $"&created_at=gt.{Uri.EscapeDataString(createdAfterIso)}" +
                $"&order=created_at.asc";

            return await FetchArrayAsync(accessToken, url);
        }

        private async Task<SupabaseResult<ChatMessageRow[]>> FetchArrayAsync(string accessToken, string url)
        {
            var response = await _httpClient.SendAsync(
                method: "GET",
                url: url,
                jsonBody: null,
                headers: CreateAuthHeaders(accessToken, prefer: null));

            if (response == null)
                return SupabaseResult<ChatMessageRow[]>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<ChatMessageRow[]>.Fail(response.ErrorMessage ?? response.Body ?? "chat_fetch_failed");

            try
            {
                var rows = _jsonSerializer.FromJsonArray<ChatMessageRow>(response.Body);
                return SupabaseResult<ChatMessageRow[]>.Success(rows ?? Array.Empty<ChatMessageRow>());
            }
            catch (Exception e)
            {
                return SupabaseResult<ChatMessageRow[]>.Fail("chat_parse_exception:" + e.Message);
            }
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
        private sealed class ChatInsertRow
        {
            public string channel_id;
            public string user_id;
            public string display_name;
            public string content;
        }

        [Serializable]
        public sealed class ChatMessageRow
        {
            public string id;
            public string channel_id;
            public string user_id;
            public string display_name;
            public string content;
            public string created_at;
        }
    }
}
