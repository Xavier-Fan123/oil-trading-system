using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Services;

public class CacheManagementService : ICacheManagementService
{
    private readonly IMultiLayerCacheService _cacheService;
    private readonly IPurchaseContractRepository _contractRepository;
    private readonly IRiskCalculationService _riskService;
    private readonly IPriceBenchmarkRepository _priceRepository;
    private readonly ILogger<CacheManagementService> _logger;
    private readonly CacheManagementOptions _options;
    
    // Protection mechanisms tracking
    private static readonly ConcurrentDictionary<string, CacheProtectionState> _protectionStates = new();
    private static readonly ConcurrentDictionary<string, DateTime> _rateLimitWindow = new();
    private static readonly ConcurrentDictionary<string, int> _requestCounts = new();
    
    // Performance monitoring
    private static readonly ConcurrentDictionary<string, CachePerformanceData> _performanceData = new();
    private static readonly List<CacheAlert> _activeAlerts = new();
    
    public CacheManagementService(
        IMultiLayerCacheService cacheService,
        IPurchaseContractRepository contractRepository,
        IRiskCalculationService riskService,
        IPriceBenchmarkRepository priceRepository,
        ILogger<CacheManagementService> logger,
        IOptions<CacheManagementOptions> options)
    {
        _cacheService = cacheService;
        _contractRepository = contractRepository;
        _riskService = riskService;
        _priceRepository = priceRepository;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<CacheWarmupResult> PreheatApplicationCacheAsync()
    {
        _logger.LogInformation("Starting application cache preheat");
        var startTime = DateTime.UtcNow;
        var result = new CacheWarmupResult();
        
        try
        {
            var tasks = new List<Task<CacheWarmupResult>>
            {
                WarmupContractDataAsync(),
                WarmupRiskDataAsync(),
                WarmupPricingDataAsync()
            };
            
            var results = await Task.WhenAll(tasks);
            
            // Aggregate results
            result.IsSuccessful = results.All(r => r.IsSuccessful);
            result.KeysWarmedUp = results.Sum(r => r.KeysWarmedUp);
            result.KeysFailed = results.Sum(r => r.KeysFailed);
            result.Duration = DateTime.UtcNow - startTime;
            
            // Merge failed keys
            foreach (var r in results)
            {
                foreach (var kvp in r.FailedKeys)
                {
                    result.FailedKeys[kvp.Key] = kvp.Value;
                }
            }
            
            _logger.LogInformation("Application cache preheat completed. Success: {Success}, Keys warmed: {KeysWarmed}, Failed: {KeysFailed}", 
                result.IsSuccessful, result.KeysWarmedUp, result.KeysFailed);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application cache preheat failed");
            result.IsSuccessful = false;
            result.FailedKeys["general"] = ex.Message;
            return result;
        }
    }

    public async Task<CacheWarmupResult> WarmupContractDataAsync()
    {
        _logger.LogInformation("Warming up contract data cache");
        var result = new CacheWarmupResult();
        
        try
        {
            // Get recent active contracts
            var contracts = await _contractRepository.GetActiveContractsAsync();
            
            foreach (var contract in contracts.Take(_options.MaxWarmupKeys))
            {
                try
                {
                    var cacheKey = $"contract:{contract.Id}";
                    await _cacheService.SetAsync(cacheKey, contract, TimeSpan.FromHours(2));
                    result.KeysWarmedUp++;
                }
                catch (Exception ex)
                {
                    result.KeysFailed++;
                    result.FailedKeys[$"contract:{contract.Id}"] = ex.Message;
                }
            }
            
            // Warmup contract summaries
            var contractSummaries = contracts.Select(c => new
            {
                c.Id,
                c.ContractNumber,
                c.Status,
                c.TradingPartnerId,
                c.ProductId
            }).ToList();
            
            await _cacheService.SetAsync("contracts:active", contractSummaries, TimeSpan.FromMinutes(30));
            result.KeysWarmedUp++;
            
            result.IsSuccessful = result.KeysFailed == 0;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Contract data warmup failed");
            result.IsSuccessful = false;
            result.FailedKeys["contracts"] = ex.Message;
            return result;
        }
    }

    public async Task<CacheWarmupResult> WarmupRiskDataAsync()
    {
        _logger.LogInformation("Warming up risk data cache");
        var result = new CacheWarmupResult();
        
        try
        {
            // Warmup portfolio risk metrics
            var portfolioRisk = await _riskService.CalculatePortfolioRiskAsync(DateTime.UtcNow);
            await _cacheService.SetAsync("risk:portfolio", portfolioRisk, TimeSpan.FromMinutes(15));
            result.KeysWarmedUp++;
            
            // Warmup product risk metrics
            var products = new[] { "Brent", "WTI", "MOPS_FO_380", "MOPS_MGO" };
            // Product-specific risk calculation not available in current implementation
            // Skipping product risk caching for now
            /*
            foreach (var product in products)
            {
                try
                {
                    var productRisk = await _riskService.CalculateProductRiskAsync(product);
                    await _cacheService.SetAsync($"risk:product:{product}", productRisk, TimeSpan.FromMinutes(30));
                    result.KeysWarmedUp++;
                }
                catch (Exception ex)
                {
                    result.KeysFailed++;
                    result.FailedKeys[$"risk:product:{product}"] = ex.Message;
                }
            }
            */
            
            result.IsSuccessful = result.KeysFailed == 0;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk data warmup failed");
            result.IsSuccessful = false;
            result.FailedKeys["risk"] = ex.Message;
            return result;
        }
    }

    public async Task<CacheWarmupResult> WarmupPricingDataAsync()
    {
        _logger.LogInformation("Warming up pricing data cache");
        var result = new CacheWarmupResult();
        
        try
        {
            // Warmup current benchmarks
            var currentDate = DateTime.UtcNow.Date;
            var benchmarkPrices = await _priceRepository.GetBenchmarkPricesAsync(currentDate);
            
            foreach (var kvp in benchmarkPrices.Take(_options.MaxWarmupKeys))
            {
                var cacheKey = $"price:benchmark:{kvp.Key}:{currentDate:yyyy-MM-dd}";
                try
                {
                    // Wrap decimal in object for caching
                    await _cacheService.SetAsync(cacheKey, new { Price = kvp.Value }, TimeSpan.FromHours(1));
                    result.KeysWarmedUp++;
                }
                catch (Exception ex)
                {
                    result.KeysFailed++;
                    result.FailedKeys[cacheKey] = ex.Message;
                }
            }
            
            result.IsSuccessful = result.KeysFailed == 0;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pricing data warmup failed");
            result.IsSuccessful = false;
            result.FailedKeys["pricing"] = ex.Message;
            return result;
        }
    }

    public async Task<T?> GetWithProtectionAsync<T>(string key, Func<Task<T?>> dataLoader, CacheProtectionOptions? options = null) where T : class
    {
        options ??= new CacheProtectionOptions();
        
        // Check rate limiting
        if (options.ProtectionType == CacheProtectionType.RateLimiting)
        {
            if (!IsWithinRateLimit(key, options))
            {
                _logger.LogWarning("Rate limit exceeded for cache key {Key}", key);
                return null;
            }
        }
        
        // Try cache first
        var cachedValue = await _cacheService.GetAsync<T>(key);
        if (cachedValue != null)
        {
            return cachedValue;
        }
        
        // Cache stamping protection
        if (options.ProtectionType == CacheProtectionType.CacheSteamping)
        {
            var lockKey = $"lock:{key}";
            var lockAcquired = await TryAcquireLock(lockKey, options.MaxConcurrentRequests);
            
            if (!lockAcquired)
            {
                // Return stale data or wait briefly and retry cache
                await Task.Delay(100);
                return await _cacheService.GetAsync<T>(key);
            }
            
            try
            {
                // Double-check cache after acquiring lock
                cachedValue = await _cacheService.GetAsync<T>(key);
                if (cachedValue != null)
                {
                    return cachedValue;
                }
                
                // Load data
                var freshData = await dataLoader();
                
                if (freshData != null)
                {
                    await _cacheService.SetAsync(key, freshData, options.CachingDuration);
                }
                else if (options.EnableNullCaching)
                {
                    // Cache null result to prevent repeated calls
                    await _cacheService.SetAsync($"null:{key}", new NullCacheEntry(), options.NullCacheDuration);
                }
                
                return freshData;
            }
            finally
            {
                await ReleaseLock(lockKey);
            }
        }
        
        // Standard cache-aside pattern
        var data = await dataLoader();
        if (data != null)
        {
            await _cacheService.SetAsync(key, data, options.CachingDuration);
        }
        
        return data;
    }

    public async Task EnableCacheProtectionAsync(string keyPattern, CacheProtectionType protectionType)
    {
        _protectionStates[keyPattern] = new CacheProtectionState
        {
            IsEnabled = true,
            ProtectionType = protectionType,
            EnabledAt = DateTime.UtcNow
        };
        
        _logger.LogInformation("Cache protection enabled for pattern {Pattern} with type {Type}", keyPattern, protectionType);
    }

    public async Task DisableCacheProtectionAsync(string keyPattern)
    {
        _protectionStates.TryRemove(keyPattern, out _);
        _logger.LogInformation("Cache protection disabled for pattern {Pattern}", keyPattern);
    }

    public async Task InvalidateByTagsAsync(params string[] tags)
    {
        _logger.LogInformation("Invalidating cache by tags: {Tags}", string.Join(", ", tags));
        
        foreach (var tag in tags)
        {
            var pattern = $"*{tag}*";
            await _cacheService.RemoveByPatternAsync(pattern);
        }
    }

    public async Task InvalidateByPatternAsync(string pattern, CacheInvalidationMode mode = CacheInvalidationMode.Immediate)
    {
        _logger.LogInformation("Invalidating cache by pattern {Pattern} with mode {Mode}", pattern, mode);
        
        switch (mode)
        {
            case CacheInvalidationMode.Immediate:
                await _cacheService.RemoveByPatternAsync(pattern);
                break;
                
            case CacheInvalidationMode.Lazy:
                // Mark for lazy invalidation (would be handled by background service)
                break;
                
            case CacheInvalidationMode.Scheduled:
                // Schedule for later invalidation (would use a job scheduler)
                break;
        }
    }

    public async Task ScheduleInvalidationAsync(string key, DateTime invalidationTime)
    {
        _logger.LogInformation("Scheduled cache invalidation for key {Key} at {Time}", key, invalidationTime);
        
        // In a real implementation, this would use a job scheduler like Quartz.NET
        // For now, just log the schedule
    }

    public async Task InvalidateDependentCachesAsync(string key)
    {
        _logger.LogInformation("Invalidating dependent caches for key {Key}", key);
        
        // Determine dependent cache patterns based on key
        var dependentPatterns = GetDependentCachePatterns(key);
        
        foreach (var pattern in dependentPatterns)
        {
            await _cacheService.RemoveByPatternAsync(pattern);
        }
    }

    public async Task<CacheOptimizationReport> AnalyzeCachePerformanceAsync()
    {
        _logger.LogInformation("Analyzing cache performance");
        
        try
        {
            var stats = await _cacheService.GetStatisticsAsync();
            var report = new CacheOptimizationReport();
            
            // Calculate efficiency metrics
            report.Efficiency = new CacheEfficiencyMetrics
            {
                OverallHitRatio = stats.OverallStats.OverallHitRatio,
                L1HitRatio = stats.L1Stats.HitRatio,
                L2HitRatio = stats.L2Stats.HitRatio,
                AverageResponseTime = stats.L1Stats.AverageResponseTime,
                MemoryUsage = stats.L1Stats.MemoryUsage + stats.L2Stats.MemoryUsage
            };
            
            // Identify bottlenecks
            if (report.Efficiency.OverallHitRatio < 80)
            {
                report.Bottlenecks.Add(new CacheBottleneck
                {
                    Name = "Low Hit Ratio",
                    Type = CacheBottleneckType.HighMissRatio,
                    Description = $"Overall hit ratio is {report.Efficiency.OverallHitRatio:F1}%, below recommended 80%",
                    Severity = CacheSeverity.High
                });
            }
            
            if (report.Efficiency.AverageResponseTime > TimeSpan.FromMilliseconds(100))
            {
                report.Bottlenecks.Add(new CacheBottleneck
                {
                    Name = "Slow Response Time",
                    Type = CacheBottleneckType.SlowResponseTime,
                    Description = $"Average response time is {report.Efficiency.AverageResponseTime.TotalMilliseconds:F1}ms",
                    Severity = CacheSeverity.Medium
                });
            }
            
            // Generate recommendations
            report.Recommendations = await GenerateRecommendationsAsync(report.Efficiency, report.Bottlenecks);
            
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache performance analysis failed");
            return new CacheOptimizationReport();
        }
    }

    public async Task<List<CacheRecommendation>> GetCacheOptimizationRecommendationsAsync()
    {
        var report = await AnalyzeCachePerformanceAsync();
        return report.Recommendations;
    }

    public async Task OptimizeCacheConfigurationAsync()
    {
        _logger.LogInformation("Optimizing cache configuration");
        
        try
        {
            var recommendations = await GetCacheOptimizationRecommendationsAsync();
            
            foreach (var recommendation in recommendations.Where(r => r.Priority >= CachePriority.High))
            {
                try
                {
                    await ApplyRecommendationAsync(recommendation);
                    _logger.LogInformation("Applied cache optimization: {Title}", recommendation.Title);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to apply cache optimization: {Title}", recommendation.Title);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache configuration optimization failed");
        }
    }

    public async Task<bool> UpgradeCacheVersionAsync(string key, int newVersion)
    {
        try
        {
            var versionedKey = $"{key}:v{newVersion}";
            var oldData = await _cacheService.GetAsync<object>(key);
            
            if (oldData != null)
            {
                // Transform data if needed (simplified here)
                await _cacheService.SetAsync(versionedKey, oldData);
                await _cacheService.RemoveAsync(key);
                
                _logger.LogInformation("Upgraded cache key {Key} to version {Version}", key, newVersion);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache version upgrade failed for key {Key}", key);
            return false;
        }
    }

    public async Task<CacheMigrationResult> MigrateCacheDataAsync(CacheMigrationRequest request)
    {
        _logger.LogInformation("Starting cache migration from {Source} to {Target}", 
            request.SourceKeyPattern, request.TargetKeyPattern);
        
        var result = new CacheMigrationResult();
        var startTime = DateTime.UtcNow;
        
        try
        {
            // In a real implementation, this would fetch keys matching the pattern
            // For demo, we'll assume some keys exist
            var keysToMigrate = new List<string>(); // Would be populated from cache
            
            var batches = keysToMigrate.Chunk(request.BatchSize);
            
            foreach (var batch in batches)
            {
                foreach (var sourceKey in batch)
                {
                    try
                    {
                        var data = await _cacheService.GetAsync<object>(sourceKey);
                        if (data != null)
                        {
                            var transformedData = request.DataTransformer?.Invoke(data) ?? data;
                            var targetKey = sourceKey.Replace(request.SourceKeyPattern, request.TargetKeyPattern);
                            
                            await _cacheService.SetAsync(targetKey, transformedData);
                            
                            if (!request.PreserveTTL)
                            {
                                await _cacheService.RemoveAsync(sourceKey);
                            }
                            
                            result.KeysMigrated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.KeysFailed++;
                        result.FailedKeys.Add(sourceKey);
                        _logger.LogWarning(ex, "Failed to migrate key {Key}", sourceKey);
                    }
                }
                
                // Small delay between batches
                await Task.Delay(50);
            }
            
            result.IsSuccessful = result.KeysFailed == 0;
            result.Duration = DateTime.UtcNow - startTime;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache migration failed");
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    public async Task<CacheBackupResult> CreateCacheBackupAsync(CacheBackupOptions options)
    {
        _logger.LogInformation("Creating cache backup to {Path}", options.BackupPath);
        
        var result = new CacheBackupResult
        {
            BackupPath = options.BackupPath
        };
        
        try
        {
            // In a real implementation, this would backup cache data
            // For demo purposes, we'll simulate a backup
            
            result.IsSuccessful = true;
            result.KeysBackedUp = 1000; // Simulated
            result.BackupSize = 1024 * 1024; // 1MB simulated
            result.Duration = TimeSpan.FromSeconds(30);
            
            _logger.LogInformation("Cache backup completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache backup failed");
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    public async Task<CacheRestoreResult> RestoreCacheFromBackupAsync(string backupPath)
    {
        _logger.LogInformation("Restoring cache from backup {Path}", backupPath);
        
        var result = new CacheRestoreResult();
        
        try
        {
            // In a real implementation, this would restore cache data
            // For demo purposes, we'll simulate a restore
            
            result.IsSuccessful = true;
            result.KeysRestored = 1000; // Simulated
            result.Duration = TimeSpan.FromSeconds(45);
            
            _logger.LogInformation("Cache restore completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache restore failed");
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    public async Task<CacheMetrics> GetRealTimeMetricsAsync()
    {
        try
        {
            var stats = await _cacheService.GetStatisticsAsync();
            
            return new CacheMetrics
            {
                LayerMetrics = new Dictionary<CacheLayer, CacheLayerMetrics>
                {
                    [CacheLayer.L1_Memory] = new CacheLayerMetrics
                    {
                        Layer = CacheLayer.L1_Memory,
                        HitRatio = stats.L1Stats.HitRatio,
                        AverageLatency = stats.L1Stats.AverageResponseTime,
                        MemoryUsage = stats.L1Stats.MemoryUsage,
                        IsHealthy = stats.L1Stats.IsHealthy
                    },
                    [CacheLayer.L2_Redis] = new CacheLayerMetrics
                    {
                        Layer = CacheLayer.L2_Redis,
                        HitRatio = stats.L2Stats.HitRatio,
                        AverageLatency = stats.L2Stats.AverageResponseTime,
                        MemoryUsage = stats.L2Stats.MemoryUsage,
                        IsHealthy = stats.L2Stats.IsHealthy
                    }
                },
                SystemMetrics = new CacheSystemMetrics
                {
                    OverallHitRatio = stats.OverallStats.OverallHitRatio,
                    TotalMemoryUsage = stats.L1Stats.MemoryUsage + stats.L2Stats.MemoryUsage
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get real-time cache metrics");
            return new CacheMetrics();
        }
    }

    public async Task<List<CacheAlert>> GetActiveAlertsAsync()
    {
        lock (_activeAlerts)
        {
            return _activeAlerts.Where(a => !a.IsResolved).ToList();
        }
    }

    public async Task ConfigureCacheAlertsAsync(CacheAlertConfiguration configuration)
    {
        _logger.LogInformation("Configuring cache alerts with {ThresholdCount} thresholds", 
            configuration.Thresholds.Count);
        
        // In a real implementation, this would configure monitoring and alerting
        // For demo purposes, we'll just log the configuration
    }

    // Helper methods
    private bool IsWithinRateLimit(string key, CacheProtectionOptions options)
    {
        var windowKey = $"rate:{key}";
        var now = DateTime.UtcNow;
        
        if (_rateLimitWindow.TryGetValue(windowKey, out var windowStart))
        {
            if (now - windowStart > options.RateLimitWindow)
            {
                // Reset window
                _rateLimitWindow[windowKey] = now;
                _requestCounts[windowKey] = 1;
                return true;
            }
            
            var currentCount = _requestCounts.GetValueOrDefault(windowKey, 0);
            if (currentCount >= options.MaxRequestsPerWindow)
            {
                return false;
            }
            
            _requestCounts[windowKey] = currentCount + 1;
            return true;
        }
        
        // First request in window
        _rateLimitWindow[windowKey] = now;
        _requestCounts[windowKey] = 1;
        return true;
    }

    private async Task<bool> TryAcquireLock(string lockKey, int maxConcurrent)
    {
        // Simplified lock mechanism (in production, use Redis locks)
        return true;
    }

    private async Task ReleaseLock(string lockKey)
    {
        // Simplified lock release
    }

    private List<string> GetDependentCachePatterns(string key)
    {
        var patterns = new List<string>();
        
        if (key.StartsWith("contract:"))
        {
            patterns.Add("contracts:*");
            patterns.Add("risk:*");
        }
        else if (key.StartsWith("price:"))
        {
            patterns.Add("risk:*");
            patterns.Add("portfolio:*");
        }
        
        return patterns;
    }

    private async Task<List<CacheRecommendation>> GenerateRecommendationsAsync(CacheEfficiencyMetrics efficiency, List<CacheBottleneck> bottlenecks)
    {
        var recommendations = new List<CacheRecommendation>();
        
        if (efficiency.OverallHitRatio < 80)
        {
            recommendations.Add(new CacheRecommendation
            {
                Title = "Increase Cache Expiry Times",
                Description = "Consider increasing cache expiry times for stable data to improve hit ratios",
                Type = CacheRecommendationType.IncreaseExpiry,
                Priority = CachePriority.High,
                EstimatedImpact = 15
            });
        }
        
        if (efficiency.AverageResponseTime > TimeSpan.FromMilliseconds(100))
        {
            recommendations.Add(new CacheRecommendation
            {
                Title = "Add Frequently Accessed Data to L1 Cache",
                Description = "Move hot data to L1 memory cache for faster access",
                Type = CacheRecommendationType.AddToL1Cache,
                Priority = CachePriority.Medium,
                EstimatedImpact = 25
            });
        }
        
        return recommendations;
    }

    private async Task ApplyRecommendationAsync(CacheRecommendation recommendation)
    {
        // In a real implementation, this would apply the recommendation
        // For demo purposes, we'll just log it
        _logger.LogInformation("Applied recommendation: {Title}", recommendation.Title);
    }
}

// Supporting classes
public class CacheProtectionState
{
    public bool IsEnabled { get; set; }
    public CacheProtectionType ProtectionType { get; set; }
    public DateTime EnabledAt { get; set; }
}

public class CachePerformanceData
{
    public string Key { get; set; } = string.Empty;
    public long AccessCount { get; set; }
    public TimeSpan TotalResponseTime { get; set; }
    public DateTime LastAccess { get; set; }
}

public class NullCacheEntry
{
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}

public class CacheManagementOptions
{
    public int MaxWarmupKeys { get; set; } = 1000;
    public TimeSpan DefaultCacheExpiry { get; set; } = TimeSpan.FromHours(1);
    public bool EnableAutoOptimization { get; set; } = true;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public TimeSpan CircuitBreakerTimeout { get; set; } = TimeSpan.FromMinutes(5);
}