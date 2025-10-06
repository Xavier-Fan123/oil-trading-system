namespace OilTrading.Application.Services;

public interface ICacheManagementService
{
    // Cache preheating and warming
    Task<CacheWarmupResult> PreheatApplicationCacheAsync();
    Task<CacheWarmupResult> WarmupContractDataAsync();
    Task<CacheWarmupResult> WarmupRiskDataAsync();
    Task<CacheWarmupResult> WarmupPricingDataAsync();
    
    // Cache protection mechanisms
    Task<T?> GetWithProtectionAsync<T>(string key, Func<Task<T?>> dataLoader, CacheProtectionOptions? options = null) where T : class;
    Task EnableCacheProtectionAsync(string keyPattern, CacheProtectionType protectionType);
    Task DisableCacheProtectionAsync(string keyPattern);
    
    // Cache invalidation strategies
    Task InvalidateByTagsAsync(params string[] tags);
    Task InvalidateByPatternAsync(string pattern, CacheInvalidationMode mode = CacheInvalidationMode.Immediate);
    Task ScheduleInvalidationAsync(string key, DateTime invalidationTime);
    Task InvalidateDependentCachesAsync(string key);
    
    // Cache monitoring and optimization
    Task<CacheOptimizationReport> AnalyzeCachePerformanceAsync();
    Task<List<CacheRecommendation>> GetCacheOptimizationRecommendationsAsync();
    Task OptimizeCacheConfigurationAsync();
    
    // Cache versioning and migration
    Task<bool> UpgradeCacheVersionAsync(string key, int newVersion);
    Task<CacheMigrationResult> MigrateCacheDataAsync(CacheMigrationRequest request);
    
    // Cache backup and recovery
    Task<CacheBackupResult> CreateCacheBackupAsync(CacheBackupOptions options);
    Task<CacheRestoreResult> RestoreCacheFromBackupAsync(string backupPath);
    
    // Real-time cache monitoring
    Task<CacheMetrics> GetRealTimeMetricsAsync();
    Task<List<CacheAlert>> GetActiveAlertsAsync();
    Task ConfigureCacheAlertsAsync(CacheAlertConfiguration configuration);
}

public class CacheProtectionOptions
{
    public CacheProtectionType ProtectionType { get; set; } = CacheProtectionType.RateLimiting;
    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);
    public int MaxRequestsPerWindow { get; set; } = 100;
    public TimeSpan CachingDuration { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableNullCaching { get; set; } = true;
    public TimeSpan NullCacheDuration { get; set; } = TimeSpan.FromMinutes(1);
    public int MaxConcurrentRequests { get; set; } = 10;
}

public enum CacheProtectionType
{
    RateLimiting,
    CacheSteamping,
    NullCaching,
    ConcurrencyControl
}

public enum CacheInvalidationMode
{
    Immediate,
    Lazy,
    Scheduled
}

public class CacheWarmupResult
{
    public bool IsSuccessful { get; set; }
    public int KeysWarmedUp { get; set; }
    public int KeysFailed { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, string> FailedKeys { get; set; } = new();
    public CacheWarmupStatistics Statistics { get; set; } = new();
}

public class CacheWarmupStatistics
{
    public long TotalDataLoaded { get; set; } // in bytes
    public Dictionary<CacheLayer, int> KeysByLayer { get; set; } = new();
    public Dictionary<string, TimeSpan> AverageLoadTimeByType { get; set; } = new();
}

public class CacheOptimizationReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public CacheEfficiencyMetrics Efficiency { get; set; } = new();
    public List<CacheBottleneck> Bottlenecks { get; set; } = new();
    public List<CacheRecommendation> Recommendations { get; set; } = new();
    public Dictionary<string, CacheUsagePattern> UsagePatterns { get; set; } = new();
}

public class CacheEfficiencyMetrics
{
    public double OverallHitRatio { get; set; }
    public double L1HitRatio { get; set; }
    public double L2HitRatio { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public long MemoryUsage { get; set; }
    public int HotKeys { get; set; }
    public int ColdKeys { get; set; }
}

public class CacheBottleneck
{
    public string Name { get; set; } = string.Empty;
    public CacheBottleneckType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public CacheSeverity Severity { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public enum CacheBottleneckType
{
    HighMissRatio,
    SlowResponseTime,
    MemoryPressure,
    HotKeyContention,
    NetworkLatency
}

public enum CacheSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class CacheRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CacheRecommendationType Type { get; set; }
    public CachePriority Priority { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public decimal EstimatedImpact { get; set; } // Percentage improvement
}

public enum CacheRecommendationType
{
    IncreaseExpiry,
    DecreaseExpiry,
    AddToL1Cache,
    RemoveFromCache,
    ChangeEvictionPolicy,
    IncreaseMemoryAllocation,
    OptimizeKeyStructure
}

public enum CachePriority
{
    Low,
    Medium,
    High,
    Critical
}

public class CacheUsagePattern
{
    public string KeyPattern { get; set; } = string.Empty;
    public int AccessCount { get; set; }
    public DateTime FirstAccess { get; set; }
    public DateTime LastAccess { get; set; }
    public TimeSpan AverageAccessInterval { get; set; }
    public Dictionary<int, int> HourlyAccessDistribution { get; set; } = new();
}

public class CacheMigrationRequest
{
    public string SourceKeyPattern { get; set; } = string.Empty;
    public string TargetKeyPattern { get; set; } = string.Empty;
    public int SourceVersion { get; set; }
    public int TargetVersion { get; set; }
    public Func<object, object>? DataTransformer { get; set; }
    public bool PreserveTTL { get; set; } = true;
    public int BatchSize { get; set; } = 100;
}

public class CacheMigrationResult
{
    public bool IsSuccessful { get; set; }
    public int KeysMigrated { get; set; }
    public int KeysFailed { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> FailedKeys { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class CacheBackupOptions
{
    public List<string> KeyPatterns { get; set; } = new();
    public List<CacheLayer> Layers { get; set; } = new();
    public string BackupPath { get; set; } = string.Empty;
    public bool CompressBackup { get; set; } = true;
    public bool IncludeMetadata { get; set; } = true;
    public DateTime? BackupTimestamp { get; set; }
}

public class CacheBackupResult
{
    public bool IsSuccessful { get; set; }
    public string BackupPath { get; set; } = string.Empty;
    public int KeysBackedUp { get; set; }
    public long BackupSize { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CacheRestoreResult
{
    public bool IsSuccessful { get; set; }
    public int KeysRestored { get; set; }
    public int KeysFailed { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> FailedKeys { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class CacheMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<CacheLayer, CacheLayerMetrics> LayerMetrics { get; set; } = new();
    public CacheSystemMetrics SystemMetrics { get; set; } = new();
    public List<CacheTopKey> TopKeys { get; set; } = new();
}

public class CacheLayerMetrics
{
    public CacheLayer Layer { get; set; }
    public long RequestsPerSecond { get; set; }
    public double HitRatio { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public long MemoryUsage { get; set; }
    public int KeyCount { get; set; }
    public bool IsHealthy { get; set; }
}

public class CacheSystemMetrics
{
    public long TotalRequestsPerSecond { get; set; }
    public double OverallHitRatio { get; set; }
    public long TotalMemoryUsage { get; set; }
    public int TotalKeyCount { get; set; }
    public double CpuUsage { get; set; }
    public double NetworkBandwidth { get; set; }
}

public class CacheTopKey
{
    public string Key { get; set; } = string.Empty;
    public long RequestCount { get; set; }
    public double HitRatio { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public long DataSize { get; set; }
}

public class CacheAlert
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public CacheAlertSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public enum CacheAlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public class CacheAlertConfiguration
{
    public List<CacheThreshold> Thresholds { get; set; } = new();
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(1);
    public bool EnableEmailAlerts { get; set; }
    public bool EnableSlackAlerts { get; set; }
    public List<string> AlertRecipients { get; set; } = new();
}

public class CacheThreshold
{
    public string Name { get; set; } = string.Empty;
    public CacheMetricType MetricType { get; set; }
    public double WarningThreshold { get; set; }
    public double CriticalThreshold { get; set; }
    public string? KeyPattern { get; set; }
    public CacheLayer? Layer { get; set; }
}

public enum CacheMetricType
{
    HitRatio,
    ResponseTime,
    MemoryUsage,
    RequestRate,
    ErrorRate
}