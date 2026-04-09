using System;
using System.Threading.Tasks;

namespace api.Interfaces
{
    public interface IRedisCacheService
    {
        Task<T?> GetOrAddAsync<T>(string key, Func<Task<T>> addItemFactory, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task RemoveByPrefixAsync(string prefixKey);
    }
}
