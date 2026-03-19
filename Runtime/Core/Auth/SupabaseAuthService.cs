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
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _defaultHeaders = new Dictionary<string, string>
            {
                { "apikey", publishableKey },
                { "Content-Type", "application/json" }
            };
        }

        public async Task<SupabaseResult<SupabaseSession>> SignInWithGoogleIdTokenAsync(string idToken)
        {
            return await SignInWithIdTokenAsync("google", idToken);
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