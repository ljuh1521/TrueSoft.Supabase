using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Truesoft.Supabase.Core.Http
{
    public sealed class UnitySupabaseHttpClient : ISupabaseHttpClient
    {
        private readonly int _timeoutSeconds;

        public UnitySupabaseHttpClient(int timeoutSeconds = 30)
        {
            _timeoutSeconds = timeoutSeconds;
        }

        public async Task<SupabaseHttpResponse> SendAsync(
            string method,
            string url,
            string jsonBody,
            Dictionary<string, string> headers)
        {
            using var request = new UnityWebRequest(url, method);

            request.timeout = _timeoutSeconds;

            if (string.IsNullOrEmpty(jsonBody) == false)
            {
                var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.downloadHandler = new DownloadHandlerBuffer();

            if (headers != null)
            {
                foreach (var pair in headers)
                {
                    request.SetRequestHeader(pair.Key, pair.Value);
                }
            }

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

#if UNITY_2020_2_OR_NEWER
            var success = request.result == UnityWebRequest.Result.Success;
#else
            var success = !request.isNetworkError && !request.isHttpError;
#endif

            var body = request.downloadHandler?.text ?? "";
            var status = request.responseCode;

            if (success)
            {
                return SupabaseHttpResponse.Success(status, body);
            }

            return SupabaseHttpResponse.Fail(status, body, request.error);
        }
    }
}