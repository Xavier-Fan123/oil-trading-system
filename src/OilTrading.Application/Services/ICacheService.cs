namespace OilTrading.Application.Services;

public interface ICacheService
{
    /// <summary>
    /// Gets a value from cache by key
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in cache with optional expiry
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from cache by key
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries matching a pattern
    /// </summary>
    Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value from cache, or sets it if not found
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a cache key from prefix and parts
    /// </summary>
    string GenerateKey(string prefix, params object[] keyParts);
}