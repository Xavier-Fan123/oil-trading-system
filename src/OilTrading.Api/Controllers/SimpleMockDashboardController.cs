using Microsoft.AspNetCore.Mvc;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/mock-dashboard")]
public class SimpleMockDashboardController : ControllerBase
{
    private readonly ILogger<SimpleMockDashboardController> _logger;

    public SimpleMockDashboardController(ILogger<SimpleMockDashboardController> logger)
    {
        _logger = logger;
    }

    [HttpGet("overview")]
    public IActionResult GetOverview()
    {
        _logger.LogInformation("Returning mock dashboard overview");
        
        var data = new
        {
            totalPositions = 24,
            totalExposure = 158.9,
            netExposure = 142.3,
            longPositions = 15,
            shortPositions = 9,
            flatPositions = 0,
            dailyPnL = 125.3,
            unrealizedPnL = 89.7,
            vaR95 = 2.1,
            vaR99 = 3.8,
            portfolioVolatility = 18.5,
            activePurchaseContracts = 16,
            activeSalesContracts = 8,
            pendingContracts = 5,
            marketDataPoints = 1250,
            lastMarketUpdate = DateTime.UtcNow.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss"),
            alertCount = 3,
            calculatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        return Ok(data);
    }

    [HttpGet("trading-metrics")]
    public IActionResult GetTradingMetrics()
    {
        _logger.LogInformation("Returning mock trading metrics");
        
        var data = new
        {
            totalVolume = 125000,
            volumeUnit = "MT",
            tradingFrequency = 28,
            avgDealSize = 4.2,
            avgDealSizeCurrency = "USD",
            productDistribution = new[]
            {
                new { productType = "Brent", volumePercentage = 35.5, pnlContribution = 285 },
                new { productType = "WTI", volumePercentage = 25.2, pnlContribution = 180 },
                new { productType = "MGO", volumePercentage = 18.8, pnlContribution = 125 },
                new { productType = "Gasoil", volumePercentage = 12.3, pnlContribution = 95 },
                new { productType = "Fuel Oil", volumePercentage = 8.2, pnlContribution = 65 }
            },
            counterpartyConcentration = new[]
            {
                new { counterpartyName = "Shell Trading", exposurePercentage = 22.5, creditRating = "AA-" },
                new { counterpartyName = "BP Oil", exposurePercentage = 18.3, creditRating = "A+" },
                new { counterpartyName = "Total Energy", exposurePercentage = 15.7, creditRating = "A" },
                new { counterpartyName = "Exxon Mobil", exposurePercentage = 12.9, creditRating = "AA" },
                new { counterpartyName = "Other Partners", exposurePercentage = 30.6, creditRating = "A-" }
            },
            lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        return Ok(data);
    }

    [HttpGet("performance-analytics")]
    public IActionResult GetPerformanceAnalytics()
    {
        _logger.LogInformation("Returning mock performance analytics");
        
        var data = new
        {
            monthlyPnL = new[]
            {
                new { month = "2024-07", pnl = 450, cumulativePnL = 450 },
                new { month = "2024-08", pnl = -180, cumulativePnL = 270 },
                new { month = "2024-09", pnl = 320, cumulativePnL = 590 },
                new { month = "2024-10", pnl = 275, cumulativePnL = 865 },
                new { month = "2024-11", pnl = -95, cumulativePnL = 770 },
                new { month = "2024-12", pnl = 185, cumulativePnL = 955 },
                new { month = "2025-01", pnl = 125, cumulativePnL = 1080 }
            },
            sharpeRatio = 1.85,
            maxDrawdown = -12.3,
            winRate = 68.5,
            avgWinSize = 245,
            avgLossSize = -135,
            volatility = 18.7,
            lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        return Ok(data);
    }

    [HttpGet("market-insights")]
    public IActionResult GetMarketInsights()
    {
        _logger.LogInformation("Returning mock market insights");
        
        var data = new
        {
            benchmarkPrices = new[]
            {
                new { benchmark = "Brent", currentPrice = 82.45, change24h = 1.25, changePercent24h = 1.54, currency = "USD" },
                new { benchmark = "WTI", currentPrice = 78.92, change24h = 0.87, changePercent24h = 1.11, currency = "USD" },
                new { benchmark = "Dubai", currentPrice = 81.33, change24h = -0.45, changePercent24h = -0.55, currency = "USD" },
                new { benchmark = "Urals", currentPrice = 76.18, change24h = 0.32, changePercent24h = 0.42, currency = "USD" }
            },
            volatility = new[]
            {
                new { product = "Brent", impliedVolatility = 24.5, historicalVolatility = 22.8, volatilityTrend = "Rising" },
                new { product = "WTI", impliedVolatility = 26.2, historicalVolatility = 24.1, volatilityTrend = "Stable" },
                new { product = "MGO", impliedVolatility = 18.7, historicalVolatility = 19.3, volatilityTrend = "Falling" }
            },
            correlations = new[]
            {
                new { product1 = "Brent", product2 = "WTI", correlation = 0.87, trend = "Stable" },
                new { product1 = "Brent", product2 = "MGO", correlation = 0.65, trend = "Rising" },
                new { product1 = "WTI", product2 = "Gasoil", correlation = 0.73, trend = "Falling" }
            },
            marketSentiment = "Bullish",
            riskFactors = new[]
            {
                "Geopolitical tensions in Middle East",
                "OPEC+ production cuts",
                "Global economic slowdown concerns",
                "USD strength impact on commodities"
            },
            lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        return Ok(data);
    }

    [HttpGet("operational-status")]
    public IActionResult GetOperationalStatus()
    {
        _logger.LogInformation("Returning mock operational status");
        
        var data = new
        {
            activeContracts = 24,
            pendingContracts = 8,
            completedContractsThisMonth = 15,
            shipmentStatus = new[]
            {
                new { shipmentId = "SH001", status = "In Transit", vessel = "Nordic Star", origin = "Rotterdam", destination = "Singapore", eta = DateTime.Now.AddDays(5).ToString("yyyy-MM-dd"), quantity = 50000, unit = "MT" },
                new { shipmentId = "SH002", status = "Loading", vessel = "Pacific Glory", origin = "Houston", destination = "Tokyo", eta = DateTime.Now.AddDays(12).ToString("yyyy-MM-dd"), quantity = 75000, unit = "MT" },
                new { shipmentId = "SH003", status = "Completed", vessel = "Atlantic Pearl", origin = "Fujairah", destination = "Mumbai", eta = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd"), quantity = 45000, unit = "MT" }
            },
            riskAlerts = new[]
            {
                new { alertType = "Credit Risk", severity = "High", message = "Counterparty exposure exceeds 25% limit", timestamp = DateTime.Now.AddHours(-2).ToString("yyyy-MM-dd HH:mm:ss") },
                new { alertType = "Market Risk", severity = "Medium", message = "Oil price volatility increased 15%", timestamp = DateTime.Now.AddHours(-4).ToString("yyyy-MM-dd HH:mm:ss") },
                new { alertType = "Operational", severity = "Low", message = "Delayed shipment notification", timestamp = DateTime.Now.AddHours(-6).ToString("yyyy-MM-dd HH:mm:ss") }
            },
            upcomingDeliveries = new[]
            {
                new { contractNumber = "PC-2025-001", counterparty = "Shell Trading", deliveryDate = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"), quantity = 25000, unit = "MT", product = "Brent", status = "Confirmed" },
                new { contractNumber = "PC-2025-002", counterparty = "BP Oil", deliveryDate = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"), quantity = 35000, unit = "MT", product = "WTI", status = "Pending" },
                new { contractNumber = "SC-2025-003", counterparty = "Total Energy", deliveryDate = DateTime.Now.AddDays(10).ToString("yyyy-MM-dd"), quantity = 20000, unit = "MT", product = "MGO", status = "Confirmed" }
            },
            lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        return Ok(data);
    }
}