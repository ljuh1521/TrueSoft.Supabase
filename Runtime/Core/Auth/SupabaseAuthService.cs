using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Common;
using Truesoft.Supabase.Core.Http;

namespace Truesoft.Supabase.Core.Auth
{
    public sealed class SupabaseAuthService
    {
        private readonly string _supabaseUrl;
        private readonly string _publishableKey;
        private readonly ISupabaseHttpClient _httpClient;
        private readonly ISupabaseJsonSerializer _jsonSerializer;
        private readonly Dictionary<string, string> _defaultHeaders;

        public SupabaseAuthService(
            string supabaseUrl,
            string publishableKey,
            ISupabaseHttpClient httpClient,
            ISupabaseJsonSerializer jsonSerializer)
        {
            if (string.IsNullOrWhiteSpace(supabaseUrl))
                throw new ArgumentException("supabaseUrl is null or empty", nameof(supabaseUrl));

            if (string.IsNullOrWhiteSpace(publishableKey))
                throw new ArgumentException("publishableKey is null or empty", nameof(publishableKey));

            _supabaseUrl = supabaseUrl.TrimEnd('/');
            _publishableKey = publishableKey.Trim();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _defaultHeaders = new Dictionary<string, string>
            {
                { "apikey", _publishableKey },
                { "Content-Type", "application/json" }
            };
        }

        public async Task<SupabaseResult<SupabaseSession>> SignInWithGoogleIdTokenAsync(string idToken)
        {
            return await SignInWithIdTokenAsync("google", idToken);
        }

        /// <summary>
        /// 게스트(익명) 로그인.
        /// Supabase Auth의 signInAnonymously()와 동일한 개념으로, 사용자가 입력 없이 가입만 수행합니다.
        /// </summary>
        public async Task<SupabaseResult<SupabaseSession>> SignInAnonymouslyAsync()
        {
            // Supabase Auth 익명 가입은 POST /auth/v1/signup 요청에 빈 JSON 바디({})를 보내는 방식으로 동작합니다.
            // 프로젝트 설정에서 "Anonymous sign-ins" 활성화가 필요합니다.
            var url = $"{_supabaseUrl}/auth/v1/signup";

            var bodyJson = _jsonSerializer.ToJson(new AnonymousSignupRequest());

            var response = await _httpClient.SendAsync(
                method: "POST",
                url: url,
                jsonBody: bodyJson,
                headers: _defaultHeaders);

            return HandleSessionResponse(response, "anonymous_signin_failed");
        }

        /// <summary>
        /// 현재 로그인된 사용자(게스트 포함)에 OAuth identity를 ID token으로 링크합니다.
        /// 예: 익명 로그인 후 Google idToken을 받아 linkIdentityWithIdToken과 유사한 동작을 수행합니다.
        /// </summary>
        public async Task<SupabaseResult<bool>> LinkIdentityWithIdTokenAsync(
            string accessToken,
            string provider,
            string idToken,
            string nonce = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return SupabaseResult<bool>.Fail("access_token_empty");

            if (string.IsNullOrWhiteSpace(provider))
                return SupabaseResult<bool>.Fail("provider_empty");

            if (string.IsNullOrWhiteSpace(idToken))
                return SupabaseResult<bool>.Fail("id_token_empty");

            var url = $"{_supabaseUrl}/auth/v1/user/identities/link_token";

            var body = new LinkIdentityWithIdTokenRequest
            {
                provider = provider,
                id_token = idToken,
                nonce = nonce
            };

            var bodyJson = _jsonSerializer.ToJson(body);

            var headers = new Dictionary<string, string>
            {
                { "apikey", _publishableKey },
                { "Authorization", "Bearer " + accessToken },
                { "Content-Type", "application/json" }
            };

            var response = await _httpClient.SendAsync(
                method: "POST",
                url: url,
                jsonBody: bodyJson,
                headers: headers);

            if (response == null)
                return SupabaseResult<bool>.Fail("http_response_null");

            if (response.IsSuccess == false)
            {
                var errorMessage = ExtractErrorMessage(response.Body);
                if (string.IsNullOrWhiteSpace(errorMessage))
                    errorMessage = response.ErrorMessage ?? response.Body ?? "link_identity_failed";
                return SupabaseResult<bool>.Fail(errorMessage);
            }

            return SupabaseResult<bool>.Success(true);
        }

        public async Task<SupabaseResult<SupabaseSession>> SignInWithIdTokenAsync(
            string provider,
            string idToken,
            string nonce = null)
        {
            if (string.IsNullOrWhiteSpace(provider))
                return SupabaseResult<SupabaseSession>.Fail("provider_empty");

            if (string.IsNullOrWhiteSpace(idToken))
                return SupabaseResult<SupabaseSession>.Fail("id_token_empty");

            var url = $"{_supabaseUrl}/auth/v1/token?grant_type=id_token";

            var body = new SignInWithIdTokenRequest
            {
                provider = provider,
                id_token = idToken,
                nonce = nonce
            };

            var bodyJson = _jsonSerializer.ToJson(body);

            var response = await _httpClient.SendAsync(
                method: "POST",
                url: url,
                jsonBody: bodyJson,
                headers: _defaultHeaders);

            return HandleSessionResponse(response, "supabase_auth_failed");
        }

        public async Task<SupabaseResult<SupabaseSession>> RefreshSessionAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return SupabaseResult<SupabaseSession>.Fail("refresh_token_empty");

            var url = $"{_supabaseUrl}/auth/v1/token?grant_type=refresh_token";

            var body = new RefreshTokenRequest
            {
                refresh_token = refreshToken
            };

            var bodyJson = _jsonSerializer.ToJson(body);

            var response = await _httpClient.SendAsync(
                method: "POST",
                url: url,
                jsonBody: bodyJson,
                headers: _defaultHeaders);

            return HandleSessionResponse(response, "refresh_failed");
        }

        private SupabaseResult<SupabaseSession> HandleSessionResponse(SupabaseHttpResponse response, string defaultError)
        {
            if (response == null)
                return SupabaseResult<SupabaseSession>.Fail("http_response_null");

            if (response.IsSuccess == false)
            {
                var errorMessage = ExtractErrorMessage(response.Body);
                if (string.IsNullOrWhiteSpace(errorMessage))
                    errorMessage = response.ErrorMessage ?? defaultError;

                return SupabaseResult<SupabaseSession>.Fail(errorMessage);
            }

            if (string.IsNullOrWhiteSpace(response.Body))
                return SupabaseResult<SupabaseSession>.Fail("response_body_empty");

            try
            {
                var session = _jsonSerializer.FromJson<SupabaseSession>(response.Body);

                if (session == null)
                    return SupabaseResult<SupabaseSession>.Fail("session_null");

                if (string.IsNullOrWhiteSpace(session.access_token))
                    return SupabaseResult<SupabaseSession>.Fail("access_token_empty");

                return SupabaseResult<SupabaseSession>.Success(session);
            }
            catch (Exception e)
            {
                return SupabaseResult<SupabaseSession>.Fail("session_parse_exception:" + e.Message);
            }
        }

        private string ExtractErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            try
            {
                var error = _jsonSerializer.FromJson<SupabaseErrorResponse>(body);

                if (error == null)
                    return null;

                if (string.IsNullOrWhiteSpace(error.msg) == false)
                    return error.msg;

                if (string.IsNullOrWhiteSpace(error.error_description) == false)
                    return error.error_description;

                if (string.IsNullOrWhiteSpace(error.error) == false)
                    return error.error;

                return null;
            }
            catch
            {
                return null;
            }
        }

        [Serializable]
        private sealed class SignInWithIdTokenRequest
        {
            public string provider;
            public string id_token;
            public string nonce;
        }

        [Serializable]
        private sealed class AnonymousSignupRequest
        {
            // JsonUtility로 "{}"를 만들기 위한 용도입니다.
        }

        [Serializable]
        private sealed class LinkIdentityWithIdTokenRequest
        {
            public string provider;
            public string id_token;
            public string nonce;
        }

        [Serializable]
        private sealed class RefreshTokenRequest
        {
            public string refresh_token;
        }

        [Serializable]
        private sealed class SupabaseErrorResponse
        {
            public string error;
            public string error_description;
            public string msg;
            public int code;
        }

    }
}