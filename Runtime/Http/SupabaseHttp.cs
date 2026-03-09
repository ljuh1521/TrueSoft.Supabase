using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Truesoft.Supabase
{
    public sealed class SupabaseHttp
    {
        private readonly SupabaseSettings _settings;

        public SupabaseHttp(SupabaseSettings settings)
        {
            _settings = settings;
        }

        public async Task<SupabaseResult<string>> SendAsync(
            string method,
            string url,
            string bodyJson = null,
            string userAccessToken = null,
            string contentType = SupabaseConstants.ContentTypeJson)
        {
            using var request = new UnityWebRequest(url, method);

            request.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrEmpty(bodyJson))
            {
                var bodyRaw = Encoding.UTF8.GetBytes(bodyJson);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", contentType);
            }

            // 최신 방식: apikey에는 publishable key
            request.SetRequestHeader(SupabaseConstants.ApiKeyHeader, _settings.PublishableKey);

            // 로그인 후에만 사용자 JWT를 Authorization에 넣음
            if (!string.IsNullOrEmpty(userAccessToken))
            {
                request.SetRequestHeader(
                    SupabaseConstants.AuthorizationHeader,
                    SupabaseConstants.BearerPrefix + userAccessToken);
            }

            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            var text = request.downloadHandler?.text ?? string.Empty;

            if (request.result == UnityWebRequest.Result.Success &&
                request.responseCode >= 200 &&
                request.responseCode < 300)
            {
                return new SupabaseResult<string>(text);
            }

            var error = new SupabaseError
            {
                HttpStatus = request.responseCode,
                Code = request.error,
                Message = request.error,
                Raw = text
            };

            return new SupabaseResult<string>(error);
        }
    }
}