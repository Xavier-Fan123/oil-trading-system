using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using System.Collections.Concurrent;

namespace OilTrading.Application.Services;

/// <summary>
/// 多层缓存服务实现
/// </summary>
public class MultiLayerCacheService : IMultiLayerCacheService
{
    private readonly IMemoryCache _l1Cache;           // L1: 内存缓存
    private readonly IDatabase _l2Cache;              // L2: Redis缓存
    private readonly ILogger<MultiLayerCacheService> _logger;
    private readonly MultiLayerCacheOptions _options;
    private readonly CacheStatisticsCollector _statistics;

    public MultiLayerCacheService(
        IMemoryCache memoryCache,
        IConnectionMultiplexer redis,
        ILogger<MultiLayerCacheService> logger,
        IOptions<MultiLayerCacheOptions> options)
    {
        _l1Cache = memoryCache;
        _l2Cache = redis.GetDatabase();
        _logger = logger;
        _options = options.Value;
        _statistics = new CacheStatisticsCollector();
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var fullKey = GetFullKey(key);

        try
        {
            // 尝试从L1缓存获取
            if (_l1Cache.TryGetValue(fullKey, out T? l1Value))
            {
                _statistics.RecordHit(CacheLayer.L1_Memory);
                _logger.LogDebug("Cache hit in L1 for key: {Key}", key);
                return l1Value;
            }

            // 尝试从L2缓存获取
            if (_options.EnableL2Cache)
            {
                var l2Value = await GetFromL2CacheAsync<T>(fullKey);
                if (l2Value != null)
                {
                    _statistics.RecordHit(CacheLayer.L2_Redis);
                    _logger.LogDebug("Cache hit in L2 for key: {Key}", key);

                    // 将数据提升到L1缓存
                    await PromoteToL1CacheAsync(fullKey, l2Value);
                    return l2Value;
                }
            }

            _statistics.RecordMiss();
            _logger.LogDebug("Cache miss for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogWarning(ex, "Error retrieving from cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        if (value == null) return;

        var fullKey = GetFullKey(key);
        var effectiveExpiry = expiry ?? _options.DefaultExpiry;

        try
        {
            // 设置L1缓存
            var memoryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = effectiveExpiry,
                Priority = GetCachePriority(key),
                Size = EstimateSize(value)
            };

            _l1Cache.Set(fullKey, value, memoryOptions);
            _logger.LogDebug("Set L1 cache for key: {Key}, expiry: {Expiry}", key, effectiveExpiry);

            // 设置L2缓存
            if (_options.EnableL2Cache)
            {
                await SetL2CacheAsync(fullKey, value, effectiveExpiry);
                _logger.LogDebug("Set L2 cache for key: {Key}, expiry: {Expiry}", key, effectiveExpiry);
            }

            _statistics.RecordSet(CacheLayer.L1_Memory);
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogWarning(ex, "Error setting cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        var fullKey = GetFullKey(key);

        try
        {
            // 从L1缓存移除
            _l1Cache.Remove(fullKey);

            // 从L2缓存移除
            if (_options.EnableL2Cache)
            {
                await _l2Cache.KeyDeleteAsync(fullKey);
            }

            _logger.LogDebug("Removed cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogWarning(ex, "Error removing cache for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            // 清空L1缓存（内存缓存不支持模式匹配，需要重建缓存实例或使用标记）
            if (_l1Cache is MemoryCache mc)
            {
                mc.Clear();
            }

            // 清空L2缓存（支持模式匹配）
            if (_options.EnableL2Cache && pattern != "*")
            {
                var server = _l2Cache.Multiplexer.GetServer(_l2Cache.Multiplexer.GetEndPoints()[0]);
                var keys = server.Keys(pattern: $"{_options.KeyPrefix}:{pattern}");
                
                var batch = _l2Cache.CreateBatch();
                foreach (var key in keys)
                {
                    batch.KeyDeleteAsync(key);
                }
                batch.Execute();
            }

            _logger.LogInformation("Cleared cache with pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogWarning(ex, "Error clearing cache with pattern: {Pattern}", pattern);
        }
    }

    public async Task<T?> GetFromLayerAsync<T>(string key, CacheLayer layer) where T : class
    {
        var fullKey = GetFullKey(key);

        try
        {
            switch (layer)
            {
                case CacheLayer.L1_Memory:
                    if (_l1Cache.TryGetValue(fullKey, out T? l1Value))
                    {
                        _statistics.RecordHit(CacheLayer.L1_Memory);
                        return l1Value;
                    }
                    break;

                case CacheLayer.L2_Redis:
                    if (_options.EnableL2Cache)
                    {
                        var l2Value = await GetFromL2CacheAsync<T>(fullKey);
                        if (l2Value != null)
                        {
                            _statistics.RecordHit(CacheLayer.L2_Redis);
                            return l2Value;
                        }
                    }
                    break;
            }

            _statistics.RecordMiss();
            return null;
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogWarning(ex, "Error retrieving from layer {Layer} for key: {Key}", layer, key);
            return null;
        }
    }

    public async Task SetToLayerAsync<T>(string key, T value, CacheLayer layer, TimeSpan? expiry = null) where T : class
    {
        if (value == null) return;

        var fullKey = GetFullKey(key);
        var effectiveExpiry = expiry ?? _options.DefaultExpiry;

        try
        {
            switch (layer)
            {
                case CacheLayer.L1_Memory:
                    var memoryOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = effectiveExpiry,
                        Priority = GetCachePriority(key),
                        Size = EstimateSize(value)
                    };
                    _l1Cache.Set(fullKey, value, memoryOptions);
                    break;

                case CacheLayer.L2_Redis:
                    if (_options.EnableL2Cache)
                    {
                        await SetL2CacheAsync(fullKey, value, effectiveExpiry);
                    }
                    break;

                case CacheLayer.L3_Distributed:
                    // Placeholder for external distributed cache
                    break;
            }

            _statistics.RecordSet(layer);
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogWarning(ex, "Error setting cache for layer {Layer} and key: {Key}", layer, key);
        }
    }

    public async Task WarmupCacheAsync(CacheWarmupRequest request)
    {
        _logger.LogInformation("Starting cache warmup for group: {CacheGroup}", request.CacheGroup);

        try
        {
            var warmupTasks = request.Keys.Select(async key =>
            {
                try
                {
                    _logger.LogDebug("Warming up cache for key: {Key}", key);
                    // 暂时跳过实际实现，等待业务逻辑集成
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warmup cache for key: {Key}", key);
                }
            });

            await Task.WhenAll(warmupTasks);
            _logger.LogInformation("Cache warmup completed for group: {CacheGroup}", request.CacheGroup);
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogError(ex, "Error during cache warmup for group: {CacheGroup}", request.CacheGroup);
        }
    }

    public async Task PreheatCacheAsync(string cacheGroup)
    {
        _logger.LogInformation("Starting cache preheat for group: {CacheGroup}", cacheGroup);

        try
        {
            // 根据缓存组预热不同类型的数据
            switch (cacheGroup.ToLower())
            {
                case "contracts":
                    await PreheatContractDataAsync();
                    break;
                case "risk":
                    await PreheatRiskDataAsync();
                    break;
                case "pricing":
                    await PreheatPricingDataAsync();
                    break;
                default:
                    _logger.LogWarning("Unknown cache group for preheating: {CacheGroup}", cacheGroup);
                    break;
            }

            _logger.LogInformation("Cache preheat completed for group: {CacheGroup}", cacheGroup);
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogError(ex, "Error during cache preheat for group: {CacheGroup}", cacheGroup);
        }
    }

    public async Task<T?> GetWithFallbackAsync<T>(string key, Func<Task<T?>> fallbackFunction, TimeSpan? expiry = null) where T : class
    {
        // 先尝试从缓存获取
        var cached = await GetAsync<T>(key);
        if (cached != null)
        {
            return cached;
        }

        // 缓存不存在，使用分布式锁避免缓存穿透
        var lockKey = $"lock:{key}";
        var lockValue = Guid.NewGuid().ToString();
        var lockExpiry = TimeSpan.FromMinutes(1);

        try
        {
            // 尝试获取分布式锁
            if (_options.EnableL2Cache && await _l2Cache.StringSetAsync(lockKey, lockValue, lockExpiry, When.NotExists))
            {
                try
                {
                    // 再次检查缓存（可能在等待锁的过程中被其他线程设置）
                    cached = await GetAsync<T>(key);
                    if (cached != null)
                    {
                        return cached;
                    }

                    // 执行回退函数创建数据
                    var value = await fallbackFunction();
                    
                    // 设置缓存
                    if (value != null)
                    {
                        await SetAsync(key, value, expiry);
                    }
                    
                    _logger.LogDebug("Created and cached value for key: {Key}", key);
                    return value;
                }
                finally
                {
                    // 释放锁
                    await ReleaseLockAsync(lockKey, lockValue);
                }
            }
            else
            {
                // 无法获取锁，等待短时间后重试从缓存获取
                await Task.Delay(100);
                cached = await GetAsync<T>(key);
                if (cached != null)
                {
                    return cached;
                }

                // 如果仍然没有缓存，直接执行回退函数（不缓存结果）
                return await fallbackFunction();
            }
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogError(ex, "Error in GetWithFallbackAsync for key: {Key}", key);
            
            // 出错时直接执行回退函数
            return await fallbackFunction();
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        var stats = _statistics.GetStatistics();

        if (_options.EnableL2Cache)
        {
            try
            {
                var info = await _l2Cache.ExecuteAsync("INFO", "memory");
                stats.OverallStats.AverageResponseTimeByOperation["L2CacheInfo"] = TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get L2 cache info");
            }
        }

        return stats;
    }

    public async Task<CacheHealthStatus> GetHealthStatusAsync()
    {
        var healthStatus = new CacheHealthStatus
        {
            CheckedAt = DateTime.UtcNow,
            IsHealthy = true
        };

        try
        {
            // 检查L1缓存健康状态
            healthStatus.LayerHealth[CacheLayer.L1_Memory] = true;

            // 检查L2缓存健康状态
            if (_options.EnableL2Cache)
            {
                try
                {
                    await _l2Cache.PingAsync();
                    healthStatus.LayerHealth[CacheLayer.L2_Redis] = true;
                }
                catch (Exception ex)
                {
                    healthStatus.LayerHealth[CacheLayer.L2_Redis] = false;
                    healthStatus.Issues.Add($"Redis connection failed: {ex.Message}");
                    healthStatus.IsHealthy = false;
                }
            }

            // L3缓存（占位符）
            healthStatus.LayerHealth[CacheLayer.L3_Distributed] = true;
        }
        catch (Exception ex)
        {
            healthStatus.IsHealthy = false;
            healthStatus.Issues.Add($"General health check failed: {ex.Message}");
        }

        return healthStatus;
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) where T : class
    {
        var results = new Dictionary<string, T?>();

        try
        {
            var tasks = keys.Select(async key =>
            {
                var value = await GetAsync<T>(key);
                return new KeyValuePair<string, T?>(key, value);
            });

            var taskResults = await Task.WhenAll(tasks);
            
            foreach (var result in taskResults)
            {
                results[result.Key] = result.Value;
            }
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogWarning(ex, "Error in GetManyAsync");
        }

        return results;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiry = null) where T : class
    {
        try
        {
            var tasks = items.Select(async item =>
            {
                await SetAsync(item.Key, item.Value, expiry);
            });

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogWarning(ex, "Error in SetManyAsync");
        }
    }

    public async Task SynchronizeCacheAsync(string key)
    {
        try
        {
            var fullKey = GetFullKey(key);

            // 从L2缓存获取数据并同步到L1
            if (_options.EnableL2Cache)
            {
                var l2Value = await _l2Cache.StringGetAsync(fullKey);
                if (l2Value.HasValue)
                {
                    var value = JsonSerializer.Deserialize<object>(l2Value!);
                    if (value != null)
                    {
                        _l1Cache.Set(fullKey, value, _options.DefaultExpiry);
                        _logger.LogDebug("Synchronized cache for key: {Key}", key);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogWarning(ex, "Error synchronizing cache for key: {Key}", key);
        }
    }

    public async Task InvalidateDistributedCacheAsync(string key)
    {
        try
        {
            var fullKey = GetFullKey(key);

            // 从所有缓存层移除
            _l1Cache.Remove(fullKey);

            if (_options.EnableL2Cache)
            {
                await _l2Cache.KeyDeleteAsync(fullKey);
            }

            _logger.LogDebug("Invalidated distributed cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogWarning(ex, "Error invalidating distributed cache for key: {Key}", key);
        }
    }

    // 私有辅助方法
    private async Task<T?> GetFromL2CacheAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _l2Cache.StringGetAsync(key);
            if (value.HasValue)
            {
                return JsonSerializer.Deserialize<T>(value!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error deserializing from L2 cache for key: {Key}", key);
        }

        return null;
    }

    private async Task SetL2CacheAsync<T>(string key, T value, TimeSpan expiry) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _l2Cache.StringSetAsync(key, json, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error serializing to L2 cache for key: {Key}", key);
        }
    }

    private async Task PromoteToL1CacheAsync<T>(string key, T value) where T : class
    {
        try
        {
            var memoryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.L1PromotionExpiry,
                Priority = CacheItemPriority.High,
                Size = EstimateSize(value)
            };

            _l1Cache.Set(key, value, memoryOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error promoting to L1 cache for key: {Key}", key);
        }
    }

    private async Task ReleaseLockAsync(string lockKey, string lockValue)
    {
        try
        {
            const string script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

            await _l2Cache.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { lockValue });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error releasing lock for key: {LockKey}", lockKey);
        }
    }

    private async Task PreheatContractDataAsync()
    {
        // 预热合同相关数据
        _logger.LogDebug("Preheating contract data");
        await Task.CompletedTask;
    }

    private async Task PreheatRiskDataAsync()
    {
        // 预热风险相关数据
        _logger.LogDebug("Preheating risk data");
        await Task.CompletedTask;
    }

    private async Task PreheatPricingDataAsync()
    {
        // 预热价格相关数据
        _logger.LogDebug("Preheating pricing data");
        await Task.CompletedTask;
    }

    private string GetFullKey(string key) => $"{_options.KeyPrefix}:{key}";

    private CacheItemPriority GetCachePriority(string key)
    {
        // 根据key的类型确定缓存优先级
        if (key.Contains("contract") || key.Contains("position"))
            return CacheItemPriority.High;
        if (key.Contains("price") || key.Contains("market"))
            return CacheItemPriority.Normal;
        return CacheItemPriority.Low;
    }

    private long EstimateSize<T>(T value) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            return json.Length * 2; // 估算字符串大小
        }
        catch
        {
            return 1024; // 默认1KB
        }
    }
}

/// <summary>
/// 多层缓存配置选项
/// </summary>
public class MultiLayerCacheOptions
{
    public string KeyPrefix { get; set; } = "OilTrading";
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan L1PromotionExpiry { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableL2Cache { get; set; } = true;
    public bool EnableCacheProtection { get; set; } = true;
    public int MaxL1CacheSize { get; set; } = 100 * 1024 * 1024; // 100MB
}

/// <summary>
/// 缓存统计信息收集器
/// </summary>
public class CacheStatisticsCollector
{
    private readonly ConcurrentDictionary<CacheLayer, long> _hits = new();
    private readonly ConcurrentDictionary<CacheLayer, long> _sets = new();
    private long _misses = 0;
    private long _errors = 0;
    private readonly DateTime _startTime = DateTime.UtcNow;

    public void RecordHit(CacheLayer layer)
    {
        _hits.AddOrUpdate(layer, 1, (_, count) => count + 1);
    }

    public void RecordMiss()
    {
        Interlocked.Increment(ref _misses);
    }

    public void RecordSet(CacheLayer layer)
    {
        _sets.AddOrUpdate(layer, 1, (_, count) => count + 1);
    }

    public void RecordError()
    {
        Interlocked.Increment(ref _errors);
    }

    public CacheStatistics GetStatistics()
    {
        var totalHits = _hits.Values.Sum();
        var totalOperations = totalHits + _misses;

        return new CacheStatistics
        {
            GeneratedAt = DateTime.UtcNow,
            L1Stats = new CacheLayerStats
            {
                Layer = CacheLayer.L1_Memory,
                HitCount = _hits.GetValueOrDefault(CacheLayer.L1_Memory, 0),
                MissCount = _misses,
                TotalRequests = totalOperations,
                IsHealthy = true
            },
            L2Stats = new CacheLayerStats
            {
                Layer = CacheLayer.L2_Redis,
                HitCount = _hits.GetValueOrDefault(CacheLayer.L2_Redis, 0),
                MissCount = _misses,
                TotalRequests = totalOperations,
                IsHealthy = true
            },
            L3Stats = new CacheLayerStats
            {
                Layer = CacheLayer.L3_Distributed,
                HitCount = 0,
                MissCount = 0,
                TotalRequests = 0,
                IsHealthy = true
            },
            OverallStats = new CacheOverallStats
            {
                TotalHits = totalHits,
                TotalMisses = _misses,
                TotalRequests = totalOperations,
                PopularKeys = new Dictionary<string, int>(),
                AverageResponseTimeByOperation = new Dictionary<string, TimeSpan>()
            }
        };
    }
}

