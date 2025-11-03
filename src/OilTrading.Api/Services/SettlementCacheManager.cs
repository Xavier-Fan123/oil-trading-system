using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace OilTrading.Api.Services;

/// <summary>
/// Cache management for settlement queries
/// Part of Phase 12: Monitoring & Performance
/// Implements caching strategy for frequently accessed settlement data
/// </summary>
public interface ISettlementCacheManager
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task InvalidateContractSettlementsAsync(Guid contractId);
    Task InvalidateAllSettlementsAsync();

    // Cache key builders
    string GetSettlementKey(Guid settlementId);
    string GetContractSettlementsKey(Guid contractId, string type);
    string GetAllSettlementsKey();
}

public class SettlementCacheManager : ISettlementCacheManager
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<SettlementCacheManager> _logger;

    // Cache expiration times
    private readonly TimeSpan _settlementExpiration = TimeSpan.FromMinutes(15); // 15 minutes
    private readonly TimeSpan _listExpiration = TimeSpan.FromMinutes(5); // 5 minutes
    private readonly TimeSpan _allSettlementsExpiration = TimeSpan.FromMinutes(10); // 10 minutes

    // Cache key prefixes
    private const string SettlementPrefix = "settlement:";
    private const string ContractSettlementsPrefix = "contract-settlements:";
    private const string AllSettlementsKey = "all-settlements";

    public SettlementCacheManager(IDistributedCache cache, ILogger<SettlementCacheManager> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves value from cache
    /// </summary>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key);

            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache miss: {CacheKey}", key);
                return null;
            }

            var deserialized = JsonSerializer.Deserialize<T>(cachedValue);
            _logger.LogDebug("Cache hit: {CacheKey}", key);

            return deserialized;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving from cache: {CacheKey}", key);
            return null;
        }
    }

    /// <summary>
    /// Stores value in cache with optional expiration
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _settlementExpiration
            };

            await _cache.SetStringAsync(key, serialized, cacheOptions);
            _logger.LogDebug("Cache set: {CacheKey} with expiration {Expiration}ms", key, cacheOptions.AbsoluteExpirationRelativeToNow?.TotalMilliseconds ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache: {CacheKey}", key);
            // Don't throw - cache failures shouldn't break functionality
        }
    }

    /// <summary>
    /// Removes single value from cache
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogDebug("Cache removed: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing from cache: {CacheKey}", key);
        }
    }

    /// <summary>
    /// Invalidates all settlement-related caches for a contract
    /// Called when settlement is created, updated, or deleted
    /// </summary>
    public async Task InvalidateContractSettlementsAsync(Guid contractId)
    {
        var keysToInvalidate = new List<string>
        {
            GetContractSettlementsKey(contractId, "purchase"),
            GetContractSettlementsKey(contractId, "sales"),
            GetAllSettlementsKey()
        };

        foreach (var key in keysToInvalidate)
        {
            await RemoveAsync(key);
        }

        _logger.LogInformation("Invalidated settlement caches for contract: {ContractId}", contractId);
    }

    /// <summary>
    /// Invalidates all settlement caches
    /// </summary>
    public async Task InvalidateAllSettlementsAsync()
    {
        await RemoveAsync(GetAllSettlementsKey());
        _logger.LogInformation("Invalidated all settlement caches");
    }

    /// <summary>
    /// Gets cache key for specific settlement
    /// </summary>
    public string GetSettlementKey(Guid settlementId)
    {
        return $"{SettlementPrefix}{settlementId}";
    }

    /// <summary>
    /// Gets cache key for contract settlements
    /// </summary>
    public string GetContractSettlementsKey(Guid contractId, string type)
    {
        return $"{ContractSettlementsPrefix}{type}:{contractId}";
    }

    /// <summary>
    /// Gets cache key for all settlements
    /// </summary>
    public string GetAllSettlementsKey()
    {
        return AllSettlementsKey;
    }
}

/// <summary>
/// Cache statistics tracker for monitoring cache effectiveness
/// </summary>
public interface ICacheStatisticsTracker
{
    void RecordHit(string key);
    void RecordMiss(string key);
    void RecordSet(string key);
    void RecordRemove(string key);

    int GetTotalHits();
    int GetTotalMisses();
    double GetHitRate();
    Dictionary<string, int> GetHitsByKey();
}

public class CacheStatisticsTracker : ICacheStatisticsTracker
{
    private readonly Dictionary<string, int> _hits = new();
    private readonly Dictionary<string, int> _misses = new();
    private int _totalSets;
    private int _totalRemoves;
    private readonly object _lockObject = new();

    public void RecordHit(string key)
    {
        lock (_lockObject)
        {
            if (!_hits.ContainsKey(key))
                _hits[key] = 0;
            _hits[key]++;
        }
    }

    public void RecordMiss(string key)
    {
        lock (_lockObject)
        {
            if (!_misses.ContainsKey(key))
                _misses[key] = 0;
            _misses[key]++;
        }
    }

    public void RecordSet(string key)
    {
        lock (_lockObject)
        {
            _totalSets++;
        }
    }

    public void RecordRemove(string key)
    {
        lock (_lockObject)
        {
            _totalRemoves++;
        }
    }

    public int GetTotalHits()
    {
        lock (_lockObject)
        {
            return _hits.Values.Sum();
        }
    }

    public int GetTotalMisses()
    {
        lock (_lockObject)
        {
            return _misses.Values.Sum();
        }
    }

    public double GetHitRate()
    {
        lock (_lockObject)
        {
            var total = GetTotalHits() + GetTotalMisses();
            return total > 0 ? (double)GetTotalHits() / total : 0;
        }
    }

    public Dictionary<string, int> GetHitsByKey()
    {
        lock (_lockObject)
        {
            return new Dictionary<string, int>(_hits);
        }
    }
}
