using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Http;

namespace Truesoft.Supabase.Core.Data
{
    /// <summary>
    /// Supabase remote_config 테이블을 통해 원격 설정을 조회합니다.
    /// 권장 테이블: remote_config (key text pk, value_json text, updated_at timestamptz, version int)
    /// </summary>
    public sealed class SupabaseRemoteConfigService
    {
        private readonly string _supabaseUrl;
        private readonly string _publishableKey;
        private readonly string _remoteConfigTable;
        private readonly ISupabaseHttpClient _httpClient;
        private readonly ISupabaseJsonSerializer _jsonSerializer;

        public SupabaseRemoteConfigService(
            string supabaseUrl,
            string publishableKey,
            ISupabaseHttpClient httpClient,
            ISupabaseJsonSerializer jsonSerializer,
            string remoteConfigTable = "remote_config")
        {
            _supabaseUrl = supabaseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(supabaseUrl));
            _publishableKey = publishableKey ?? throw new ArgumentNullException(nameof(publishableKey));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _remoteConfigTable = SupabaseRestTableRef.Normalize(remoteConfigTable, nameof(remoteConfigTable));
        }

        public async Task<SupabaseResult<RemoteConfigRow[]>> GetAllAsync(string accessToken = null)
        {
            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _remoteConfigTable)}" +
                $"?select=key,value_json,updated_at,version";

            var response = await _httpClient.SendAsync(
                method: "GET",
                url: url,
                jsonBody: null,
                headers: CreateHeaders(accessToken));

            return ParseRows(response);
        }

        public async Task<SupabaseResult<RemoteConfigRow[]>> GetChangedSinceAsync(string updatedAfterIso, string accessToken = null)
        {
            if (string.IsNullOrWhiteSpace(updatedAfterIso))
                return await GetAllAsync(accessToken);

            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _remoteConfigTable)}" +
                $"?select=key,value_json,updated_at,version" +
                $"&updated_at=gt.{Uri.EscapeDataString(updatedAfterIso)}";

            var response = await _httpClient.SendAsync(
                method: "GET",
                url: url,
                jsonBody: null,
                headers: CreateHeaders(accessToken));

            return ParseRows(response);
        }

        private SupabaseResult<RemoteConfigRow[]> ParseRows(SupabaseHttpResponse response)
        {
            if (response == null)
                return SupabaseResult<RemoteConfigRow[]>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<RemoteConfigRow[]>.Fail(response.ErrorMessage ?? response.Body ?? "remote_config_fetch_failed");

            try
            {
                var rows = _jsonSerializer.FromJsonArray<RemoteConfigRow>(response.Body);
                return SupabaseResult<RemoteConfigRow[]>.Success(rows ?? Array.Empty<RemoteConfigRow>());
            }
            catch (Exception e)
            {
                return SupabaseResult<RemoteConfigRow[]>.Fail("remote_config_parse_exception:" + e.Message);
            }
        }

        private Dictionary<string, string> CreateHeaders(string accessToken)
        {
            var headers = new Dictionary<string, string>
            {
                { "apikey", _publishableKey },
                { "Content-Type", "application/json" },
            };

            if (string.IsNullOrWhiteSpace(accessToken) == false)
                headers["Authorization"] = "Bearer " + accessToken;

            return headers;
        }

        [Serializable]
        public sealed class RemoteConfigRow
        {
            public string key;
            public string value_json;
            public string updated_at;
            public int version;
        }
    }
}

