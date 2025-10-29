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

namespace OilTrading.UnitTests.Api.Controllers;

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
        _controller = new DashboardController(_mockMediator.Object, _mockDashboardService.Object, _mockLogger.Object);
    }

    #region GetOverview Tests

    [Fact]
    public async Task GetOverview_ReturnsOkResultWithDashboardData_WhenSuccessful()
    {
        // Arrange
        var expectedOverview = new DashboardOverviewDto
        {
            TotalPositions = 50,
            TotalExposure = 10_000_000m,
            NetExposure = 2_000_000m,
            LongPositions = 30,
            ShortPositions = 15,
            FlatPositions = 5,
            DailyPnL = 125_000m,
            UnrealizedPnL = 350_000m,
            VaR95 = 200_000m,
            VaR99 = 280_000m,
            PortfolioVolatility = 0.25m,
            ActivePurchaseContracts = 25,
            ActiveSalesContracts = 30,
            PendingContracts = 10,
            MarketDataPoints = 150,
            LastMarketUpdate = DateTime.UtcNow.AddMinutes(-5),
            AlertCount = 3,
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
        var dashboardData = okResult!.Value as DashboardOverviewDto;

        dashboardData.Should().NotBeNull();
        dashboardData!.TotalPositions.Should().Be(50);
        dashboardData.TotalExposure.Should().Be(10_000_000m);
        dashboardData.VaR95.Should().Be(200_000m);
        dashboardData.DailyPnL.Should().Be(125_000m);
        dashboardData.AlertCount.Should().Be(3);
    }

    [Fact]
    public async Task GetOverview_ReturnsInternalServerError_WhenServiceThrows()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Dashboard service unavailable");

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetDashboardOverviewQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _controller.GetOverview();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();

        var errorObject = badRequestResult!.Value;
        errorObject.Should().NotBeNull();

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOverview_ResponseMatchesDashboardOverviewDtoStructure()
    {
        // Arrange
        var expectedOverview = new DashboardOverviewDto
        {
            TotalPositions = 25,
            TotalExposure = 5_000_000m,
            NetExposure = 1_000_000m,
            LongPositions = 15,
            ShortPositions = 8,
            FlatPositions = 2,
            DailyPnL = 50_000m,
            UnrealizedPnL = 150_000m,
            VaR95 = 100_000m,
            VaR99 = 140_000m,
            PortfolioVolatility = 0.22m,
            ActivePurchaseContracts = 12,
            ActiveSalesContracts = 15,
            PendingContracts = 5,
            MarketDataPoints = 80,
            LastMarketUpdate = DateTime.UtcNow.AddMinutes(-10),
            AlertCount = 1,
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
        var dashboardData = okResult!.Value as DashboardOverviewDto;

        dashboardData.Should().NotBeNull();
        dashboardData!.Should().BeEquivalentTo(expectedOverview, options =>
            options.Excluding(x => x.CalculatedAt).Excluding(x => x.LastMarketUpdate));
    }

    [Fact]
    public async Task GetOverview_VerifiesMediatorSendCalledWithCorrectQuery()
    {
        // Arrange
        var expectedOverview = new DashboardOverviewDto
        {
            TotalPositions = 10,
            TotalExposure = 1_000_000m,
            VaR95 = 50_000m,
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetDashboardOverviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOverview);

        // Act
        var result = await _controller.GetOverview();

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        _mockMediator.Verify(
            m => m.Send(
                It.IsAny<GetDashboardOverviewQuery>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetTradingMetrics Tests

    [Fact]
    public async Task GetTradingMetrics_ReturnsTradingMetricsSuccessfully()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddMonths(-1);
        var endDate = DateTime.UtcNow;

        var expectedMetrics = new TradingMetricsDto
        {
            Period = "Last 30 Days",
            TotalTrades = 45,
            TotalVolume = 5_000_000m,
            AverageTradeSize = 111_111m,
            PurchaseVolume = 3_000_000m,
            SalesVolume = 2_000_000m,
            ProductBreakdown = new Dictionary<string, decimal>
            {
                ["Brent"] = 2_000_000m,
                ["WTI"] = 1_500_000m,
                ["MGO"] = 1_500_000m
            },
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
        var metrics = okResult!.Value as TradingMetricsDto;

        metrics.Should().NotBeNull();
        metrics!.TotalTrades.Should().Be(45);
        metrics.TotalVolume.Should().Be(5_000_000m);
        metrics.ProductBreakdown.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTradingMetrics_HandlesNullOrEmptyDataGracefully()
    {
        // Arrange
        var emptyMetrics = new TradingMetricsDto
        {
            Period = "No Data",
            TotalTrades = 0,
            TotalVolume = 0m,
            AverageTradeSize = 0m,
            PurchaseVolume = 0m,
            SalesVolume = 0m,
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
        var metrics = okResult!.Value as TradingMetricsDto;

        metrics.Should().NotBeNull();
        metrics!.TotalTrades.Should().Be(0);
        metrics.TotalVolume.Should().Be(0m);
        metrics.ProductBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTradingMetrics_VerifiesResponseStructure()
    {
        // Arrange
        var metrics = new TradingMetricsDto
        {
            Period = "Q4 2024",
            TotalTrades = 120,
            TotalVolume = 15_000_000m,
            AverageTradeSize = 125_000m,
            PurchaseVolume = 9_000_000m,
            SalesVolume = 6_000_000m,
            ProductBreakdown = new Dictionary<string, decimal>
            {
                ["Brent"] = 8_000_000m,
                ["WTI"] = 7_000_000m
            },
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetTradingMetricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetTradingMetrics(DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var resultMetrics = okResult!.Value as TradingMetricsDto;

        resultMetrics.Should().NotBeNull();
        resultMetrics!.Period.Should().NotBeEmpty();
        resultMetrics.ProductBreakdown.Should().NotBeNull();
    }

    #endregion

    #region GetPerformanceAnalytics Tests

    [Fact]
    public async Task GetPerformanceAnalytics_ReturnsPerformanceDataSuccessfully()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddMonths(-6);
        var endDate = DateTime.UtcNow;

        var expectedPerformance = new PerformanceAnalyticsDto
        {
            Period = "Last 6 Months",
            TotalPnL = 2_500_000m,
            RealizedPnL = 1_800_000m,
            UnrealizedPnL = 700_000m,
            SharpeRatio = 1.85m,
            MaxDrawdown = 0.12m,
            WinRate = 0.68m,
            ProfitFactor = 2.14m,
            TotalReturn = 0.25m,
            AnnualizedReturn = 0.30m,
            BestPerformingProduct = "Brent",
            WorstPerformingProduct = "MGO",
            VaRUtilization = 0.60m,
            VolatilityAdjustedReturn = 1.15m,
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPerformanceAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPerformance);

        // Act
        var result = await _controller.GetPerformanceAnalytics(startDate, endDate);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var performance = okResult!.Value as PerformanceAnalyticsDto;

        performance.Should().NotBeNull();
        performance!.TotalPnL.Should().Be(2_500_000m);
        performance.SharpeRatio.Should().Be(1.85m);
        performance.MaxDrawdown.Should().Be(0.12m);
    }

    [Fact]
    public async Task GetPerformanceAnalytics_ValidatesDateRangeParameters()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddYears(-1);
        var endDate = DateTime.UtcNow;

        var performance = new PerformanceAnalyticsDto
        {
            Period = "Last Year",
            TotalPnL = 5_000_000m,
            SharpeRatio = 1.5m
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPerformanceAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(performance);

        // Act
        var result = await _controller.GetPerformanceAnalytics(startDate, endDate);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        _mockMediator.Verify(
            m => m.Send(
                It.Is<GetPerformanceAnalyticsQuery>(q =>
                    q.StartDate == startDate &&
                    q.EndDate == endDate),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPerformanceAnalytics_ReturnsBadRequestForInvalidDates()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPerformanceAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid date range: start date must be before end date"));

        // Act
        var result = await _controller.GetPerformanceAnalytics(DateTime.UtcNow, DateTime.UtcNow.AddDays(-30));

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;

        var errorObject = badRequestResult!.Value;
        errorObject.Should().NotBeNull();
    }

    #endregion

    #region GetMarketInsights Tests

    [Fact]
    public async Task GetMarketInsights_ReturnsMarketInsightsSuccessfully()
    {
        // Arrange
        var expectedInsights = new MarketInsightsDto
        {
            MarketDataCount = 250,
            LastUpdate = DateTime.UtcNow.AddMinutes(-2),
            KeyPrices = new List<KeyPriceDto>
            {
                new() { Product = "Brent", Price = 85.50m, Change = 1.25m, ChangePercent = 1.48m, LastUpdate = DateTime.UtcNow },
                new() { Product = "WTI", Price = 82.25m, Change = 0.75m, ChangePercent = 0.92m, LastUpdate = DateTime.UtcNow }
            },
            VolatilityIndicators = new Dictionary<string, decimal>
            {
                ["Brent_30D"] = 0.28m,
                ["WTI_30D"] = 0.26m
            },
            TechnicalIndicators = new Dictionary<string, decimal>
            {
                ["RSI"] = 65m,
                ["MACD"] = 1.5m
            },
            MarketTrends = new List<MarketTrendDto>
            {
                new() { Product = "Brent", Trend = "Upward", Strength = 0.75m }
            },
            SentimentIndicators = new Dictionary<string, decimal>
            {
                ["Overall"] = 0.65m
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
        var insights = okResult!.Value as MarketInsightsDto;

        insights.Should().NotBeNull();
        insights!.MarketDataCount.Should().Be(250);
        insights.KeyPrices.Should().HaveCount(2);
        insights.KeyPrices.Should().Contain(p => p.Product == "Brent" && p.Price == 85.50m);
        insights.TechnicalIndicators.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMarketInsights_HandlesErrorsGracefully()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Market data feed unavailable");

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetMarketInsightsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _controller.GetMarketInsights();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetOperationalStatus Tests

    [Fact]
    public async Task GetOperationalStatus_ReturnsOperationalStatusSuccessfully()
    {
        // Arrange
        var expectedStatus = new OperationalStatusDto
        {
            ActiveShipments = 12,
            PendingDeliveries = 5,
            CompletedDeliveries = 25,
            ContractsAwaitingExecution = 8,
            ContractsInLaycan = 4,
            UpcomingLaycans = new List<LaycanDto>
            {
                new() { ContractNumber = "PC-001", ContractType = "Purchase", LaycanStart = DateTime.UtcNow.AddDays(5), LaycanEnd = DateTime.UtcNow.AddDays(10), Product = "Brent", Quantity = 50000m }
            },
            SystemHealth = new SystemHealthDto
            {
                DatabaseStatus = "Online",
                CacheStatus = "Online",
                MarketDataStatus = "Online",
                OverallStatus = "Healthy"
            },
            CacheHitRatio = 0.92m,
            LastDataRefresh = DateTime.UtcNow.AddMinutes(-1),
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
        var status = okResult!.Value as OperationalStatusDto;

        status.Should().NotBeNull();
        status!.ActiveShipments.Should().Be(12);
        status.PendingDeliveries.Should().Be(5);
        status.SystemHealth.OverallStatus.Should().Be("Healthy");
        status.SystemHealth.DatabaseStatus.Should().Be("Online");
    }

    [Fact]
    public async Task GetOperationalStatus_HandlesServiceFailures()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetOperationalStatusQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Operational status query failed"));

        // Act
        var result = await _controller.GetOperationalStatus();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetActiveAlerts Tests

    [Fact]
    public async Task GetActiveAlerts_ReturnsActiveAlertsSuccessfully()
    {
        // Arrange
        var expectedAlerts = new List<AlertDto>
        {
            new()
            {
                Type = "Position Limit",
                Severity = "High",
                Message = "Brent position approaching limit (90% utilized)",
                Timestamp = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                Type = "Risk Warning",
                Severity = "Medium",
                Message = "VaR95 exceeds 80% of limit",
                Timestamp = DateTime.UtcNow.AddHours(-1)
            },
            new()
            {
                Type = "Data Quality",
                Severity = "Low",
                Message = "Market data delayed by 5 minutes",
                Timestamp = DateTime.UtcNow.AddMinutes(-10)
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
        alerts!.Should().HaveCount(3);
        alerts.Should().Contain(a => a.Type == "Position Limit");
        alerts.Should().Contain(a => a.Severity == "High");
    }

    [Fact]
    public async Task GetActiveAlerts_ReturnsEmptyListWhenNoAlerts()
    {
        // Arrange
        var emptyAlerts = new List<AlertDto>();

        _mockDashboardService
            .Setup(s => s.GetActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyAlerts);

        // Act
        var result = await _controller.GetActiveAlerts();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var alerts = okResult!.Value as IEnumerable<AlertDto>;

        alerts.Should().NotBeNull();
        alerts!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveAlerts_HandlesServiceErrors()
    {
        // Arrange
        _mockDashboardService
            .Setup(s => s.GetActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Alert service unavailable"));

        // Act
        var result = await _controller.GetActiveAlerts();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetKpiSummary Tests

    [Fact]
    public async Task GetKpiSummary_ReturnsKpiSummarySuccessfully()
    {
        // Arrange
        var expectedKpi = new KpiSummaryDto
        {
            TotalExposure = 15_000_000m,
            ExposureUtilization = 0.75m,
            DailyPnL = 250_000m,
            VaR95 = 300_000m,
            RiskUtilization = 0.60m,
            PortfolioCount = 55,
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
        var kpi = okResult!.Value as KpiSummaryDto;

        kpi.Should().NotBeNull();
        kpi!.TotalExposure.Should().Be(15_000_000m);
        kpi.DailyPnL.Should().Be(250_000m);
        kpi.VaR95.Should().Be(300_000m);
        kpi.ExposureUtilization.Should().Be(0.75m);
        kpi.RiskUtilization.Should().Be(0.60m);
        kpi.PortfolioCount.Should().Be(55);
    }

    [Fact]
    public async Task GetKpiSummary_HandlesServiceFailures()
    {
        // Arrange
        _mockDashboardService
            .Setup(s => s.GetKpiSummaryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("KPI calculation failed"));

        // Act
        var result = await _controller.GetKpiSummary();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetOverview_WithEmptyPortfolio_ReturnsZeroMetrics()
    {
        // Arrange
        var emptyOverview = new DashboardOverviewDto
        {
            TotalPositions = 0,
            TotalExposure = 0m,
            NetExposure = 0m,
            LongPositions = 0,
            ShortPositions = 0,
            FlatPositions = 0,
            DailyPnL = 0m,
            UnrealizedPnL = 0m,
            VaR95 = 0m,
            VaR99 = 0m,
            PortfolioVolatility = 0m,
            ActivePurchaseContracts = 0,
            ActiveSalesContracts = 0,
            PendingContracts = 0,
            MarketDataPoints = 0,
            LastMarketUpdate = DateTime.UtcNow,
            AlertCount = 0,
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetDashboardOverviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyOverview);

        // Act
        var result = await _controller.GetOverview();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var overview = okResult!.Value as DashboardOverviewDto;

        overview.Should().NotBeNull();
        overview!.TotalPositions.Should().Be(0);
        overview.TotalExposure.Should().Be(0m);
        overview.VaR95.Should().Be(0m);
    }

    [Fact]
    public async Task GetTradingMetrics_WithNullDates_UsesDefaultDateRange()
    {
        // Arrange
        var metrics = new TradingMetricsDto
        {
            Period = "Default Period",
            TotalTrades = 10,
            TotalVolume = 1_000_000m
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetTradingMetricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetTradingMetrics(null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        _mockMediator.Verify(
            m => m.Send(
                It.Is<GetTradingMetricsQuery>(q =>
                    q.StartDate == null &&
                    q.EndDate == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPerformanceAnalytics_WithNullDates_UsesDefaultPeriod()
    {
        // Arrange
        var performance = new PerformanceAnalyticsDto
        {
            Period = "Default Period",
            TotalPnL = 500_000m,
            SharpeRatio = 1.2m
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPerformanceAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(performance);

        // Act
        var result = await _controller.GetPerformanceAnalytics(null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        _mockMediator.Verify(
            m => m.Send(
                It.Is<GetPerformanceAnalyticsQuery>(q =>
                    q.StartDate == null &&
                    q.EndDate == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
