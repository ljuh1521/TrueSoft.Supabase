using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Truesoft.Supabase
{
    public sealed class SupabaseAuthService
    {
        private readonly SupabaseOptions _options;
        private readonly ISupabaseHttpClient _http;
        private readonly ISupabaseJsonSerializer _json;
        private readonly ISupabaseAuthStorage _storage;

        private SupabaseSession _session;

        public SupabaseSession Session => _session;

        public SupabaseAuthService(
            SupabaseOptions options,
            ISupabaseHttpClient http,
            ISupabaseJsonSerializer json,
            ISupabaseAuthStorage storage)
        {
            _options = options;
            _http = http;
            _json = json;
            _storage = storage;
        }
        
        [System.Serializable]
        private sealed class SignInWithPasswordRequest
        {
            public string email;
            public string password;
        }

        public async Task<SupabaseResult<SupabaseSession>> SignInWithPasswordAsync(
            string email,
            string password,
            CancellationToken ct = default)
        {
            var url = $"{_options.ProjectURL}/auth/v1/token?grant_type=password";

            var payload = new SignInWithPasswordRequest
            {
                email = email,
                password = password
            };

            var body = _json.ToJson(payload);

            var request = new SupabaseHttpRequest
            {
                Url = url,
                Method = "POST",
                Body = body,
                TimeoutSeconds = _options.TimeoutSeconds
            };

            request.Headers["apikey"] = _options.PublishableKey;
            request.Headers["Content-Type"] = "application/json";

            var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccess)
            {
                var error = TryParseError(response.Text);
                return error != null
                    ? SupabaseResult<SupabaseSession>.Fail(error, response.ErrorMessage)
                    : SupabaseResult<SupabaseSession>.Fail(response.Text ?? response.ErrorMessage ?? "Request failed.");
            }

            var session = _json.FromJson<SupabaseSession>(response.Text);
            if (session == null)
                return SupabaseResult<SupabaseSession>.Fail("Session parse failed.");

            session.created_at = DateTime.UtcNow;

            _session = session;
            SaveSession(session);

            return SupabaseResult<SupabaseSession>.Success(session);
        }

        public async Task RefreshSessionAsync(CancellationToken ct = default)
        {
            if (_session == null)
                return;

            if (!_session.IsExpired())
                return;

            var url = $"{_options.ProjectURL}/auth/v1/token?grant_type=refresh_token";

            var body = _json.ToJson(new
            {
                refresh_token = _session.refresh_token
            });

            var request = new SupabaseHttpRequest
            {
                Url = url,
                Method = "POST",
                Body = body
            };

            request.Headers["apikey"] = _options.PublishableKey;
            request.Headers["Authorization"] = "Bearer " + _options.PublishableKey;
            request.Headers["Content-Type"] = "application/json";

            var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccess)
                return;

            var session = _json.FromJson<SupabaseSession>(response.Text);

            session.created_at = System.DateTime.UtcNow;

            _session = session;

            SaveSession(session);
        }

        public void SaveSession(SupabaseSession session)
        {
            var json = _json.ToJson(session);
            _storage.SaveSession(json);
        }

        public void LoadSession()
        {
            var json = _storage.LoadSession();

            if (string.IsNullOrEmpty(json))
                return;

            _session = _json.FromJson<SupabaseSession>(json);
        }

        public void Logout()
        {
            _session = null;
            _storage.ClearSession();
        }
        
        private SupabaseError TryParseError(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return _json.FromJson<SupabaseError>(text);
        }
    }
}