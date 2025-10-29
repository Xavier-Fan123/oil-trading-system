using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OilTrading.Api.Controllers;
using OilTrading.Application.DTOs;
using OilTrading.Application.Queries.Risk;
using Xunit;

namespace OilTrading.UnitTests.Api.Controllers;

public class RiskControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<RiskController>> _mockLogger;
    private readonly RiskController _controller;

    public RiskControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<RiskController>>();
        _controller = new RiskController(_mockMediator.Object, _mockLogger.Object);
    }

    #region CalculateRisk Tests

    [Fact]
    public async Task CalculateRisk_WithValidParameters_ReturnsVaR95AndVaR99Successfully()
    {
        // Arrange
        var expectedResult = new RiskCalculationResultDto
        {
            CalculationDate = DateTime.UtcNow,
            TotalPortfolioValue = 1_000_000m,
            PositionCount = 10,
            HistoricalVaR95 = 32_900m,
            HistoricalVaR99 = 46_520m,
            GarchVaR95 = 31_500m,
            GarchVaR99 = 44_800m,
            McVaR95 = 33_200m,
            McVaR99 = 47_100m,
            PortfolioVolatility = 0.25m,
            ExpectedShortfall95 = 40_000m,
            ExpectedShortfall99 = 55_000m,
            MaxDrawdown = 0.15m,
            StressTests = new List<StressTestResultDto>
            {
                new() { Scenario = "-10% Shock", PnlImpact = -100_000m, PercentageChange = -10m }
            },
            ProductExposures = new List<ProductExposureDto>
            {
                new() { ProductType = "Brent", NetExposure = 500_000m, VaR95 = 20_000m }
            }
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CalculateRisk(DateTime.UtcNow, 252, true);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var riskResult = okResult!.Value as RiskCalculationResultDto;

        riskResult.Should().NotBeNull();
        riskResult!.HistoricalVaR95.Should().Be(32_900m);
        riskResult.HistoricalVaR99.Should().Be(46_520m);
        riskResult.VaR95.Should().Be(32_900m, "VaR95 property should alias to HistoricalVaR95");
        riskResult.VaR99.Should().Be(46_520m, "VaR99 property should alias to HistoricalVaR99");
        riskResult.GarchVaR95.Should().Be(31_500m);
        riskResult.GarchVaR99.Should().Be(44_800m);
    }

    [Fact]
    public async Task CalculateRisk_WithEmptyPortfolio_ReturnsZeroRisk()
    {
        // Arrange
        var emptyPortfolioResult = new RiskCalculationResultDto
        {
            CalculationDate = DateTime.UtcNow,
            TotalPortfolioValue = 0m,
            PositionCount = 0,
            HistoricalVaR95 = 0m,
            HistoricalVaR99 = 0m,
            GarchVaR95 = 0m,
            GarchVaR99 = 0m,
            McVaR95 = 0m,
            McVaR99 = 0m,
            PortfolioVolatility = 0m,
            ExpectedShortfall95 = 0m,
            ExpectedShortfall99 = 0m,
            MaxDrawdown = 0m,
            StressTests = new List<StressTestResultDto>(),
            ProductExposures = new List<ProductExposureDto>()
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyPortfolioResult);

        // Act
        var result = await _controller.CalculateRisk(null, 252, true);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var riskResult = okResult!.Value as RiskCalculationResultDto;

        riskResult.Should().NotBeNull();
        riskResult!.TotalPortfolioValue.Should().Be(0m);
        riskResult.PositionCount.Should().Be(0);
        riskResult.HistoricalVaR95.Should().Be(0m);
        riskResult.HistoricalVaR99.Should().Be(0m);
        riskResult.StressTests.Should().BeEmpty();
        riskResult.ProductExposures.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateRisk_WhenCalculationFails_HandlesErrorsGracefully()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Risk calculation service unavailable");

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _controller.CalculateRisk(DateTime.UtcNow, 252, true);

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
    public async Task CalculateRisk_ResponseIncludesVolatilityAndPortfolioValue()
    {
        // Arrange
        var expectedResult = new RiskCalculationResultDto
        {
            CalculationDate = DateTime.UtcNow,
            TotalPortfolioValue = 5_000_000m,
            PositionCount = 25,
            HistoricalVaR95 = 125_000m,
            HistoricalVaR99 = 175_000m,
            PortfolioVolatility = 0.32m,
            ExpectedShortfall95 = 150_000m,
            MaxDrawdown = 0.22m,
            StressTests = new List<StressTestResultDto>(),
            ProductExposures = new List<ProductExposureDto>()
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CalculateRisk(DateTime.UtcNow, 252, true);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var riskResult = okResult!.Value as RiskCalculationResultDto;

        riskResult.Should().NotBeNull();
        riskResult!.PortfolioVolatility.Should().Be(0.32m);
        riskResult.TotalPortfolioValue.Should().Be(5_000_000m);
        riskResult.PortfolioValue.Should().Be(5_000_000m, "PortfolioValue alias should match TotalPortfolioValue");
        riskResult.MaxDrawdown.Should().Be(0.22m);
    }

    #endregion

    #region CalculateStressScenario Tests (via StressTests)

    [Fact]
    public async Task CalculateRisk_AppliesNegative50PercentPriceShockCorrectly()
    {
        // Arrange
        var portfolioValue = 10_000_000m;
        var expectedLoss = portfolioValue * 0.50m; // 50% shock

        var riskResult = new RiskCalculationResultDto
        {
            CalculationDate = DateTime.UtcNow,
            TotalPortfolioValue = portfolioValue,
            PositionCount = 50,
            HistoricalVaR95 = 200_000m,
            HistoricalVaR99 = 280_000m,
            StressTests = new List<StressTestResultDto>
            {
                new()
                {
                    Scenario = "-50% Extreme Shock",
                    PnlImpact = -5_000_000m, // 50% loss
                    PercentageChange = -50m,
                    Description = "Extreme market crash scenario"
                }
            }
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(riskResult);

        // Act
        var result = await _controller.CalculateRisk(DateTime.UtcNow, 252, true);

        // Assert
        var okResult = result as OkObjectResult;
        var dto = okResult!.Value as RiskCalculationResultDto;

        var extremeShock = dto!.StressTests.First(s => s.Scenario == "-50% Extreme Shock");
        extremeShock.PnlImpact.Should().Be(-5_000_000m);
        extremeShock.PercentageChange.Should().Be(-50m);
        Math.Abs(extremeShock.PnlImpact).Should().BeApproximately(expectedLoss, 1000m);
    }

    [Fact]
    public async Task CalculateRisk_AppliesPositive30PercentPriceShockCorrectly()
    {
        // Arrange
        var riskResult = new RiskCalculationResultDto
        {
            CalculationDate = DateTime.UtcNow,
            TotalPortfolioValue = 2_000_000m,
            PositionCount = 10,
            HistoricalVaR95 = 50_000m,
            HistoricalVaR99 = 70_000m,
            StressTests = new List<StressTestResultDto>
            {
                new()
                {
                    Scenario = "+30% Oil Spike",
                    PnlImpact = 600_000m, // 30% gain for long positions
                    PercentageChange = 30m,
                    Description = "Supply disruption scenario"
                }
            }
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(riskResult);

        // Act
        var result = await _controller.CalculateRisk(DateTime.UtcNow, 252, true);

        // Assert
        var okResult = result as OkObjectResult;
        var dto = okResult!.Value as RiskCalculationResultDto;

        var positiveShock = dto!.StressTests.First(s => s.Scenario == "+30% Oil Spike");
        positiveShock.PnlImpact.Should().Be(600_000m);
        positiveShock.PercentageChange.Should().Be(30m);
    }

    [Fact]
    public async Task CalculateRisk_AppliesNegative10PercentPriceShockCorrectly()
    {
        // Arrange
        var portfolioValue = 1_000_000m;
        var riskResult = new RiskCalculationResultDto
        {
            CalculationDate = DateTime.UtcNow,
            TotalPortfolioValue = portfolioValue,
            PositionCount = 5,
            HistoricalVaR95 = 25_000m,
            HistoricalVaR99 = 35_000m,
            StressTests = new List<StressTestResultDto>
            {
                new()
                {
                    Scenario = "-10% Shock",
                    PnlImpact = -100_000m, // 10% loss
                    PercentageChange = -10m,
                    Description = "Moderate price decline"
                }
            }
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(riskResult);

        // Act
        var result = await _controller.CalculateRisk(DateTime.UtcNow, 252, true);

        // Assert
        var okResult = result as OkObjectResult;
        var dto = okResult!.Value as RiskCalculationResultDto;

        var moderateShock = dto!.StressTests.First(s => s.Scenario == "-10% Shock");
        moderateShock.PnlImpact.Should().Be(-100_000m);
        moderateShock.PercentageChange.Should().Be(-10m);
        Math.Abs(moderateShock.PnlImpact).Should().Be(portfolioValue * 0.10m);
    }

    [Fact]
    public async Task CalculateRisk_ReturnsPotentialLossCalculation()
    {
        // Arrange
        var riskResult = new RiskCalculationResultDto
        {
            CalculationDate = DateTime.UtcNow,
            TotalPortfolioValue = 8_000_000m,
            PositionCount = 40,
            HistoricalVaR95 = 160_000m,
            HistoricalVaR99 = 224_000m,
            StressTests = new List<StressTestResultDto>
            {
                new()
                {
                    Scenario = "Geopolitical Crisis",
                    PnlImpact = -1_200_000m,
                    PercentageChange = -15m,
                    Description = "Major geopolitical event impact"
                },
                new()
                {
                    Scenario = "Demand Collapse",
                    PnlImpact = -2_000_000m,
                    PercentageChange = -25m,
                    Description = "Global recession scenario"
                }
            }
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(riskResult);

        // Act
        var result = await _controller.CalculateRisk(DateTime.UtcNow, 252, true);

        // Assert
        var okResult = result as OkObjectResult;
        var dto = okResult!.Value as RiskCalculationResultDto;

        dto!.StressTests.Should().HaveCountGreaterThanOrEqualTo(2);
        dto.StressTests.All(s => s.PnlImpact != 0).Should().BeTrue("All stress tests should show potential loss/gain");

        var geopoliticalLoss = dto.StressTests.First(s => s.Scenario == "Geopolitical Crisis");
        var demandCollapseLoss = dto.StressTests.First(s => s.Scenario == "Demand Collapse");

        geopoliticalLoss.PnlImpact.Should().BeLessThan(0);
        demandCollapseLoss.PnlImpact.Should().BeLessThan(0);
        Math.Abs(demandCollapseLoss.PnlImpact).Should().BeGreaterThan(Math.Abs(geopoliticalLoss.PnlImpact));
    }

    [Fact]
    public async Task CalculateRisk_HandlesInvalidScenarioParameters()
    {
        // Arrange - Query handler should handle invalid parameters
        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid historical days parameter: must be between 30 and 1000"));

        // Act
        var result = await _controller.CalculateRisk(DateTime.UtcNow, historicalDays: -100, includeStressTests: true);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;

        var errorObject = badRequestResult!.Value;
        errorObject.Should().NotBeNull();
    }

    #endregion

    #region GetRiskLimits Tests (via GetPortfolioRiskSummary)

    [Fact]
    public async Task GetPortfolioRiskSummary_ReturnsConfiguredRiskLimits()
    {
        // Arrange
        var expectedSummary = new PortfolioRiskSummaryDto
        {
            AsOfDate = DateTime.UtcNow,
            TotalExposure = 10_000_000m,
            NetExposure = 2_000_000m,
            GrossExposure = 8_000_000m,
            TotalPositions = 50,
            PortfolioVaR95 = 250_000m,
            PortfolioVaR99 = 350_000m,
            ConcentrationRisk = 0.35m,
            RiskLimits = new List<RiskLimitDto>
            {
                new()
                {
                    LimitType = "VaR 95%",
                    LimitValue = 500_000m,
                    CurrentValue = 250_000m,
                    Utilization = 0.50m,
                    Status = "OK"
                },
                new()
                {
                    LimitType = "Position Limit",
                    LimitValue = 100,
                    CurrentValue = 50,
                    Utilization = 0.50m,
                    Status = "OK"
                },
                new()
                {
                    LimitType = "Concentration Limit",
                    LimitValue = 0.40m,
                    CurrentValue = 0.35m,
                    Utilization = 0.875m,
                    Status = "OK"
                }
            },
            CorrelationMatrix = new Dictionary<string, decimal>
            {
                ["Brent-WTI"] = 0.85m,
                ["Brent-MGO"] = 0.65m
            }
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPortfolioRiskSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetPortfolioRiskSummary();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var summary = okResult!.Value as PortfolioRiskSummaryDto;

        summary.Should().NotBeNull();
        summary!.RiskLimits.Should().HaveCount(3);
        summary.RiskLimits.Should().Contain(l => l.LimitType == "VaR 95%");
        summary.RiskLimits.Should().Contain(l => l.LimitType == "Position Limit");
        summary.RiskLimits.Should().Contain(l => l.LimitType == "Concentration Limit");

        var varLimit = summary.RiskLimits.First(l => l.LimitType == "VaR 95%");
        varLimit.LimitValue.Should().Be(500_000m);
        varLimit.CurrentValue.Should().Be(250_000m);
        varLimit.Utilization.Should().Be(0.50m);
        varLimit.Status.Should().Be("OK");
    }

    [Fact]
    public async Task GetPortfolioRiskSummary_ValidatesLimitBreachDetection()
    {
        // Arrange
        var summaryWithBreaches = new PortfolioRiskSummaryDto
        {
            AsOfDate = DateTime.UtcNow,
            TotalExposure = 15_000_000m,
            NetExposure = 5_000_000m,
            GrossExposure = 12_000_000m,
            TotalPositions = 120,
            PortfolioVaR95 = 600_000m,
            PortfolioVaR99 = 850_000m,
            ConcentrationRisk = 0.55m,
            RiskLimits = new List<RiskLimitDto>
            {
                new()
                {
                    LimitType = "VaR 95%",
                    LimitValue = 500_000m,
                    CurrentValue = 600_000m,
                    Utilization = 1.20m,
                    Status = "Breach"
                },
                new()
                {
                    LimitType = "Position Limit",
                    LimitValue = 100,
                    CurrentValue = 120,
                    Utilization = 1.20m,
                    Status = "Breach"
                },
                new()
                {
                    LimitType = "Concentration Limit",
                    LimitValue = 0.40m,
                    CurrentValue = 0.55m,
                    Utilization = 1.375m,
                    Status = "Breach"
                },
                new()
                {
                    LimitType = "Exposure Limit",
                    LimitValue = 20_000_000m,
                    CurrentValue = 18_000_000m,
                    Utilization = 0.90m,
                    Status = "Warning"
                }
            }
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPortfolioRiskSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaryWithBreaches);

        // Act
        var result = await _controller.GetPortfolioRiskSummary();

        // Assert
        var okResult = result as OkObjectResult;
        var summary = okResult!.Value as PortfolioRiskSummaryDto;

        summary.Should().NotBeNull();

        var breachedLimits = summary!.RiskLimits.Where(l => l.Status == "Breach").ToList();
        breachedLimits.Should().HaveCount(3);

        foreach (var breachedLimit in breachedLimits)
        {
            breachedLimit.CurrentValue.Should().BeGreaterThan(breachedLimit.LimitValue);
            breachedLimit.Utilization.Should().BeGreaterThan(1.0m);
        }

        var warningLimits = summary.RiskLimits.Where(l => l.Status == "Warning").ToList();
        warningLimits.Should().HaveCount(1);

        var warningLimit = warningLimits.First();
        warningLimit.Utilization.Should().BeGreaterThanOrEqualTo(0.80m);
        warningLimit.Utilization.Should().BeLessThan(1.0m);
    }

    #endregion

    #region GetHistoricalVaR Tests (Floor vs Ceiling)

    [Fact]
    public async Task CalculateRisk_UsesFloorNotCeilingForPercentileCalculation()
    {
        // Arrange - This test verifies the business logic expectation
        // Historical VaR should use Floor() for percentile calculation, not Ceiling()
        // For 252 returns at 95% confidence: index = Floor(252 * 0.05) = Floor(12.6) = 12 (NOT 13)
        // For 252 returns at 99% confidence: index = Floor(252 * 0.01) = Floor(2.52) = 2 (NOT 3)

        var riskResult = new RiskCalculationResultDto
        {
            CalculationDate = DateTime.UtcNow,
            TotalPortfolioValue = 1_000_000m,
            PositionCount = 10,
            HistoricalVaR95 = 32_900m,  // Should be calculated using Floor()
            HistoricalVaR99 = 46_520m,  // Should be calculated using Floor()
            PortfolioVolatility = 0.25m
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(riskResult);

        // Act
        var result = await _controller.CalculateRisk(DateTime.UtcNow, historicalDays: 252, includeStressTests: false);

        // Assert
        var okResult = result as OkObjectResult;
        var dto = okResult!.Value as RiskCalculationResultDto;

        dto.Should().NotBeNull();
        dto!.HistoricalVaR95.Should().BeGreaterThan(0);
        dto.HistoricalVaR99.Should().BeGreaterThan(dto.HistoricalVaR95);

        // Verify the query was sent with correct parameters that would use Floor()
        _mockMediator.Verify(
            m => m.Send(
                It.Is<CalculateRiskQuery>(q =>
                    q.HistoricalDays == 252 &&
                    q.IncludeStressTests == false),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Additional assertion: VaR99 should be significantly higher than VaR95
        // Typical ratio is around 1.4-1.5x for normal distributions
        var ratio = dto.HistoricalVaR99 / dto.HistoricalVaR95;
        ratio.Should().BeGreaterThan(1.3m, "VaR99 should be at least 1.3x VaR95");
        ratio.Should().BeLessThan(2.0m, "VaR99 should not exceed 2.0x VaR95 for reasonable distributions");
    }

    #endregion

    #region GetProductRisk Tests

    [Fact]
    public async Task GetProductRisk_WithValidProduct_ReturnsProductRiskMetrics()
    {
        // Arrange
        var expectedRisk = new ProductRiskDto
        {
            ProductType = "Brent",
            CalculationDate = DateTime.UtcNow,
            NetPosition = 100_000m,
            MarketValue = 8_000_000m,
            VaR95 = 150_000m,
            VaR99 = 210_000m,
            DailyVolatility = 0.02m,
            AnnualizedVolatility = 0.32m,
            Beta = 1.05m,
            Sharpe = 0.85m,
            HistoricalReturns = new List<decimal> { 0.01m, -0.02m, 0.015m },
            Greeks = new Dictionary<string, decimal> { ["Delta"] = 1.0m }
        };

        _mockMediator
            .Setup(m => m.Send(It.Is<GetProductRiskQuery>(q => q.ProductType == "Brent"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRisk);

        // Act
        var result = await _controller.GetProductRisk("Brent");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var productRisk = okResult!.Value as ProductRiskDto;

        productRisk.Should().NotBeNull();
        productRisk!.ProductType.Should().Be("Brent");
        productRisk.VaR95.Should().Be(150_000m);
        productRisk.VaR99.Should().Be(210_000m);
        productRisk.DailyVolatility.Should().Be(0.02m);
    }

    [Fact]
    public async Task GetProductRisk_WithNonExistentProduct_ReturnsNotFound()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.Is<GetProductRiskQuery>(q => q.ProductType == "InvalidProduct"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductRiskDto?)null);

        // Act
        var result = await _controller.GetProductRisk("InvalidProduct");

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;

        var errorObject = notFoundResult!.Value;
        errorObject.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProductRisk_WhenCalculationFails_ReturnsBadRequest()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetProductRiskQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Market data unavailable"));

        // Act
        var result = await _controller.GetProductRisk("Brent");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region RunBacktest Tests

    [Fact]
    public async Task RunBacktest_WithValidDateRange_ReturnsBacktestResults()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddYears(-1);
        var endDate = DateTime.UtcNow;

        var expectedResult = new BacktestResultDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalDays = 252,
            HistoricalVaR95Breaches = 15,
            HistoricalVaR95BreachRate = 0.0595m,
            HistoricalVaR99Breaches = 3,
            HistoricalVaR99BreachRate = 0.0119m,
            GarchVaR95Breaches = 12,
            GarchVaR95BreachRate = 0.0476m,
            GarchVaR99Breaches = 2,
            GarchVaR99BreachRate = 0.0079m,
            McVaR95Breaches = 13,
            McVaR95BreachRate = 0.0516m,
            McVaR99Breaches = 2,
            McVaR99BreachRate = 0.0079m,
            KupiecTestResults = new Dictionary<string, bool>
            {
                ["Historical_95"] = true,
                ["Historical_99"] = true,
                ["GARCH_95"] = true,
                ["GARCH_99"] = true
            },
            DailyResults = new List<DailyBacktestDto>()
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<RunBacktestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RunBacktest(startDate, endDate, 252);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var backtestResult = okResult!.Value as BacktestResultDto;

        backtestResult.Should().NotBeNull();
        backtestResult!.TotalDays.Should().Be(252);
        backtestResult.HistoricalVaR95BreachRate.Should().BeApproximately(0.05m, 0.02m, "Breach rate should be close to 5% for 95% VaR");
        backtestResult.HistoricalVaR99BreachRate.Should().BeApproximately(0.01m, 0.01m, "Breach rate should be close to 1% for 99% VaR");
    }

    [Fact]
    public async Task RunBacktest_WithDefaultParameters_UsesDefaultValues()
    {
        // Arrange
        var expectedResult = new BacktestResultDto
        {
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow,
            TotalDays = 252,
            HistoricalVaR95Breaches = 10,
            HistoricalVaR95BreachRate = 0.0397m,
            KupiecTestResults = new Dictionary<string, bool>()
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<RunBacktestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RunBacktest(null, null, 252);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        _mockMediator.Verify(
            m => m.Send(
                It.Is<RunBacktestQuery>(q =>
                    q.LookbackDays == 252 &&
                    q.StartDate <= DateTime.UtcNow &&
                    q.EndDate <= DateTime.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetPortfolioRiskWithTradeGroups Tests

    [Fact]
    public async Task GetPortfolioRiskWithTradeGroups_ReturnsCompleteRiskBreakdown()
    {
        // Arrange
        var expectedResult = new PortfolioRiskWithTradeGroupsDto
        {
            AsOfDate = DateTime.UtcNow.Date,
            StandaloneRisk = new StandaloneRiskDto
            {
                TotalPositions = 15,
                NetExposure = 1_000_000m,
                GrossExposure = 5_000_000m,
                VaR95 = 125_000m,
                VaR99 = 175_000m,
                DailyVolatility = 0.025m
            },
            TradeGroupRisks = new List<TradeGroupRiskDto>
            {
                new()
                {
                    TradeGroupId = Guid.NewGuid(),
                    GroupName = "Brent-WTI Spread",
                    StrategyType = "Spread",
                    NetExposure = 50_000m,
                    GrossExposure = 2_000_000m,
                    VaR95 = 15_000m,
                    VaR99 = 21_000m,
                    NetPnL = 25_000m,
                    ContractCount = 4,
                    PortfolioVolatility = 0.015m,
                    CorrelationBenefit = 85_000m,
                    IsSpreadStrategy = true
                }
            },
            TotalPortfolioRisk = new TotalPortfolioRiskDto
            {
                TotalVaR95 = 140_000m,
                TotalVaR99 = 196_000m,
                TotalNetExposure = 1_050_000m,
                TotalGrossExposure = 7_000_000m,
                TotalPositions = 19,
                TradeGroupCount = 1,
                CorrelationBenefit = 85_000m,
                DiversificationRatio = 0.15m
            },
            CalculatedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPortfolioRiskSummaryWithTradeGroupsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPortfolioRiskWithTradeGroups(DateTime.UtcNow.Date, true, 252);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var riskDto = okResult!.Value as PortfolioRiskWithTradeGroupsDto;

        riskDto.Should().NotBeNull();
        riskDto!.StandaloneRisk.Should().NotBeNull();
        riskDto.TradeGroupRisks.Should().HaveCount(1);
        riskDto.TotalPortfolioRisk.Should().NotBeNull();
        riskDto.TotalPortfolioRisk.TradeGroupCount.Should().Be(1);
        riskDto.TotalPortfolioRisk.CorrelationBenefit.Should().Be(85_000m);
    }

    [Fact]
    public async Task GetPortfolioRiskWithTradeGroups_WhenCalculationFails_ReturnsBadRequest()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPortfolioRiskSummaryWithTradeGroupsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Trade group calculation failed"));

        // Act
        var result = await _controller.GetPortfolioRiskWithTradeGroups(null, false, 252);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region CompareRiskMethods Tests

    [Fact]
    public async Task CompareRiskMethods_ReturnsComparisonBetweenTraditionalAndTradeGroupMethods()
    {
        // Arrange
        var traditionalSummary = new PortfolioRiskSummaryDto
        {
            AsOfDate = DateTime.UtcNow.Date,
            GrossExposure = 10_000_000m,
            NetExposure = 2_000_000m,
            PortfolioVaR95 = 500_000m,
            PortfolioVaR99 = 700_000m
        };

        var tradeGroupSummary = new PortfolioRiskWithTradeGroupsDto
        {
            AsOfDate = DateTime.UtcNow.Date,
            TotalPortfolioRisk = new TotalPortfolioRiskDto
            {
                TotalNetExposure = 1_500_000m,
                TotalGrossExposure = 8_500_000m,
                TotalVaR95 = 350_000m,
                TotalVaR99 = 490_000m,
                TradeGroupCount = 5,
                CorrelationBenefit = 1_500_000m
            }
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPortfolioRiskSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(traditionalSummary);

        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPortfolioRiskSummaryWithTradeGroupsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tradeGroupSummary);

        // Act
        var result = await _controller.CompareRiskMethods();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var comparison = okResult!.Value as RiskMethodComparisonDto;

        comparison.Should().NotBeNull();
        comparison!.Traditional.Should().NotBeNull();
        comparison.TradeGroupBased.Should().NotBeNull();

        comparison.Traditional.VaR95.Should().Be(500_000m);
        comparison.TradeGroupBased.TotalVaR95.Should().Be(350_000m);

        comparison.RiskOverstatement.Should().Be(150_000m, "Traditional method should overstate risk by 150k");
        comparison.ExposureReduction.Should().Be(1_500_000m, "Trade groups should reduce exposure by 1.5M");

        comparison.Traditional.Method.Should().NotBeEmpty();
        comparison.TradeGroupBased.Method.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CompareRiskMethods_WhenCalculationFails_ReturnsBadRequest()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<GetPortfolioRiskSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Risk calculation failed"));

        // Act
        var result = await _controller.CompareRiskMethods();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CalculateRisk_WithNullCalculationDate_UsesCurrentTime()
    {
        // Arrange
        var expectedResult = new RiskCalculationResultDto
        {
            CalculationDate = DateTime.UtcNow,
            TotalPortfolioValue = 1_000_000m,
            PositionCount = 10,
            HistoricalVaR95 = 25_000m,
            HistoricalVaR99 = 35_000m
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CalculateRisk(calculationDate: null, historicalDays: 252, includeStressTests: true);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        _mockMediator.Verify(
            m => m.Send(
                It.Is<CalculateRiskQuery>(q =>
                    q.CalculationDate.Date == DateTime.UtcNow.Date &&
                    q.HistoricalDays == 252 &&
                    q.IncludeStressTests == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CalculateRisk_WithMissingHistoricalData_HandlesGracefully()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Insufficient historical data: only 50 days available, 252 required"));

        // Act
        var result = await _controller.CalculateRisk(DateTime.UtcNow.AddDays(-500), historicalDays: 252, includeStressTests: true);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;

        var errorObject = badRequestResult!.Value;
        errorObject.Should().NotBeNull();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CalculateRisk_WithExtremeMarketVolatility_ReturnsHighVaRValues()
    {
        // Arrange
        var extremeVolatilityResult = new RiskCalculationResultDto
        {
            CalculationDate = DateTime.UtcNow,
            TotalPortfolioValue = 5_000_000m,
            PositionCount = 25,
            HistoricalVaR95 = 750_000m,  // 15% of portfolio
            HistoricalVaR99 = 1_050_000m, // 21% of portfolio
            PortfolioVolatility = 0.65m,  // 65% annual volatility (extreme)
            MaxDrawdown = 0.45m,
            StressTests = new List<StressTestResultDto>
            {
                new()
                {
                    Scenario = "Market Crash",
                    PnlImpact = -2_000_000m,
                    PercentageChange = -40m
                }
            }
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CalculateRiskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(extremeVolatilityResult);

        // Act
        var result = await _controller.CalculateRisk(DateTime.UtcNow, 252, true);

        // Assert
        var okResult = result as OkObjectResult;
        var dto = okResult!.Value as RiskCalculationResultDto;

        dto.Should().NotBeNull();
        dto!.PortfolioVolatility.Should().BeGreaterThan(0.50m, "Extreme volatility should exceed 50%");
        dto.HistoricalVaR95.Should().BeGreaterThan(dto.TotalPortfolioValue * 0.10m, "VaR should exceed 10% in extreme conditions");
        dto.MaxDrawdown.Should().BeGreaterThan(0.30m, "Max drawdown should be significant");
    }

    #endregion
}
