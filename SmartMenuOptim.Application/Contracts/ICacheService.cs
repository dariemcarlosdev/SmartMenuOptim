namespace SmartMenuOptim.Application.Contracts;

/// <summary>
/// Interface for caching services to improve performance.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Provides abstraction over caching mechanisms (in-memory, Redis, etc.)
/// for improved read performance and reduced database load.</para>
/// 
/// <para><strong>Clean Architecture:</strong></para>
/// <para>Interface defined in Application layer, implementations in Infrastructure layer.</para>
/// </remarks>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached item by key.
    /// </summary>
    /// <typeparam name="T">The type of the cached item.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached item or default if not found.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a cached item with optional expiration.
    /// </summary>
    /// <typeparam name="T">The type of the item to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Optional expiration time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached item by key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cached items matching a key pattern.
    /// </summary>
    /// <param name="pattern">The key pattern (e.g., "menu:*").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates menu-related caches for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateMenuCacheAsync(int restaurantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates analytics-related caches for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateAnalyticsCacheAsync(int restaurantId, CancellationToken cancellationToken = default);
}
