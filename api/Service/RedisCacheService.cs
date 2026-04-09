using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using api.Interfaces;
using LazyCache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace api.Service
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IAppCache _lazyCache;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(IDistributedCache distributedCache, IAppCache lazyCache, ILogger<RedisCacheService> logger)
        {
            _distributedCache = distributedCache;
            _lazyCache = lazyCache;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<T?> GetOrAddAsync<T>(string key, Func<Task<T>> addItemFactory, TimeSpan? expiration = null)
        {
            return await _lazyCache.GetOrAddAsync(key, async cacheEntry =>
            {
                if (expiration.HasValue)
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = expiration;
                }

                string? cachedData = null;
                try
                {
                    cachedData = await _distributedCache.GetStringAsync(key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis connection failed. Skipping distributed cache for key: {CacheKey}", key);
                }
                
                if (!string.IsNullOrEmpty(cachedData))
                {
                    try
                    {
                        var deserialized = JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
                        if (deserialized != null)
                            return deserialized;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize Redis cache value for key: {CacheKey}", key);
                    }
                }

                _logger.LogInformation("Cache miss. Fetching data for key: {CacheKey}", key);
                var data = await addItemFactory();

                if (data != null)
                {
                    try
                    {
                        var serializedData = JsonSerializer.Serialize(data, _jsonOptions);
                        var options = new DistributedCacheEntryOptions();
                        if (expiration.HasValue)
                        {
                            options.AbsoluteExpirationRelativeToNow = expiration;
                        }

                        await _distributedCache.SetStringAsync(key, serializedData, options);
                    }
                    catch (Exception ex)
                    {
                         _logger.LogWarning(ex, "Failed to serialize and set Redis cache value for key: {CacheKey}", key);
                    }
                }

                return data;
            });
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _lazyCache.Remove(key);
                await _distributedCache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove cache key: {CacheKey}", key);
            }
        }
        
        public Task RemoveByPrefixAsync(string prefixKey)
        {
            _logger.LogWarning("RemoveByPrefixAsync is not fully implemented for standard IDistributedCache. Use exact key matching.");
            return Task.CompletedTask;
        }
    }
}
