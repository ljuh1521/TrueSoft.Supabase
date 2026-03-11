using System.Threading;
using System.Threading.Tasks;

namespace Truesoft.Supabase
{
    public interface ISupabaseHttpClient
    {
        Task<SupabaseHttpResponse> SendAsync(
            SupabaseHttpRequest request,
            CancellationToken cancellationToken = default);
    }
}