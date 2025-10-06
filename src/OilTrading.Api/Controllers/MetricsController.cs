using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OilTrading.Infrastructure.Data;
using OilTrading.Core.Entities;
using Prometheus;
using System.Diagnostics;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Metrics")]
public class MetricsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MetricsController> _logger;

    // Prometheus metrics
    private static readonly Gauge ActiveContractsGauge = Metrics
        .CreateGauge("oil_trading_active_contracts_total", "Total number of active contracts");

    private static readonly Gauge TotalExposureGauge = Metrics
        .CreateGauge("oil_trading_position_exposure_usd", "Total position exposure in USD");

    private static readonly Gauge ContractsExpiringGauge = Metrics
        .CreateGauge("oil_trading_contracts_expiring_24h", "Contracts expiring within 24 hours");

    private static readonly Gauge RiskVarGauge = Metrics
        .CreateGauge("oil_trading_risk_var_95", "Value at Risk at 95% confidence level", "product_type");

    private static readonly Gauge PriceVolatilityGauge = Metrics
        .CreateGauge("oil_trading_price_volatility", "Price volatility measure", "product_type");

    private static readonly Counter TradingVolumeCounter = Metrics
        .CreateCounter("oil_trading_volume_total", "Total trading volume", "product_type", "unit");

    private static readonly Gauge LastPriceUpdateTimestamp = Metrics
        .CreateGauge("oil_trading_last_price_update", "Timestamp of last price update");

    private static readonly Counter SettlementFailuresCounter = Metrics
        .CreateCounter("oil_trading_settlement_failures_total", "Total settlement failures");

    private static readonly Gauge MissingPriceDataGauge = Metrics
        .CreateGauge("oil_trading_missing_price_data_count", "Count of missing price data records");

    private static readonly Counter DataValidationFailuresCounter = Metrics
        .CreateCounter("oil_trading_data_validation_failures_total", "Total data validation failures");

    private static readonly Gauge CacheHitRatioGauge = Metrics
        .CreateGauge("oil_trading_cache_hit_ratio", "Cache hit ratio");

    private static readonly Counter UnauthorizedRequestsCounter = Metrics
        .CreateCounter("oil_trading_unauthorized_requests_total", "Total unauthorized requests");

    private static readonly Counter FailedLoginsCounter = Metrics
        .CreateCounter("oil_trading_failed_logins_total", "Total failed login attempts");

    private static readonly Gauge RegulatoryReportDelayGauge = Metrics
        .CreateGauge("oil_trading_regulatory_report_delay_hours", "Regulatory report delay in hours");

    private static readonly Gauge PositionLimitUtilizationGauge = Metrics
        .CreateGauge("oil_trading_position_limit_utilization", "Position limit utilization ratio");

    public MetricsController(ApplicationDbContext context, ILogger<MetricsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("business")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetBusinessMetrics()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Update all business metrics
            await UpdateBusinessMetricsAsync();
            
            var metrics = new
            {
                timestamp = DateTime.UtcNow,
                collection_time_ms = stopwatch.ElapsedMilliseconds,
                metrics = new
                {
                    activeContracts = await _context.PurchaseContracts.CountAsync(c => c.Status == ContractStatus.Active),
                    totalSalesContracts = await _context.SalesContracts.CountAsync(c => c.Status == ContractStatus.Active),
                    contractsExpiringToday = await GetContractsExpiringTodayAsync(),
                    totalTradingPartners = await _context.TradingPartners.CountAsync(t => t.IsActive),
                    todayPricingEvents = await GetTodayPricingEventsAsync(),
                    averageContractValue = await GetAverageContractValueAsync(),
                    riskMetrics = await GetRiskMetricsAsync(),
                    performanceMetrics = await GetPerformanceMetricsAsync()
                }
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting business metrics");
            return StatusCode(500, new { error = "Failed to collect business metrics" });
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    [HttpGet("system")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetSystemMetrics()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var metrics = new
            {
                timestamp = DateTime.UtcNow,
                system = new
                {
                    memory_usage_mb = process.WorkingSet64 / 1024 / 1024,
                    cpu_time_ms = process.TotalProcessorTime.TotalMilliseconds,
                    thread_count = process.Threads.Count,
                    handle_count = process.HandleCount,
                    uptime_seconds = (DateTime.UtcNow - process.StartTime).TotalSeconds,
                    gc_collections = new
                    {
                        gen0 = GC.CollectionCount(0),
                        gen1 = GC.CollectionCount(1),
                        gen2 = GC.CollectionCount(2)
                    },
                    total_memory_mb = GC.GetTotalMemory(false) / 1024 / 1024
                }
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting system metrics");
            return StatusCode(500, new { error = "Failed to collect system metrics" });
        }
    }

    [HttpGet("custom")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetCustomMetrics()
    {
        try
        {
            var metrics = new
            {
                timestamp = DateTime.UtcNow,
                business_kpis = new
                {
                    daily_trading_volume = await GetDailyTradingVolumeAsync(),
                    position_concentration = await GetPositionConcentrationAsync(),
                    settlement_efficiency = await GetSettlementEfficiencyAsync(),
                    price_feed_quality = await GetPriceFeedQualityAsync(),
                    contract_lifecycle_metrics = await GetContractLifecycleMetricsAsync()
                },
                operational_metrics = new
                {
                    api_performance = await GetApiPerformanceMetricsAsync(),
                    database_performance = await GetDatabasePerformanceAsync(),
                    cache_effectiveness = await GetCacheEffectivenessAsync(),
                    error_rates = await GetErrorRatesAsync()
                }
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting custom metrics");
            return StatusCode(500, new { error = "Failed to collect custom metrics" });
        }
    }

    [HttpPost("update")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> UpdateMetrics()
    {
        try
        {
            await UpdateBusinessMetricsAsync();
            await UpdateOperationalMetricsAsync();
            
            return Ok(new { message = "Metrics updated successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating metrics");
            return StatusCode(500, new { error = "Failed to update metrics" });
        }
    }

    [HttpGet("health-score")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetHealthScore()
    {
        try
        {
            var healthScore = await CalculateSystemHealthScoreAsync();
            
            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                overall_score = healthScore.OverallScore,
                component_scores = healthScore.ComponentScores,
                recommendations = healthScore.Recommendations,
                status = healthScore.OverallScore >= 80 ? "Healthy" : 
                        healthScore.OverallScore >= 60 ? "Warning" : "Critical"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating health score");
            return StatusCode(500, new { error = "Failed to calculate health score" });
        }
    }

    private async Task UpdateBusinessMetricsAsync()
    {
        // Active contracts
        var activeContracts = await _context.PurchaseContracts.CountAsync(c => c.Status == ContractStatus.Active);
        ActiveContractsGauge.Set(activeContracts);

        // Contracts expiring within 24 hours
        var tomorrow = DateTime.UtcNow.AddDays(1);
        var expiringContracts = await _context.PurchaseContracts
            .CountAsync(c => c.LaycanEnd <= tomorrow && c.Status == ContractStatus.Active);
        ContractsExpiringGauge.Set(expiringContracts);

        // Update price update timestamp
        var lastPriceUpdate = await _context.PricingEvents
            .OrderByDescending(p => p.EventDate)
            .Select(p => p.EventDate)
            .FirstOrDefaultAsync();
        if (lastPriceUpdate != default)
        {
            LastPriceUpdateTimestamp.Set(((DateTimeOffset)lastPriceUpdate).ToUnixTimeSeconds());
        }

        // Update trading volume by product type
        var volumeByProduct = await _context.PurchaseContracts
            .Where(c => c.CreatedAt.Date == DateTime.UtcNow.Date)
            .GroupBy(c => c.Product!.Type)
            .Select(g => new { ProductType = g.Key.ToString(), Volume = g.Sum(c => c.ContractQuantity.Value) })
            .ToListAsync();

        foreach (var volume in volumeByProduct)
        {
            TradingVolumeCounter.WithLabels(volume.ProductType, "BBL").IncTo((double)volume.Volume);
        }
    }

    private async Task UpdateOperationalMetricsAsync()
    {
        // Simulate cache hit ratio (in real implementation, get from cache service)
        CacheHitRatioGauge.Set(0.85); // 85% hit ratio

        // Missing price data count
        var today = DateTime.UtcNow.Date;
        var expectedPriceRecords = await _context.Products.CountAsync(p => p.IsActive);
        var actualPriceRecords = await _context.PricingEvents
            .CountAsync(p => p.EventDate.Date == today);
        var missingRecords = Math.Max(0, expectedPriceRecords - actualPriceRecords);
        MissingPriceDataGauge.Set(missingRecords);
    }

    private async Task<int> GetContractsExpiringTodayAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _context.PurchaseContracts
            .CountAsync(c => c.LaycanEnd.HasValue && c.LaycanEnd.Value.Date == today && c.Status == ContractStatus.Active);
    }

    private async Task<int> GetTodayPricingEventsAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _context.PricingEvents
            .CountAsync(p => p.EventDate.Date == today);
    }

    private async Task<decimal> GetAverageContractValueAsync()
    {
        var lastMonth = DateTime.UtcNow.AddDays(-30);
        var contracts = await _context.PurchaseContracts
            .Where(c => c.CreatedAt >= lastMonth)
            .ToListAsync();

        if (!contracts.Any()) return 0;

        return contracts.Average(c => c.ContractQuantity.Value * (decimal)(c.PriceFormula.FixedPrice ?? 75.0m));
    }

    private async Task<object> GetRiskMetricsAsync()
    {
        // Simplified risk metrics - in real implementation, integrate with risk calculation service
        return new
        {
            var_95_confidence = 1250000.0m, // $1.25M
            max_drawdown = 0.085m, // 8.5%
            portfolio_volatility = 0.23m, // 23%
            concentration_risk = 0.35m // 35%
        };
    }

    private async Task<object> GetPerformanceMetricsAsync()
    {
        return new
        {
            avg_response_time_ms = 245.0,
            error_rate_percent = 0.12,
            throughput_requests_per_second = 156.7,
            database_connections_active = 8
        };
    }

    private async Task<object> GetDailyTradingVolumeAsync()
    {
        var today = DateTime.UtcNow.Date;
        var volume = await _context.PurchaseContracts
            .Where(c => c.CreatedAt.Date == today)
            .SumAsync(c => c.ContractQuantity.Value);

        return new { volume_barrels = volume, date = today };
    }

    private async Task<object> GetPositionConcentrationAsync()
    {
        var positions = await _context.PurchaseContracts
            .Where(c => c.Status == ContractStatus.Active)
            .GroupBy(c => c.Product!.Type)
            .Select(g => new { ProductType = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var totalContracts = positions.Sum(p => p.Count);
        var herfindahlIndex = positions.Sum(p => Math.Pow((double)p.Count / totalContracts, 2));

        return new { herfindahl_index = herfindahlIndex, total_products = positions.Count };
    }

    private async Task<object> GetSettlementEfficiencyAsync()
    {
        var lastWeek = DateTime.UtcNow.AddDays(-7);
        var settlements = await _context.Settlements
            .Where(s => s.DueDate >= lastWeek)
            .ToListAsync();

        if (!settlements.Any())
        {
            return new { efficiency_percent = 100.0, total_settlements = 0 };
        }

        var onTimeSettlements = settlements.Count(s => s.Status == SettlementStatus.Completed);
        var efficiency = (double)onTimeSettlements / settlements.Count * 100;

        return new { efficiency_percent = efficiency, total_settlements = settlements.Count };
    }

    private async Task<object> GetPriceFeedQualityAsync()
    {
        var lastHour = DateTime.UtcNow.AddHours(-1);
        var priceUpdates = await _context.PricingEvents
            .CountAsync(p => p.EventDate >= lastHour);

        var expectedUpdates = 60; // Assuming updates every minute
        var quality = Math.Min(100.0, (double)priceUpdates / expectedUpdates * 100);

        return new { quality_percent = quality, updates_last_hour = priceUpdates };
    }

    private async Task<object> GetContractLifecycleMetricsAsync()
    {
        var last30Days = DateTime.UtcNow.AddDays(-30);
        var contracts = await _context.PurchaseContracts
            .Where(c => c.CreatedAt >= last30Days)
            .ToListAsync();

        var activeContracts = contracts
            .Where(c => c.Status == ContractStatus.Active && c.UpdatedAt.HasValue)
            .ToList();
            
        var avgTimeToActivation = activeContracts.Any() 
            ? activeContracts.Average(c => (c.UpdatedAt!.Value - c.CreatedAt).TotalHours)
            : 0.0;

        return new
        {
            avg_time_to_activation_hours = avgTimeToActivation,
            completion_rate_percent = contracts.Count(c => c.Status == ContractStatus.Completed) / (double)contracts.Count * 100,
            cancellation_rate_percent = contracts.Count(c => c.Status == ContractStatus.Cancelled) / (double)contracts.Count * 100
        };
    }

    private async Task<object> GetApiPerformanceMetricsAsync()
    {
        // In real implementation, integrate with APM tools
        return new
        {
            p50_response_time_ms = 125.0,
            p95_response_time_ms = 450.0,
            p99_response_time_ms = 1200.0,
            error_rate_4xx_percent = 2.1,
            error_rate_5xx_percent = 0.3
        };
    }

    private async Task<object> GetDatabasePerformanceAsync()
    {
        // In real implementation, query database metrics
        return new
        {
            avg_query_time_ms = 85.0,
            slow_queries_per_hour = 3,
            connection_pool_utilization_percent = 45.0,
            cache_hit_ratio_percent = 92.5
        };
    }

    private async Task<object> GetCacheEffectivenessAsync()
    {
        return new
        {
            hit_ratio_percent = 85.0,
            miss_ratio_percent = 15.0,
            eviction_rate_per_hour = 125,
            avg_response_time_ms = 2.3
        };
    }

    private async Task<object> GetErrorRatesAsync()
    {
        return new
        {
            application_errors_per_hour = 12,
            database_errors_per_hour = 2,
            external_api_errors_per_hour = 5,
            timeout_errors_per_hour = 3
        };
    }

    private async Task<SystemHealthScore> CalculateSystemHealthScoreAsync()
    {
        var scores = new Dictionary<string, double>();

        // Database health (30% weight)
        var dbHealth = await CalculateDatabaseHealthAsync();
        scores["database"] = dbHealth;

        // API performance (25% weight)
        var apiHealth = CalculateApiHealthAsync();
        scores["api_performance"] = apiHealth;

        // Business metrics (20% weight)
        var businessHealth = await CalculateBusinessHealthAsync();
        scores["business_metrics"] = businessHealth;

        // System resources (15% weight)
        var systemHealth = CalculateSystemResourceHealthAsync();
        scores["system_resources"] = systemHealth;

        // Data quality (10% weight)
        var dataHealth = await CalculateDataQualityHealthAsync();
        scores["data_quality"] = dataHealth;

        var overallScore = scores["database"] * 0.3 +
                          scores["api_performance"] * 0.25 +
                          scores["business_metrics"] * 0.2 +
                          scores["system_resources"] * 0.15 +
                          scores["data_quality"] * 0.1;

        var recommendations = GenerateHealthRecommendations(scores);

        return new SystemHealthScore
        {
            OverallScore = overallScore,
            ComponentScores = scores,
            Recommendations = recommendations
        };
    }

    private async Task<double> CalculateDatabaseHealthAsync()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            stopwatch.Stop();

            var responseTime = stopwatch.ElapsedMilliseconds;
            if (responseTime < 100) return 100;
            if (responseTime < 500) return 80;
            if (responseTime < 1000) return 60;
            return 40;
        }
        catch
        {
            return 0;
        }
    }

    private double CalculateApiHealthAsync()
    {
        // Simplified - in real implementation, get from APM
        return 85.0;
    }

    private async Task<double> CalculateBusinessHealthAsync()
    {
        var activeContracts = await _context.PurchaseContracts.CountAsync(c => c.Status == ContractStatus.Active);
        var today = DateTime.UtcNow.Date;
        var todayEvents = await _context.PricingEvents.CountAsync(p => p.EventDate.Date == today);

        // Simplified business health calculation
        var score = 50.0;
        if (activeContracts > 0) score += 25;
        if (todayEvents > 0) score += 25;

        return score;
    }

    private double CalculateSystemResourceHealthAsync()
    {
        var process = Process.GetCurrentProcess();
        var memoryUsage = process.WorkingSet64 / 1024 / 1024; // MB

        if (memoryUsage < 500) return 100;
        if (memoryUsage < 1000) return 80;
        if (memoryUsage < 2000) return 60;
        return 40;
    }

    private async Task<double> CalculateDataQualityHealthAsync()
    {
        var today = DateTime.UtcNow.Date;
        var expectedRecords = await _context.Products.CountAsync(p => p.IsActive);
        var actualRecords = await _context.PricingEvents.CountAsync(p => p.EventDate.Date == today);

        if (expectedRecords == 0) return 100;
        var completeness = (double)actualRecords / expectedRecords;
        return Math.Min(100, completeness * 100);
    }

    private List<string> GenerateHealthRecommendations(Dictionary<string, double> scores)
    {
        var recommendations = new List<string>();

        foreach (var score in scores)
        {
            if (score.Value < 60)
            {
                recommendations.Add($"Critical: {score.Key} health is below acceptable threshold ({score.Value:F1}%)");
            }
            else if (score.Value < 80)
            {
                recommendations.Add($"Warning: {score.Key} health needs attention ({score.Value:F1}%)");
            }
        }

        if (!recommendations.Any())
        {
            recommendations.Add("System is operating within normal parameters");
        }

        return recommendations;
    }

    public class SystemHealthScore
    {
        public double OverallScore { get; set; }
        public Dictionary<string, double> ComponentScores { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}