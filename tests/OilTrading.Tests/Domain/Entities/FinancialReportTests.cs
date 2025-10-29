using FluentAssertions;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using Xunit;

namespace OilTrading.Tests.Domain.Entities;

public class FinancialReportTests
{
    private readonly Guid _tradingPartnerId = Guid.NewGuid();
    private readonly DateTime _validStartDate = DateTime.UtcNow.AddDays(-365);
    private readonly DateTime _validEndDate = DateTime.UtcNow.AddDays(-1);

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidData_ShouldCreateFinancialReport()
    {
        // Act
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);

        // Assert
        report.Should().NotBeNull();
        report.TradingPartnerId.Should().Be(_tradingPartnerId);
        report.ReportStartDate.Should().Be(_validStartDate);
        report.ReportEndDate.Should().Be(_validEndDate);
        report.ReportYear.Should().Be(_validStartDate.Year);
        report.IsAnnualReport.Should().BeTrue(); // 365 days period
    }

    [Fact]
    public void Constructor_WithStartDateAfterEndDate_ShouldThrowDomainException()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(-2);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            new FinancialReport(_tradingPartnerId, startDate, endDate));
        
        exception.Message.Should().Contain("Report start date must be before end date");
    }

    [Fact]
    public void Constructor_WithEndDateInFuture_ShouldThrowDomainException()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(2);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            new FinancialReport(_tradingPartnerId, startDate, endDate));
        
        exception.Message.Should().Contain("Report end date cannot be in the future");
    }

    [Fact]
    public void Constructor_WithPeriodExceeding366Days_ShouldThrowDomainException()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-400);
        var endDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            new FinancialReport(_tradingPartnerId, startDate, endDate));
        
        exception.Message.Should().Contain("Report period cannot exceed 366 days");
    }

    [Fact]
    public void Constructor_WithZeroDayPeriod_ShouldThrowDomainException()
    {
        // Arrange
        var sameDate = DateTime.UtcNow.AddDays(-3).Date;

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            new FinancialReport(_tradingPartnerId, sameDate, sameDate));

        exception.Message.Should().Contain("Report start date must be before end date");
    }

    #endregion

    #region Computed Properties Tests

    [Theory]
    [InlineData(100, 50, 2.0)]
    [InlineData(150, 75, 2.0)]
    [InlineData(50, 100, 0.5)]
    [InlineData(0, 50, 0.0)]
    public void CurrentRatio_WithValidValues_ShouldCalculateCorrectly(decimal currentAssets, decimal currentLiabilities, decimal expectedRatio)
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(200, 100, 100, currentAssets, currentLiabilities);

        // Act
        var ratio = report.CurrentRatio;

        // Assert
        ratio.Should().Be(expectedRatio);
    }

    [Fact]
    public void CurrentRatio_WithNullCurrentAssets_ShouldReturnNull()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(200, 100, 100, null, 50m);

        // Act
        var ratio = report.CurrentRatio;

        // Assert
        ratio.Should().BeNull();
    }

    [Fact]
    public void CurrentRatio_WithNullCurrentLiabilities_ShouldReturnNull()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(200, 100, 100, 100m, null);

        // Act
        var ratio = report.CurrentRatio;

        // Assert
        ratio.Should().BeNull();
    }

    [Fact]
    public void CurrentRatio_WithZeroCurrentLiabilities_ShouldReturnNull()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(200, 100, 100, 100m, 0m);

        // Act
        var ratio = report.CurrentRatio;

        // Assert
        ratio.Should().BeNull();
    }

    [Theory]
    [InlineData(200, 100, 0.5)]
    [InlineData(150, 75, 0.5)]
    [InlineData(100, 80, 0.8)]
    [InlineData(100, 50, 0.5)]
    public void DebtToAssetRatio_WithValidValues_ShouldCalculateCorrectly(decimal totalAssets, decimal totalLiabilities, decimal expectedRatio)
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(totalAssets, totalLiabilities, 100, 50, 25);

        // Act
        var ratio = report.DebtToAssetRatio;

        // Assert
        ratio.Should().Be(expectedRatio);
    }

    [Fact]
    public void DebtToAssetRatio_WithNullTotalAssets_ShouldReturnNull()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(null, 100m, 100, 50, 25);

        // Act
        var ratio = report.DebtToAssetRatio;

        // Assert
        ratio.Should().BeNull();
    }

    [Fact]
    public void DebtToAssetRatio_WithNullTotalLiabilities_ShouldReturnNull()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(200m, null, 100, 50, 25);

        // Act
        var ratio = report.DebtToAssetRatio;

        // Assert
        ratio.Should().BeNull();
    }

    [Fact]
    public void DebtToAssetRatio_WithZeroTotalAssets_ShouldReturnNull()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(100m, 50m, 50, null, null);

        // Act
        var ratio = report.DebtToAssetRatio;

        // Assert
        ratio.Should().Be(0.5m);
    }

    [Theory]
    [InlineData(100, 50, 0.5)]    // 50/100 = 0.5
    [InlineData(50, 100, 2.0)]    // 100/50 = 2.0
    [InlineData(25, 100, 4.0)]    // 100/25 = 4.0
    [InlineData(100, 0, 0.0)]     // 0/100 = 0.0
    public void ROE_WithValidValues_ShouldCalculateCorrectly(decimal netAssets, decimal netProfit, decimal expectedROE)
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(200, 100, netAssets, 50, 25);
        report.UpdatePerformanceData(1000, netProfit, 80);

        // Act
        var roe = report.ROE;

        // Assert
        roe.Should().Be(expectedROE);
    }

    [Fact]
    public void ROE_WithNullNetAssets_ShouldReturnNull()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(200, 100, null, 50, 25);
        report.UpdatePerformanceData(1000, 50m, 80);

        // Act
        var roe = report.ROE;

        // Assert
        roe.Should().BeNull();
    }

    [Fact]
    public void ROE_WithNullNetProfit_ShouldReturnNull()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(200, 100, 100m, 50, 25);
        report.UpdatePerformanceData(1000, null, 80);

        // Act
        var roe = report.ROE;

        // Assert
        roe.Should().BeNull();
    }

    [Fact]
    public void ROE_WithZeroNetAssets_ShouldReturnNull()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(200, 100, 0m, 50, 25);
        report.UpdatePerformanceData(1000, 50m, 80);

        // Act
        var roe = report.ROE;

        // Assert
        roe.Should().BeNull();
    }

    [Theory]
    [InlineData(200, 50, 0.25)]
    [InlineData(100, 25, 0.25)]
    [InlineData(500, 100, 0.2)]
    [InlineData(100, 0, 0.0)]
    public void ROA_WithValidValues_ShouldCalculateCorrectly(decimal totalAssets, decimal netProfit, decimal expectedROA)
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(totalAssets, 100, 100, 50, 25);
        report.UpdatePerformanceData(1000, netProfit, 80);

        // Act
        var roa = report.ROA;

        // Assert
        roa.Should().Be(expectedROA);
    }

    [Fact]
    public void ROA_WithNullTotalAssets_ShouldReturnNull()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(null, 100, 100, 50, 25);
        report.UpdatePerformanceData(1000, 50m, 80);

        // Act
        var roa = report.ROA;

        // Assert
        roa.Should().BeNull();
    }

    [Fact]
    public void ROA_WithNullNetProfit_ShouldReturnNull()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(200m, 100, 100, 50, 25);
        report.UpdatePerformanceData(1000, null, 80);

        // Act
        var roa = report.ROA;

        // Assert
        roa.Should().BeNull();
    }

    [Fact]
    public void ROA_WithZeroTotalAssets_ShouldReturnNull()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        report.UpdateFinancialPosition(100m, 50, 50, null, null);
        report.UpdatePerformanceData(1000, 50m, 80);

        // Act
        var roa = report.ROA;

        // Assert
        roa.Should().Be(0.5m);
    }

    [Theory]
    [InlineData("2024-01-01", 2024)]
    [InlineData("2023-06-15", 2023)]
    [InlineData("2024-09-01", 2024)]
    public void ReportYear_ShouldReturnStartDateYear(string startDateString, int expectedYear)
    {
        // Arrange
        var startDate = DateTime.Parse(startDateString);
        var endDate = startDate.AddDays(30);
        var report = new FinancialReport(_tradingPartnerId, startDate, endDate);

        // Act
        var year = report.ReportYear;

        // Assert
        year.Should().Be(expectedYear);
    }

    [Theory]
    [InlineData(365, true)]
    [InlineData(366, true)]
    [InlineData(360, true)]
    [InlineData(359, false)]
    [InlineData(30, false)]
    [InlineData(90, false)]
    public void IsAnnualReport_WithDifferentPeriods_ShouldReturnCorrectValue(int days, bool expectedIsAnnual)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-days);
        var endDate = DateTime.UtcNow.AddDays(-1);
        var report = new FinancialReport(_tradingPartnerId, startDate, endDate);

        // Act
        var isAnnual = report.IsAnnualReport;

        // Assert
        isAnnual.Should().Be(expectedIsAnnual);
    }

    #endregion

    #region UpdateFinancialPosition Tests

    [Fact]
    public void UpdateFinancialPosition_WithValidData_ShouldUpdateAllFields()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        var totalAssets = 1000m;
        var totalLiabilities = 600m;
        var netAssets = 400m;
        var currentAssets = 500m;
        var currentLiabilities = 300m;

        // Act
        report.UpdateFinancialPosition(totalAssets, totalLiabilities, netAssets, currentAssets, currentLiabilities);

        // Assert
        report.TotalAssets.Should().Be(totalAssets);
        report.TotalLiabilities.Should().Be(totalLiabilities);
        report.NetAssets.Should().Be(netAssets);
        report.CurrentAssets.Should().Be(currentAssets);
        report.CurrentLiabilities.Should().Be(currentLiabilities);
    }

    [Fact]
    public void UpdateFinancialPosition_WithNegativeTotalAssets_ShouldThrowDomainException()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            report.UpdateFinancialPosition(-100, 50, 50, 25, 20));
        
        exception.Message.Should().Contain("Total assets cannot be negative");
    }

    [Fact]
    public void UpdateFinancialPosition_WithNegativeCurrentAssets_ShouldThrowDomainException()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            report.UpdateFinancialPosition(100, 50, 50, -25, 20));
        
        exception.Message.Should().Contain("Current assets cannot be negative");
    }

    [Fact]
    public void UpdateFinancialPosition_WithCurrentAssetsExceedingTotal_ShouldThrowDomainException()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            report.UpdateFinancialPosition(100, 50, 50, 150, 20));
        
        exception.Message.Should().Contain("Current assets cannot exceed total assets");
    }

    [Fact]
    public void UpdateFinancialPosition_WithCurrentLiabilitiesExceedingTotal_ShouldThrowDomainException()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            report.UpdateFinancialPosition(200, 50, 50, 100, 80));
        
        exception.Message.Should().Contain("Current liabilities cannot exceed total liabilities");
    }

    [Fact]
    public void UpdateFinancialPosition_WithNullValues_ShouldAllowNullValues()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);

        // Act
        report.UpdateFinancialPosition(null, null, null, null, null);

        // Assert
        report.TotalAssets.Should().BeNull();
        report.TotalLiabilities.Should().BeNull();
        report.NetAssets.Should().BeNull();
        report.CurrentAssets.Should().BeNull();
        report.CurrentLiabilities.Should().BeNull();
    }

    #endregion

    #region UpdatePerformanceData Tests

    [Fact]
    public void UpdatePerformanceData_WithValidData_ShouldUpdateAllFields()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        var revenue = 10000m;
        var netProfit = 1500m;
        var operatingCashFlow = 2000m;

        // Act
        report.UpdatePerformanceData(revenue, netProfit, operatingCashFlow);

        // Assert
        report.Revenue.Should().Be(revenue);
        report.NetProfit.Should().Be(netProfit);
        report.OperatingCashFlow.Should().Be(operatingCashFlow);
    }

    [Fact]
    public void UpdatePerformanceData_WithNullValues_ShouldAllowNullValues()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);

        // Act
        report.UpdatePerformanceData(null, null, null);

        // Assert
        report.Revenue.Should().BeNull();
        report.NetProfit.Should().BeNull();
        report.OperatingCashFlow.Should().BeNull();
    }

    [Fact]
    public void UpdatePerformanceData_WithNegativeValues_ShouldAllowNegativeValues()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        var revenue = 5000m;
        var netProfit = -500m; // Loss
        var operatingCashFlow = -200m; // Negative cash flow

        // Act
        report.UpdatePerformanceData(revenue, netProfit, operatingCashFlow);

        // Assert
        report.Revenue.Should().Be(revenue);
        report.NetProfit.Should().Be(netProfit);
        report.OperatingCashFlow.Should().Be(operatingCashFlow);
    }

    #endregion

    #region UpdateReportPeriod Tests

    [Fact]
    public void UpdateReportPeriod_WithValidDates_ShouldUpdateDates()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        var newStartDate = DateTime.UtcNow.AddDays(-180);
        var newEndDate = DateTime.UtcNow.AddDays(-90);

        // Act
        report.UpdateReportPeriod(newStartDate, newEndDate);

        // Assert
        report.ReportStartDate.Should().Be(newStartDate);
        report.ReportEndDate.Should().Be(newEndDate);
        report.ReportYear.Should().Be(newStartDate.Year);
    }

    [Fact]
    public void UpdateReportPeriod_WithInvalidDates_ShouldThrowDomainException()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        var newStartDate = DateTime.UtcNow.AddDays(-90);
        var newEndDate = DateTime.UtcNow.AddDays(-180);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            report.UpdateReportPeriod(newStartDate, newEndDate));
        
        exception.Message.Should().Contain("Report start date must be before end date");
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public void FinancialReport_WithCompleteDataSet_ShouldCalculateAllRatiosCorrectly()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        
        // Act
        report.UpdateFinancialPosition(1000m, 600m, 400m, 500m, 300m);
        report.UpdatePerformanceData(2000m, 200m, 250m);

        // Assert
        report.CurrentRatio.Should().BeApproximately(1.67m, 0.01m);
        report.DebtToAssetRatio.Should().Be(0.6m);
        report.ROE.Should().Be(0.5m);
        report.ROA.Should().Be(0.2m);
    }

    [Fact]
    public void FinancialReport_WithPartialData_ShouldHandleNullRatiosGracefully()
    {
        // Arrange
        var report = new FinancialReport(_tradingPartnerId, _validStartDate, _validEndDate);
        
        // Act - Only update some fields
        report.UpdateFinancialPosition(1000m, null, 400m, 500m, null);
        report.UpdatePerformanceData(2000m, 200m, 250m);

        // Assert
        report.CurrentRatio.Should().BeNull(); // No current liabilities
        report.DebtToAssetRatio.Should().BeNull(); // No total liabilities
        report.ROE.Should().Be(0.5m); // Has net assets and net profit
        report.ROA.Should().Be(0.2m); // Has total assets and net profit
    }

    [Theory]
    [InlineData(2, false)] // 2 days
    [InlineData(30, false)] // 1 month
    [InlineData(90, false)] // 1 quarter
    [InlineData(180, false)] // 6 months
    [InlineData(359, false)] // Just under annual
    [InlineData(360, true)] // Annual threshold
    [InlineData(365, true)] // Full year
    [InlineData(366, true)] // Leap year
    public void IsAnnualReport_WithVariousPeriods_ShouldClassifyCorrectly(int days, bool expectedIsAnnual)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        var endDate = DateTime.UtcNow.AddDays(-1).Date;
        var report = new FinancialReport(_tradingPartnerId, startDate, endDate);

        // Act
        var isAnnual = report.IsAnnualReport;

        // Assert
        isAnnual.Should().Be(expectedIsAnnual);
    }

    #endregion
}