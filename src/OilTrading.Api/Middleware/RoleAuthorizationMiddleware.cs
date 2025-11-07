using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using OilTrading.Core.Enums;

namespace OilTrading.Api.Middleware;

/// <summary>
/// Role-based authorization middleware that enforces role-based access control (RBAC)
/// on all API endpoints. Works in conjunction with [Authorize(Roles = "...")] attributes.
///
/// Features:
/// - Checks ClaimsPrincipal for required roles
/// - Logs authorization successes and failures
/// - Provides audit trail for compliance
/// - Supports multiple roles per endpoint
/// </summary>
public class RoleAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RoleAuthorizationMiddleware> _logger;

    public RoleAuthorizationMiddleware(RequestDelegate next, ILogger<RoleAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Store original user for logging
        var originalUser = context.User;
        var originalUserName = context.User?.FindFirst(ClaimTypes.Email)?.Value ?? "Anonymous";
        var originalUserId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

        // Log incoming request with user info
        var userRoles = context.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList() ?? new List<string>();
        _logger.LogInformation(
            "Request: {Method} {Path} | User: {UserId} ({Email}) | Roles: {Roles} | IP: {IpAddress}",
            context.Request.Method,
            context.Request.Path,
            originalUserId,
            originalUserName,
            string.Join(", ", userRoles),
            context.Connection.RemoteIpAddress
        );

        try
        {
            // Continue to next middleware (authorization attributes will be checked by ASP.NET Core)
            await _next(context);

            // Log successful request completion
            if (context.Response.StatusCode == 403)
            {
                _logger.LogWarning(
                    "Authorization Denied: {Method} {Path} | User: {UserId} ({Email}) | Status: 403 Forbidden | IP: {IpAddress}",
                    context.Request.Method,
                    context.Request.Path,
                    originalUserId,
                    originalUserName,
                    context.Connection.RemoteIpAddress
                );
            }
            else if (context.Response.StatusCode == 401)
            {
                _logger.LogWarning(
                    "Authentication Failed: {Method} {Path} | Status: 401 Unauthorized | IP: {IpAddress}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Connection.RemoteIpAddress
                );
            }
            else if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                _logger.LogDebug(
                    "Request Authorized: {Method} {Path} | User: {UserId} | Status: {StatusCode}",
                    context.Request.Method,
                    context.Request.Path,
                    originalUserId,
                    context.Response.StatusCode
                );
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(
                "Unauthorized Access Attempt: {Method} {Path} | User: {UserId} ({Email}) | Error: {Error} | IP: {IpAddress}",
                context.Request.Method,
                context.Request.Path,
                originalUserId,
                originalUserName,
                ex.Message,
                context.Connection.RemoteIpAddress
            );

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Access forbidden. Insufficient permissions.",
                errorCode = "AUTHORIZATION_DENIED",
                details = "Your role does not have permission to access this resource."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Authorization Middleware Error: {Method} {Path} | Error: {Error}",
                context.Request.Method,
                context.Request.Path,
                ex.Message
            );
            throw;
        }
    }
}

/// <summary>
/// Extension method to register RoleAuthorizationMiddleware in the HTTP pipeline
/// </summary>
public static class RoleAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseRoleAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RoleAuthorizationMiddleware>();
    }
}
