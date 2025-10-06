namespace OilTrading.Application.Services;

public interface IMultiLayerCacheService
{
    // Basic cache operations with multi-layer support
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    
    // Layer-specific operations
    Task<T?> GetFromLayerAsync<T>(string key, CacheLayer layer) where T : class;
    Task SetToLayerAsync<T>(string key, T value, CacheLayer layer, TimeSpan? expiry = null) where T : class;
    
    // Cache warming and preheating
    Task WarmupCacheAsync(CacheWarmupRequest request);
    Task PreheatCacheAsync(string cacheGroup);
    
    // Cache protection and circuit breaker
    Task<T?> GetWithFallbackAsync<T>(string key, Func<Task<T?>> fallbackFunction, TimeSpan? expiry = null) where T : class;
    
    // Cache statistics and health
    Task<CacheStatistics> GetStatisticsAsync();
    Task<CacheHealthStatus> GetHealthStatusAsync();
    
    // Bulk operations
    Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) where T : class;
    Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiry = null) where T : class;
    
    // Cache synchronization
    Task SynchronizeCacheAsync(string key);
    Task InvalidateDistributedCacheAsync(string key);
}

public enum CacheLayer
{
    L1_Memory = 1,      // In-process memory cache
    L2_Redis = 2,       // Redis distributed cache
    L3_Distributed = 3  // External distributed cache system
}

public class CacheWarmupRequest
{
    public string CacheGroup { get; set; } = string.Empty;
    public List<string> Keys { get; set; } = new();
    public CacheLayer TargetLayer { get; set; } = CacheLayer.L1_Memory;
    public bool WarmAllLayers { get; set; } = true;
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromHours(1);
    public int BatchSize { get; set; } = 100;
}

public class CacheStatistics
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public CacheLayerStats L1Stats { get; set; } = new();
    public CacheLayerStats L2Stats { get; set; } = new();
    public CacheLayerStats L3Stats { get; set; } = new();
    public CacheOverallStats OverallStats { get; set; } = new();
}

public class CacheLayerStats
{
    public CacheLayer Layer { get; set; }
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public long TotalRequests { get; set; }
    public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests * 100 : 0;
    public long ItemCount { get; set; }
    public long MemoryUsage { get; set; } // in bytes
    public TimeSpan AverageResponseTime { get; set; }
    public DateTime LastHit { get; set; }
    public bool IsHealthy { get; set; } = true;
    public string? HealthMessage { get; set; }
}

public class CacheOverallStats
{
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public long TotalRequests { get; set; }
    public double OverallHitRatio => TotalRequests > 0 ? (double)TotalHits / TotalRequests * 100 : 0;
    public Dictionary<string, int> PopularKeys { get; set; } = new();
    public Dictionary<string, TimeSpan> AverageResponseTimeByOperation { get; set; } = new();
}

public class CacheHealthStatus
{
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public bool IsHealthy { get; set; }
    public Dictionary<CacheLayer, bool> LayerHealth { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, string> AdditionalInfo { get; set; } = new();
}