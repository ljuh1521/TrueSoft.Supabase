using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Truesoft.Supabase.Unity.Diagnostics;
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

            // #region agent log
            TryLogAuthHttpForDebug(method, url, success, status, request.error);
            // #endregion

            if (success)
            {
                return SupabaseHttpResponse.Success(status, body);
            }

            return SupabaseHttpResponse.Fail(status, body, request.error);
        }

        // #region agent log
        private static void TryLogAuthHttpForDebug(string method, string url, bool success, long status, string unityError)
        {
            if (string.IsNullOrEmpty(url) || url.IndexOf("/auth/v1/", System.StringComparison.OrdinalIgnoreCase) < 0)
                return;

            var kind = "other";
            if (url.IndexOf("identities/link_token", System.StringComparison.OrdinalIgnoreCase) >= 0)
                kind = "link_token";
            else if (url.IndexOf("grant_type=refresh_token", System.StringComparison.OrdinalIgnoreCase) >= 0)
                kind = "refresh_token";
            else if (url.IndexOf("grant_type=id_token", System.StringComparison.OrdinalIgnoreCase) >= 0)
                kind = "id_token";

            if (kind == "other" && status != 405)
                return;

            var err = unityError ?? "";
            if (err.Length > 180)
                err = err.Substring(0, 180);
            var u = url.Length > 220 ? url.Substring(0, 220) : url;

            var hypothesisId = kind == "link_token"
                ? "H1"
                : kind == "refresh_token"
                    ? "H4"
                    : status == 405
                        ? "H5"
                        : "H3";

            SupabaseAgentDebugLog.Write(
                hypothesisId,
                "UnitySupabaseHttpClient.SendAsync",
                "auth_http",
                new Dictionary<string, string>
                {
                    ["method"] = method ?? "",
                    ["url"] = u,
                    ["status"] = status.ToString(),
                    ["unitySuccess"] = success.ToString(),
                    ["kind"] = kind,
                    ["unityError"] = err,
                });
        }
        // #endregion
    }
}