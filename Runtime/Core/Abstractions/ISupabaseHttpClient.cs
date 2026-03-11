using System.Collections.Generic;
using System.Threading.Tasks;

namespace Truesoft.Supabase.Core.Http
{
    public interface ISupabaseHttpClient
    {
        Task<SupabaseHttpResponse> SendAsync(
            string method,
            string url,
            string jsonBody,
            Dictionary<string, string> headers);
    }
}