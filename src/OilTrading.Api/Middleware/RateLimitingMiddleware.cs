using OilTrading.Infrastructure.Services;
using System.Security.Claims;

namespace OilTrading.Api.Middleware;

/// <summary>
/// Rate limiting middleware that enforces request rate limits at three levels:
/// 1. Global: Total requests across entire system per minute
/// 2. Per-User: Requests per authenticated user per minute
/// 3. Per-Endpoint: Requests to specific endpoints per minute
///
/// Adds X-RateLimit-* response headers for client visibility and returns 429 Too Many Requests
/// when any rate limit is exceeded.
///
/// Features:
/// - Three-level rate limit checking (global → per-user → per-endpoint)
/// - Automatic response headers (X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset)
/// - Per-endpoint limit configuration
/// - Comprehensive logging at WARNING level for limit violations
/// - Fail-open design: Request allowed if rate limit service unavailable
/// - Skips rate limiting for health check and metrics endpoints
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // Endpoints that bypass rate limiting (health checks, metrics)
    private static readonly HashSet<string> ExemptEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health/live",
        "/health/ready",
        "/metrics",
        "/.well-known/openapi.json",
        "/swagger",
        "/swagger-ui.html",
        "/swagger/",
        "/api/swagger",
        "/api/swagger/"
    };

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimitService rateLimitService)
    {
        var path = context.Request.Path.Value ?? "/";

        // Skip rate limiting for exempt endpoints
        if (IsExemptEndpoint(path))
        {
            await _next(context);
            return;
        }

        try
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var userEmail = context.User?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var endpoint = $"{context.Request.Method} {path}";

            // Check rate limit
            var rateLimitResult = await rateLimitService.CheckRateLimitAsync(
                userId,
                path,
                ipAddress,
                context.RequestAborted
            );

            // Add rate limit headers to response (use Append for idempotency)
            context.Response.Headers.Append("X-RateLimit-Limit", rateLimitResult.RequestsLimit.ToString());
            context.Response.Headers.Append("X-RateLimit-Remaining", Math.Max(0, rateLimitResult.RequestsRemaining).ToString());
            context.Response.Headers.Append("X-RateLimit-Reset", rateLimitResult.ResetTime.ToString("o"));

            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded: {Method} {Path} | User: {UserId} ({Email}) | " +
                    "Limit Type: {LimitType} | Limit: {Limit} | Reason: {Reason} | IP: {IpAddress}",
                    context.Request.Method,
                    path,
                    userId,
                    userEmail,
                    rateLimitResult.ExceededLimitType,
                    rateLimitResult.RequestsLimit,
                    rateLimitResult.RejectionReason ?? "Unknown",
                    ipAddress
                );

                // Return 429 Too Many Requests
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    message = "Rate limit exceeded",
                    errorCode = "RATE_LIMIT_EXCEEDED",
                    details = new
                    {
                        limitType = rateLimitResult.ExceededLimitType?.ToString() ?? "Unknown",
                        requestsLimit = rateLimitResult.RequestsLimit,
                        requestsRemaining = 0,
                        resetTime = rateLimitResult.ResetTime,
                        windowDuration = rateLimitResult.WindowDuration.TotalSeconds,
                        reason = rateLimitResult.RejectionReason ?? "Too many requests in this time window"
                    }
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
                return;
            }

            _logger.LogDebug(
                "Rate limit check passed: {Method} {Path} | User: {UserId} | " +
                "Remaining: {Remaining}/{Limit} | Reset: {ResetTime}",
                context.Request.Method,
                path,
                userId,
                Math.Max(0, rateLimitResult.RequestsRemaining),
                rateLimitResult.RequestsLimit,
                rateLimitResult.ResetTime
            );

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error in rate limiting middleware: {Method} {Path}",
                context.Request.Method,
                context.Request.Path
            );

            // Fail-open: Allow request if rate limit service fails
            await _next(context);
        }
    }

    /// <summary>
    /// Determines if an endpoint should bypass rate limiting (health checks, metrics, etc.)
    /// </summary>
    private bool IsExemptEndpoint(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return ExemptEndpoints.Any(exempt =>
            path.StartsWith(exempt, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension method to register RateLimitingMiddleware in the HTTP pipeline
/// Usage: app.UseRateLimiting();
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
