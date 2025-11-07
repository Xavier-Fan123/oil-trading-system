using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace OilTrading.Infrastructure.Services;

/// <summary>
/// Rate limiting service that enforces per-user, per-endpoint, and global rate limits.
/// Uses distributed cache (Redis) for storage to support horizontal scaling.
///
/// Supports three types of rate limiting:
/// 1. Global: Total requests across entire system (10,000/minute default)
/// 2. Per-User: Maximum requests per user (1,000/minute default)
/// 3. Per-Endpoint: Maximum requests to specific endpoint (configurable per endpoint)
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Checks if a request should be allowed based on rate limiting rules
    /// </summary>
    Task<RateLimitResult> CheckRateLimitAsync(
        string userId,
        string endpoint,
        string? ipAddress = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets current rate limit status for a user
    /// </summary>
    Task<RateLimitStatus> GetStatusAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets rate limit for a user (admin operation)
    /// </summary>
    Task ResetUserLimitAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets global rate limit statistics
    /// </summary>
    Task<GlobalRateLimitStats> GetGlobalStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Rate limit result returned when checking rate limit
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int RequestsRemaining { get; set; }
    public int RequestsLimit { get; set; }
    public DateTime ResetTime { get; set; }
    public TimeSpan WindowDuration { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>
    /// The rate limit that was exceeded (if IsAllowed = false)
    /// </summary>
    public RateLimitType? ExceededLimitType { get; set; }
}

/// <summary>
/// Current rate limit status for a user
/// </summary>
public class RateLimitStatus
{
    public int UserRequests { get; set; }
    public int UserLimit { get; set; }
    public DateTime UserResetTime { get; set; }
    public int GlobalRequests { get; set; }
    public int GlobalLimit { get; set; }
    public DateTime GlobalResetTime { get; set; }
}

/// <summary>
/// Global rate limit statistics
/// </summary>
public class GlobalRateLimitStats
{
    public int TotalRequests { get; set; }
    public int GlobalLimit { get; set; }
    public int RequestsRemaining { get; set; }
    public DateTime ResetTime { get; set; }
    public int BlockedRequests { get; set; }
    public double AverageRequestsPerSecond { get; set; }
    public Dictionary<string, int> TopEndpoints { get; set; } = new();
    public Dictionary<string, int> BlockedUsers { get; set; } = new();
}

/// <summary>
/// Type of rate limit that was exceeded
/// </summary>
public enum RateLimitType
{
    Global,
    PerUser,
    PerEndpoint
}

/// <summary>
/// Endpoint-specific rate limit configuration
/// </summary>
public class EndpointRateLimit
{
    public string Endpoint { get; set; } = string.Empty;
    public int RequestsPerMinute { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class RateLimitService : IRateLimitService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RateLimitService> _logger;
    private readonly RateLimitConfiguration _config;

    // Cache key prefixes
    private const string GlobalLimitKey = "rate-limit:global";
    private const string UserLimitKeyPrefix = "rate-limit:user:";
    private const string EndpointLimitKeyPrefix = "rate-limit:endpoint:";
    private const string BlockedUsersKey = "rate-limit:blocked-users";
    private const string EndpointStatsKeyPrefix = "rate-limit:stats:endpoint:";

    public RateLimitService(
        IDistributedCache cache,
        ILogger<RateLimitService> logger,
        RateLimitConfiguration config
    )
    {
        _cache = cache;
        _logger = logger;
        _config = config;
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(
        string userId,
        string endpoint,
        string? ipAddress = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Check global rate limit first (most restrictive)
            var globalResult = await CheckGlobalLimitAsync(cancellationToken);
            if (!globalResult.IsAllowed)
            {
                _logger.LogWarning(
                    "Global rate limit exceeded | Endpoint: {Endpoint} | User: {UserId} | IP: {IpAddress} | Remaining: {Remaining}/{Limit}",
                    endpoint,
                    userId,
                    ipAddress,
                    globalResult.RequestsRemaining,
                    globalResult.RequestsLimit
                );
                globalResult.ExceededLimitType = RateLimitType.Global;
                return globalResult;
            }

            // Check per-user rate limit
            var userResult = await CheckPerUserLimitAsync(userId, cancellationToken);
            if (!userResult.IsAllowed)
            {
                _logger.LogWarning(
                    "Per-user rate limit exceeded | User: {UserId} | IP: {IpAddress} | Endpoint: {Endpoint} | Remaining: {Remaining}/{Limit}",
                    userId,
                    ipAddress,
                    endpoint,
                    userResult.RequestsRemaining,
                    userResult.RequestsLimit
                );
                userResult.ExceededLimitType = RateLimitType.PerUser;
                return userResult;
            }

            // Check per-endpoint rate limit
            var endpointResult = await CheckPerEndpointLimitAsync(endpoint, cancellationToken);
            if (!endpointResult.IsAllowed)
            {
                _logger.LogWarning(
                    "Per-endpoint rate limit exceeded | Endpoint: {Endpoint} | User: {UserId} | IP: {IpAddress} | Remaining: {Remaining}/{Limit}",
                    endpoint,
                    userId,
                    ipAddress,
                    endpointResult.RequestsRemaining,
                    endpointResult.RequestsLimit
                );
                endpointResult.ExceededLimitType = RateLimitType.PerEndpoint;
                return endpointResult;
            }

            // All limits passed - increment counters and return success
            await IncrementCountersAsync(userId, endpoint, cancellationToken);

            _logger.LogDebug(
                "Request allowed | User: {UserId} | Endpoint: {Endpoint} | IP: {IpAddress} | Global remaining: {GlobalRemaining}/{GlobalLimit} | User remaining: {UserRemaining}/{UserLimit}",
                userId,
                endpoint,
                ipAddress,
                userResult.RequestsRemaining - 1,
                userResult.RequestsLimit,
                userResult.RequestsRemaining - 1,
                userResult.RequestsLimit
            );

            return new RateLimitResult
            {
                IsAllowed = true,
                RequestsRemaining = userResult.RequestsRemaining - 1,
                RequestsLimit = userResult.RequestsLimit,
                ResetTime = userResult.ResetTime,
                WindowDuration = userResult.WindowDuration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking rate limit | User: {UserId} | Endpoint: {Endpoint}",
                userId,
                endpoint
            );

            // On error, allow request (fail open)
            return new RateLimitResult
            {
                IsAllowed = true,
                RequestsRemaining = int.MaxValue,
                RequestsLimit = int.MaxValue,
                ResetTime = DateTime.UtcNow.AddMinutes(1),
                WindowDuration = TimeSpan.FromMinutes(1),
                RejectionReason = "Rate limit service error (allowing request)"
            };
        }
    }

    public async Task<RateLimitStatus> GetStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userKey = $"{UserLimitKeyPrefix}{userId}";
        var globalKey = GlobalLimitKey;

        var userData = await _cache.GetStringAsync(userKey, cancellationToken);
        var globalData = await _cache.GetStringAsync(globalKey, cancellationToken);

        var userCounter = userData != null
            ? JsonSerializer.Deserialize<RateLimitCounter>(userData) ?? new RateLimitCounter()
            : new RateLimitCounter();

        var globalCounter = globalData != null
            ? JsonSerializer.Deserialize<RateLimitCounter>(globalData) ?? new RateLimitCounter()
            : new RateLimitCounter();

        return new RateLimitStatus
        {
            UserRequests = userCounter.Count,
            UserLimit = _config.PerUserLimit,
            UserResetTime = userCounter.ResetTime,
            GlobalRequests = globalCounter.Count,
            GlobalLimit = _config.GlobalLimit,
            GlobalResetTime = globalCounter.ResetTime
        };
    }

    public async Task ResetUserLimitAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userKey = $"{UserLimitKeyPrefix}{userId}";
        await _cache.RemoveAsync(userKey, cancellationToken);
        _logger.LogInformation("Rate limit reset for user: {UserId}", userId);
    }

    public async Task<GlobalRateLimitStats> GetGlobalStatsAsync(CancellationToken cancellationToken = default)
    {
        var globalKey = GlobalLimitKey;
        var blockedUsersKey = BlockedUsersKey;

        var globalData = await _cache.GetStringAsync(globalKey, cancellationToken);
        var blockedUsersData = await _cache.GetStringAsync(blockedUsersKey, cancellationToken);

        var globalCounter = globalData != null
            ? JsonSerializer.Deserialize<RateLimitCounter>(globalData) ?? new RateLimitCounter()
            : new RateLimitCounter();

        var blockedUsers = blockedUsersData != null
            ? JsonSerializer.Deserialize<Dictionary<string, int>>(blockedUsersData) ?? new Dictionary<string, int>()
            : new Dictionary<string, int>();

        return new GlobalRateLimitStats
        {
            TotalRequests = globalCounter.Count,
            GlobalLimit = _config.GlobalLimit,
            RequestsRemaining = Math.Max(0, _config.GlobalLimit - globalCounter.Count),
            ResetTime = globalCounter.ResetTime,
            BlockedRequests = blockedUsers.Values.Sum(),
            AverageRequestsPerSecond = CalculateAverageRps(globalCounter),
            BlockedUsers = blockedUsers
        };
    }

    private async Task<RateLimitResult> CheckGlobalLimitAsync(CancellationToken cancellationToken)
    {
        var key = GlobalLimitKey;
        var counter = await GetCounterAsync(key, _config.GlobalLimit, cancellationToken);

        return new RateLimitResult
        {
            IsAllowed = counter.Count < _config.GlobalLimit,
            RequestsRemaining = Math.Max(0, _config.GlobalLimit - counter.Count),
            RequestsLimit = _config.GlobalLimit,
            ResetTime = counter.ResetTime,
            WindowDuration = TimeSpan.FromMinutes(1)
        };
    }

    private async Task<RateLimitResult> CheckPerUserLimitAsync(string userId, CancellationToken cancellationToken)
    {
        var key = $"{UserLimitKeyPrefix}{userId}";
        var counter = await GetCounterAsync(key, _config.PerUserLimit, cancellationToken);

        return new RateLimitResult
        {
            IsAllowed = counter.Count < _config.PerUserLimit,
            RequestsRemaining = Math.Max(0, _config.PerUserLimit - counter.Count),
            RequestsLimit = _config.PerUserLimit,
            ResetTime = counter.ResetTime,
            WindowDuration = TimeSpan.FromMinutes(1)
        };
    }

    private async Task<RateLimitResult> CheckPerEndpointLimitAsync(string endpoint, CancellationToken cancellationToken)
    {
        if (!_config.EndpointLimits.TryGetValue(endpoint, out var limit))
        {
            // No specific limit for this endpoint
            return new RateLimitResult
            {
                IsAllowed = true,
                RequestsRemaining = int.MaxValue,
                RequestsLimit = int.MaxValue,
                ResetTime = DateTime.UtcNow.AddMinutes(1),
                WindowDuration = TimeSpan.FromMinutes(1)
            };
        }

        var key = $"{EndpointLimitKeyPrefix}{endpoint}";
        var counter = await GetCounterAsync(key, limit, cancellationToken);

        return new RateLimitResult
        {
            IsAllowed = counter.Count < limit,
            RequestsRemaining = Math.Max(0, limit - counter.Count),
            RequestsLimit = limit,
            ResetTime = counter.ResetTime,
            WindowDuration = TimeSpan.FromMinutes(1)
        };
    }

    private async Task IncrementCountersAsync(string userId, string endpoint, CancellationToken cancellationToken)
    {
        // Increment global counter
        var globalKey = GlobalLimitKey;
        var globalCounter = await GetCounterAsync(globalKey, _config.GlobalLimit, cancellationToken);
        globalCounter.Count++;
        await SetCounterAsync(globalKey, globalCounter, cancellationToken);

        // Increment user counter
        var userKey = $"{UserLimitKeyPrefix}{userId}";
        var userCounter = await GetCounterAsync(userKey, _config.PerUserLimit, cancellationToken);
        userCounter.Count++;
        await SetCounterAsync(userKey, userCounter, cancellationToken);

        // Increment endpoint counter
        if (_config.EndpointLimits.TryGetValue(endpoint, out var limit))
        {
            var endpointKey = $"{EndpointLimitKeyPrefix}{endpoint}";
            var endpointCounter = await GetCounterAsync(endpointKey, limit, cancellationToken);
            endpointCounter.Count++;
            await SetCounterAsync(endpointKey, endpointCounter, cancellationToken);
        }

        // Track endpoint statistics
        var statsKey = $"{EndpointStatsKeyPrefix}{endpoint}";
        var statsData = await _cache.GetStringAsync(statsKey, cancellationToken);
        var stats = statsData != null
            ? JsonSerializer.Deserialize<Dictionary<string, int>>(statsData) ?? new Dictionary<string, int>()
            : new Dictionary<string, int>();

        if (stats.ContainsKey(endpoint))
            stats[endpoint]++;
        else
            stats[endpoint] = 1;

        await _cache.SetStringAsync(statsKey, JsonSerializer.Serialize(stats), GetCacheOptions(), cancellationToken);
    }

    private async Task<RateLimitCounter> GetCounterAsync(
        string key,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var data = await _cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrEmpty(data))
        {
            return new RateLimitCounter
            {
                Count = 0,
                ResetTime = DateTime.UtcNow.AddMinutes(1)
            };
        }

        var counter = JsonSerializer.Deserialize<RateLimitCounter>(data);
        return counter ?? new RateLimitCounter { ResetTime = DateTime.UtcNow.AddMinutes(1) };
    }

    private async Task SetCounterAsync(
        string key,
        RateLimitCounter counter,
        CancellationToken cancellationToken
    )
    {
        var json = JsonSerializer.Serialize(counter);
        await _cache.SetStringAsync(key, json, GetCacheOptions(), cancellationToken);
    }

    private DistributedCacheEntryOptions GetCacheOptions()
    {
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2) // Allow 1 minute of buffer
        };
    }

    private double CalculateAverageRps(RateLimitCounter counter)
    {
        var elapsed = DateTime.UtcNow - (counter.ResetTime - TimeSpan.FromMinutes(1));
        if (elapsed.TotalSeconds <= 0) return 0;
        return counter.Count / elapsed.TotalSeconds;
    }
}

/// <summary>
/// Internal counter for rate limiting
/// </summary>
internal class RateLimitCounter
{
    public int Count { get; set; }
    public DateTime ResetTime { get; set; } = DateTime.UtcNow.AddMinutes(1);
}

/// <summary>
/// Rate limiting configuration
/// </summary>
public class RateLimitConfiguration
{
    /// <summary>
    /// Global requests per minute (across entire system)
    /// </summary>
    public int GlobalLimit { get; set; } = 10000;

    /// <summary>
    /// Requests per minute per user
    /// </summary>
    public int PerUserLimit { get; set; } = 1000;

    /// <summary>
    /// Endpoint-specific rate limits (endpoint path -> requests per minute)
    /// </summary>
    public Dictionary<string, int> EndpointLimits { get; set; } = new()
    {
        // Authentication endpoints - very strict
        ["/api/identity/login"] = 10,
        ["/api/identity/refresh"] = 20,
        ["/api/identity/logout"] = 30,

        // Contract operations - moderate
        ["/api/purchase-contracts"] = 100,
        ["/api/sales-contracts"] = 100,
        ["/api/contracts/resolve"] = 50,

        // Settlement operations - moderate
        ["/api/settlements"] = 100,
        ["/api/purchase-settlements"] = 100,
        ["/api/sales-settlements"] = 100,

        // Export operations - strict
        ["/api/contracts/export"] = 50,
        ["/api/settlements/export"] = 50,
        ["/api/reports/export"] = 30,

        // Reporting operations - moderate
        ["/api/report-configurations"] = 200,
        ["/api/report-executions"] = 100,
        ["/api/report-distributions"] = 100,

        // Risk operations - moderate
        ["/api/risk-metrics"] = 100,
        ["/api/var-calculation"] = 50,

        // Dashboard - high
        ["/api/dashboard"] = 300,
        ["/api/dashboard/summary"] = 300,

        // Positions - moderate
        ["/api/position"] = 200,
        ["/api/position/current"] = 200
    };

    /// <summary>
    /// Whether to enable rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;
}
