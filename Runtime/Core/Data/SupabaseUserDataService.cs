using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Http;

namespace Truesoft.Supabase.Core.Data
{
    public sealed class SupabaseUserDataService
    {
        private readonly string _supabaseUrl;
        private readonly string _publishableKey;
        private readonly ISupabaseHttpClient _httpClient;
        private readonly ISupabaseJsonSerializer _jsonSerializer;

        public SupabaseUserDataService(
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

        public async Task<SupabaseResult<bool>> SaveAsync<T>(
            string accessToken,
            string userId,
            T data)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<bool>.Fail("user_id_empty");

            if (data == null)
                return SupabaseResult<bool>.Fail("save_data_null");

            var url = $"{_supabaseUrl}/rest/v1/user_saves?on_conflict=user_id";

            var body = new SaveRowRequest<T>
            {
                user_id = userId,
                save_data = data,
                updated_at = DateTime.UtcNow.ToString("o")
            };

            var singleJson = _jsonSerializer.ToJson(body);
            var bodyJson = "[" + singleJson + "]";

            var response = await _httpClient.SendAsync(
                method: "POST",
                url: url,
                jsonBody: bodyJson,
                headers: CreateAuthHeaders(accessToken, "resolution=merge-duplicates,return=minimal"));

            if (response == null)
                return SupabaseResult<bool>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<bool>.Fail(response.ErrorMessage ?? response.Body ?? "save_failed");

            return SupabaseResult<bool>.Success(true);
        }

        public async Task<SupabaseResult<T>> LoadAsync<T>(
            string accessToken,
            string userId) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<T>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<T>.Fail("user_id_empty");

            var url =
                $"{_supabaseUrl}/rest/v1/user_saves" +
                $"?select=save_data,updated_at" +
                $"&user_id=eq.{Uri.EscapeDataString(userId)}" +
                $"&limit=1";

            var response = await _httpClient.SendAsync(
                method: "GET",
                url: url,
                jsonBody: null,
                headers: CreateAuthHeaders(accessToken, prefer: null));

            if (response == null)
                return SupabaseResult<T>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<T>.Fail(response.ErrorMessage ?? response.Body ?? "load_failed");

            try
            {
                var rows = _jsonSerializer.FromJsonArray<SaveRowResponse<T>>(response.Body);

                if (rows == null || rows.Length == 0 || rows[0] == null || rows[0].save_data == null)
                {
                    return SupabaseResult<T>.Success(new T());
                }

                return SupabaseResult<T>.Success(rows[0].save_data);
            }
            catch (Exception e)
            {
                return SupabaseResult<T>.Fail("load_parse_exception:" + e.Message);
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
        private sealed class SaveRowRequest<T>
        {
            public string user_id;
            public T save_data;
            public string updated_at;
        }

        [Serializable]
        private sealed class SaveRowResponse<T>
        {
            public T save_data;
            public string updated_at;
        }
    }
}