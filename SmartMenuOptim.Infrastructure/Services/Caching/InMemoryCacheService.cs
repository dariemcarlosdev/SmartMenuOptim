using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;

namespace SmartMenuOptim.Infrastructure.Services.Caching;

/// <summary>
/// In-memory implementation of <see cref="ICacheService"/> using IMemoryCache.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Provides in-memory caching for improved performance. Suitable for single-instance
/// deployments. For distributed deployments, use <c>RedisCacheService</c> instead.</para>
/// 
/// <para><strong>Cache Keys:</strong></para>
/// <list type="bullet">
///     <item><description><c>menu:{restaurantId}:*</c> - Menu data for a restaurant</description></item>
///     <item><description><c>analytics:{restaurantId}:*</c> - Analytics data for a restaurant</description></item>
///     <item><description><c>dish:{dishId}</c> - Individual dish data</description></item>
/// </list>
/// 
/// <para><strong>Production Considerations:</strong></para>
/// <para>For scaled-out deployments (multiple instances), use Redis or another distributed cache
/// to ensure cache consistency across all instances.</para>
/// </remarks>
public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryCacheService> _logger;
    private readonly HashSet<string> _trackedKeys = new();
    private readonly object _keysLock = new();

    /// <summary>
    /// Default cache expiration time.
    /// </summary>
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);

    public InMemoryCacheService(
        IMemoryCache cache,
        ILogger<InMemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return Task.FromResult(default(T?));
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
        };

        _cache.Set(key, value, options);

        // Track key for pattern-based removal
        lock (_keysLock)
        {
            _trackedKeys.Add(key);
        }

        _logger.LogDebug(
            "Cached value for key: {Key}, Expiration: {Expiration}",
            key,
            expiration ?? DefaultExpiration);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);

        lock (_keysLock)
        {
            _trackedKeys.Remove(key);
        }

        _logger.LogDebug("Removed cache entry for key: {Key}", key);

        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var keysToRemove = new List<string>();

        lock (_keysLock)
        {
            // Simple pattern matching - convert "key:*" to prefix matching
            var prefix = pattern.TrimEnd('*');

            keysToRemove = _trackedKeys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);

            lock (_keysLock)
            {
                _trackedKeys.Remove(key);
            }
        }

        _logger.LogDebug(
            "Removed {Count} cache entries matching pattern: {Pattern}",
            keysToRemove.Count,
            pattern);

        return Task.CompletedTask;
    }

    public Task InvalidateMenuCacheAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        var pattern = $"menu:{restaurantId}:*";

        _logger.LogInformation(
            "Invalidating menu cache for RestaurantId={RestaurantId}",
            restaurantId);

        return RemoveByPatternAsync(pattern, cancellationToken);
    }

    public Task InvalidateAnalyticsCacheAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        var pattern = $"analytics:{restaurantId}:*";

        _logger.LogInformation(
            "Invalidating analytics cache for RestaurantId={RestaurantId}",
            restaurantId);

        return RemoveByPatternAsync(pattern, cancellationToken);
    }
}
