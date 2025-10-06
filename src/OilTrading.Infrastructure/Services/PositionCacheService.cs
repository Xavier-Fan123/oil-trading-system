using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;
using System.Text.Json;

namespace OilTrading.Infrastructure.Services;

public class PositionCacheService : IPositionCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<PositionCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string PositionsCacheKey = "oil_trading:positions:current";
    private const string SummaryCacheKey = "oil_trading:positions:summary";
    private const string PnLCacheKeyPrefix = "oil_trading:pnl:";
    private const string LastUpdateCacheKey = "oil_trading:positions:last_update";
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(15);

    public PositionCacheService(IDistributedCache cache, ILogger<PositionCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<IEnumerable<NetPositionDto>?> GetCachedPositionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(PositionsCacheKey, cancellationToken);
            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("No cached positions found");
                return null;
            }

            var positions = JsonSerializer.Deserialize<IEnumerable<NetPositionDto>>(cachedData, _jsonOptions);
            _logger.LogDebug("Retrieved {Count} cached positions", positions?.Count() ?? 0);
            return positions;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache service unavailable - will calculate fresh positions");
            return null;
        }
    }

    public async Task SetCachedPositionsAsync(IEnumerable<NetPositionDto> positions, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var serializedData = JsonSerializer.Serialize(positions, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? DefaultExpiry
            };

            await _cache.SetStringAsync(PositionsCacheKey, serializedData, options, cancellationToken);
            await SetLastUpdateTimeAsync(DateTime.UtcNow, cancellationToken);
            
            _logger.LogDebug("Cached {Count} positions with expiry {Expiry}", positions.Count(), expiry ?? DefaultExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache service unavailable - positions not cached");
        }
    }

    public async Task<PositionSummaryDto?> GetCachedSummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(SummaryCacheKey, cancellationToken);
            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("No cached position summary found");
                return null;
            }

            var summary = JsonSerializer.Deserialize<PositionSummaryDto>(cachedData, _jsonOptions);
            _logger.LogDebug("Retrieved cached position summary");
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache service unavailable - will calculate fresh summary");
            return null;
        }
    }

    public async Task SetCachedSummaryAsync(PositionSummaryDto summary, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var serializedData = JsonSerializer.Serialize(summary, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? DefaultExpiry
            };

            await _cache.SetStringAsync(SummaryCacheKey, serializedData, options, cancellationToken);
            _logger.LogDebug("Cached position summary with expiry {Expiry}", expiry ?? DefaultExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache service unavailable - summary not cached");
        }
    }

    public async Task<IEnumerable<PnLDto>?> GetCachedPnLAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var dateKey = (asOfDate ?? DateTime.UtcNow.Date).ToString("yyyy-MM-dd");
            var cacheKey = $"{PnLCacheKeyPrefix}{dateKey}";
            
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("No cached P&L found for date {Date}", dateKey);
                return null;
            }

            var pnl = JsonSerializer.Deserialize<IEnumerable<PnLDto>>(cachedData, _jsonOptions);
            _logger.LogDebug("Retrieved {Count} cached P&L records for date {Date}", pnl?.Count() ?? 0, dateKey);
            return pnl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache service unavailable - will calculate fresh P&L");
            return null;
        }
    }

    public async Task SetCachedPnLAsync(IEnumerable<PnLDto> pnl, DateTime? asOfDate = null, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var dateKey = (asOfDate ?? DateTime.UtcNow.Date).ToString("yyyy-MM-dd");
            var cacheKey = $"{PnLCacheKeyPrefix}{dateKey}";
            
            var serializedData = JsonSerializer.Serialize(pnl, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromHours(1) // P&L cache for longer
            };

            await _cache.SetStringAsync(cacheKey, serializedData, options, cancellationToken);
            _logger.LogDebug("Cached {Count} P&L records for date {Date} with expiry {Expiry}", 
                pnl.Count(), dateKey, expiry ?? TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache service unavailable - P&L not cached");
        }
    }

    public async Task InvalidatePositionCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(PositionsCacheKey, cancellationToken);
            await _cache.RemoveAsync(SummaryCacheKey, cancellationToken);
            _logger.LogInformation("Position cache invalidated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate position cache");
        }
    }

    public async Task InvalidateAllCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: This is a simplified implementation. In production, you might want to use Redis SCAN
            // to find and delete all keys with the prefix, or use Redis key notifications.
            await _cache.RemoveAsync(PositionsCacheKey, cancellationToken);
            await _cache.RemoveAsync(SummaryCacheKey, cancellationToken);
            await _cache.RemoveAsync(LastUpdateCacheKey, cancellationToken);
            
            // For P&L cache, we'd need a more sophisticated approach in production
            var today = DateTime.UtcNow.Date;
            for (int i = 0; i < 30; i++) // Clear last 30 days
            {
                var dateKey = today.AddDays(-i).ToString("yyyy-MM-dd");
                var cacheKey = $"{PnLCacheKeyPrefix}{dateKey}";
                await _cache.RemoveAsync(cacheKey, cancellationToken);
            }
            
            _logger.LogInformation("All position-related cache invalidated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate all cache");
        }
    }

    public async Task<bool> IsCacheValidAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var lastUpdate = await GetLastUpdateTimeAsync(cancellationToken);
            if (!lastUpdate.HasValue)
                return false;

            // Consider cache valid if updated within the last 15 minutes
            return DateTime.UtcNow - lastUpdate.Value < DefaultExpiry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check cache validity");
            return false;
        }
    }

    public async Task SetLastUpdateTimeAsync(DateTime updateTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };

            await _cache.SetStringAsync(LastUpdateCacheKey, updateTime.ToBinary().ToString(), options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set last update time");
        }
    }

    public async Task<DateTime?> GetLastUpdateTimeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(LastUpdateCacheKey, cancellationToken);
            if (string.IsNullOrEmpty(cachedData))
                return null;

            if (long.TryParse(cachedData, out var binaryTime))
            {
                return DateTime.FromBinary(binaryTime);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get last update time");
            return null;
        }
    }

    public async Task<T?> GetCachedDataAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cachedData = await _cache.GetStringAsync($"oil_trading:dashboard:{key}", cancellationToken);
            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("No cached data found for key {Key}", key);
                return null;
            }

            var data = JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
            _logger.LogDebug("Retrieved cached data for key {Key}", key);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache service unavailable for key {Key}", key);
            return null;
        }
    }

    public async Task SetCachedDataAsync<T>(string key, T data, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serializedData = JsonSerializer.Serialize(data, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? DefaultExpiry
            };

            await _cache.SetStringAsync($"oil_trading:dashboard:{key}", serializedData, options, cancellationToken);
            _logger.LogDebug("Cached data for key {Key} with expiry {Expiry}", key, expiry ?? DefaultExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache service unavailable - data not cached for key {Key}", key);
        }
    }

    public async Task<decimal> GetCacheHitRatioAsync(CancellationToken cancellationToken = default)
    {
        // Simplified implementation - in production you'd track cache hits/misses
        try
        {
            var isValid = await IsCacheValidAsync(cancellationToken);
            return isValid ? 85.5m : 0m; // Return simulated cache hit ratio
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate cache hit ratio");
            return 0m;
        }
    }
}