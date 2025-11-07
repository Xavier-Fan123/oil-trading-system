using Microsoft.AspNetCore.Http;
using OilTrading.Infrastructure.Services;
using System.Security.Claims;

namespace OilTrading.Api.Middleware;

/// <summary>
/// Middleware for JWT token validation and authentication.
/// Validates JWT tokens from Authorization header and extracts user information.
///
/// Token Format: Authorization: Bearer {jwt_token}
///
/// On success:
/// - HttpContext.User set to ClaimsPrincipal from token
/// - Request proceeds to next middleware
///
/// On failure (invalid/missing token):
/// - Request proceeds to next middleware with empty user identity
/// - Endpoint-level [Authorize] attributes will reject the request with 401
///
/// Design: Non-blocking - invalid tokens don't fail here; let ASP.NET Core's
/// authorization middleware handle rejections. This allows public endpoints
/// while protecting private ones.
/// </summary>
public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(RequestDelegate next, ILogger<JwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IJwtTokenService jwtTokenService)
    {
        // Extract authorization header
        var authorizationHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            try
            {
                // Validate token
                var principal = jwtTokenService.ValidateToken(token);

                if (principal != null)
                {
                    // Token is valid - set user identity
                    context.User = principal;

                    var userId = jwtTokenService.GetUserIdFromToken(token);
                    var userEmail = principal.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

                    _logger.LogInformation(
                        "JWT token validated for user {UserId} ({Email})",
                        userId, userEmail);
                }
                else
                {
                    _logger.LogWarning(
                        "Invalid JWT token provided from IP {RemoteIpAddress}",
                        context.Connection.RemoteIpAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error validating JWT token from IP {RemoteIpAddress}",
                    context.Connection.RemoteIpAddress);
            }
        }

        // Proceed to next middleware regardless of token validation result
        // Authorization attributes on endpoints will enforce access control
        await _next(context);
    }
}

/// <summary>
/// Extension method for registering JWT authentication middleware.
/// </summary>
public static class JwtAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtAuthenticationMiddleware>();
    }
}
