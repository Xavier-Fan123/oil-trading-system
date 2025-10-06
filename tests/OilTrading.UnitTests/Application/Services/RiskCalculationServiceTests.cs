using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;
using OilTrading.Core.Entities;
using OilTrading.Core.Entities.TimeSeries;
using OilTrading.Core.Repositories;
using Xunit;

namespace OilTrading.UnitTests.Application.Services;

public class RiskCalculationServiceTests
{
    private readonly Mock<IMarketDataRepository> _mockMarketDataRepository;
    private readonly Mock<IPaperContractRepository> _mockPaperContractRepository;
    private readonly Mock<ILogger<RiskCalculationService>> _mockLogger;
    private readonly RiskCalculationService _service;

    public RiskCalculationServiceTests()
    {
        _mockMarketDataRepository = new Mock<IMarketDataRepository>();
        _mockPaperContractRepository = new Mock<IPaperContractRepository>();
        _mockLogger = new Mock<ILogger<RiskCalculationService>>();
        _service = new RiskCalculationService(
            _mockMarketDataRepository.Object,
            _mockPaperContractRepository.Object,
            _mockLogger.Object);
    }

    #region CalculateHistoricalVaRAsync Tests

    [Fact]
    public async Task CalculateHistoricalVaRAsync_WithValidReturns_CalculatesCorrectQuantiles()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);
        var historicalReturns = CreateNormalDistributionReturns(mean: 0m, stdDev: 0.02m, count: 252);

        // Act
        var (var95, var99) = await _service.CalculateHistoricalVaRAsync(positions, historicalReturns);

        // Assert
        var95.Should().BeGreaterThan(0, "VaR95 should be positive for losses");
        var99.Should().BeGreaterThan(var95, "VaR99 should be higher than VaR95 for more extreme losses");

        // VaR should be reasonable for 2% daily volatility: roughly $1M * 0.02 * 1.645 = $32,900 for 95%
        var95.Should().BeInRange(20000m, 60000m, "VaR95 should be within expected range");
        var99.Should().BeInRange(30000m, 80000m, "VaR99 should be within expected range");
    }

    [Fact]
    public async Task CalculateHistoricalVaRAsync_WithEmptyReturns_ReturnsZero()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);
        var emptyReturns = new List<decimal>();

        // Act
        var (var95, var99) = await _service.CalculateHistoricalVaRAsync(positions, emptyReturns);

        // Assert
        var95.Should().Be(0);
        var99.Should().Be(0);
    }

    [Fact]
    public async Task CalculateHistoricalVaRAsync_WithSingleReturn_ReturnsCalculatedValue()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);
        var singleReturn = new List<decimal> { -0.05m }; // -5% return

        // Act
        var (var95, var99) = await _service.CalculateHistoricalVaRAsync(positions, singleReturn);

        // Assert
        var95.Should().BeGreaterThan(0);
        var99.Should().BeGreaterThan(0);
        // With single return, both VaR measures should use that same return
        var95.Should().Be(var99);
    }

    [Fact]
    public async Task CalculateHistoricalVaRAsync_WithHighVolatility_ProducesHigherVaR()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);
        var lowVolReturns = CreateNormalDistributionReturns(mean: 0m, stdDev: 0.01m, count: 252);
        var highVolReturns = CreateNormalDistributionReturns(mean: 0m, stdDev: 0.05m, count: 252);

        // Act
        var (var95Low, var99Low) = await _service.CalculateHistoricalVaRAsync(positions, lowVolReturns);
        var (var95High, var99High) = await _service.CalculateHistoricalVaRAsync(positions, highVolReturns);

        // Assert
        var95High.Should().BeGreaterThan(var95Low, "Higher volatility should produce higher VaR95");
        var99High.Should().BeGreaterThan(var99Low, "Higher volatility should produce higher VaR99");
    }

    #endregion

    #region CalculateDeltaNormalVaRAsync Tests

    [Fact]
    public async Task CalculateDeltaNormalVaRAsync_WithValidPositions_CalculatesVaRUsingCovarianceMatrix()
    {
        // Arrange
        var positions = CreateMultiProductPositions();
        var productReturns = CreateProductReturns(new Dictionary<string, decimal>
        {
            ["Brent"] = 0.02m,
            ["WTI"] = 0.025m
        });

        // Act
        var (var95, var99) = await _service.CalculateDeltaNormalVaRAsync(positions, productReturns);

        // Assert
        var95.Should().BeGreaterThan(0);
        var99.Should().BeGreaterThan(var95, "VaR99 should exceed VaR95");

        // Check z-score relationship: VaR99 = 2.326 * sigma, VaR95 = 1.645 * sigma
        var ratio = var99 / var95;
        ratio.Should().BeApproximately(2.326m / 1.645m, 0.2m, "VaR ratio should match z-score ratio");
    }

    [Fact]
    public async Task CalculateDeltaNormalVaRAsync_WithEmptyPositions_ReturnsZero()
    {
        // Arrange
        var emptyPositions = new List<PaperContract>();
        var productReturns = CreateProductReturns(new Dictionary<string, decimal> { ["Brent"] = 0.02m });

        // Act
        var (var95, var99) = await _service.CalculateDeltaNormalVaRAsync(emptyPositions, productReturns);

        // Assert
        var95.Should().Be(0);
        var99.Should().Be(0);
    }

    [Fact]
    public async Task CalculateDeltaNormalVaRAsync_WithNoReturnData_ReturnsZero()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);
        var emptyReturns = new Dictionary<string, List<decimal>>();

        // Act
        var (var95, var99) = await _service.CalculateDeltaNormalVaRAsync(positions, emptyReturns);

        // Assert
        var95.Should().Be(0);
        var99.Should().Be(0);
    }

    [Fact]
    public async Task CalculateDeltaNormalVaRAsync_WithLongAndShortPositions_AccountsForHedging()
    {
        // Arrange - Create hedged portfolio (long Brent, short WTI)
        var hedgedPositions = new List<PaperContract>
        {
            CreatePaperContract("Brent", PositionType.Long, quantity: 100, price: 80m),
            CreatePaperContract("WTI", PositionType.Short, quantity: 100, price: 75m)
        };

        var productReturns = CreateProductReturns(new Dictionary<string, decimal>
        {
            ["Brent"] = 0.02m,
            ["WTI"] = 0.02m  // Highly correlated
        });

        // Act
        var (var95Hedged, var99Hedged) = await _service.CalculateDeltaNormalVaRAsync(hedgedPositions, productReturns);

        // Compare to unhedged long-only portfolio
        var unhedgedPositions = new List<PaperContract>
        {
            CreatePaperContract("Brent", PositionType.Long, quantity: 100, price: 80m)
        };
        var (var95Unhedged, var99Unhedged) = await _service.CalculateDeltaNormalVaRAsync(unhedgedPositions, productReturns);

        // Assert - Hedged portfolio should have lower VaR
        var95Hedged.Should().BeLessThan(var95Unhedged, "Hedged portfolio should have lower VaR95");
        var99Hedged.Should().BeLessThan(var99Unhedged, "Hedged portfolio should have lower VaR99");
    }

    [Fact]
    public async Task CalculateDeltaNormalVaRAsync_WithPerfectlyCorrelatedProducts_ProducesHigherVaR()
    {
        // Arrange
        var positions = CreateMultiProductPositions();

        // Create perfectly correlated returns (correlation = 1.0)
        var perfectlyCorrelatedReturns = new Dictionary<string, List<decimal>>
        {
            ["Brent"] = new List<decimal> { 0.01m, 0.02m, -0.01m, 0.015m, -0.005m },
            ["WTI"] = new List<decimal> { 0.01m, 0.02m, -0.01m, 0.015m, -0.005m }  // Identical returns
        };

        // Create uncorrelated returns (correlation closer to 0)
        var uncorrelatedReturns = new Dictionary<string, List<decimal>>
        {
            ["Brent"] = new List<decimal> { 0.01m, 0.02m, -0.01m, 0.015m, -0.005m },
            ["WTI"] = new List<decimal> { -0.01m, 0.005m, 0.02m, -0.015m, 0.01m }
        };

        // Act
        var (var95Correlated, _) = await _service.CalculateDeltaNormalVaRAsync(positions, perfectlyCorrelatedReturns);
        var (var95Uncorrelated, _) = await _service.CalculateDeltaNormalVaRAsync(positions, uncorrelatedReturns);

        // Assert - Perfect correlation should produce higher VaR than uncorrelated
        var95Correlated.Should().BeGreaterThanOrEqualTo(var95Uncorrelated,
            "Perfectly correlated products should have higher or equal VaR");
    }

    #endregion

    #region CalculatePortfolioVolatility Tests

    [Fact]
    public void CalculatePortfolioVolatility_WithValidData_ReturnsPositiveVolatility()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);
        var productReturns = CreateProductReturns(new Dictionary<string, decimal> { ["Brent"] = 0.02m });

        // Act
        var volatility = _service.CalculatePortfolioVolatility(positions, productReturns);

        // Assert
        volatility.Should().BeGreaterThan(0, "Volatility should be positive");
        volatility.Should().BeLessThan(1m, "Annual volatility should be less than 100%");
    }

    [Fact]
    public void CalculatePortfolioVolatility_WithEmptyPositions_ReturnsZero()
    {
        // Arrange
        var emptyPositions = new List<PaperContract>();
        var productReturns = CreateProductReturns(new Dictionary<string, decimal> { ["Brent"] = 0.02m });

        // Act
        var volatility = _service.CalculatePortfolioVolatility(emptyPositions, productReturns);

        // Assert
        volatility.Should().Be(0);
    }

    [Fact]
    public void CalculatePortfolioVolatility_WithNoReturnData_UsesIndustryStandardFallback()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);
        var emptyReturns = new Dictionary<string, List<decimal>>();

        // Act
        var volatility = _service.CalculatePortfolioVolatility(positions, emptyReturns);

        // Assert - Should fallback to industry standard (25% for oil markets)
        volatility.Should().Be(0.25m, "Should use 25% industry standard fallback");
    }

    [Fact]
    public void CalculatePortfolioVolatility_WithDiversifiedPortfolio_LowerThanSumOfIndividual()
    {
        // Arrange - Create portfolio with multiple uncorrelated products
        var diversifiedPositions = new List<PaperContract>
        {
            CreatePaperContract("Brent", PositionType.Long, quantity: 50, price: 80m),
            CreatePaperContract("WTI", PositionType.Long, quantity: 50, price: 75m),
            CreatePaperContract("MGO", PositionType.Long, quantity: 50, price: 500m)
        };

        var uncorrelatedReturns = new Dictionary<string, List<decimal>>
        {
            ["Brent"] = new List<decimal> { 0.02m, -0.01m, 0.015m, -0.005m, 0.01m },
            ["WTI"] = new List<decimal> { -0.01m, 0.02m, -0.015m, 0.01m, -0.005m },
            ["MGO"] = new List<decimal> { 0.005m, -0.015m, 0.02m, -0.01m, 0.015m }
        };

        // Act
        var portfolioVol = _service.CalculatePortfolioVolatility(diversifiedPositions, uncorrelatedReturns);

        // Calculate naive sum (ignoring diversification)
        decimal naiveSum = 0;
        foreach (var product in uncorrelatedReturns.Keys)
        {
            var mean = uncorrelatedReturns[product].Average();
            var variance = uncorrelatedReturns[product].Sum(r => (double)Math.Pow((double)(r - mean), 2)) / uncorrelatedReturns[product].Count;
            naiveSum += (decimal)Math.Sqrt(variance * 252) / 3; // Divided by 3 for equal weights
        }

        // Assert - Diversification benefit should exist
        portfolioVol.Should().BeLessThanOrEqualTo(naiveSum,
            "Diversified portfolio volatility should benefit from imperfect correlation");
    }

    #endregion

    #region CalculateMaxDrawdown Tests

    [Fact]
    public void CalculateMaxDrawdown_WithDecreasingReturns_ReturnsMaximumDrop()
    {
        // Arrange
        var returns = new List<decimal> { 100m, 110m, 105m, 90m, 95m, 85m };

        // Act
        var maxDrawdown = _service.CalculateMaxDrawdown(returns);

        // Assert
        // Peak at 110, trough at 85 = (110-85)/110 = 22.7%
        maxDrawdown.Should().BeApproximately(0.227m, 0.01m);
    }

    [Fact]
    public void CalculateMaxDrawdown_WithIncreasingReturns_ReturnsZero()
    {
        // Arrange
        var returns = new List<decimal> { 100m, 105m, 110m, 115m, 120m };

        // Act
        var maxDrawdown = _service.CalculateMaxDrawdown(returns);

        // Assert
        maxDrawdown.Should().Be(0m, "No drawdown when returns only increase");
    }

    [Fact]
    public void CalculateMaxDrawdown_WithEmptyReturns_ReturnsZero()
    {
        // Arrange
        var emptyReturns = new List<decimal>();

        // Act
        var maxDrawdown = _service.CalculateMaxDrawdown(emptyReturns);

        // Assert
        maxDrawdown.Should().Be(0m);
    }

    [Fact]
    public void CalculateMaxDrawdown_WithMultiplePeaksAndTroughs_ReturnsLargestDrawdown()
    {
        // Arrange - Multiple drawdown periods
        var returns = new List<decimal>
        {
            100m, 120m, 110m,  // First drawdown: 8.3%
            130m, 115m,        // Second drawdown: 11.5%
            140m, 100m         // Third drawdown: 28.6% (largest)
        };

        // Act
        var maxDrawdown = _service.CalculateMaxDrawdown(returns);

        // Assert - Should return the largest drawdown (28.6%)
        maxDrawdown.Should().BeApproximately(0.286m, 0.01m);
    }

    #endregion

    #region RunStressTestsAsync Tests

    [Fact]
    public async Task RunStressTestsAsync_WithValidPositions_ReturnsAllScenarios()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);
        var currentPrices = new Dictionary<string, decimal> { ["Brent"] = 80m };

        // Act
        var stressTests = await _service.RunStressTestsAsync(positions, currentPrices);

        // Assert
        stressTests.Should().HaveCount(5, "Should have 5 stress test scenarios");
        stressTests.Should().Contain(s => s.Scenario == "-10% Shock");
        stressTests.Should().Contain(s => s.Scenario == "+10% Shock");
        stressTests.Should().Contain(s => s.Scenario == "Historical Worst");
        stressTests.Should().Contain(s => s.Scenario == "Geopolitical Crisis");
        stressTests.Should().Contain(s => s.Scenario == "Demand Collapse");
    }

    [Fact]
    public async Task RunStressTestsAsync_WithLongPosition_NegativeShockProducesLoss()
    {
        // Arrange
        var longPositions = new List<PaperContract>
        {
            CreatePaperContract("Brent", PositionType.Long, quantity: 1000, price: 80m)
        };
        var currentPrices = new Dictionary<string, decimal> { ["Brent"] = 80m };

        // Act
        var stressTests = await _service.RunStressTestsAsync(longPositions, currentPrices);
        var negativeShock = stressTests.First(s => s.Scenario == "-10% Shock");

        // Assert
        negativeShock.PnlImpact.Should().BeLessThan(0, "Long position should lose money on negative price shock");
        // Expected: 1000 lots * 1000 bbl/lot * 80 USD/bbl * -10% = -8,000,000 USD
        negativeShock.PnlImpact.Should().BeApproximately(-8000000m, 100000m);
    }

    [Fact]
    public async Task RunStressTestsAsync_WithShortPosition_PositiveShockProducesLoss()
    {
        // Arrange
        var shortPositions = new List<PaperContract>
        {
            CreatePaperContract("Brent", PositionType.Short, quantity: 1000, price: 80m)
        };
        var currentPrices = new Dictionary<string, decimal> { ["Brent"] = 80m };

        // Act
        var stressTests = await _service.RunStressTestsAsync(shortPositions, currentPrices);
        var positiveShock = stressTests.First(s => s.Scenario == "+10% Shock");

        // Assert
        positiveShock.PnlImpact.Should().BeLessThan(0, "Short position should lose money on positive price shock");
        // Expected: 1000 lots * 1000 bbl/lot * 80 USD/bbl * -10% = -8,000,000 USD (same magnitude, opposite sign)
        positiveShock.PnlImpact.Should().BeApproximately(-8000000m, 100000m);
    }

    [Fact]
    public async Task RunStressTestsAsync_DemandCollapseScenario_ProducesMostSevereLoss()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);
        var currentPrices = new Dictionary<string, decimal> { ["Brent"] = 80m };

        // Act
        var stressTests = await _service.RunStressTestsAsync(positions, currentPrices);
        var demandCollapse = stressTests.First(s => s.Scenario == "Demand Collapse");
        var tenPercentShock = stressTests.First(s => s.Scenario == "-10% Shock");

        // Assert
        // Demand collapse is -25% vs -10% shock, should be more severe
        Math.Abs(demandCollapse.PnlImpact).Should().BeGreaterThan(Math.Abs(tenPercentShock.PnlImpact));
    }

    #endregion

    #region CalculateExpectedShortfallAsync Tests

    [Fact]
    public async Task CalculateExpectedShortfallAsync_WithValidData_ExceedsVaR()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);
        var returns = CreateNormalDistributionReturns(mean: 0m, stdDev: 0.02m, count: 252);

        // Act
        var (es95, es99) = await _service.CalculateExpectedShortfallAsync(returns, positions);
        var (var95, var99) = await _service.CalculateHistoricalVaRAsync(positions, returns);

        // Assert
        es95.Should().BeGreaterThanOrEqualTo(var95, "ES95 should be at least as large as VaR95");
        es99.Should().BeGreaterThanOrEqualTo(var99, "ES99 should be at least as large as VaR99");
    }

    [Fact]
    public async Task CalculateExpectedShortfallAsync_WithEmptyReturns_ReturnsZero()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);
        var emptyReturns = new List<decimal>();

        // Act
        var (es95, es99) = await _service.CalculateExpectedShortfallAsync(emptyReturns, positions);

        // Assert
        es95.Should().Be(0);
        es99.Should().Be(0);
    }

    [Fact]
    public async Task CalculateExpectedShortfallAsync_WithFatTailedDistribution_SignificantlyExceedsVaR()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);

        // Create fat-tailed distribution (extreme losses more likely)
        var fatTailedReturns = new List<decimal>();
        for (int i = 0; i < 240; i++) fatTailedReturns.Add(0.001m); // Normal small gains
        for (int i = 0; i < 10; i++) fatTailedReturns.Add(-0.10m);  // Several extreme losses
        for (int i = 0; i < 2; i++) fatTailedReturns.Add(-0.20m);   // Very extreme losses

        // Act
        var (es99, _) = await _service.CalculateExpectedShortfallAsync(fatTailedReturns, positions);
        var (var99, _) = await _service.CalculateHistoricalVaRAsync(positions, fatTailedReturns);

        // Assert - ES should be significantly higher than VaR for fat-tailed distributions
        var ratio = es99 / var99;
        ratio.Should().BeGreaterThan(1.2m, "ES should exceed VaR by at least 20% for fat-tailed distributions");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CalculatePortfolioRiskAsync_WithNoPositions_ReturnsZeroRisk()
    {
        // Arrange
        _mockPaperContractRepository
            .Setup(r => r.GetOpenPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaperContract>());

        // Act
        var result = await _service.CalculatePortfolioRiskAsync(DateTime.UtcNow, 252, true);

        // Assert
        result.TotalPortfolioValue.Should().Be(0);
        result.PositionCount.Should().Be(0);
        result.HistoricalVaR95.Should().Be(0);
        result.HistoricalVaR99.Should().Be(0);
    }

    [Fact]
    public async Task CalculatePortfolioRiskAsync_WithValidPositions_ReturnsCompleteRiskMetrics()
    {
        // Arrange
        var positions = CreateSamplePositions(portfolioValue: 1000000m);
        var marketPrices = CreateMarketPrices("Brent", 80m, count: 260);

        _mockPaperContractRepository
            .Setup(r => r.GetOpenPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        _mockMarketDataRepository
            .Setup(r => r.GetHistoricalPricesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(marketPrices);

        _mockMarketDataRepository
            .Setup(r => r.GetLatestPriceAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(marketPrices.Last());

        // Act
        var result = await _service.CalculatePortfolioRiskAsync(DateTime.UtcNow, 252, true);

        // Assert
        result.Should().NotBeNull();
        result.TotalPortfolioValue.Should().BeGreaterThan(0);
        result.PositionCount.Should().Be(positions.Count);
        result.HistoricalVaR95.Should().BeGreaterThan(0);
        result.HistoricalVaR99.Should().BeGreaterThan(result.HistoricalVaR95);
        result.PortfolioVolatility.Should().BeGreaterThan(0);
        result.StressTests.Should().HaveCount(5);
        result.ProductExposures.Should().NotBeEmpty();
    }

    #endregion

    #region Helper Methods

    private List<PaperContract> CreateSamplePositions(decimal portfolioValue)
    {
        var lotSize = 1000m; // 1000 barrels per lot
        var price = 80m; // USD per barrel
        var quantity = portfolioValue / (lotSize * price);

        return new List<PaperContract>
        {
            CreatePaperContract("Brent", PositionType.Long, quantity, price)
        };
    }

    private List<PaperContract> CreateMultiProductPositions()
    {
        return new List<PaperContract>
        {
            CreatePaperContract("Brent", PositionType.Long, quantity: 100, price: 80m),
            CreatePaperContract("WTI", PositionType.Long, quantity: 50, price: 75m)
        };
    }

    private PaperContract CreatePaperContract(string productType, PositionType position, decimal quantity, decimal price)
    {
        var contract = new PaperContract
        {
            ProductType = productType,
            Position = position,
            Quantity = (int)quantity,
            LotSize = 1000m, // Standard: 1000 barrels per lot
            EntryPrice = price,
            CurrentPrice = price,
            TradeDate = DateTime.UtcNow.AddDays(-30),
            Status = PaperContractStatus.Open
        };
        contract.SetId(Guid.NewGuid());
        return contract;
    }

    private List<decimal> CreateNormalDistributionReturns(decimal mean, decimal stdDev, int count)
    {
        // Simple normal distribution approximation for testing
        var random = new Random(42); // Fixed seed for reproducibility
        var returns = new List<decimal>();

        for (int i = 0; i < count; i++)
        {
            // Box-Muller transform for normal distribution
            var u1 = (decimal)random.NextDouble();
            var u2 = (decimal)random.NextDouble();
            var randStdNormal = (decimal)Math.Sqrt(-2.0 * Math.Log((double)u1)) * (decimal)Math.Sin(2.0 * Math.PI * (double)u2);
            var randNormal = mean + stdDev * randStdNormal;
            returns.Add(randNormal);
        }

        return returns;
    }

    private Dictionary<string, List<decimal>> CreateProductReturns(Dictionary<string, decimal> volatilities)
    {
        var result = new Dictionary<string, List<decimal>>();
        foreach (var kvp in volatilities)
        {
            result[kvp.Key] = CreateNormalDistributionReturns(mean: 0m, stdDev: kvp.Value, count: 252);
        }
        return result;
    }

    private List<MarketPrice> CreateMarketPrices(string productCode, decimal basePrice, int count)
    {
        var prices = new List<MarketPrice>();
        var random = new Random(42);
        var currentPrice = basePrice;

        for (int i = 0; i < count; i++)
        {
            var dailyReturn = (decimal)(random.NextDouble() - 0.5) * 0.04m; // +/- 2% daily
            currentPrice *= (1 + dailyReturn);

            var marketPrice = new MarketPrice
            {
                ProductCode = productCode,
                ProductName = productCode,
                Price = currentPrice,
                PriceDate = DateTime.UtcNow.AddDays(-count + i),
                PriceType = MarketPriceType.Spot,
                DataSource = "Test",
                ImportedAt = DateTime.UtcNow
            };
            marketPrice.SetId(Guid.NewGuid());
            prices.Add(marketPrice);
        }

        return prices;
    }

    #endregion
}
