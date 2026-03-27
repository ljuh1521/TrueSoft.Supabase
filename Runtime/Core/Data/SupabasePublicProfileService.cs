using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Http;

namespace Truesoft.Supabase.Core.Data
{
    /// <summary>
    /// 공개 프로필(soft 탈퇴 시각) 및 표시 이름(displayName) 관련 API.
    /// - DB: <c>profiles</c> (user_id / account_id / withdrawn_at)
    /// - 표시 이름: Edge Function <c>displayname-get</c> (내부적으로 <c>display_names</c> 테이블 사용)
    /// - 유니크 강제: <c>display_names</c>의 unique index (lower(trim(display_name)))
    /// </summary>
    public sealed class SupabasePublicProfileService
    {
        private const int DisplayNameMaxLength = 64;

        /// <summary>PostgREST <c>IS NOT NULL</c> — 활성 행만 (<c>account_id</c>가 있는 프로필).</summary>
        private const string ActiveProfileFilter = "account_id=is.not_null";

        private readonly string _supabaseUrl;
        private readonly string _publishableKey;
        private readonly string _profilesTable;
        private readonly string _displayNamesTable;
        private readonly ISupabaseHttpClient _httpClient;
        private readonly ISupabaseJsonSerializer _jsonSerializer;

        public SupabasePublicProfileService(
            string supabaseUrl,
            string publishableKey,
            ISupabaseHttpClient httpClient,
            ISupabaseJsonSerializer jsonSerializer,
            string profilesTable = "profiles",
            string displayNamesTable = "display_names")
        {
            _supabaseUrl = supabaseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(supabaseUrl));
            _publishableKey = publishableKey ?? throw new ArgumentNullException(nameof(publishableKey));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _profilesTable = SupabaseRestTableRef.Normalize(profilesTable, nameof(profilesTable));
            _displayNamesTable = SupabaseRestTableRef.Normalize(displayNamesTable, nameof(displayNamesTable));
        }

        /// <summary>
        /// 로그인 없이 publishable key만으로 표시 이름을 조회합니다.
        /// Edge Function <c>displayname-get</c>를 호출하며, <paramref name="playerUserId"/>는 <c>profiles.user_id</c>(안정 플레이어 id)입니다.
        /// </summary>
        public async Task<SupabaseResult<string>> GetDisplayNameAsync(string playerUserId)
        {
            if (string.IsNullOrWhiteSpace(playerUserId))
                return SupabaseResult<string>.Fail("player_user_id_empty");

            var id = playerUserId.Trim();
            var url = $"{_supabaseUrl}/functions/v1/displayname-get";
            var bodyJson = _jsonSerializer.ToJson(new DisplayNameGetRequest { user_id = id });

            var response = await _httpClient.SendAsync(
                method: "POST",
                url: url,
                jsonBody: bodyJson,
                headers: CreateAnonHeaders());

            if (response == null)
                return SupabaseResult<string>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<string>.Fail(response.ErrorMessage ?? response.Body ?? "public_profile_fetch_failed");

            try
            {
                var parsed = _jsonSerializer.FromJson<DisplayNameGetResponse>(response.Body);
                if (parsed == null || !parsed.ok)
                    return SupabaseResult<string>.Fail(string.IsNullOrWhiteSpace(parsed?.reason) ? "display_name_get_failed" : parsed.reason);

                return SupabaseResult<string>.Success(parsed.display_name ?? string.Empty);
            }
            catch (Exception e)
            {
                return SupabaseResult<string>.Fail("display_name_get_parse_exception:" + e.Message);
            }
        }

        /// <summary>
        /// displayName이 사용 가능한지 조회합니다(공개).
        /// </summary>
        public async Task<SupabaseResult<bool>> IsDisplayNameAvailableAsync(string displayName)
        {
            var norm = NormalizeDisplayName(displayName);
            if (norm.Length == 0)
                return SupabaseResult<bool>.Fail("display_name_empty");
            if (norm.Length > DisplayNameMaxLength)
                return SupabaseResult<bool>.Fail("display_name_too_long");

            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _displayNamesTable)}" +
                $"?select=account_id" +
                $"&display_name=eq.{Uri.EscapeDataString(norm)}" +
                $"&limit=1";

            var response = await _httpClient.SendAsync(
                method: "GET",
                url: url,
                jsonBody: null,
                headers: CreateAnonHeaders());

            if (response == null)
                return SupabaseResult<bool>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<bool>.Fail(response.ErrorMessage ?? response.Body ?? "display_name_check_failed");

            try
            {
                var rows = _jsonSerializer.FromJsonArray<AccountIdRow>(response.Body);
                if (rows == null || rows.Length == 0 || string.IsNullOrWhiteSpace(rows[0]?.account_id))
                    return SupabaseResult<bool>.Success(true);
                return SupabaseResult<bool>.Success(false);
            }
            catch (Exception e)
            {
                return SupabaseResult<bool>.Fail("display_name_check_parse_exception:" + e.Message);
            }
        }

        /// <summary>
        /// 본인 displayName 유니크 claim을 upsert 합니다(<c>display_names</c>).
        /// auth.user_metadata(displayName) 업데이트는 별도(<c>/auth/v1/user</c>).
        /// </summary>
        public async Task<SupabaseResult<bool>> UpsertMyDisplayNameClaimAsync(
            string accessToken,
            string accountId,
            string playerUserId,
            string displayName)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(accountId))
                return SupabaseResult<bool>.Fail("account_id_empty");

            var stable = string.IsNullOrWhiteSpace(playerUserId) ? accountId.Trim() : playerUserId.Trim();
            var norm = NormalizeDisplayName(displayName);
            if (norm.Length == 0)
                return SupabaseResult<bool>.Fail("display_name_empty");

            if (norm.Length > DisplayNameMaxLength)
                return SupabaseResult<bool>.Fail("display_name_too_long");

            var url = $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _displayNamesTable)}?on_conflict=account_id";
            var body = new UpsertDisplayNameRow
            {
                account_id = accountId.Trim(),
                user_id = stable,
                display_name = norm,
                updated_at = DateTime.UtcNow.ToString("o")
            };

            var singleJson = _jsonSerializer.ToJson(body);
            var bodyJson = "[" + singleJson + "]";

            var response = await _httpClient.SendAsync(
                method: "POST",
                url: url,
                jsonBody: bodyJson,
                headers: CreateUserHeaders(accessToken, "resolution=merge-duplicates,return=minimal"));

            if (response == null)
                return SupabaseResult<bool>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<bool>.Fail(response.ErrorMessage ?? response.Body ?? "display_name_upsert_failed");

            return SupabaseResult<bool>.Success(true);
        }

        /// <summary>
        /// 공개 프로필 한 행을 조회합니다. <paramref name="playerUserId"/>는 <c>profiles.user_id</c>입니다.
        /// 행이 없으면 displayName·탈퇴 시각은 비어 있는 스냅샷을 반환합니다.
        /// </summary>
        public async Task<SupabaseResult<PublicProfileSnapshot>> GetProfileAsync(string playerUserId)
        {
            if (string.IsNullOrWhiteSpace(playerUserId))
                return SupabaseResult<PublicProfileSnapshot>.Fail("player_user_id_empty");

            var id = playerUserId.Trim();
            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _profilesTable)}" +
                $"?select=id,user_id,withdrawn_at" +
                $"&user_id=eq.{Uri.EscapeDataString(id)}" +
                $"&{ActiveProfileFilter}" +
                $"&limit=1";

            var response = await _httpClient.SendAsync(
                method: "GET",
                url: url,
                jsonBody: null,
                headers: CreateAnonHeaders());

            if (response == null)
                return SupabaseResult<PublicProfileSnapshot>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<PublicProfileSnapshot>.Fail(response.ErrorMessage ?? response.Body ?? "public_profile_fetch_failed");

            try
            {
                var rows = _jsonSerializer.FromJsonArray<ProfileRowFull>(response.Body);
                if (rows == null || rows.Length == 0 || rows[0] == null)
                {
                    var displayNameWhenNoProfile = await GetDisplayNameAsync(id);
                    var resolvedName = (displayNameWhenNoProfile != null && displayNameWhenNoProfile.IsSuccess)
                        ? (displayNameWhenNoProfile.Data ?? string.Empty)
                        : string.Empty;
                    return SupabaseResult<PublicProfileSnapshot>.Success(new PublicProfileSnapshot(string.Empty, id, resolvedName, null));
                }

                var row = rows[0];
                var w = row.withdrawn_at;
                if (string.IsNullOrWhiteSpace(w))
                    w = null;

                var rowId = string.IsNullOrWhiteSpace(row.id) ? string.Empty : row.id.Trim();
                var stable = string.IsNullOrWhiteSpace(row.user_id) ? id : row.user_id.Trim();
                var displayNameWhenProfile = await GetDisplayNameAsync(stable);
                var displayName = (displayNameWhenProfile != null && displayNameWhenProfile.IsSuccess)
                    ? (displayNameWhenProfile.Data ?? string.Empty)
                    : string.Empty;
                return SupabaseResult<PublicProfileSnapshot>.Success(new PublicProfileSnapshot(rowId, stable, displayName, w));
            }
            catch (Exception e)
            {
                return SupabaseResult<PublicProfileSnapshot>.Fail("public_profile_parse_exception:" + e.Message);
            }
        }

        /// <summary>
        /// 로그인 직후, 현재 계정(account_id)에 대응하는 profiles 행이 항상 존재하도록 upsert합니다.
        /// withdrawn_at은 활성 계정 기준으로 null로 정리합니다.
        /// </summary>
        public async Task<SupabaseResult<bool>> EnsureMyProfileRowAsync(
            string accessToken,
            string accountId,
            string playerUserId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(accountId))
                return SupabaseResult<bool>.Fail("account_id_empty");

            var stable = string.IsNullOrWhiteSpace(playerUserId) ? accountId.Trim() : playerUserId.Trim();
            if (string.IsNullOrWhiteSpace(stable))
                stable = accountId.Trim();

            var url = $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _profilesTable)}?on_conflict=account_id";
            var body = new UpsertProfileRow
            {
                user_id = stable,
                account_id = accountId.Trim(),
                withdrawn_at = null
            };

            var singleJson = _jsonSerializer.ToJson(body);
            var bodyJson = "[" + singleJson + "]";

            var response = await _httpClient.SendAsync(
                method: "POST",
                url: url,
                jsonBody: bodyJson,
                headers: CreateUserHeaders(accessToken, "resolution=merge-duplicates,return=minimal"));

            if (response == null)
                return SupabaseResult<bool>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<bool>.Fail(response.ErrorMessage ?? response.Body ?? "profile_upsert_failed");

            return SupabaseResult<bool>.Success(true);
        }

        /// <summary>
        /// 본인 행의 <c>withdrawn_at</c>만 갱신합니다. <paramref name="withdrawnAtIso"/>가 null/빈 문자열이면 SQL NULL로 지웁니다(탈퇴 표시 해제).
        /// </summary>
        public async Task<SupabaseResult<bool>> PatchMyWithdrawnAtAsync(
            string accessToken,
            string accountId,
            string withdrawnAtIso)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(accountId))
                return SupabaseResult<bool>.Fail("account_id_empty");

            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _profilesTable)}" +
                $"?account_id=eq.{Uri.EscapeDataString(accountId.Trim())}";

            string jsonBody;
            if (string.IsNullOrWhiteSpace(withdrawnAtIso))
                jsonBody = "{\"withdrawn_at\":null}";
            else
                jsonBody = _jsonSerializer.ToJson(new WithdrawnPatchBody { withdrawn_at = withdrawnAtIso.Trim() });

            var response = await _httpClient.SendAsync(
                method: "PATCH",
                url: url,
                jsonBody: jsonBody,
                headers: CreateUserHeaders(accessToken, "return=minimal"));

            if (response == null)
                return SupabaseResult<bool>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<bool>.Fail(response.ErrorMessage ?? response.Body ?? "withdrawn_at_patch_failed");

            return SupabaseResult<bool>.Success(true);
        }

        /// <summary>
        /// 서버에서 현재 시각 기준으로 유예 기간(일) 뒤의 <c>withdrawn_at</c>을 계산해 예약합니다.
        /// RPC: <c>ts_request_withdrawal</c>.
        /// </summary>
        public async Task<SupabaseResult<string>> RequestMyWithdrawalByDelayDaysAsync(
            string accessToken,
            int delayDays)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<string>.Fail("access_token_empty");

            if (delayDays < 0)
                delayDays = 0;

            var url = $"{_supabaseUrl}/rest/v1/rpc/ts_request_withdrawal";
            var bodyJson = _jsonSerializer.ToJson(new WithdrawalRequestBody
            {
                p_delay_days = delayDays
            });

            var response = await _httpClient.SendAsync(
                method: "POST",
                url: url,
                jsonBody: bodyJson,
                headers: CreateUserHeaders(accessToken, prefer: null));

            if (response == null)
                return SupabaseResult<string>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<string>.Fail(response.ErrorMessage ?? response.Body ?? "withdrawal_request_failed");

            try
            {
                var rows = _jsonSerializer.FromJsonArray<WithdrawalRequestRow>(response.Body);
                if (rows == null || rows.Length == 0 || rows[0] == null || string.IsNullOrWhiteSpace(rows[0].scheduled_at))
                    return SupabaseResult<string>.Fail("withdrawal_request_scheduled_at_empty");

                return SupabaseResult<string>.Success(rows[0].scheduled_at.Trim());
            }
            catch (Exception e)
            {
                return SupabaseResult<string>.Fail("withdrawal_request_parse_exception:" + e.Message);
            }
        }

        /// <summary>
        /// 로그인한 본인의 탈퇴 예약 게이트 상태를 조회합니다.
        /// RPC: <c>ts_my_withdrawal_status</c>.
        /// </summary>
        public async Task<SupabaseResult<MyWithdrawalStatus>> GetMyWithdrawalStatusAsync(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<MyWithdrawalStatus>.Fail("access_token_empty");

            var url = $"{_supabaseUrl}/rest/v1/rpc/ts_my_withdrawal_status";

            var response = await _httpClient.SendAsync(
                method: "POST",
                url: url,
                jsonBody: "{}",
                headers: CreateUserHeaders(accessToken, prefer: null));

            if (response == null)
                return SupabaseResult<MyWithdrawalStatus>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<MyWithdrawalStatus>.Fail(response.ErrorMessage ?? response.Body ?? "withdrawal_status_failed");

            try
            {
                var rows = _jsonSerializer.FromJsonArray<WithdrawalStatusRow>(response.Body);
                if (rows == null || rows.Length == 0 || rows[0] == null)
                    return SupabaseResult<MyWithdrawalStatus>.Success(new MyWithdrawalStatus(string.Empty, null, null, false, 0));

                var row = rows[0];
                var status = new MyWithdrawalStatus(
                    row.display_name ?? string.Empty,
                    string.IsNullOrWhiteSpace(row.withdrawn_at) ? null : row.withdrawn_at.Trim(),
                    string.IsNullOrWhiteSpace(row.server_now) ? null : row.server_now.Trim(),
                    row.is_scheduled,
                    row.seconds_remaining);

                return SupabaseResult<MyWithdrawalStatus>.Success(status);
            }
            catch (Exception e)
            {
                return SupabaseResult<MyWithdrawalStatus>.Fail("withdrawal_status_parse_exception:" + e.Message);
            }
        }

        private static string NormalizeDisplayName(string displayName)
        {
            return displayName == null ? string.Empty : displayName.Trim();
        }

        private Dictionary<string, string> CreateAnonHeaders()
        {
            return new Dictionary<string, string>
            {
                { "apikey", _publishableKey },
                { "Content-Type", "application/json" }
            };
        }

        private Dictionary<string, string> CreateUserHeaders(string accessToken, string prefer)
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
        private sealed class AccountIdRow
        {
            public string account_id;
        }

        [Serializable]
        private sealed class UpsertDisplayNameRow
        {
            public string account_id;
            public string user_id;
            public string display_name;
            public string updated_at;
        }

        [Serializable]
        private sealed class ProfileRowFull
        {
            public string id;
            public string user_id;
            public string withdrawn_at;
        }

        [Serializable]
        private sealed class DisplayNameGetRequest
        {
            public string user_id;
        }

        [Serializable]
        private sealed class DisplayNameGetResponse
        {
            public bool ok;
            public string display_name;
            public string reason;
        }

        [Serializable]
        private sealed class UpsertProfileRow
        {
            public string user_id;
            public string account_id;
            public string withdrawn_at;
        }

        [Serializable]
        private sealed class WithdrawnPatchBody
        {
            public string withdrawn_at;
        }

        [Serializable]
        private sealed class WithdrawalRequestBody
        {
            public int p_delay_days;
        }

        [Serializable]
        private sealed class WithdrawalRequestRow
        {
            public string scheduled_at;
        }

        [Serializable]
        private sealed class WithdrawalStatusRow
        {
            public string display_name;
            public string withdrawn_at;
            public string server_now;
            public bool is_scheduled;
            public long seconds_remaining;
        }
    }
}
