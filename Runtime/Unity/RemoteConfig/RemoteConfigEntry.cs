using System.Threading.Tasks;
using Truesoft.Supabase.Core.Common;

namespace Truesoft.Supabase.Unity.RemoteConfig
{
    /// <summary>
    /// 단일 RemoteConfig 키에 대한 타입 안전 fetch.
    /// </summary>
    public sealed class RemoteConfigEntry<T> where T : class, new()
    {
        private readonly string _key;

        public RemoteConfigEntry(string key) => _key = key;

        public string Key => _key;

        public Task<SupabaseResult<T>> FetchAsync() => SupabaseSDK.GetRemoteConfigAsync<T>(_key);

        public async Task<(bool success, T value)> TryFetchAsync()
        {
            var r = await FetchAsync().ConfigureAwait(true);
            return (r.IsSuccess, r.IsSuccess ? r.Data : null);
        }
    }
}
