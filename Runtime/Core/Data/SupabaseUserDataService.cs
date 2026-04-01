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
        private readonly string _userSavesTable;
        private readonly ISupabaseHttpClient _httpClient;
        private readonly ISupabaseJsonSerializer _jsonSerializer;

        public SupabaseUserDataService(
            string supabaseUrl,
            string publishableKey,
            ISupabaseHttpClient httpClient,
            ISupabaseJsonSerializer jsonSerializer,
            string userSavesTable = "user_saves")
        {
            _supabaseUrl = supabaseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(supabaseUrl));
            _publishableKey = publishableKey ?? throw new ArgumentNullException(nameof(publishableKey));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _userSavesTable = SupabaseRestTableRef.Normalize(userSavesTable, nameof(userSavesTable));
        }

        /// <param name="accountId"><c>auth.users.id</c> — RLS의 <c>auth.uid()</c>와 같아야 합니다.</param>
        /// <param name="playerUserId"><c>user_saves.user_id</c> — OAuth <c>sub</c> 등 안정 id(익명이면 <paramref name="accountId"/>와 같아도 됨).</param>
        [Obsolete("권장: 명시 컬럼 + PatchAsync. 이 API는 save_data(jsonb)에 객체 전체를 넣는 경로입니다.")]
        public async Task<SupabaseResult<bool>> SaveAsync<T>(
            string accessToken,
            string accountId,
            string playerUserId,
            T data)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(accountId))
                return SupabaseResult<bool>.Fail("account_id_empty");

            var stable = string.IsNullOrWhiteSpace(playerUserId) ? accountId.Trim() : playerUserId.Trim();

            if (data == null)
                return SupabaseResult<bool>.Fail("save_data_null");

            var url = $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _userSavesTable)}?on_conflict=account_id";

            var body = new SaveRowRequest<T>
            {
                user_id = stable,
                account_id = accountId.Trim(),
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

        /// <summary>
        /// 로그인 직후 본인 <c>user_saves</c> 행이 존재하도록 보장합니다.
        /// DB RPC: <c>ts_ensure_my_user_save_row</c> (SECURITY DEFINER).
        /// </summary>
        public async Task<SupabaseResult<bool>> EnsureMyRowAsync(
            string accessToken,
            string playerUserId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            var stable = string.IsNullOrWhiteSpace(playerUserId) ? null : playerUserId.Trim();

            var url = $"{_supabaseUrl}/rest/v1/rpc/ts_ensure_my_user_save_row";
            var bodyJson = _jsonSerializer.ToJson(new EnsureMyUserSaveRowBody { p_user_id = stable });

            var response = await _httpClient.SendAsync(
                method: "POST",
                url: url,
                jsonBody: bodyJson,
                headers: CreateAuthHeaders(accessToken, prefer: null));

            if (response == null)
                return SupabaseResult<bool>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<bool>.Fail(response.ErrorMessage ?? response.Body ?? "ensure_user_save_row_failed");

            return SupabaseResult<bool>.Success(true);
        }

        /// <summary>
        /// 명시 컬럼 기반으로 부분 저장(PATCH)합니다. <paramref name="patch"/>에는 변경된 필드만 넣는 것을 전제로 합니다.
        /// <para>
        /// 주의: 이 API는 <c>save_data(jsonb)</c>를 병합하지 않습니다(프로젝트별 컬럼/정규화 테이블 전제).
        /// </para>
        /// </summary>
        public async Task<SupabaseResult<bool>> PatchAsync(
            string accessToken,
            string accountId,
            string playerUserId,
            Dictionary<string, object> patch,
            bool ensureRowFirst = true,
            bool setUpdatedAtIsoUtc = true)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(accountId))
                return SupabaseResult<bool>.Fail("account_id_empty");

            if (patch == null || patch.Count == 0)
                return SupabaseResult<bool>.Fail("patch_empty");

            if (ensureRowFirst)
            {
                var ensured = await EnsureMyRowAsync(accessToken, playerUserId);
                if (ensured == null || !ensured.IsSuccess)
                    return SupabaseResult<bool>.Fail(ensured?.ErrorMessage ?? "ensure_user_save_row_failed");
            }

            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _userSavesTable)}" +
                $"?account_id=eq.{Uri.EscapeDataString(accountId.Trim())}";

            var payload = patch;
            if (setUpdatedAtIsoUtc)
            {
                payload = new Dictionary<string, object>(patch);
                payload["updated_at"] = DateTime.UtcNow.ToString("o");
            }

            var bodyJson = _jsonSerializer.ToJson(payload);

            var response = await _httpClient.SendAsync(
                method: "PATCH",
                url: url,
                jsonBody: bodyJson,
                headers: CreateAuthHeaders(accessToken, prefer: "return=minimal"));

            if (response == null)
                return SupabaseResult<bool>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<bool>.Fail(response.ErrorMessage ?? response.Body ?? "patch_failed");

            return SupabaseResult<bool>.Success(true);
        }

        /// <param name="accountId">현재 세션의 Auth 사용자 id (<c>auth.uid()</c>).</param>
        [Obsolete("권장: LoadColumnsAsync(select). 이 API는 save_data(jsonb)만 조회합니다.")]
        public async Task<SupabaseResult<T>> LoadAsync<T>(
            string accessToken,
            string accountId) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<T>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(accountId))
                return SupabaseResult<T>.Fail("account_id_empty");

            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _userSavesTable)}" +
                $"?select=save_data,updated_at" +
                $"&account_id=eq.{Uri.EscapeDataString(accountId.Trim())}" +
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

        /// <summary>
        /// 프로젝트별 명시 컬럼을 select로 지정해 로드합니다.
        /// </summary>
        public async Task<SupabaseResult<T>> LoadColumnsAsync<T>(
            string accessToken,
            string accountId,
            string selectColumnsCsv) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<T>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(accountId))
                return SupabaseResult<T>.Fail("account_id_empty");

            if (string.IsNullOrWhiteSpace(selectColumnsCsv))
                return SupabaseResult<T>.Fail("select_columns_empty");

            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _userSavesTable)}" +
                $"?select={Uri.EscapeDataString(selectColumnsCsv.Trim())}" +
                $"&account_id=eq.{Uri.EscapeDataString(accountId.Trim())}" +
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
                var rows = _jsonSerializer.FromJsonArray<T>(response.Body);
                if (rows == null || rows.Length == 0 || rows[0] == null)
                    return SupabaseResult<T>.Success(new T());
                return SupabaseResult<T>.Success(rows[0]);
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
            public string account_id;
            public T save_data;
            public string updated_at;
        }

        [Serializable]
        private sealed class SaveRowResponse<T>
        {
            public T save_data;
            public string updated_at;
        }

        [Serializable]
        private sealed class EnsureMyUserSaveRowBody
        {
            public string p_user_id;
        }
    }
}
