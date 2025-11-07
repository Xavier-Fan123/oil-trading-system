using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OilTrading.Api.Authorization;
using OilTrading.Infrastructure.Services;
using System.Security.Claims;

namespace OilTrading.Api.Controllers;

/// <summary>
/// Rate Limiting Metrics Controller
/// Provides administrative endpoints for monitoring and managing rate limit statistics
///
/// Base Route: /api/rate-limit-metrics
/// Requires: Admin role (SystemAdmin only)
///
/// Features:
/// - View current rate limit status for authenticated user
/// - View global rate limiting statistics
/// - Reset rate limits for specific users (admin only)
/// - Monitor endpoint-specific rate limit hits
/// - Track blocked requests and users
/// </summary>
[ApiController]
[Route("api/rate-limit-metrics")]
[Authorize(Policy = "AdminOnly")]
public class RateLimitMetricsController : ControllerBase
{
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<RateLimitMetricsController> _logger;

    public RateLimitMetricsController(
        IRateLimitService rateLimitService,
        ILogger<RateLimitMetricsController> logger)
    {
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/rate-limit-metrics/status
    /// Get current rate limit status for the authenticated user
    ///
    /// Returns:
    /// - User's current request count and remaining requests
    /// - Global system rate limit status
    /// - Reset times for both user and global limits
    ///
    /// Example Response:
    /// {
    ///   "userRequests": 245,
    ///   "userLimit": 1000,
    ///   "userResetTime": "2025-11-07T15:32:00Z",
    ///   "globalRequests": 8432,
    ///   "globalLimit": 10000,
    ///   "globalResetTime": "2025-11-07T15:32:00Z"
    /// }
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        try
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userEmail = User?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            _logger.LogDebug("Getting rate limit status for user {UserId} ({Email})", userId, userEmail);

            var status = await _rateLimitService.GetStatusAsync(userId, cancellationToken);

            var response = new RateLimitStatusResponse
            {
                UserRequests = status.UserRequests,
                UserLimit = status.UserLimit,
                UserResetTime = status.UserResetTime,
                GlobalRequests = status.GlobalRequests,
                GlobalLimit = status.GlobalLimit,
                GlobalResetTime = status.GlobalResetTime
            };

            _logger.LogInformation(
                "Rate limit status retrieved for user {UserId}: User {UserRequests}/{UserLimit}, Global {GlobalRequests}/{GlobalLimit}",
                userId,
                status.UserRequests,
                status.UserLimit,
                status.GlobalRequests,
                status.GlobalLimit
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rate limit status");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Error retrieving rate limit status",
                errorCode = "RATE_LIMIT_STATUS_ERROR",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// GET /api/rate-limit-metrics/global-stats
    /// Get global rate limiting statistics (admin only)
    ///
    /// Returns:
    /// - Overall system request count and limits
    /// - Top endpoints by request count
    /// - Currently blocked users
    /// - Average requests per second
    /// - Total blocked requests
    ///
    /// Example Response:
    /// {
    ///   "totalRequests": 8432,
    ///   "globalLimit": 10000,
    ///   "requestsRemaining": 1568,
    ///   "resetTime": "2025-11-07T15:32:00Z",
    ///   "blockedRequests": 12,
    ///   "averageRequestsPerSecond": 14.05,
    ///   "topEndpoints": {
    ///     "/api/purchase-contracts": 1245,
    ///     "/api/position/current": 1089,
    ///     "/api/dashboard": 876
    ///   },
    ///   "blockedUsers": ["user1-id", "user2-id"]
    /// }
    /// </summary>
    [HttpGet("global-stats")]
    public async Task<IActionResult> GetGlobalStats(CancellationToken cancellationToken)
    {
        try
        {
            var adminId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var adminEmail = User?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            _logger.LogDebug("Admin {AdminId} ({Email}) requesting global rate limit stats", adminId, adminEmail);

            var stats = await _rateLimitService.GetGlobalStatsAsync(cancellationToken);

            var response = new GlobalRateLimitStatsResponse
            {
                TotalRequests = stats.TotalRequests,
                GlobalLimit = stats.GlobalLimit,
                RequestsRemaining = stats.RequestsRemaining,
                ResetTime = stats.ResetTime,
                BlockedRequests = stats.BlockedRequests,
                AverageRequestsPerSecond = stats.AverageRequestsPerSecond,
                TopEndpoints = stats.TopEndpoints,
                BlockedUsers = stats.BlockedUsers?.Keys.ToList() ?? new List<string>()
            };

            _logger.LogInformation(
                "Global rate limit stats retrieved by {AdminId}: {TotalRequests}/{GlobalLimit} requests, " +
                "{BlockedRequests} blocked, {AverageRPS} RPS, {TopEndpointCount} top endpoints tracked, " +
                "{BlockedUserCount} blocked users",
                adminId,
                stats.TotalRequests,
                stats.GlobalLimit,
                stats.BlockedRequests,
                stats.AverageRequestsPerSecond,
                stats.TopEndpoints?.Count ?? 0,
                stats.BlockedUsers?.Count ?? 0
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global rate limit statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Error retrieving global rate limit statistics",
                errorCode = "GLOBAL_STATS_ERROR",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// POST /api/rate-limit-metrics/reset-user-limit
    /// Reset rate limit for a specific user (admin only)
    ///
    /// Request Body:
    /// {
    ///   "userId": "user-id-to-reset"
    /// }
    ///
    /// Returns: Success message with timestamp
    ///
    /// Example Response:
    /// {
    ///   "message": "Rate limit reset for user",
    ///   "userId": "user-id-to-reset",
    ///   "resetAt": "2025-11-07T15:30:00Z",
    ///   "adminId": "admin-id-who-performed-reset"
    /// }
    /// </summary>
    [HttpPost("reset-user-limit")]
    public async Task<IActionResult> ResetUserLimit(
        [FromBody] ResetUserLimitRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.UserId))
            {
                _logger.LogWarning("Reset user limit request with empty user ID");
                return BadRequest(new
                {
                    message = "User ID is required",
                    errorCode = "INVALID_REQUEST",
                    details = "UserId field cannot be empty"
                });
            }

            var adminId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var adminEmail = User?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            _logger.LogInformation(
                "Admin {AdminId} ({Email}) resetting rate limit for user {UserId}",
                adminId,
                adminEmail,
                request.UserId
            );

            await _rateLimitService.ResetUserLimitAsync(request.UserId, cancellationToken);

            var response = new ResetUserLimitResponse
            {
                Message = "Rate limit reset for user",
                UserId = request.UserId,
                ResetAt = DateTime.UtcNow,
                AdminId = adminId
            };

            _logger.LogInformation(
                "Rate limit successfully reset for user {UserId} by admin {AdminId}",
                request.UserId,
                adminId
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting user rate limit for user {UserId}", request?.UserId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Error resetting user rate limit",
                errorCode = "RESET_LIMIT_ERROR",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// GET /api/rate-limit-metrics/health
    /// Check rate limiting system health
    ///
    /// Returns:
    /// - Whether rate limiting is enabled
    /// - Connection status to cache (Redis)
    /// - Current global request count
    /// - System health status
    ///
    /// Example Response:
    /// {
    ///   "isHealthy": true,
    ///   "rateLimitingEnabled": true,
    ///   "cacheStatus": "Connected",
    ///   "currentGlobalRequests": 8432,
    ///   "timestamp": "2025-11-07T15:30:00Z"
    /// }
    /// </summary>
    [AllowAnonymous]
    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _rateLimitService.GetGlobalStatsAsync(cancellationToken);

            var response = new RateLimitHealthResponse
            {
                IsHealthy = true,
                RateLimitingEnabled = true,
                CacheStatus = "Connected",
                CurrentGlobalRequests = stats.TotalRequests,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogDebug("Rate limiting health check: {Status}", response.CacheStatus);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rate limiting health check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                isHealthy = false,
                message = "Rate limiting system unavailable",
                errorCode = "RATE_LIMIT_UNAVAILABLE",
                timestamp = DateTime.UtcNow
            });
        }
    }
}

/// <summary>
/// Request/Response DTOs for Rate Limit Metrics Controller
/// </summary>

public class RateLimitStatusResponse
{
    /// <summary>Current requests made by user in current window</summary>
    public int UserRequests { get; set; }

    /// <summary>User's request limit per minute</summary>
    public int UserLimit { get; set; }

    /// <summary>When the user's rate limit window resets</summary>
    public DateTime UserResetTime { get; set; }

    /// <summary>Current total requests across entire system</summary>
    public int GlobalRequests { get; set; }

    /// <summary>Global system request limit per minute</summary>
    public int GlobalLimit { get; set; }

    /// <summary>When the global rate limit window resets</summary>
    public DateTime GlobalResetTime { get; set; }
}

public class GlobalRateLimitStatsResponse
{
    /// <summary>Total requests processed by system in current window</summary>
    public int TotalRequests { get; set; }

    /// <summary>System-wide request limit per minute</summary>
    public int GlobalLimit { get; set; }

    /// <summary>Remaining requests before hitting global limit</summary>
    public int RequestsRemaining { get; set; }

    /// <summary>When the global limit resets</summary>
    public DateTime ResetTime { get; set; }

    /// <summary>Total requests rejected due to rate limiting</summary>
    public int BlockedRequests { get; set; }

    /// <summary>Average requests per second over the current window</summary>
    public double AverageRequestsPerSecond { get; set; }

    /// <summary>Top 10 most requested endpoints and their hit counts</summary>
    public Dictionary<string, int> TopEndpoints { get; set; } = new();

    /// <summary>User IDs that are currently blocked due to rate limiting</summary>
    public List<string> BlockedUsers { get; set; } = new();
}

public class ResetUserLimitRequest
{
    /// <summary>User ID whose rate limit should be reset</summary>
    public string UserId { get; set; } = string.Empty;
}

public class ResetUserLimitResponse
{
    /// <summary>Confirmation message</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>User ID that was reset</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Timestamp when reset occurred</summary>
    public DateTime ResetAt { get; set; }

    /// <summary>Admin user who performed the reset</summary>
    public string AdminId { get; set; } = string.Empty;
}

public class RateLimitHealthResponse
{
    /// <summary>Overall health status of rate limiting system</summary>
    public bool IsHealthy { get; set; }

    /// <summary>Whether rate limiting is currently enabled</summary>
    public bool RateLimitingEnabled { get; set; }

    /// <summary>Status of cache connection (Redis)</summary>
    public string CacheStatus { get; set; } = string.Empty;

    /// <summary>Current global request count</summary>
    public int CurrentGlobalRequests { get; set; }

    /// <summary>Timestamp of health check</summary>
    public DateTime Timestamp { get; set; }
}
