using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OilTrading.Application.Services;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using Xunit;

namespace OilTrading.Tests.Application.Services;

public class RiskCalculationServiceTests
{
    private readonly Mock<IPaperContractRepository> _mockPaperContractRepository;
    private readonly Mock<IMarketDataRepository> _mockMarketDataRepository;
    private readonly Mock<ILogger<RiskCalculationService>> _mockLogger;
    private readonly RiskCalculationService _riskCalculationService;

    public RiskCalculationServiceTests()
    {
        _mockPaperContractRepository = new Mock<IPaperContractRepository>();
        _mockMarketDataRepository = new Mock<IMarketDataRepository>();
        _mockLogger = new Mock<ILogger<RiskCalculationService>>();

        _riskCalculationService = new RiskCalculationService(
            _mockMarketDataRepository.Object,
            _mockPaperContractRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CalculatePortfolioRisk_ShouldReturnRiskMetrics_WhenPositionsExist()
    {
        // Arrange
        var positions = new List<PaperContract>
        {
            CreateTestPosition("BRENT", 1000m, 75.50m, true),
            CreateTestPosition("WTI", -500m, 72.00m, true),
            CreateTestPosition("DUBAI", 750m, 70.25m, true)
        };

        var marketData = new List<MarketPrice>
        {
            CreateMarketPrice("BRENT", 76.00m),
            CreateMarketPrice("WTI", 71.50m),
            CreateMarketPrice("DUBAI", 70.75m)
        };

        _mockPaperContractRepository
            .Setup(x => x.GetOpenPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        _mockMarketDataRepository
            .Setup(x => x.GetLatestPricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(marketData);

        // Jane Street best practice: Mock ALL data dependencies
        // Setup historical prices for each product-month combination (format: ProductType|ContractMonth)
        var brentPrices = GenerateHistoricalPrices("BRENT|JAN24", 75m, 252);
        var wtiPrices = GenerateHistoricalPrices("WTI|JAN24", 72m, 252);
        var dubaiPrices = GenerateHistoricalPrices("DUBAI|JAN24", 70m, 252);

        _mockMarketDataRepository
            .Setup(x => x.GetHistoricalPricesAsync("BRENT|JAN24", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(brentPrices);

        _mockMarketDataRepository
            .Setup(x => x.GetHistoricalPricesAsync("WTI|JAN24", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(wtiPrices);

        _mockMarketDataRepository
            .Setup(x => x.GetHistoricalPricesAsync("DUBAI|JAN24", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dubaiPrices);

        // Setup current prices for stress testing (using product-month keys)
        _mockMarketDataRepository
            .Setup(x => x.GetLatestPriceAsync("BRENT|JAN24", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(marketData[0]);

        _mockMarketDataRepository
            .Setup(x => x.GetLatestPriceAsync("WTI|JAN24", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(marketData[1]);

        _mockMarketDataRepository
            .Setup(x => x.GetLatestPriceAsync("DUBAI|JAN24", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(marketData[2]);

        // Act
        var result = await _riskCalculationService.CalculatePortfolioRiskAsync(DateTime.UtcNow);

        // Assert - Jane Street validation criteria
        result.Should().NotBeNull();
        result.TotalPortfolioValue.Should().BeGreaterThan(0, "Portfolio must have value");
        result.PositionCount.Should().Be(3, "Should track all positions");
        result.HistoricalVaR95.Should().BeGreaterThan(0, "VaR95 must be positive");
        result.HistoricalVaR99.Should().BeGreaterThan(result.HistoricalVaR95, "VaR99 > VaR95");
        result.PortfolioVolatility.Should().BeGreaterThan(0, "Volatility must be positive");
        result.StressTests.Should().NotBeEmpty("Stress tests should be included");
    }

    [Fact]
    public async Task CalculatePortfolioRisk_ShouldReturnEmptyMetrics_WhenNoPositionsExist()
    {
        // Arrange
        _mockPaperContractRepository
            .Setup(x => x.GetOpenPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaperContract>());

        // Act
        var result = await _riskCalculationService.CalculatePortfolioRiskAsync(DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result.TotalPortfolioValue.Should().Be(0);
        result.PositionCount.Should().Be(0);
    }

    [Fact]
    public async Task CalculateHistoricalVaR_ShouldReturnCorrectValues()
    {
        // Arrange
        var positions = new List<PaperContract>
        {
            CreateTestPosition("BRENT", 1000m, 75.50m, true) // Portfolio value = 75,500
        };

        // Jane Street standard: Use sufficient data for statistical validity
        // Need at least 100 observations for proper 95%/99% percentile separation
        // Generate realistic oil return distribution: mean ≈ 0, std ≈ 2% daily
        var random = new Random(42);
        var returns = new List<decimal>();
        for (int i = 0; i < 252; i++) // 1 year of daily returns
        {
            // Box-Muller transform for normal distribution
            var u1 = (decimal)random.NextDouble();
            var u2 = (decimal)random.NextDouble();
            var randStdNormal = (decimal)Math.Sqrt(-2.0 * Math.Log((double)u1)) *
                               (decimal)Math.Sin(2.0 * Math.PI * (double)u2);
            var dailyReturn = 0.0m + 0.02m * randStdNormal; // mean=0, std=2%
            returns.Add(dailyReturn);
        }

        // Act
        var (var95, var99) = await _riskCalculationService.CalculateHistoricalVaRAsync(positions, returns);

        // Assert - Jane Street validation
        var95.Should().BeGreaterThan(0, "VaR must be positive");
        var99.Should().BeGreaterThan(var95, "99% VaR must exceed 95% VaR");

        // VaR should be reasonable: ~2-5% of portfolio value for 1-day, 95% confidence
        var portfolioValue = 75500m;
        var95.Should().BeGreaterThan(portfolioValue * 0.01m, "VaR should be at least 1% of portfolio");
        var95.Should().BeLessThan(portfolioValue * 0.10m, "VaR should not exceed 10% (sanity check)");

        // 99% VaR should be roughly 1.4x the 95% VaR
        var ratio = var99 / var95;
        ratio.Should().BeGreaterThan(1.2m, "99% VaR should be significantly higher than 95% VaR");
        ratio.Should().BeLessThan(2.0m, "Ratio should be reasonable for normal-ish distribution");
    }

    [Fact]
    public async Task CalculateExpectedShortfall_ShouldReturnCorrectValues()
    {
        // Arrange - Generate sufficient data for proper tail statistics
        var positions = new List<PaperContract>
        {
            CreateTestPosition("BRENT", 1000m, 75.50m, true) // Portfolio = 75,500
        };

        // Jane Street standard: CVaR requires good tail sampling
        // Generate 252 returns with fat tails (Student's t distribution approximation)
        var random = new Random(42);
        var returns = new List<decimal>();
        for (int i = 0; i < 252; i++)
        {
            var u1 = (decimal)random.NextDouble();
            var u2 = (decimal)random.NextDouble();
            var randStdNormal = (decimal)Math.Sqrt(-2.0 * Math.Log((double)u1)) *
                               (decimal)Math.Sin(2.0 * Math.PI * (double)u2);

            // Add some fat tail observations (10% of data has 3x volatility)
            var volatilityMultiplier = random.NextDouble() < 0.1 ? 3.0m : 1.0m;
            var dailyReturn = 0.0m + 0.02m * randStdNormal * volatilityMultiplier;
            returns.Add(dailyReturn);
        }

        // Act
        var (es95, es99) = await _riskCalculationService.CalculateExpectedShortfallAsync(returns, positions);

        // Assert - Jane Street CVaR validation
        es95.Should().BeGreaterThan(0, "ES95 must be positive");
        es99.Should().BeGreaterThan(es95, "ES99 must exceed ES95 (worse tail losses)");

        // ES should be greater than VaR (coherent risk measure property)
        // For fat-tailed distributions, ES can be 1.2-1.5x the VaR
        var portfolioValue = 75500m;
        es95.Should().BeGreaterThan(portfolioValue * 0.015m, "ES should account for tail risk");
        es99.Should().BeGreaterThan(portfolioValue * 0.025m, "ES99 should capture extreme tail");

        // ES99 / ES95 ratio should be meaningful (not 1.0)
        var ratio = es99 / es95;
        ratio.Should().BeGreaterThan(1.1m, "ES99 should be significantly higher than ES95");
    }

    [Fact]
    public async Task CalculateDeltaNormalVaR_ShouldReturnCorrectValues()
    {
        // Arrange - Create a realistic multi-product portfolio
        var positions = new List<PaperContract>
        {
            CreateTestPosition("BRENT", 1000m, 75.50m, true),   // Long 1000 lots @ $75.50 = $75,500
            CreateTestPosition("WTI", 500m, 72.00m, false),     // Short 500 lots @ $72.00 = -$36,000
            CreateTestPosition("DUBAI", 750m, 70.25m, true)     // Long 750 lots @ $70.25 = $52,687.50
        };

        // Net exposure: $75,500 - $36,000 + $52,687.50 = $92,187.50

        // Historical returns with realistic oil market correlation (~0.85-0.95)
        // Keys must match format "ProductType|ContractMonth" as used in RiskCalculationService
        var productReturns = new Dictionary<string, List<decimal>>
        {
            { "BRENT|JAN24", new List<decimal> { -0.02m, -0.01m, 0m, 0.01m, 0.02m, 0.015m, -0.015m, 0.01m, -0.01m, 0.005m } },
            { "WTI|JAN24", new List<decimal> { -0.018m, -0.012m, 0.002m, 0.012m, 0.018m, 0.013m, -0.014m, 0.008m, -0.009m, 0.004m } },
            { "DUBAI|JAN24", new List<decimal> { -0.019m, -0.011m, 0.001m, 0.011m, 0.019m, 0.014m, -0.013m, 0.009m, -0.011m, 0.006m } }
        };

        // Act
        var (var95, var99) = await _riskCalculationService.CalculateDeltaNormalVaRAsync(positions, productReturns);

        // Assert - Jane Street validation criteria
        var95.Should().BeGreaterThan(0, "VaR must be positive");
        var99.Should().BeGreaterThan(var95, "99% VaR must exceed 95% VaR");

        // VaR should be reasonable relative to net exposure
        // Typical 1-day VaR for oil: 2-5% of exposure
        var netExposure = 75500m - 36000m + 52687.50m; // $92,187.50
        var95.Should().BeGreaterThan(netExposure * 0.005m, "VaR should be at least 0.5% of net exposure");
        var95.Should().BeLessThan(netExposure * 0.10m, "VaR should not exceed 10% of net exposure (sanity check)");

        // 99% VaR should be roughly 1.4x the 95% VaR (2.326/1.645 = 1.414)
        var ratio = var99 / var95;
        ratio.Should().BeGreaterThan(1.3m, "99% VaR ratio should be close to theoretical 1.414");
        ratio.Should().BeLessThan(1.5m, "99% VaR ratio should be close to theoretical 1.414");
    }

    [Fact]
    public async Task CalculateHistoricalVaR_ShouldUseCorrectQuantileCalculation()
    {
        // Arrange - 100 returns to test percentile calculation
        var returns = Enumerable.Range(1, 100).Select(i => (decimal)i / 100m - 0.5m).ToList(); // -0.49 to 0.50
        var positions = new List<PaperContract>
        {
            CreateTestPosition("BRENT", 1000m, 100m, true) // Portfolio value = 100,000
        };

        // Act
        var (var95, var99) = await _riskCalculationService.CalculateHistoricalVaRAsync(positions, returns);

        // Assert
        // For 100 returns, 5th percentile index = Floor(100 * 0.05) = 5 (6th element when 0-indexed)
        // 1st percentile index = Floor(100 * 0.01) = 1 (2nd element when 0-indexed)
        // With our test data: returns[5] ≈ -0.44, returns[1] ≈ -0.48
        var95.Should().BeGreaterThan(40000m); // Approximately 44,000
        var95.Should().BeLessThan(45000m);

        var99.Should().BeGreaterThan(47000m); // Approximately 48,000
        var99.Should().BeLessThan(50000m);

        var99.Should().BeGreaterThan(var95); // 99% VaR should be higher
    }

    [Fact]
    public async Task RunStressTests_ShouldReturnMultipleScenarios()
    {
        // Arrange
        var positions = new List<PaperContract>
        {
            CreateTestPosition("BRENT", 1000m, 75.50m, true),
            CreateTestPosition("WTI", 500m, 72.00m, true)
        };

        var currentPrices = new Dictionary<string, decimal>
        {
            { "BRENT", 75.50m },
            { "WTI", 72.00m }
        };

        // Act
        var results = await _riskCalculationService.RunStressTestsAsync(positions, currentPrices);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCountGreaterThan(0);
        results.Should().Contain(r => r.Scenario.Contains("10%"));
    }

    [Fact]
    public async Task GetHistoricalReturns_ShouldReturnCorrectData()
    {
        // Arrange
        var productTypes = new List<string> { "BRENT", "WTI" };
        var endDate = DateTime.UtcNow;
        var days = 30;

        var historicalPrices = GenerateHistoricalPrices("BRENT", 75m, days);

        _mockMarketDataRepository
            .Setup(x => x.GetHistoricalPricesAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalPrices);

        // Act
        var returns = await _riskCalculationService.GetHistoricalReturnsAsync(productTypes, endDate, days);

        // Assert
        returns.Should().NotBeNull();
        returns.Should().ContainKey("BRENT");
        returns["BRENT"].Should().HaveCount(days - 1); // Returns are calculated from price differences
    }

    [Fact]
    public async Task CalculateDeltaNormalVaR_ShouldHandleHedgedPortfolio()
    {
        // Arrange - Create a perfectly hedged portfolio (long = short)
        // This tests that correlation properly reduces VaR
        var positions = new List<PaperContract>
        {
            CreateTestPosition("BRENT", 1000m, 100m, true),    // Long $100,000
            CreateTestPosition("BRENT", 1000m, 100m, false),   // Short $100,000 (net = 0)
        };

        // Identical returns (perfect correlation)
        // Keys must match format "ProductType|ContractMonth" as used in RiskCalculationService
        var returns = new List<decimal> { -0.02m, 0.01m, 0.015m, -0.01m, 0.005m };
        var productReturns = new Dictionary<string, List<decimal>>
        {
            { "BRENT|JAN24", returns }
        };

        // Act
        var (var95, var99) = await _riskCalculationService.CalculateDeltaNormalVaRAsync(positions, productReturns);

        // Assert - Jane Street hedge validation
        // For a perfectly hedged position, VaR should be approximately zero
        // (In practice, small rounding errors may occur)
        var95.Should().BeLessThan(100m, "Perfectly hedged portfolio should have near-zero VaR");
        var99.Should().BeLessThan(200m, "Perfectly hedged portfolio should have near-zero VaR");
    }

    [Fact]
    public async Task CalculateDeltaNormalVaR_ShouldReflectCorrelationBenefit()
    {
        // Arrange - Test that correlation reduces VaR vs. sum of individual VaRs
        var positions = new List<PaperContract>
        {
            CreateTestPosition("BRENT", 1000m, 100m, true),    // Long $100,000
            CreateTestPosition("WTI", 1000m, 100m, true),      // Long $100,000
        };

        // Positively correlated returns (ρ ≈ 0.8)
        // Keys must match format "ProductType|ContractMonth" as used in RiskCalculationService
        var productReturns = new Dictionary<string, List<decimal>>
        {
            { "BRENT|JAN24", new List<decimal> { -0.02m, 0.01m, 0.015m, -0.01m, 0.005m, 0.02m, -0.015m } },
            { "WTI|JAN24", new List<decimal> { -0.018m, 0.012m, 0.013m, -0.009m, 0.004m, 0.018m, -0.014m } }
        };

        // Act
        var (portfolioVar95, portfolioVar99) = await _riskCalculationService.CalculateDeltaNormalVaRAsync(positions, productReturns);

        // Also calculate individual VaRs
        var brentOnly = new List<PaperContract> { positions[0] };
        var wtiOnly = new List<PaperContract> { positions[1] };

        var brentReturns = new Dictionary<string, List<decimal>> { { "BRENT|JAN24", productReturns["BRENT|JAN24"] } };
        var wtiReturns = new Dictionary<string, List<decimal>> { { "WTI|JAN24", productReturns["WTI|JAN24"] } };

        var (brentVar95, _) = await _riskCalculationService.CalculateDeltaNormalVaRAsync(brentOnly, brentReturns);
        var (wtiVar95, _) = await _riskCalculationService.CalculateDeltaNormalVaRAsync(wtiOnly, wtiReturns);

        var sumOfIndividualVaRs = brentVar95 + wtiVar95;

        // Assert - Jane Street diversification principle
        // Portfolio VaR should be LESS than sum of individual VaRs (unless correlation = 1)
        // Due to diversification benefit: VaR(A+B) < VaR(A) + VaR(B) when ρ < 1
        portfolioVar95.Should().BeLessThan(sumOfIndividualVaRs,
            "Portfolio VaR must be less than sum of components due to correlation < 1 (diversification benefit)");

        // For ρ ≈ 0.8, portfolio VaR should be roughly √(1² + 1² + 2×1×1×0.8) = √3.6 ≈ 1.9 times individual VaR
        // So portfolio VaR / individual VaR ≈ 1.9 / 2 = 0.95
        var diversificationRatio = portfolioVar95 / sumOfIndividualVaRs;
        diversificationRatio.Should().BeGreaterThan(0.85m, "Diversification benefit should reduce total VaR");
        diversificationRatio.Should().BeLessThan(1.0m, "Portfolio VaR must be subadditive");
    }

    // CalculatePortfolioVolatilityAsync method doesn't exist in RiskCalculationService
    // Removed test as it's not part of the service interface

    private static PaperContract CreateTestPosition(string productType, decimal quantity, decimal entryPrice, bool isLong)
    {
        var contract = new PaperContract
        {
            ContractNumber = $"TEST-{Guid.NewGuid().ToString()[..8]}",
            ProductType = productType,
            Quantity = Math.Abs(quantity),
            LotSize = 1, // Simplified for testing
            EntryPrice = entryPrice,
            Position = isLong ? PositionType.Long : PositionType.Short,
            TradeDate = DateTime.UtcNow.AddDays(-1),
            Status = PaperContractStatus.Open,
            ContractMonth = "JAN24",
            CurrentPrice = entryPrice * 1.01m // Slight price movement
        };
        return contract;
    }

    private static MarketPrice CreateMarketPrice(string productType, decimal price)
    {
        return MarketPrice.Create(
            DateTime.UtcNow,
            productType,
            productType,
            MarketPriceType.Spot,
            price,
            "USD",
            "TEST",
            "TEST",
            false,
            DateTime.UtcNow,
            "test");
    }

    private static List<MarketPrice> GenerateHistoricalPrices(string productType, decimal basePrice, int count)
    {
        var prices = new List<MarketPrice>();
        var random = new Random(42); // Fixed seed for reproducible tests
        
        for (int i = count; i > 0; i--)
        {
            var variation = (decimal)(random.NextDouble() - 0.5) * 0.1m; // ±5% variation
            var price = basePrice * (1 + variation);

            prices.Add(MarketPrice.Create(
                DateTime.UtcNow.AddDays(-i),
                productType,
                productType,
                MarketPriceType.Spot,
                price,
                "USD",
                "TEST",
                "TEST",
                false,
                DateTime.UtcNow,
                "test"));
        }
        
        return prices;
    }
}