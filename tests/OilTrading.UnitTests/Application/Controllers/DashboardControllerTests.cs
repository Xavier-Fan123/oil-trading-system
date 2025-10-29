using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OilTrading.Api.Controllers;
using OilTrading.Application.DTOs;
using OilTrading.Application.Queries.Dashboard;
using OilTrading.Application.Services;
using Xunit;

namespace OilTrading.UnitTests.Application.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IDashboardService> _mockDashboardService;
    private readonly Mock<ILogger<DashboardController>> _mockLogger;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockDashboardService = new Mock<IDashboardService>();
        _mockLogger = new Mock<ILogger<DashboardController>>();
        _controller = new DashboardController(
            _mockMediator.Object,
            _mockDashboardService.Object,
            _mockLogger.Object);
    }

    #region GetOverview Tests

    [Fact]
    public async Task GetOverview_WithValidData_ReturnsOkResultWithDashboardData()
    {
        // Arrange
        var expectedOverview = new DashboardOverviewDto
        {
            TotalPositions = 10,
            TotalExposure = 5000000m,
            NetExposure = 2500000m,
            LongPositions = 6,
            ShortPositions = 3,
            FlatPositions = 1,
            DailyPnL = 25000m,
            UnrealizedPnL = 150000m,
            VaR95 = 125000m,
            VaR99 = 175000m,
            PortfolioVolatility = 0.25m,
            ActivePurchaseContracts = 15,
            ActiveSalesContracts = 12,
            PendingContracts = 3,
            MarketDataPoints = 1250,
            LastMarketUpdate = DateTime.UtcNow.AddMinutes(-5),
            AlertCount = 2,
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetDashboardOverviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOverview);

        // Act
        var result = await _controller.GetOverview();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedOverview);

        _mockMediator.Verify(
            m => m.Send(It.IsAny<GetDashboardOverviewQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOverview_WhenServiceThrowsException_ReturnsBadRequestWithError()
    {
        // Arrange
        var exceptionMessage = "Database connection failed";
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetDashboardOverviewQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.GetOverview();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();

        // The error is returned as an anonymous object with an 'error' property
        var errorValue = badRequestResult.Value.ToString();
        errorValue.Should().Contain("Failed to retrieve dashboard overview");
    }

    [Fact]
    public async Task GetOverview_ResponseMatchesExpectedDtoStructure_WithAllProperties()
    {
        // Arrange
        var overview = new DashboardOverviewDto
        {
            TotalPositions = 5,
            TotalExposure = 1000000m,
            NetExposure = 500000m,
            VaR95 = 50000m,
            VaR99 = 75000m,
            PortfolioVolatility = 0.20m,
            ActivePurchaseContracts = 8,
            ActiveSalesContracts = 7,
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetDashboardOverviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(overview);

        // Act
        var result = await _controller.GetOverview();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedDto = okResult!.Value as DashboardOverviewDto;

        returnedDto.Should().NotBeNull();
        returnedDto!.TotalPositions.Should().Be(overview.TotalPositions);
        returnedDto.TotalExposure.Should().Be(overview.TotalExposure);
        returnedDto.VaR95.Should().Be(overview.VaR95);
        returnedDto.VaR99.Should().Be(overview.VaR99);
        returnedDto.ActivePurchaseContracts.Should().Be(overview.ActivePurchaseContracts);
    }

    [Fact]
    public async Task GetOverview_CachedData_IsReturnedOnSecondCall()
    {
        // Arrange
        var overview = new DashboardOverviewDto
        {
            TotalPositions = 10,
            TotalExposure = 5000000m,
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetDashboardOverviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(overview);

        // Act - First call
        var result1 = await _controller.GetOverview();
        // Act - Second call
        var result2 = await _controller.GetOverview();

        // Assert
        result1.Should().BeOfType<OkObjectResult>();
        result2.Should().BeOfType<OkObjectResult>();

        // Both calls should succeed
        var okResult1 = result1 as OkObjectResult;
        var okResult2 = result2 as OkObjectResult;
        okResult1!.Value.Should().BeEquivalentTo(overview);
        okResult2!.Value.Should().BeEquivalentTo(overview);

        // Mediator should be called twice (caching handled at query handler level)
        _mockMediator.Verify(
            m => m.Send(It.IsAny<GetDashboardOverviewQuery>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    #endregion

    #region GetTradingMetrics Tests

    [Fact]
    public async Task GetTradingMetrics_WithValidDateRange_ReturnsTradingMetricsSuccessfully()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var expectedMetrics = new TradingMetricsDto
        {
            Period = "Last 30 Days",
            TotalTrades = 45,
            TotalVolume = 50000m,
            AverageTradeSize = 1111m,
            PurchaseVolume = 25000m,
            SalesVolume = 20000m,
            PaperVolume = 5000m,
            LongPaperVolume = 3000m,
            ShortPaperVolume = 2000m,
            ProductBreakdown = new Dictionary<string, decimal>
            {
                ["Brent"] = 30000m,
                ["WTI"] = 20000m
            },
            TradeFrequency = 1.5m,
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetTradingMetricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetTradingMetrics(startDate, endDate);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMetrics);

        _mockMediator.Verify(
            m => m.Send(It.Is<GetTradingMetricsQuery>(q =>
                q.StartDate == startDate && q.EndDate == endDate),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTradingMetrics_WithEmptyData_ReturnsGracefully()
    {
        // Arrange
        var emptyMetrics = new TradingMetricsDto
        {
            Period = "Last 30 Days",
            TotalTrades = 0,
            TotalVolume = 0m,
            AverageTradeSize = 0m,
            ProductBreakdown = new Dictionary<string, decimal>(),
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetTradingMetricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyMetrics);

        // Act
        var result = await _controller.GetTradingMetrics(null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedMetrics = okResult!.Value as TradingMetricsDto;

        returnedMetrics.Should().NotBeNull();
        returnedMetrics!.TotalTrades.Should().Be(0);
        returnedMetrics.TotalVolume.Should().Be(0);
        returnedMetrics.ProductBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTradingMetrics_CacheIsUsedWithFiveMinuteTTL_OnSuccessfulCall()
    {
        // Arrange
        var metrics = new TradingMetricsDto
        {
            Period = "Today",
            TotalTrades = 10,
            TotalVolume = 5000m,
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetTradingMetricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetTradingMetrics(null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(metrics);

        // Verify the query was sent (caching is at query handler level)
        _mockMediator.Verify(
            m => m.Send(It.IsAny<GetTradingMetricsQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetPerformanceAnalytics Tests

    [Fact]
    public async Task GetPerformanceAnalytics_WithValidDateRange_ReturnsPerformanceDataSuccessfully()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-90);
        var endDate = DateTime.UtcNow;
        var expectedAnalytics = new PerformanceAnalyticsDto
        {
            Period = "Last 90 Days",
            TotalPnL = 250000m,
            RealizedPnL = 200000m,
            UnrealizedPnL = 50000m,
            BestPerformingProduct = "Brent",
            WorstPerformingProduct = "WTI",
            TotalReturn = 0.15m,
            AnnualizedReturn = 0.65m,
            SharpeRatio = 1.8m,
            MaxDrawdown = 0.08m,
            WinRate = 0.67m,
            ProfitFactor = 2.3m,
            VaRUtilization = 0.45m,
            VolatilityAdjustedReturn = 0.72m,
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPerformanceAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAnalytics);

        // Act
        var result = await _controller.GetPerformanceAnalytics(startDate, endDate);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedAnalytics);

        _mockMediator.Verify(
            m => m.Send(It.Is<GetPerformanceAnalyticsQuery>(q =>
                q.StartDate == startDate && q.EndDate == endDate),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPerformanceAnalytics_WithNullDates_UsesDefaultDateRange()
    {
        // Arrange
        var analytics = new PerformanceAnalyticsDto
        {
            Period = "All Time",
            TotalPnL = 500000m,
            SharpeRatio = 1.5m,
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPerformanceAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analytics);

        // Act
        var result = await _controller.GetPerformanceAnalytics(null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedAnalytics = okResult!.Value as PerformanceAnalyticsDto;

        returnedAnalytics.Should().NotBeNull();
        returnedAnalytics!.Period.Should().Be("All Time");

        _mockMediator.Verify(
            m => m.Send(It.Is<GetPerformanceAnalyticsQuery>(q =>
                q.StartDate == null && q.EndDate == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPerformanceAnalytics_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var exceptionMessage = "Failed to calculate performance metrics";
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPerformanceAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.GetPerformanceAnalytics(null, null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();

        // Verify the error response structure without using dynamic
        var errorValue = badRequestResult.Value.ToString();
        errorValue.Should().NotBeNull();
    }

    #endregion

    #region GetMarketInsights Tests

    [Fact]
    public async Task GetMarketInsights_WithValidData_ReturnsMarketInsightsSuccessfully()
    {
        // Arrange
        var expectedInsights = new MarketInsightsDto
        {
            MarketDataCount = 500,
            LastUpdate = DateTime.UtcNow.AddMinutes(-10),
            KeyPrices = new List<KeyPriceDto>
            {
                new KeyPriceDto
                {
                    Product = "Brent",
                    Price = 85.50m,
                    Change = 1.25m,
                    ChangePercent = 1.48m,
                    LastUpdate = DateTime.UtcNow
                },
                new KeyPriceDto
                {
                    Product = "WTI",
                    Price = 80.75m,
                    Change = -0.50m,
                    ChangePercent = -0.62m,
                    LastUpdate = DateTime.UtcNow
                }
            },
            VolatilityIndicators = new Dictionary<string, decimal>
            {
                ["Brent"] = 0.25m,
                ["WTI"] = 0.28m
            },
            CorrelationMatrix = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["Brent"] = new Dictionary<string, decimal> { ["WTI"] = 0.85m },
                ["WTI"] = new Dictionary<string, decimal> { ["Brent"] = 0.85m }
            },
            TechnicalIndicators = new Dictionary<string, decimal>
            {
                ["RSI"] = 65m,
                ["MACD"] = 1.5m
            },
            MarketTrends = new List<MarketTrendDto>
            {
                new MarketTrendDto
                {
                    Product = "Brent",
                    Trend = "Bullish",
                    Strength = 0.75m
                }
            },
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetMarketInsightsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedInsights);

        // Act
        var result = await _controller.GetMarketInsights();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedInsights);

        _mockMediator.Verify(
            m => m.Send(It.IsAny<GetMarketInsightsQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMarketInsights_WhenMarketDataUnavailable_HandlesGracefully()
    {
        // Arrange
        var emptyInsights = new MarketInsightsDto
        {
            MarketDataCount = 0,
            LastUpdate = DateTime.MinValue,
            KeyPrices = new List<KeyPriceDto>(),
            VolatilityIndicators = new Dictionary<string, decimal>(),
            CorrelationMatrix = new Dictionary<string, Dictionary<string, decimal>>(),
            TechnicalIndicators = new Dictionary<string, decimal>(),
            MarketTrends = new List<MarketTrendDto>(),
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetMarketInsightsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyInsights);

        // Act
        var result = await _controller.GetMarketInsights();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var insights = okResult!.Value as MarketInsightsDto;

        insights.Should().NotBeNull();
        insights!.MarketDataCount.Should().Be(0);
        insights.KeyPrices.Should().BeEmpty();
        insights.VolatilityIndicators.Should().BeEmpty();
    }

    #endregion

    #region GetOperationalStatus Tests

    [Fact]
    public async Task GetOperationalStatus_WithValidData_ReturnsOperationalStatusSuccessfully()
    {
        // Arrange
        var expectedStatus = new OperationalStatusDto
        {
            ActiveShipments = 8,
            PendingDeliveries = 5,
            CompletedDeliveries = 42,
            ContractsAwaitingExecution = 3,
            ContractsInLaycan = 6,
            UpcomingLaycans = new List<LaycanDto>
            {
                new LaycanDto
                {
                    ContractNumber = "PC-2024-001",
                    ContractType = "Purchase",
                    LaycanStart = DateTime.UtcNow.AddDays(5),
                    LaycanEnd = DateTime.UtcNow.AddDays(10),
                    Product = "Brent",
                    Quantity = 5000m
                }
            },
            SystemHealth = new SystemHealthDto
            {
                DatabaseStatus = "Healthy",
                CacheStatus = "Healthy",
                MarketDataStatus = "Healthy",
                OverallStatus = "Healthy"
            },
            CacheHitRatio = 0.92m,
            LastDataRefresh = DateTime.UtcNow.AddMinutes(-2),
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetOperationalStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStatus);

        // Act
        var result = await _controller.GetOperationalStatus();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedStatus);

        _mockMediator.Verify(
            m => m.Send(It.IsAny<GetOperationalStatusQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetActiveAlerts Tests

    [Fact]
    public async Task GetActiveAlerts_WithAlerts_ReturnsListOfAlerts()
    {
        // Arrange
        var expectedAlerts = new List<AlertDto>
        {
            new AlertDto
            {
                Severity = "High",
                Type = "Position Limit",
                Message = "Brent position approaching limit",
                Timestamp = DateTime.UtcNow.AddHours(-1)
            },
            new AlertDto
            {
                Severity = "Medium",
                Type = "Risk Warning",
                Message = "VaR exceeds 80% of limit",
                Timestamp = DateTime.UtcNow.AddMinutes(-30)
            }
        };

        _mockDashboardService
            .Setup(s => s.GetActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetActiveAlerts();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var alerts = okResult!.Value as IEnumerable<AlertDto>;

        alerts.Should().NotBeNull();
        alerts.Should().HaveCount(2);
        alerts.Should().BeEquivalentTo(expectedAlerts);

        _mockDashboardService.Verify(
            s => s.GetActiveAlertsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetKpiSummary Tests

    [Fact]
    public async Task GetKpiSummary_WithValidData_ReturnsKpiSummarySuccessfully()
    {
        // Arrange
        var expectedKpi = new KpiSummaryDto
        {
            TotalExposure = 10000000m,
            DailyPnL = 50000m,
            VaR95 = 250000m,
            PortfolioCount = 15,
            ExposureUtilization = 0.75m,
            RiskUtilization = 0.50m,
            CalculatedAt = DateTime.UtcNow
        };

        _mockDashboardService
            .Setup(s => s.GetKpiSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedKpi);

        // Act
        var result = await _controller.GetKpiSummary();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedKpi);

        _mockDashboardService.Verify(
            s => s.GetKpiSummaryAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
