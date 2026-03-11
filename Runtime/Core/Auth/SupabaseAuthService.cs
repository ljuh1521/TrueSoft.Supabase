using System.Threading;
using System.Threading.Tasks;

namespace Truesoft.Supabase
{
    public sealed class SupabaseAuthService
    {
        private readonly SupabaseOptions _options;
        private readonly ISupabaseJsonSerializer _json;
        private readonly ISupabaseHttpClient _http;
        private readonly ISupabaseAuthStorage _storage;

        public SupabaseAuthService(
            SupabaseOptions options,
            ISupabaseJsonSerializer json,
            ISupabaseHttpClient http,
            ISupabaseAuthStorage storage = null)
        {
            _options = options;
            _json = json;
            _http = http;
            _storage = storage;
        }

        public async Task<SupabaseResult<SupabaseSession>> SignInWithPasswordAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            var payload = new SignInWithPasswordRequest
            {
                email = email,
                password = password
            };

            var request = CreateJsonRequest(
                "POST",
                _options.Url + "/auth/v1/token?grant_type=password",
                _json.ToJson(payload));

            var response = await _http.SendAsync(request, cancellationToken);

            if (response.IsSuccess == false)
                return ParseSessionFailure(response);

            var session = _json.FromJson<SupabaseSession>(response.Text);
            if (session == null)
                return SupabaseResult<SupabaseSession>.Fail("Session parse failed.");

            SaveSession(session);

            return SupabaseResult<SupabaseSession>.Success(session);
        }

        public async Task<SupabaseResult<SupabaseSession>> SignUpWithPasswordAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            var payload = new SignUpWithPasswordRequest
            {
                email = email,
                password = password
            };

            var request = CreateJsonRequest(
                "POST",
                _options.Url + "/auth/v1/signup",
                _json.ToJson(payload));

            var response = await _http.SendAsync(request, cancellationToken);

            if (response.IsSuccess == false)
                return ParseSessionFailure(response);

            var session = _json.FromJson<SupabaseSession>(response.Text);
            if (session == null)
                return SupabaseResult<SupabaseSession>.Fail("Session parse failed.");

            SaveSession(session);

            return SupabaseResult<SupabaseSession>.Success(session);
        }

        public async Task<SupabaseResult<SupabaseSession>> RefreshSessionAsync(
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            var payload = new RefreshTokenRequest
            {
                refresh_token = refreshToken
            };

            var request = CreateJsonRequest(
                "POST",
                _options.Url + "/auth/v1/token?grant_type=refresh_token",
                _json.ToJson(payload));

            var response = await _http.SendAsync(request, cancellationToken);

            if (response.IsSuccess == false)
                return ParseSessionFailure(response);

            var session = _json.FromJson<SupabaseSession>(response.Text);
            if (session == null)
                return SupabaseResult<SupabaseSession>.Fail("Session parse failed.");

            SaveSession(session);

            return SupabaseResult<SupabaseSession>.Success(session);
        }

        public SupabaseSession LoadSavedSession()
        {
            if (_storage == null)
                return null;

            var json = _storage.LoadSession();
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return _json.FromJson<SupabaseSession>(json);
        }

        public void ClearSavedSession()
        {
            _storage?.ClearSession();
        }

        private SupabaseHttpRequest CreateJsonRequest(string method, string url, string body)
        {
            var request = new SupabaseHttpRequest
            {
                Method = method,
                Url = url,
                Body = body,
                TimeoutSeconds = _options.TimeoutSeconds
            };

            request.Headers["apikey"] = _options.ApiKey;
            request.Headers["Content-Type"] = "application/json";

            return request;
        }

        private SupabaseResult<SupabaseSession> ParseSessionFailure(SupabaseHttpResponse response)
        {
            SupabaseError error = null;

            if (string.IsNullOrWhiteSpace(response.Text) == false)
                error = _json.FromJson<SupabaseError>(response.Text);

            if (error != null)
                return SupabaseResult<SupabaseSession>.Fail(error, response.ErrorMessage);

            return SupabaseResult<SupabaseSession>.Fail(response.ErrorMessage ?? response.Text ?? "Request failed.");
        }

        private void SaveSession(SupabaseSession session)
        {
            if (_storage == null || session == null)
                return;

            var json = _json.ToJson(session);
            _storage.SaveSession(json);
        }

        [System.Serializable]
        private sealed class SignInWithPasswordRequest
        {
            public string email;
            public string password;
        }

        [System.Serializable]
        private sealed class SignUpWithPasswordRequest
        {
            public string email;
            public string password;
        }

        [System.Serializable]
        private sealed class RefreshTokenRequest
        {
            public string refresh_token;
        }
    }
}