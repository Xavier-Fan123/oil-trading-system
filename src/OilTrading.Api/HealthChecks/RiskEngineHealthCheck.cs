using Microsoft.Extensions.Diagnostics.HealthChecks;
using OilTrading.Application.Services;
using OilTrading.Core.Entities;

namespace OilTrading.Api.HealthChecks;

/// <summary>
/// Health check for Risk Engine service
/// Tests if RiskCalculationService can perform basic operations
/// </summary>
public class RiskEngineHealthCheck : IHealthCheck
{
    private readonly IRiskCalculationService _riskService;
    private readonly ILogger<RiskEngineHealthCheck> _logger;

    public RiskEngineHealthCheck(
        IRiskCalculationService riskService,
        ILogger<RiskEngineHealthCheck> logger)
    {
        _riskService = riskService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a simple test position
            var testPosition = new PaperContract
            {
                ProductType = "BRENT",
                Position = PositionType.Long,
                Quantity = 1,
                LotSize = 1000,
                EntryPrice = 80.00m,
                CurrentPrice = 82.00m,
                TradeDate = DateTime.UtcNow.AddDays(-10),
                Status = PaperContractStatus.Open
            };

            var testPositions = new List<PaperContract> { testPosition };

            // Create simple test returns data
            var testReturns = new List<decimal> { 0.01m, -0.02m, 0.015m, -0.01m, 0.005m };

            // Test Historical VaR calculation (basic risk engine function)
            var startTime = DateTime.UtcNow;
            var (var95, var99) = await _riskService.CalculateHistoricalVaRAsync(testPositions, testReturns);
            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Verify reasonable results
            if (var95 <= 0 || var99 <= 0 || var99 < var95)
            {
                _logger.LogWarning("Risk engine returned invalid VaR values: VaR95={VaR95}, VaR99={VaR99}", var95, var99);
                return HealthCheckResult.Degraded(
                    "Risk engine operational but returning questionable values",
                    data: new Dictionary<string, object>
                    {
                        { "var95", var95 },
                        { "var99", var99 },
                        { "responseTimeMs", responseTime },
                        { "issue", "Invalid VaR calculation results" }
                    });
            }

            // Check response time
            if (responseTime > 5000) // 5 seconds
            {
                _logger.LogWarning("Risk engine responding slowly: {ResponseTime}ms", responseTime);
                return HealthCheckResult.Degraded(
                    "Risk engine slow but operational",
                    data: new Dictionary<string, object>
                    {
                        { "responseTimeMs", responseTime },
                        { "threshold", 5000 },
                        { "var95", var95 },
                        { "var99", var99 }
                    });
            }

            // All checks passed
            return HealthCheckResult.Healthy(
                "Risk engine fully operational",
                data: new Dictionary<string, object>
                {
                    { "responseTimeMs", responseTime },
                    { "var95_test", var95 },
                    { "var99_test", var99 },
                    { "testPositions", testPositions.Count }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk engine health check failed");
            return HealthCheckResult.Unhealthy(
                "Risk engine failed health check",
                ex,
                data: new Dictionary<string, object>
                {
                    { "error", ex.Message },
                    { "errorType", ex.GetType().Name }
                });
        }
    }
}
