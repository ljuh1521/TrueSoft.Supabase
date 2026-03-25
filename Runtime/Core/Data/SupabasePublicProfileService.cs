using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Http;

namespace Truesoft.Supabase.Core.Data
{
    /// <summary>
    /// 공개 프로필(닉네임·soft 탈퇴 시각). <c>profiles</c>에 <c>id</c>(auth UUID), <c>nickname</c>, 선택 <c>withdrawn_at</c>을 두고 RLS로 조회/수정을 제어하는 패턴을 가정합니다.
    /// </summary>
    public sealed class SupabasePublicProfileService
    {
        private const int NicknameMaxLength = 64;

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
        /// 로그인 없이 publishable key만으로 닉네임을 조회합니다. RLS에서 <c>SELECT</c>가 공개(anon)여야 합니다.
        /// </summary>
        public async Task<SupabaseResult<string>> GetNicknameAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<string>.Fail("user_id_empty");

            var id = userId.Trim();
            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _profilesTable)}" +
                $"?select=nickname" +
                $"&id=eq.{Uri.EscapeDataString(id)}" +
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
        /// 현재 사용자 행을 upsert하여 닉네임을 설정합니다. RLS에서 본인 <c>id</c>만 쓰기 가능해야 합니다.
        /// </summary>
        public async Task<SupabaseResult<bool>> UpsertMyNicknameAsync(string accessToken, string userId, string nickname)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<bool>.Fail("user_id_empty");

            var norm = NormalizeNickname(nickname);
            if (norm.Length > NicknameMaxLength)
                return SupabaseResult<bool>.Fail("nickname_too_long");

            var id = userId.Trim();
            var url = $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _profilesTable)}?on_conflict=id";

            var body = new UpsertNicknameRow
            {
                id = id,
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
        /// 닉네임이 다른 사용자에게 이미 쓰이지 않는지 확인합니다. <paramref name="exceptUserId"/>가 있으면 해당 사용자 행은 제외(본인 닉 유지·변경 시 사용).
        /// DB에 닉네임 유니크 인덱스를 두면 저장 시 최종 일관성이 보장됩니다.
        /// </summary>
        public async Task<SupabaseResult<bool>> IsNicknameAvailableAsync(string nickname, string exceptUserId = null)
        {
            var norm = NormalizeNickname(nickname);
            if (norm.Length == 0)
                return SupabaseResult<bool>.Fail("nickname_empty");

            if (norm.Length > NicknameMaxLength)
                return SupabaseResult<bool>.Fail("nickname_too_long");

            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _profilesTable)}" +
                $"?select=id" +
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
                var rows = _jsonSerializer.FromJsonArray<IdRow>(response.Body);
                if (rows == null || rows.Length == 0 || string.IsNullOrWhiteSpace(rows[0]?.id))
                    return SupabaseResult<bool>.Success(true);

                var holderId = rows[0].id.Trim();
                if (string.IsNullOrWhiteSpace(exceptUserId) == false
                    && string.Equals(holderId, exceptUserId.Trim(), StringComparison.OrdinalIgnoreCase))
                    return SupabaseResult<bool>.Success(true);

                return SupabaseResult<bool>.Success(false);
            }
            catch (Exception e)
            {
                return SupabaseResult<bool>.Fail("nickname_check_parse_exception:" + e.Message);
            }
        }

        /// <summary>
        /// 공개 프로필 한 행을 조회합니다. 행이 없으면 닉네임·탈퇴 시각은 비어 있는 스냅샷을 반환합니다.
        /// </summary>
        public async Task<SupabaseResult<PublicProfileSnapshot>> GetProfileAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<PublicProfileSnapshot>.Fail("user_id_empty");

            var id = userId.Trim();
            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _profilesTable)}" +
                $"?select=id,nickname,withdrawn_at" +
                $"&id=eq.{Uri.EscapeDataString(id)}" +
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
                    return SupabaseResult<PublicProfileSnapshot>.Success(new PublicProfileSnapshot(id, string.Empty, null));

                var row = rows[0];
                var w = row.withdrawn_at;
                if (string.IsNullOrWhiteSpace(w))
                    w = null;

                var uid = string.IsNullOrWhiteSpace(row.id) ? id : row.id.Trim();
                return SupabaseResult<PublicProfileSnapshot>.Success(
                    new PublicProfileSnapshot(uid, row.nickname ?? string.Empty, w));
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
            string userId,
            string withdrawnAtIso)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(userId))
                return SupabaseResult<bool>.Fail("user_id_empty");

            var id = userId.Trim();
            var url =
                $"{SupabaseRestTableRef.BuildTableUrl(_supabaseUrl, _profilesTable)}" +
                $"?id=eq.{Uri.EscapeDataString(id)}";

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
            public string id;
            public string nickname;
        }

        [Serializable]
        private sealed class IdRow
        {
            public string id;
        }

        [Serializable]
        private sealed class ProfileRowFull
        {
            public string id;
            public string nickname;
            public string withdrawn_at;
        }

        [Serializable]
        private sealed class WithdrawnPatchBody
        {
            public string withdrawn_at;
        }
    }
}
