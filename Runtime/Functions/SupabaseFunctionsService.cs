using System.Threading.Tasks;

namespace Truesoft.Supabase
{
    public sealed class SupabaseFunctionsService
    {
        private readonly SupabaseSettings _settings;
        private readonly SupabaseHttp _http;
        private readonly SupabaseAuthService _auth;

        public SupabaseFunctionsService(SupabaseSettings settings, SupabaseHttp http, SupabaseAuthService auth)
        {
            _settings = settings;
            _http = http;
            _auth = auth;
        }

        public async Task<SupabaseResult<TResponse>> InvokeAsync<TRequest, TResponse>(string functionName, TRequest request)
        {
            var url = $"{_settings.ProjectUrl}/functions/v1/{functionName}";
            var body = SupabaseJson.ToJson(request);

            var response = await _http.SendAsync("POST", url, body, _auth.AccessToken);
            if (!response.Success)
                return new SupabaseResult<TResponse>(response.Error);

            var data = SupabaseJson.FromJson<TResponse>(response.Data);
            return new SupabaseResult<TResponse>(data);
        }

        public async Task<SupabaseResult<string>> InvokeRawAsync<TRequest>(string functionName, TRequest request)
        {
            var url = $"{_settings.ProjectUrl}/functions/v1/{functionName}";
            var body = SupabaseJson.ToJson(request);
            return await _http.SendAsync("POST", url, body, _auth.AccessToken);
        }
    }
}