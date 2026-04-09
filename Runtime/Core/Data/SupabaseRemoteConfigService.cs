using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Http;

namespace Truesoft.Supabase.Core.Data
{
    /// <summary>
    /// Supabase remote_config 테이블을 통해 원격 설정을 조회합니다.
    /// 스키마: <c>Sql/player/10_remote_config.sql</c> (key, value_json, updated_at, version, 메타데이터 컬럼).
    /// </summary>
    public sealed class SupabaseRemoteConfigService
    {
        private const string SelectColumns =
            "key,value_json,updated_at,version,enabled,category,description,poll_interval_seconds,requires_auth,client_version_min,client_version_max,max_stale_seconds";

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

        /// <summary>전체 행 조회. <paramref name="categories"/>가 null이거나 비어 있으면 필터 없음.</summary>
        public async Task<SupabaseResult<RemoteConfigRow[]>> GetAllAsync(
            IReadOnlyList<string> categories = null,
            string accessToken = null)
        {
            var url = BuildBaseUrl() + AppendCategoryFilter(categories);
            var response = await _httpClient.SendAsync("GET", url, null, CreateHeaders(accessToken));
            return ParseRows(response);
        }

        /// <summary>
        /// 마지막 동기 이후 변경분. <paramref name="categories"/>가 null이거나 비어 있으면 카테고리 필터 없음.
        /// <paramref name="updatedAfterIso"/>가 비어 있으면 <see cref="GetAllAsync"/>와 동일하게 전체 조회합니다.
        /// </summary>
        public async Task<SupabaseResult<RemoteConfigRow[]>> GetChangedSinceAsync(
            string updatedAfterIso,
            IReadOnlyList<string> categories = null,
            string accessToken = null)
        {
            if (string.IsNullOrWhiteSpace(updatedAfterIso))
                return await GetAllAsync(categories, accessToken);

            var url =
                $"{BuildBaseUrl()}" +
                $"&updated_at=gt.{Uri.EscapeDataString(updatedAfterIso)}" +
                AppendCategoryFilter(categories);

            var response = await _httpClient.SendAsync("GET", url, null, CreateHeaders(accessToken));
            return ParseRows(response);
        }

        /// <summary>특정 키 목록만 조회(Cold Start 시 첫 조회용).</summary>
        public async Task<SupabaseResult<RemoteConfigRow[]>> GetByKeysAsync(
            IReadOnlyList<string> keys,
            string accessToken = null)
        {
            if (keys == null || keys.Count == 0)
                return SupabaseResult<RemoteConfigRow[]>.Success(Array.Empty<RemoteConfigRow>());

            var url = BuildBaseUrl() + AppendInFilter("key", keys);
            var response = await _httpClient.SendAsync("GET", url, null, CreateHeaders(accessToken));
            return ParseRows(response);
        }

        /// <summary>단일 카테고리의 모든 행 조회.</summary>
        public async Task<SupabaseResult<RemoteConfigRow[]>> GetByCategoryAsync(
            string category,
            string accessToken = null)
        {
            if (string.IsNullOrWhiteSpace(category))
                return SupabaseResult<RemoteConfigRow[]>.Fail("remote_config_category_empty");

            var url =
                $"{BuildBaseUrl()}" +
                $"&category=eq.{Uri.EscapeDataString(category.Trim())}";

            var response = await _httpClient.SendAsync("GET", url, null, CreateHeaders(accessToken));
            return ParseRows(response);
        }

        private string BuildBaseUrl()
        {
            return $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _remoteConfigTable)}?select={SelectColumns}";
        }

        private static string AppendCategoryFilter(IReadOnlyList<string> categories)
        {
            if (categories == null || categories.Count == 0)
                return string.Empty;

            return AppendInFilter("category", categories);
        }

        private static string AppendInFilter(string column, IReadOnlyList<string> values)
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var v in values)
            {
                if (string.IsNullOrWhiteSpace(v))
                    continue;
                if (first)
                {
                    sb.Append('&').Append(column).Append("=in.(");
                    first = false;
                }
                else
                    sb.Append(',');

                sb.Append(Uri.EscapeDataString(v.Trim()));
            }

            if (first)
                return string.Empty;

            sb.Append(')');
            return sb.ToString();
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
            public bool enabled;
            public string category;
            public string description;
            public int poll_interval_seconds;
            public bool requires_auth;
            public string client_version_min;
            public string client_version_max;
            public int max_stale_seconds;
        }
    }
}
