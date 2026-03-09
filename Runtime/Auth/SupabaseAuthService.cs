using System.Threading.Tasks;

namespace Truesoft.Supabase
{
    public sealed class SupabaseAuthService
    {
        private readonly SupabaseSettings _settings;
        private readonly SupabaseHttp _http;
        private readonly PlayerPrefsSessionStore _store;

        public SupabaseSession CurrentSession { get; private set; }

        public bool IsSignedIn => CurrentSession != null && !string.IsNullOrEmpty(CurrentSession.access_token);
        public string AccessToken => CurrentSession?.access_token;
        public string UserId => CurrentSession?.user_id;
        public string Email => CurrentSession?.email;

        public SupabaseAuthService(SupabaseSettings settings, SupabaseHttp http)
        {
            _settings = settings;
            _http = http;
            _store = new PlayerPrefsSessionStore(settings.SessionPrefsKey);

            if (_settings.SaveSessionToPlayerPrefs)
                CurrentSession = _store.Load();
        }

        public async Task<SupabaseResult<SupabaseSession>> SignInWithPasswordAsync(string email, string password)
        {
            var url = $"{_settings.ProjectUrl}/auth/v1/token?grant_type=password";

            var req = new SignInWithPasswordRequest
            {
                email = email,
                password = password
            };

            var response = await _http.SendAsync("POST", url, SupabaseJson.ToJson(req));

            if (!response.Success)
                return new SupabaseResult<SupabaseSession>(response.Error);

            var dto = SupabaseJson.FromJson<AuthSessionResponse>(response.Data);
            if (dto == null || string.IsNullOrEmpty(dto.access_token))
            {
                return new SupabaseResult<SupabaseSession>(new SupabaseError
                {
                    Code = "parse_error",
                    Message = "Failed to parse auth response.",
                    Raw = response.Data
                });
            }

            CurrentSession = new SupabaseSession
            {
                access_token = dto.access_token,
                refresh_token = dto.refresh_token,
                token_type = dto.token_type,
                expires_in = dto.expires_in,
                user_id = dto.user?.id,
                email = dto.user?.email
            };

            if (_settings.SaveSessionToPlayerPrefs)
                _store.Save(CurrentSession);

            return new SupabaseResult<SupabaseSession>(CurrentSession);
        }

        public async Task<SupabaseResult<SupabaseSession>> RefreshSessionAsync()
        {
            if (CurrentSession == null || string.IsNullOrEmpty(CurrentSession.refresh_token))
            {
                return new SupabaseResult<SupabaseSession>(new SupabaseError
                {
                    Code = "no_refresh_token",
                    Message = "No refresh token."
                });
            }

            var url = $"{_settings.ProjectUrl}/auth/v1/token?grant_type=refresh_token";

            var req = new RefreshTokenRequest
            {
                refresh_token = CurrentSession.refresh_token
            };

            var response = await _http.SendAsync("POST", url, SupabaseJson.ToJson(req));

            if (!response.Success)
                return new SupabaseResult<SupabaseSession>(response.Error);

            var dto = SupabaseJson.FromJson<AuthSessionResponse>(response.Data);
            if (dto == null || string.IsNullOrEmpty(dto.access_token))
            {
                return new SupabaseResult<SupabaseSession>(new SupabaseError
                {
                    Code = "parse_error",
                    Message = "Failed to parse refresh response.",
                    Raw = response.Data
                });
            }

            CurrentSession.access_token = dto.access_token;
            CurrentSession.refresh_token = dto.refresh_token;
            CurrentSession.token_type = dto.token_type;
            CurrentSession.expires_in = dto.expires_in;
            CurrentSession.user_id = dto.user?.id;
            CurrentSession.email = dto.user?.email;

            if (_settings.SaveSessionToPlayerPrefs)
                _store.Save(CurrentSession);

            return new SupabaseResult<SupabaseSession>(CurrentSession);
        }

        public void SignOut()
        {
            CurrentSession = null;

            if (_settings.SaveSessionToPlayerPrefs)
                _store.Clear();
        }
    }
}