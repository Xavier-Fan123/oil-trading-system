using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace OilTrading.Api.HealthChecks;

/// <summary>
/// Enhanced Redis cache health check with detailed metrics
/// </summary>
public class CacheHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CacheHealthCheck> _logger;

    public CacheHealthCheck(
        IConnectionMultiplexer redis,
        ILogger<CacheHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;

            // Test Redis connectivity
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("Redis not connected");
                return HealthCheckResult.Degraded(
                    "Redis cache not connected - system will work but with reduced performance",
                    data: new Dictionary<string, object>
                    {
                        { "isConnected", false },
                        { "impact", "Degraded performance - database fallback active" }
                    });
            }

            var database = _redis.GetDatabase();

            // Test read/write operations
            var testKey = "health:check:test";
            var testValue = DateTime.UtcNow.Ticks.ToString();

            await database.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(10));
            var retrievedValue = await database.StringGetAsync(testKey);

            if (retrievedValue != testValue)
            {
                _logger.LogWarning("Redis read/write test failed");
                return HealthCheckResult.Degraded(
                    "Redis cache operational but read/write test failed",
                    data: new Dictionary<string, object>
                    {
                        { "isConnected", true },
                        { "readWriteTest", "failed" }
                    });
            }

            // Clean up test key
            await database.KeyDeleteAsync(testKey);

            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Get Redis server info
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var info = await server.InfoAsync();

            // Extract key metrics
            var flatInfo = info.SelectMany(group => group).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var usedMemory = flatInfo.TryGetValue("used_memory_human", out var mem) ? mem : "N/A";
            var connectedClients = flatInfo.TryGetValue("connected_clients", out var clients) ? clients : "N/A";
            var opsPerSec = flatInfo.TryGetValue("instantaneous_ops_per_sec", out var ops) ? ops : "N/A";

            // Check response time
            if (responseTime > 1000) // 1 second
            {
                _logger.LogWarning("Redis responding slowly: {ResponseTime}ms", responseTime);
                return HealthCheckResult.Degraded(
                    "Redis cache slow but operational",
                    data: new Dictionary<string, object>
                    {
                        { "responseTimeMs", responseTime },
                        { "threshold", 1000 },
                        { "usedMemory", usedMemory },
                        { "connectedClients", connectedClients },
                        { "opsPerSec", opsPerSec }
                    });
            }

            return HealthCheckResult.Healthy(
                "Redis cache fully operational",
                data: new Dictionary<string, object>
                {
                    { "responseTimeMs", responseTime },
                    { "isConnected", true },
                    { "serverCount", _redis.GetServers().Count() },
                    { "usedMemory", usedMemory },
                    { "connectedClients", connectedClients },
                    { "opsPerSec", opsPerSec },
                    { "readWriteTest", "passed" }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");

            // Redis failure is degraded, not unhealthy - system can function without cache
            return HealthCheckResult.Degraded(
                "Redis cache unavailable - system operating with database fallback",
                ex,
                data: new Dictionary<string, object>
                {
                    { "error", ex.Message },
                    { "errorType", ex.GetType().Name },
                    { "impact", "Performance degradation - all operations use database" }
                });
        }
    }
}
