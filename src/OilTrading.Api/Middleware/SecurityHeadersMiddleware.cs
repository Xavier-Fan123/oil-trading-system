using System.Diagnostics;

namespace OilTrading.Api.Middleware;

/// <summary>
/// Security Headers Middleware
/// Adds comprehensive security headers to all HTTP responses to prevent common web vulnerabilities
///
/// Headers Added:
/// - Content-Security-Policy (CSP): Prevents XSS attacks by controlling resource loading
/// - Strict-Transport-Security (HSTS): Forces HTTPS connections (365 days)
/// - X-Content-Type-Options: Prevents MIME type sniffing
/// - X-Frame-Options: Prevents clickjacking attacks
/// - X-XSS-Protection: Legacy XSS protection (deprecated but still useful)
/// - Referrer-Policy: Controls how much referrer info is shared
/// - Permissions-Policy: Controls browser features and APIs
/// - Remove-Server-Header: Hides server version information
///
/// Features:
/// - Configurable CSP policies
/// - Environment-aware HTTPS enforcement
/// - Comprehensive attack surface reduction
/// - Best practices from OWASP and security research
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Remove Server header to avoid disclosing technology stack
        context.Response.Headers.Remove("Server");

        // Content-Security-Policy (CSP)
        // Defines which dynamic resources can be loaded and executed
        var cspPolicy = _environment.IsProduction()
            ? // Production: Strict CSP policy
              "default-src 'self'; " +
              "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +  // React + Vite require unsafe-eval
              "style-src 'self' 'unsafe-inline'; " +  // MUI and other libs use inline styles
              "img-src 'self' data: https:; " +
              "font-src 'self' data:; " +
              "connect-src 'self' https://localhost:5000 https://api.example.com; " +
              "frame-ancestors 'none'; " +
              "base-uri 'self'; " +
              "form-action 'self'; " +
              "upgrade-insecure-requests; " +  // Force HTTPS
              "block-all-mixed-content"
            : // Development: More permissive for debugging
              "default-src 'self' 'unsafe-inline' 'unsafe-eval' data: blob:; " +
              "script-src 'self' 'unsafe-inline' 'unsafe-eval' blob:; " +
              "style-src 'self' 'unsafe-inline' blob:; " +
              "img-src 'self' data: https: blob:; " +
              "font-src 'self' data: blob:; " +
              "connect-src 'self' http: https: ws: wss:; " +
              "frame-ancestors 'none'; " +
              "base-uri 'self'; " +
              "form-action 'self'";

        context.Response.Headers["Content-Security-Policy"] = cspPolicy;

        // Strict-Transport-Security (HSTS)
        // Forces HTTPS for all future connections (365 days = 31536000 seconds)
        // includeSubDomains ensures all subdomains use HTTPS
        // preload allows addition to HSTS preload list
        context.Response.Headers["Strict-Transport-Security"] =
            "max-age=31536000; includeSubDomains; preload";

        // X-Content-Type-Options
        // Prevents MIME type sniffing attacks
        // "nosniff" tells browsers to trust the Content-Type header
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // X-Frame-Options
        // Prevents clickjacking attacks by controlling whether page can be embedded in frames
        // "DENY" prevents framing from any source (most secure)
        // "SAMEORIGIN" would allow same-origin framing
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // X-XSS-Protection
        // Legacy XSS protection (deprecated in modern browsers but still useful for older clients)
        // "1; mode=block" enables filter and blocks page if XSS detected
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer-Policy
        // Controls how much referrer information is shared with third parties
        // "strict-origin-when-cross-origin":
        //   - Same-site: full referrer
        //   - Cross-site HTTPS→HTTPS: origin only
        //   - Cross-site HTTPS→HTTP: nothing (downgrade protection)
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions-Policy (formerly Feature-Policy)
        // Controls which browser features/APIs can be used
        // Restricts: camera, microphone, geolocation, payment, magnetometer, etc.
        var permissionsPolicy =
            "camera=(), " +
            "microphone=(), " +
            "geolocation=(), " +
            "payment=(), " +
            "magnetometer=(), " +
            "gyroscope=(), " +
            "accelerometer=(), " +
            "usb=(), " +
            "midi=(), " +
            "ambient-light-sensor=(), " +
            "vr=(), " +
            "xr-spatial-tracking=()";

        context.Response.Headers["Permissions-Policy"] = permissionsPolicy;

        // X-Permitted-Cross-Domain-Policies
        // Controls Adobe Flash and Reader cross-domain access
        context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";

        // X-UA-Compatible
        // Forces Internet Explorer to use latest rendering engine
        context.Response.Headers["X-UA-Compatible"] = "IE=Edge";

        _logger.LogDebug("Security headers applied to response");

        await _next(context);
    }
}

/// <summary>
/// Extension method to register SecurityHeadersMiddleware
/// Usage: app.UseSecurityHeaders();
/// Must be registered early in the middleware pipeline (before other middleware that writes to response)
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
