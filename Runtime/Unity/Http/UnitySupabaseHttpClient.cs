using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Truesoft.Supabase.Unity
{
    public sealed class UnitySupabaseHttpClient : ISupabaseHttpClient
    {
        private readonly int _defaultTimeoutSeconds;

        public UnitySupabaseHttpClient(int defaultTimeoutSeconds = 10)
        {
            _defaultTimeoutSeconds = defaultTimeoutSeconds;
        }

        public async Task<SupabaseHttpResponse> SendAsync(
            SupabaseHttpRequest request,
            CancellationToken cancellationToken = default)
        {
            using var unityRequest = CreateRequest(request);

            var operation = unityRequest.SendWebRequest();

            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            return new SupabaseHttpResponse
            {
                StatusCode = unityRequest.responseCode,
                Text = unityRequest.downloadHandler?.text,
                ErrorMessage = unityRequest.result == UnityWebRequest.Result.Success
                    ? null
                    : unityRequest.error
            };
        }

        private UnityWebRequest CreateRequest(SupabaseHttpRequest request)
        {
            var unityRequest = new UnityWebRequest(request.Url, request.Method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = request.TimeoutSeconds > 0
                    ? request.TimeoutSeconds
                    : _defaultTimeoutSeconds
            };

            if (string.IsNullOrEmpty(request.Body) == false)
            {
                var bytes = Encoding.UTF8.GetBytes(request.Body);
                unityRequest.uploadHandler = new UploadHandlerRaw(bytes);
            }

            if (request.Headers != null)
            {
                foreach (var pair in request.Headers)
                    unityRequest.SetRequestHeader(pair.Key, pair.Value);
            }

            return unityRequest;
        }
    }
}