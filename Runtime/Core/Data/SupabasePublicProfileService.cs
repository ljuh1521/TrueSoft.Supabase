using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Http;

namespace Truesoft.Supabase.Core.Data
{
    /// <summary>
    /// 공개 프로필(닉네임·soft 탈퇴 시각). <c>profiles</c>에 <c>id</c>(행 PK), <c>user_id</c>(안정 플레이어 id), <c>account_id</c>(<c>auth.users.id</c>), <c>nickname</c>, <c>withdrawn_at</c>를 두고 RLS로 조회/수정하는 패턴을 가정합니다.
    /// </summary>
    public sealed class SupabasePublicProfileService
    {
        private const int NicknameMaxLength = 64;

        /// <summary>PostgREST <c>IS NOT NULL</c> — 활성 행만 (<c>account_id</c>가 있는 프로필).</summary>
        private const string ActiveProfileFilter = "account_id=is.not_null";

        private readonly string _supabaseUrl;
        private readonly string _publishableKey;
        private readonly string _profilesTable;
        private readonly ISupabaseHttpClient _httpClient;
        private readonly ISupabaseJsonSerializer _jsonSerializer;

        public SupabasePublicProfileService(
            string supabaseUrl,
            string publishableKey,
            ISupabaseHttpClient httpClient,
            ISupabaseJsonSerializer jsonSerializer,
            string profilesTable = "profiles")
        {
            _supabaseUrl = supabaseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(supabaseUrl));
            _publishableKey = publishableKey ?? throw new ArgumentNullException(nameof(publishableKey));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _profilesTable = SupabaseRestTableRef.Normalize(profilesTable, nameof(profilesTable));
        }

        /// <summary>
        /// 로그인 없이 publishable key만으로 닉네임을 조회합니다. <paramref name="playerUserId"/>는 <c>profiles.user_id</c>입니다.
        /// </summary>
        public async Task<SupabaseResult<string>> GetNicknameAsync(string playerUserId)
        {
            if (string.IsNullOrWhiteSpace(playerUserId))
                return SupabaseResult<string>.Fail("player_user_id_empty");

            var id = playerUserId.Trim();
            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _profilesTable)}" +
                $"?select=nickname" +
                $"&user_id=eq.{Uri.EscapeDataString(id)}" +
                $"&{ActiveProfileFilter}" +
                $"&limit=1";

            var response = await _httpClient.SendAsync(
                method: "GET",
                url: url,
                jsonBody: null,
                headers: CreateAnonHeaders());

            if (response == null)
                return SupabaseResult<string>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<string>.Fail(response.ErrorMessage ?? response.Body ?? "public_profile_fetch_failed");

            try
            {
                var rows = _jsonSerializer.FromJsonArray<NicknameRow>(response.Body);
                if (rows == null || rows.Length == 0 || rows[0] == null)
                    return SupabaseResult<string>.Success(string.Empty);

                return SupabaseResult<string>.Success(rows[0].nickname ?? string.Empty);
            }
            catch (Exception e)
            {
                return SupabaseResult<string>.Fail("public_profile_parse_exception:" + e.Message);
            }
        }

        /// <summary>
        /// 현재 사용자 행을 upsert하여 닉네임을 설정합니다. RLS에서 본인 <c>account_id</c>만 쓰기 가능해야 합니다.
        /// </summary>
        public async Task<SupabaseResult<bool>> UpsertMyNicknameAsync(
            string accessToken,
            string accountId,
            string playerUserId,
            string nickname)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(accountId))
                return SupabaseResult<bool>.Fail("account_id_empty");

            if (string.IsNullOrWhiteSpace(playerUserId))
                return SupabaseResult<bool>.Fail("player_user_id_empty");

            var norm = NormalizeNickname(nickname);
            if (norm.Length > NicknameMaxLength)
                return SupabaseResult<bool>.Fail("nickname_too_long");

            var url = $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _profilesTable)}?on_conflict=account_id";

            var body = new UpsertNicknameRow
            {
                user_id = playerUserId.Trim(),
                account_id = accountId.Trim(),
                nickname = norm
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
                return SupabaseResult<bool>.Fail(response.ErrorMessage ?? response.Body ?? "public_profile_upsert_failed");

            return SupabaseResult<bool>.Success(true);
        }

        /// <summary>
        /// 닉네임이 다른 사용자에게 이미 쓰이지 않는지 확인합니다.
        /// <paramref name="ignoreUserIdForSelf"/>가 있으면 해당 <c>profiles.account_id</c>(= 현재 세션 <c>auth.uid()</c>) 행은 제외(본인 닉 유지·변경 시 사용).
        /// </summary>
        public async Task<SupabaseResult<bool>> IsNicknameAvailableAsync(string nickname, string ignoreUserIdForSelf = null)
        {
            var norm = NormalizeNickname(nickname);
            if (norm.Length == 0)
                return SupabaseResult<bool>.Fail("nickname_empty");

            if (norm.Length > NicknameMaxLength)
                return SupabaseResult<bool>.Fail("nickname_too_long");

            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _profilesTable)}" +
                $"?select=account_id" +
                $"&nickname=eq.{Uri.EscapeDataString(norm)}" +
                $"&limit=1";

            var response = await _httpClient.SendAsync(
                method: "GET",
                url: url,
                jsonBody: null,
                headers: CreateAnonHeaders());

            if (response == null)
                return SupabaseResult<bool>.Fail("http_response_null");

            if (response.IsSuccess == false)
                return SupabaseResult<bool>.Fail(response.ErrorMessage ?? response.Body ?? "nickname_check_failed");

            try
            {
                var rows = _jsonSerializer.FromJsonArray<AccountIdRow>(response.Body);
                if (rows == null || rows.Length == 0 || string.IsNullOrWhiteSpace(rows[0]?.account_id))
                    return SupabaseResult<bool>.Success(true);

                var holder = rows[0].account_id.Trim();
                if (string.IsNullOrWhiteSpace(ignoreUserIdForSelf) == false
                    && string.Equals(holder, ignoreUserIdForSelf.Trim(), StringComparison.OrdinalIgnoreCase))
                    return SupabaseResult<bool>.Success(true);

                return SupabaseResult<bool>.Success(false);
            }
            catch (Exception e)
            {
                return SupabaseResult<bool>.Fail("nickname_check_parse_exception:" + e.Message);
            }
        }

        /// <summary>
        /// 공개 프로필 한 행을 조회합니다. <paramref name="playerUserId"/>는 <c>profiles.user_id</c>입니다. 행이 없으면 닉네임·탈퇴 시각은 비어 있는 스냅샷을 반환합니다.
        /// </summary>
        public async Task<SupabaseResult<PublicProfileSnapshot>> GetProfileAsync(string playerUserId)
        {
            if (string.IsNullOrWhiteSpace(playerUserId))
                return SupabaseResult<PublicProfileSnapshot>.Fail("player_user_id_empty");

            var id = playerUserId.Trim();
            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _profilesTable)}" +
                $"?select=id,user_id,nickname,withdrawn_at" +
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
                    return SupabaseResult<PublicProfileSnapshot>.Success(new PublicProfileSnapshot(string.Empty, id, string.Empty, null));

                var row = rows[0];
                var w = row.withdrawn_at;
                if (string.IsNullOrWhiteSpace(w))
                    w = null;

                var rowId = string.IsNullOrWhiteSpace(row.id) ? string.Empty : row.id.Trim();
                var stable = string.IsNullOrWhiteSpace(row.user_id) ? id : row.user_id.Trim();
                return SupabaseResult<PublicProfileSnapshot>.Success(
                    new PublicProfileSnapshot(rowId, stable, row.nickname ?? string.Empty, w));
            }
            catch (Exception e)
            {
                return SupabaseResult<PublicProfileSnapshot>.Fail("public_profile_parse_exception:" + e.Message);
            }
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

        private static string NormalizeNickname(string nickname)
        {
            return nickname == null ? string.Empty : nickname.Trim();
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
        private sealed class NicknameRow
        {
            public string nickname;
        }

        [Serializable]
        private sealed class UpsertNicknameRow
        {
            public string user_id;
            public string account_id;
            public string nickname;
        }

        [Serializable]
        private sealed class AccountIdRow
        {
            public string account_id;
        }

        [Serializable]
        private sealed class ProfileRowFull
        {
            public string id;
            public string user_id;
            public string nickname;
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
    }
}
