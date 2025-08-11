using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace HidaSushi.Server.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
}

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CacheService> _logger;
    
    private readonly bool _redisEnabled;
    private readonly TimeSpan _defaultExpiration;
    private readonly TimeSpan _slidingExpiration;

    public CacheService(
        IMemoryCache memoryCache,
        IDistributedCache? distributedCache,
        IConfiguration configuration,
        ILogger<CacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _configuration = configuration;
        _logger = logger;
        
        _redisEnabled = _configuration.GetValue<bool>("Caching:Redis:Enabled", false);
        _defaultExpiration = TimeSpan.Parse(_configuration.GetValue<string>("Caching:Redis:DefaultExpiration", "00:15:00")!);
        _slidingExpiration = TimeSpan.Parse(_configuration.GetValue<string>("Caching:Redis:SlidingExpiration", "00:05:00")!);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // Try memory cache first
            if (_memoryCache.TryGetValue(key, out T? cachedValue))
            {
                _logger.LogDebug("Cache hit (memory): {Key}", key);
                return cachedValue;
            }

            // Try distributed cache if enabled
            if (_redisEnabled && _distributedCache != null)
            {
                var distributedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
                if (!string.IsNullOrEmpty(distributedValue))
                {
                    var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue);
                    
                    // Store in memory cache for faster subsequent access
                    var memoryEntryOptions = new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = _slidingExpiration,
                        Size = 1
                    };
                    _memoryCache.Set(key, deserializedValue, memoryEntryOptions);
                    
                    _logger.LogDebug("Cache hit (distributed): {Key}", key);
                    return deserializedValue;
                }
            }

            _logger.LogDebug("Cache miss: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (value == null) return;

        var actualExpiration = expiration ?? _defaultExpiration;

        try
        {
            // Set in memory cache
            var memoryEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = actualExpiration,
                SlidingExpiration = _slidingExpiration,
                Size = 1
            };
            _memoryCache.Set(key, value, memoryEntryOptions);

            // Set in distributed cache if enabled
            if (_redisEnabled && _distributedCache != null)
            {
                var serializedValue = JsonSerializer.Serialize(value);
                var distributedEntryOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = actualExpiration,
                    SlidingExpiration = _slidingExpiration
                };
                
                await _distributedCache.SetStringAsync(key, serializedValue, distributedEntryOptions, cancellationToken);
                _logger.LogDebug("Cache set (distributed): {Key}", key);
            }

            _logger.LogDebug("Cache set (memory): {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            // Remove from memory cache
            _memoryCache.Remove(key);

            // Remove from distributed cache if enabled
            if (_redisEnabled && _distributedCache != null)
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
                _logger.LogDebug("Cache removed (distributed): {Key}", key);
            }

            _logger.LogDebug("Cache removed (memory): {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cache: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            // Memory cache doesn't support pattern-based removal easily
            // For distributed cache, this would require Redis-specific implementation
            _logger.LogWarning("Pattern-based cache removal not fully implemented: {Pattern}", pattern);
            
            // This is a placeholder - in production, you'd implement Redis SCAN or use Redis tags
            if (_redisEnabled && _distributedCache != null)
            {
                // Would need IConnectionMultiplexer for Redis pattern operations
                _logger.LogDebug("Pattern removal requested (distributed): {Pattern}", pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        // Try to get from cache first
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        // If not in cache, get the value and cache it
        try
        {
            var value = await getItem();
            if (value != null)
            {
                await SetAsync(key, value, expiration, cancellationToken);
            }
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
            throw;
        }
    }
}

// Cache key constants for consistency
public static class CacheKeys
{
    public const string MENU_SUSHI_ROLLS = "menu:sushi_rolls";
    public const string MENU_SIGNATURE_ROLLS = "menu:signature_rolls";
    public const string MENU_INGREDIENTS = "menu:ingredients";
    public const string MENU_INGREDIENTS_BY_CATEGORY = "menu:ingredients:category:{0}";
    public const string ORDERS_PENDING = "orders:pending";
    public const string ORDERS_BY_STATUS = "orders:status:{0}";
    public const string ANALYTICS_DAILY = "analytics:daily:{0}";
    public const string CUSTOMER_BY_ID = "customer:id:{0}";
    public const string CUSTOMER_BY_EMAIL = "customer:email:{0}";
    
    public static string FormatKey(string template, params object[] args)
    {
        return string.Format(template, args);
    }
} 