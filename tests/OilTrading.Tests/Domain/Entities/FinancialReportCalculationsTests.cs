using FluentAssertions;
using OilTrading.Core.Entities;
using Xunit;

namespace OilTrading.Tests.Domain.Entities;

public class FinancialReportCalculationsTests
{
    private readonly Guid _tradingPartnerId = Guid.NewGuid();
    private readonly DateTime _validStartDate = DateTime.UtcNow.AddDays(-90);
    private readonly DateTime _validEndDate = DateTime.UtcNow.AddDays(-1);

    #region Current Ratio Tests

    [Theory]
    [InlineData(100, 50, 2.0)]
    [InlineData(150, 75, 2.0)]
    [InlineData(200, 100, 2.0)]
    [InlineData(75, 150, 0.5)]
    [InlineData(1, 1, 1.0)]
    [InlineData(0, 50, 0.0)]
    public void CurrentRatio_NormalScenarios_ShouldCalculateCorrectly(decimal currentAssets, decimal currentLiabilities, decimal expectedRatio)
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(1000, 500, 500, currentAssets, currentLiabilities);

        // Act
        var ratio = report.CurrentRatio;

        // Assert
        ratio.Should().BeApproximately(expectedRatio, 0.001m);
    }

    [Fact]
    public void CurrentRatio_ZeroCurrentLiabilities_ShouldReturnNull()
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(1000, 500, 500, 100, 0);

        // Act
        var ratio = report.CurrentRatio;

        // Assert
        ratio.Should().BeNull();
    }

    [Theory]
    [InlineData(null, 50)]
    [InlineData(100, null)]
    [InlineData(null, null)]
    public void CurrentRatio_NullValues_ShouldReturnNull(decimal? currentAssets, decimal? currentLiabilities)
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(1000, 500, 500, currentAssets, currentLiabilities);

        // Act
        var ratio = report.CurrentRatio;

        // Assert
        ratio.Should().BeNull();
    }

    [Theory]
    [InlineData(0.000001, 0.000001, 1.0)] // Very small numbers
    [InlineData(1000000, 500000, 2.0)] // Large numbers
    [InlineData(1.5, 3.7, 0.405405)] // Decimal precision
    public void CurrentRatio_EdgeCases_ShouldHandleCorrectly(decimal currentAssets, decimal currentLiabilities, double expectedRatio)
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(10000000, 5000000, 5000000, currentAssets, currentLiabilities);

        // Act
        var ratio = report.CurrentRatio;

        // Assert
        ratio.Should().BeApproximately((decimal)expectedRatio, 0.000001m);
    }

    #endregion

    #region Debt-to-Asset Ratio Tests

    [Theory]
    [InlineData(1000, 500, 0.5)]
    [InlineData(1000, 600, 0.6)]
    [InlineData(1000, 800, 0.8)]
    [InlineData(1000, 0, 0.0)]
    [InlineData(1000, 1000, 1.0)]
    public void DebtToAssetRatio_NormalScenarios_ShouldCalculateCorrectly(decimal totalAssets, decimal totalLiabilities, decimal expectedRatio)
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(totalAssets, totalLiabilities, null, null, null);

        // Act
        var ratio = report.DebtToAssetRatio;

        // Assert
        ratio.Should().Be(expectedRatio);
    }

    [Fact]
    public void DebtToAssetRatio_ZeroTotalAssets_ShouldReturnNull()
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(0, 500, null, null, null);

        // Act
        var ratio = report.DebtToAssetRatio;

        // Assert
        ratio.Should().BeNull();
    }

    [Theory]
    [InlineData(null, 500)]
    [InlineData(1000, null)]
    [InlineData(null, null)]
    public void DebtToAssetRatio_NullValues_ShouldReturnNull(decimal? totalAssets, decimal? totalLiabilities)
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(totalAssets, totalLiabilities, null, null, null);

        // Act
        var ratio = report.DebtToAssetRatio;

        // Assert
        ratio.Should().BeNull();
    }

    [Theory]
    [InlineData(100, 80, 0.8)] // High debt ratio
    [InlineData(100, 20, 0.2)] // Low debt ratio
    [InlineData(0.5, 0.3, 0.6)] // Small decimals
    [InlineData(1000000, 750000, 0.75)] // Large numbers
    public void DebtToAssetRatio_VariousScenarios_ShouldCalculateCorrectly(decimal totalAssets, decimal totalLiabilities, decimal expectedRatio)
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(totalAssets, totalLiabilities, null, null, null);

        // Act
        var ratio = report.DebtToAssetRatio;

        // Assert
        ratio.Should().BeApproximately(expectedRatio, 0.000001m);
    }

    #endregion

    #region Return on Equity (ROE) Tests

    [Theory]
    [InlineData(100, 20, 0.2)]
    [InlineData(500, 100, 0.2)]
    [InlineData(100, 5, 0.05)]
    [InlineData(100, -10, -0.1)] // Loss scenario
    [InlineData(100, 0, 0.0)] // Break-even
    public void ROE_NormalScenarios_ShouldCalculateCorrectly(decimal netAssets, decimal netProfit, decimal expectedROE)
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(null, null, netAssets, null, null);
        report.UpdatePerformanceData(null, netProfit, null);

        // Act
        var roe = report.ROE;

        // Assert
        roe.Should().BeApproximately(expectedROE, 0.000001m);
    }

    [Fact]
    public void ROE_ZeroNetAssets_ShouldReturnNull()
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(null, null, 0, null, null);
        report.UpdatePerformanceData(null, 50, null);

        // Act
        var roe = report.ROE;

        // Assert
        roe.Should().BeNull();
    }

    [Theory]
    [InlineData(null, 50)]
    [InlineData(100, null)]
    [InlineData(null, null)]
    public void ROE_NullValues_ShouldReturnNull(decimal? netAssets, decimal? netProfit)
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(null, null, netAssets, null, null);
        report.UpdatePerformanceData(null, netProfit, null);

        // Act
        var roe = report.ROE;

        // Assert
        roe.Should().BeNull();
    }

    [Theory]
    [InlineData(0.001, 0.0001, 0.1)] // Very small numbers
    [InlineData(1000000, 150000, 0.15)] // Large numbers
    [InlineData(33.33, 10, 0.30003)] // Decimal precision
    public void ROE_EdgeCases_ShouldHandleCorrectly(decimal netAssets, decimal netProfit, double expectedROE)
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(null, null, netAssets, null, null);
        report.UpdatePerformanceData(null, netProfit, null);

        // Act
        var roe = report.ROE;

        // Assert
        roe.Should().BeApproximately((decimal)expectedROE, 0.00001m);
    }

    #endregion

    #region Return on Assets (ROA) Tests

    [Theory]
    [InlineData(1000, 50, 0.05)]
    [InlineData(500, 25, 0.05)]
    [InlineData(1000, 100, 0.1)]
    [InlineData(1000, -20, -0.02)] // Loss scenario
    [InlineData(1000, 0, 0.0)] // Break-even
    public void ROA_NormalScenarios_ShouldCalculateCorrectly(decimal totalAssets, decimal netProfit, decimal expectedROA)
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(totalAssets, null, null, null, null);
        report.UpdatePerformanceData(null, netProfit, null);

        // Act
        var roa = report.ROA;

        // Assert
        roa.Should().BeApproximately(expectedROA, 0.000001m);
    }

    [Fact]
    public void ROA_ZeroTotalAssets_ShouldReturnNull()
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(0, null, null, null, null);
        report.UpdatePerformanceData(null, 50, null);

        // Act
        var roa = report.ROA;

        // Assert
        roa.Should().BeNull();
    }

    [Theory]
    [InlineData(null, 50)]
    [InlineData(1000, null)]
    [InlineData(null, null)]
    public void ROA_NullValues_ShouldReturnNull(decimal? totalAssets, decimal? netProfit)
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(totalAssets, null, null, null, null);
        report.UpdatePerformanceData(null, netProfit, null);

        // Act
        var roa = report.ROA;

        // Assert
        roa.Should().BeNull();
    }

    [Theory]
    [InlineData(0.1, 0.005, 0.05)] // Very small numbers
    [InlineData(10000000, 500000, 0.05)] // Large numbers
    [InlineData(777.77, 23.33, 0.0299979)] // Decimal precision
    public void ROA_EdgeCases_ShouldHandleCorrectly(decimal totalAssets, decimal netProfit, double expectedROA)
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(totalAssets, null, null, null, null);
        report.UpdatePerformanceData(null, netProfit, null);

        // Act
        var roa = report.ROA;

        // Assert
        roa.Should().BeApproximately((decimal)expectedROA, 0.000001m);
    }

    #endregion

    #region Year-over-Year Growth Calculation Tests (Simulated)

    [Fact]
    public void FinancialCalculations_MultipleReportsScenario_ShouldCalculateIndependently()
    {
        // Arrange - Create two reports for different periods
        var report2023 = CreateReport(DateTime.Parse("2023-01-01"), DateTime.Parse("2023-12-31"));
        var report2024 = CreateReport(DateTime.Parse("2024-01-01"), DateTime.Parse("2024-12-31"));

        // Set 2023 data
        report2023.UpdateFinancialPosition(1000, 600, 400, 500, 300);
        report2023.UpdatePerformanceData(2000, 200, 250);

        // Set 2024 data (improved performance)
        report2024.UpdateFinancialPosition(1200, 650, 550, 600, 320);
        report2024.UpdatePerformanceData(2400, 300, 380);

        // Act & Assert - Each report should calculate ratios independently
        
        // 2023 ratios
        report2023.CurrentRatio.Should().BeApproximately(1.67m, 0.01m);
        report2023.ROE.Should().Be(0.5m);
        report2023.ROA.Should().Be(0.2m);

        // 2024 ratios
        report2024.CurrentRatio.Should().BeApproximately(1.875m, 0.001m);
        report2024.ROE.Should().BeApproximately(0.545m, 0.001m);
        report2024.ROA.Should().Be(0.25m);
    }

    [Theory]
    [InlineData(1000, 1200, 20.0)] // 20% growth
    [InlineData(1000, 800, -20.0)] // 20% decline
    [InlineData(1000, 1000, 0.0)] // No change
    [InlineData(100, 200, 100.0)] // 100% growth
    [InlineData(200, 100, -50.0)] // 50% decline
    public void GrowthCalculation_Simulation_ShouldCalculateCorrectly(decimal previous, decimal current, decimal expectedGrowth)
    {
        // This simulates the growth calculation logic that would be done in query handlers
        
        // Act
        decimal? growth = null;
        if (previous != 0)
        {
            growth = Math.Round(((current - previous) / Math.Abs(previous)) * 100, 2);
        }

        // Assert
        growth.Should().Be(expectedGrowth);
    }

    [Theory]
    [InlineData(0, 100, null)] // Division by zero
    [InlineData(-100, -50, -50.0)] // Negative values
    [InlineData(-100, 50, 150.0)] // Negative to positive
    public void GrowthCalculation_EdgeCases_ShouldHandleCorrectly(decimal previous, decimal current, decimal? expectedGrowth)
    {
        // This simulates edge cases in growth calculation
        
        // Act
        decimal? growth = null;
        if (previous != 0)
        {
            growth = Math.Round(((current - previous) / Math.Abs(previous)) * 100, 2);
        }

        // Assert
        growth.Should().Be(expectedGrowth);
    }

    #endregion

    #region Extreme Values and Boundary Tests

    [Theory]
    [InlineData(1000000, 500000)]
    [InlineData(0.000000001, 0.000000001)]
    [InlineData(1, 0.5)]
    public void AllRatios_ExtremeValues_ShouldNotThrowOverflowException(decimal assets, decimal liabilities)
    {
        // Arrange
        var report = CreateReport();

        // Act & Assert - Should not throw
        var action = () =>
        {
            report.UpdateFinancialPosition(assets, liabilities, assets - liabilities, assets * 0.6m, liabilities * 0.5m);
            report.UpdatePerformanceData(assets * 0.1m, (assets - liabilities) * 0.05m, assets * 0.08m);
            
            var currentRatio = report.CurrentRatio;
            var debtRatio = report.DebtToAssetRatio;
            var roe = report.ROE;
            var roa = report.ROA;
        };

        action.Should().NotThrow();
    }

    [Fact]
    public void FinancialCalculations_AllZeroValues_ShouldHandleGracefully()
    {
        // Arrange
        var report = CreateReport();
        report.UpdateFinancialPosition(0, 0, 0, 0, 0);
        report.UpdatePerformanceData(0, 0, 0);

        // Act
        var currentRatio = report.CurrentRatio;
        var debtRatio = report.DebtToAssetRatio;
        var roe = report.ROE;
        var roa = report.ROA;

        // Assert - All should be null due to division by zero
        currentRatio.Should().BeNull();
        debtRatio.Should().BeNull();
        roe.Should().BeNull();
        roa.Should().BeNull();
    }

    [Fact]
    public void FinancialCalculations_PerfectBalance_ShouldCalculateCorrectly()
    {
        // Arrange - Perfect financial balance scenario
        var report = CreateReport();
        report.UpdateFinancialPosition(1000, 500, 500, 400, 200); // 2:1 current ratio, 50% debt ratio
        report.UpdatePerformanceData(2000, 100, 150); // 10% ROA, 20% ROE

        // Act
        var currentRatio = report.CurrentRatio;
        var debtRatio = report.DebtToAssetRatio;
        var roe = report.ROE;
        var roa = report.ROA;

        // Assert - Verify all calculations are correct
        currentRatio.Should().Be(2.0m);
        debtRatio.Should().Be(0.5m);
        roe.Should().Be(0.2m);
        roa.Should().Be(0.1m);
    }

    #endregion

    #region Helper Methods

    private FinancialReport CreateReport(DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? _validStartDate;
        var end = endDate ?? _validEndDate;
        return new FinancialReport(_tradingPartnerId, start, end);
    }

    #endregion
}