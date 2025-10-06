using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Api.HealthChecks;

/// <summary>
/// Enhanced database health check with detailed metrics
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        ApplicationDbContext context,
        ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;

            // Test database connectivity
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy(
                    "Cannot connect to database",
                    data: new Dictionary<string, object>
                    {
                        { "connectionString", MaskConnectionString(_context.Database.GetConnectionString()) }
                    });
            }

            // Test read operation
            var userCount = await _context.Users.CountAsync(cancellationToken);
            var productCount = await _context.Products.CountAsync(cancellationToken);
            var activeContractCount = await _context.PurchaseContracts
                .Where(c => c.Status == Core.Entities.ContractStatus.Active)
                .CountAsync(cancellationToken);

            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Check response time
            if (responseTime > 2000) // 2 seconds
            {
                _logger.LogWarning("Database responding slowly: {ResponseTime}ms", responseTime);
                return HealthCheckResult.Degraded(
                    "Database slow but operational",
                    data: new Dictionary<string, object>
                    {
                        { "responseTimeMs", responseTime },
                        { "threshold", 2000 },
                        { "users", userCount },
                        { "products", productCount },
                        { "activeContracts", activeContractCount }
                    });
            }

            return HealthCheckResult.Healthy(
                "Database fully operational",
                data: new Dictionary<string, object>
                {
                    { "responseTimeMs", responseTime },
                    { "users", userCount },
                    { "products", productCount },
                    { "activeContracts", activeContractCount },
                    { "connectionString", MaskConnectionString(_context.Database.GetConnectionString()) }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy(
                "Database health check failed",
                ex,
                data: new Dictionary<string, object>
                {
                    { "error", ex.Message },
                    { "errorType", ex.GetType().Name }
                });
        }
    }

    private string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "Not configured";

        // Special handling for InMemory database
        if (connectionString.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            return "InMemory";

        // Mask password in connection string
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(Password|Pwd)=([^;]*)",
            "$1=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
