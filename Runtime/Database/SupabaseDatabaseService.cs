using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Truesoft.Supabase
{
    public sealed class SupabaseDatabaseService
    {
        private readonly SupabaseSettings _settings;
        private readonly SupabaseHttp _http;
        private readonly SupabaseAuthService _auth;

        public SupabaseDatabaseService(SupabaseSettings settings, SupabaseHttp http, SupabaseAuthService auth)
        {
            _settings = settings;
            _http = http;
            _auth = auth;
        }

        public async Task<SupabaseResult<T[]>> SelectAsync<T>(string table, QueryOptions options = null)
        {
            options ??= new QueryOptions();

            var sb = new StringBuilder();
            sb.Append($"{_settings.ProjectUrl}/rest/v1/{table}?select={UnityWebRequest.EscapeURL(options.Select)}");

            if (options.Limit > 0)
                sb.Append($"&limit={options.Limit}");

            var response = await _http.SendAsync("GET", sb.ToString(), bearerToken: _auth.AccessToken);
            if (!response.Success)
                return new SupabaseResult<T[]>(response.Error);

            var data = SupabaseJson.FromJsonArray<T>(response.Data);
            return new SupabaseResult<T[]>(data);
        }

        public async Task<SupabaseResult<T[]>> FilterEqAsync<T>(string table, string column, string value, QueryOptions options = null)
        {
            options ??= new QueryOptions();

            var url =
                $"{_settings.ProjectUrl}/rest/v1/{table}" +
                $"?select={UnityWebRequest.EscapeURL(options.Select)}" +
                $"&{UnityWebRequest.EscapeURL(column)}=eq.{UnityWebRequest.EscapeURL(value)}";

            if (options.Limit > 0)
                url += $"&limit={options.Limit}";

            var response = await _http.SendAsync("GET", url, bearerToken: _auth.AccessToken);
            if (!response.Success)
                return new SupabaseResult<T[]>(response.Error);

            var data = SupabaseJson.FromJsonArray<T>(response.Data);
            return new SupabaseResult<T[]>(data);
        }

        public async Task<SupabaseResult<bool>> InsertAsync<T>(string table, T payload)
        {
            var url = $"{_settings.ProjectUrl}/rest/v1/{table}";
            var json = SupabaseJson.ToJson(payload);

            var response = await _http.SendAsync("POST", url, json, _auth.AccessToken);
            if (!response.Success)
                return new SupabaseResult<bool>(response.Error);

            return new SupabaseResult<bool>(true);
        }

        public async Task<SupabaseResult<bool>> UpsertAsync<T>(string table, T payload)
        {
            var url = $"{_settings.ProjectUrl}/rest/v1/{table}";
            var json = SupabaseJson.ToJson(payload);

            var response = await _http.SendAsync("POST", url, json, _auth.AccessToken, SupabaseConstants.ContentTypeJson);
            if (!response.Success)
                return new SupabaseResult<bool>(response.Error);

            return new SupabaseResult<bool>(true);
        }
    }
}