using FluentAssertions;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Tests.Helpers;

/// <summary>
/// Helper methods and assertions for FinancialReport testing
/// </summary>
public static class FinancialReportTestHelpers
{
    #region Calculation Helpers

    /// <summary>
    /// Calculates expected current ratio
    /// </summary>
    public static decimal? CalculateExpectedCurrentRatio(decimal? currentAssets, decimal? currentLiabilities)
    {
        if (!currentAssets.HasValue || !currentLiabilities.HasValue || currentLiabilities.Value == 0)
            return null;
        return currentAssets.Value / currentLiabilities.Value;
    }

    /// <summary>
    /// Calculates expected debt-to-asset ratio
    /// </summary>
    public static decimal? CalculateExpectedDebtToAssetRatio(decimal? totalAssets, decimal? totalLiabilities)
    {
        if (!totalAssets.HasValue || !totalLiabilities.HasValue || totalAssets.Value == 0)
            return null;
        return totalLiabilities.Value / totalAssets.Value;
    }

    /// <summary>
    /// Calculates expected ROE
    /// </summary>
    public static decimal? CalculateExpectedROE(decimal? netProfit, decimal? netAssets)
    {
        if (!netProfit.HasValue || !netAssets.HasValue || netAssets.Value == 0)
            return null;
        return netProfit.Value / netAssets.Value;
    }

    /// <summary>
    /// Calculates expected ROA
    /// </summary>
    public static decimal? CalculateExpectedROA(decimal? netProfit, decimal? totalAssets)
    {
        if (!netProfit.HasValue || !totalAssets.HasValue || totalAssets.Value == 0)
            return null;
        return netProfit.Value / totalAssets.Value;
    }

    /// <summary>
    /// Calculates expected growth percentage
    /// </summary>
    public static decimal? CalculateExpectedGrowth(decimal? current, decimal? previous)
    {
        if (!current.HasValue || !previous.HasValue || previous.Value == 0)
            return null;
        return Math.Round(((current.Value - previous.Value) / Math.Abs(previous.Value)) * 100, 2);
    }

    /// <summary>
    /// Determines if a report period should be considered annual
    /// </summary>
    public static bool IsExpectedAnnualReport(DateTime startDate, DateTime endDate)
    {
        return endDate.Subtract(startDate).Days >= 360;
    }

    #endregion

    #region Risk Assessment Helpers

    /// <summary>
    /// Determines expected financial health status based on ratios
    /// </summary>
    public static string DetermineExpectedHealthStatus(
        decimal? currentRatio,
        decimal? debtToAssetRatio,
        decimal creditUtilization,
        decimal? revenueGrowth = null,
        decimal? profitGrowth = null)
    {
        int riskScore = 0;

        // Current ratio assessment
        if (currentRatio.HasValue)
        {
            if (currentRatio.Value < 1.0m) riskScore += 3;
            else if (currentRatio.Value < 1.5m) riskScore += 1;
        }

        // Debt-to-asset ratio assessment
        if (debtToAssetRatio.HasValue)
        {
            if (debtToAssetRatio.Value > 0.8m) riskScore += 3;
            else if (debtToAssetRatio.Value > 0.6m) riskScore += 1;
        }

        // Credit utilization assessment
        if (creditUtilization > 90) riskScore += 2;
        else if (creditUtilization > 75) riskScore += 1;

        // Growth trend assessment
        if (profitGrowth.HasValue && profitGrowth.Value < -20) riskScore += 2;
        if (revenueGrowth.HasValue && revenueGrowth.Value < -15) riskScore += 2;

        return riskScore switch
        {
            0 => "Excellent",
            1 => "Good",
            2 or 3 => "Fair",
            4 or 5 or 6 => "Poor",
            _ => "Critical"
        };
    }

    /// <summary>
    /// Gets expected risk indicators based on financial metrics
    /// </summary>
    public static List<string> GetExpectedRiskIndicators(
        decimal? currentRatio,
        decimal? debtToAssetRatio,
        decimal creditUtilization,
        decimal? revenueGrowth = null,
        decimal? profitGrowth = null)
    {
        var indicators = new List<string>();

        if (currentRatio.HasValue && currentRatio.Value < 1.0m)
            indicators.Add("Low Current Ratio (< 1.0)");
        else if (currentRatio.HasValue && currentRatio.Value < 1.5m)
            indicators.Add("Below Average Current Ratio (< 1.5)");

        if (debtToAssetRatio.HasValue && debtToAssetRatio.Value > 0.8m)
            indicators.Add("High Debt-to-Asset Ratio (> 80%)");
        else if (debtToAssetRatio.HasValue && debtToAssetRatio.Value > 0.6m)
            indicators.Add("Elevated Debt-to-Asset Ratio (> 60%)");

        if (creditUtilization > 90)
            indicators.Add("Very High Credit Utilization (> 90%)");
        else if (creditUtilization > 75)
            indicators.Add("High Credit Utilization (> 75%)");

        if (profitGrowth.HasValue && profitGrowth.Value < -20)
            indicators.Add("Declining Profitability Trend");

        if (revenueGrowth.HasValue && revenueGrowth.Value < -15)
            indicators.Add("Revenue Decline Trend");

        return indicators;
    }

    #endregion

    #region Assertion Helpers

    /// <summary>
    /// Asserts that financial ratios are calculated correctly
    /// </summary>
    public static void AssertFinancialRatiosAreCorrect(
        FinancialReport report,
        decimal? expectedCurrentRatio = null,
        decimal? expectedDebtRatio = null,
        decimal? expectedROE = null,
        decimal? expectedROA = null,
        decimal precision = 0.01m)
    {
        if (expectedCurrentRatio.HasValue)
        {
            report.CurrentRatio.Should().BeApproximately(expectedCurrentRatio.Value, precision);
        }

        if (expectedDebtRatio.HasValue)
        {
            report.DebtToAssetRatio.Should().BeApproximately(expectedDebtRatio.Value, precision);
        }

        if (expectedROE.HasValue)
        {
            report.ROE.Should().BeApproximately(expectedROE.Value, precision);
        }

        if (expectedROA.HasValue)
        {
            report.ROA.Should().BeApproximately(expectedROA.Value, precision);
        }
    }

    /// <summary>
    /// Asserts that financial ratios in DTO are calculated correctly
    /// </summary>
    public static void AssertFinancialRatiosAreCorrect(
        FinancialReportDto dto,
        decimal? expectedCurrentRatio = null,
        decimal? expectedDebtRatio = null,
        decimal? expectedROE = null,
        decimal? expectedROA = null,
        decimal precision = 0.01m)
    {
        if (expectedCurrentRatio.HasValue)
        {
            dto.CurrentRatio.Should().BeApproximately(expectedCurrentRatio.Value, precision);
        }

        if (expectedDebtRatio.HasValue)
        {
            dto.DebtToAssetRatio.Should().BeApproximately(expectedDebtRatio.Value, precision);
        }

        if (expectedROE.HasValue)
        {
            dto.ROE.Should().BeApproximately(expectedROE.Value, precision);
        }

        if (expectedROA.HasValue)
        {
            dto.ROA.Should().BeApproximately(expectedROA.Value, precision);
        }
    }

    /// <summary>
    /// Asserts that growth metrics are calculated correctly
    /// </summary>
    public static void AssertGrowthMetricsAreCorrect(
        FinancialReportDto dto,
        decimal? expectedRevenueGrowth = null,
        decimal? expectedProfitGrowth = null,
        decimal? expectedAssetGrowth = null)
    {
        if (expectedRevenueGrowth.HasValue)
        {
            dto.RevenueGrowth.Should().Be(expectedRevenueGrowth.Value);
        }

        if (expectedProfitGrowth.HasValue)
        {
            dto.NetProfitGrowth.Should().Be(expectedProfitGrowth.Value);
        }

        if (expectedAssetGrowth.HasValue)
        {
            dto.TotalAssetsGrowth.Should().Be(expectedAssetGrowth.Value);
        }
    }

    /// <summary>
    /// Asserts that two financial reports have equivalent data
    /// </summary>
    public static void AssertReportsAreEquivalent(FinancialReport expected, FinancialReport actual)
    {
        actual.TradingPartnerId.Should().Be(expected.TradingPartnerId);
        actual.ReportStartDate.Should().Be(expected.ReportStartDate);
        actual.ReportEndDate.Should().Be(expected.ReportEndDate);
        actual.TotalAssets.Should().Be(expected.TotalAssets);
        actual.TotalLiabilities.Should().Be(expected.TotalLiabilities);
        actual.NetAssets.Should().Be(expected.NetAssets);
        actual.CurrentAssets.Should().Be(expected.CurrentAssets);
        actual.CurrentLiabilities.Should().Be(expected.CurrentLiabilities);
        actual.Revenue.Should().Be(expected.Revenue);
        actual.NetProfit.Should().Be(expected.NetProfit);
        actual.OperatingCashFlow.Should().Be(expected.OperatingCashFlow);
    }

    /// <summary>
    /// Asserts that entity and DTO have equivalent data
    /// </summary>
    public static void AssertEntityAndDtoAreEquivalent(FinancialReport entity, FinancialReportDto dto)
    {
        dto.Id.Should().Be(entity.Id);
        dto.TradingPartnerId.Should().Be(entity.TradingPartnerId);
        dto.ReportStartDate.Should().Be(entity.ReportStartDate);
        dto.ReportEndDate.Should().Be(entity.ReportEndDate);
        dto.TotalAssets.Should().Be(entity.TotalAssets);
        dto.TotalLiabilities.Should().Be(entity.TotalLiabilities);
        dto.NetAssets.Should().Be(entity.NetAssets);
        dto.CurrentAssets.Should().Be(entity.CurrentAssets);
        dto.CurrentLiabilities.Should().Be(entity.CurrentLiabilities);
        dto.Revenue.Should().Be(entity.Revenue);
        dto.NetProfit.Should().Be(entity.NetProfit);
        dto.OperatingCashFlow.Should().Be(entity.OperatingCashFlow);
        dto.CurrentRatio.Should().Be(entity.CurrentRatio);
        dto.DebtToAssetRatio.Should().Be(entity.DebtToAssetRatio);
        dto.ROE.Should().Be(entity.ROE);
        dto.ROA.Should().Be(entity.ROA);
        dto.ReportYear.Should().Be(entity.ReportYear);
        dto.IsAnnualReport.Should().Be(entity.IsAnnualReport);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.CreatedBy.Should().Be(entity.CreatedBy);
    }

    #endregion

    #region Data Validation Helpers

    /// <summary>
    /// Validates that financial data follows business rules
    /// </summary>
    public static void ValidateFinancialDataBusinessRules(
        decimal? totalAssets,
        decimal? totalLiabilities,
        decimal? currentAssets,
        decimal? currentLiabilities)
    {
        if (totalAssets.HasValue && totalAssets.Value < 0)
            throw new ArgumentException("Total assets cannot be negative");

        if (currentAssets.HasValue && currentAssets.Value < 0)
            throw new ArgumentException("Current assets cannot be negative");

        if (totalLiabilities.HasValue && totalLiabilities.Value < 0)
            throw new ArgumentException("Total liabilities cannot be negative");

        if (currentLiabilities.HasValue && currentLiabilities.Value < 0)
            throw new ArgumentException("Current liabilities cannot be negative");

        if (totalAssets.HasValue && currentAssets.HasValue && currentAssets.Value > totalAssets.Value)
            throw new ArgumentException("Current assets cannot exceed total assets");

        if (totalLiabilities.HasValue && currentLiabilities.HasValue && currentLiabilities.Value > totalLiabilities.Value)
            throw new ArgumentException("Current liabilities cannot exceed total liabilities");
    }

    /// <summary>
    /// Validates that report period follows business rules
    /// </summary>
    public static void ValidateReportPeriodBusinessRules(DateTime startDate, DateTime endDate)
    {
        if (startDate >= endDate)
            throw new ArgumentException("Report start date must be before end date");

        if (endDate > DateTime.UtcNow.Date)
            throw new ArgumentException("Report end date cannot be in the future");

        var reportDays = (endDate - startDate).Days;
        if (reportDays > 366)
            throw new ArgumentException("Report period cannot exceed 366 days");

        if (reportDays < 1)
            throw new ArgumentException("Report period must be at least 1 day");
    }

    #endregion

    #region Test Data Helpers

    /// <summary>
    /// Creates a set of financial reports with realistic progression over time
    /// </summary>
    public static List<FinancialReport> CreateProgressiveReports(
        Guid tradingPartnerId,
        int reportCount = 3,
        DateTime? startDate = null)
    {
        var reports = new List<FinancialReport>();
        var baseDate = startDate ?? DateTime.UtcNow.AddYears(-reportCount);
        
        var baseAssets = 10000m;
        var baseRevenue = 15000m;
        var baseProfit = 1000m;

        for (int i = 0; i < reportCount; i++)
        {
            var year = baseDate.Year + i;
            var growthFactor = 1 + (i * 0.1m); // 10% annual growth
            
            var report = new FinancialReport(
                tradingPartnerId,
                new DateTime(year, 1, 1),
                new DateTime(year, 12, 31));

            report.UpdateFinancialPosition(
                totalAssets: baseAssets * growthFactor,
                totalLiabilities: baseAssets * growthFactor * 0.5m,
                netAssets: baseAssets * growthFactor * 0.5m,
                currentAssets: baseAssets * growthFactor * 0.6m,
                currentLiabilities: baseAssets * growthFactor * 0.3m);

            report.UpdatePerformanceData(
                revenue: baseRevenue * growthFactor,
                netProfit: baseProfit * growthFactor,
                operatingCashFlow: baseProfit * growthFactor * 1.2m);

            report.SetCreated($"test.user.{year}", new DateTime(year + 1, 1, 15)); // Created in January following year

            reports.Add(report);
        }

        return reports;
    }

    #endregion

    #region Currency Conversion Helpers (for Trading Partner Analysis)

    /// <summary>
    /// Simulates currency conversion to USD (simplified for testing)
    /// </summary>
    public static decimal ConvertToUsd(decimal amount, string currency)
    {
        return currency.ToUpper() switch
        {
            "USD" => amount,
            "EUR" => amount * 1.1m,
            "GBP" => amount * 1.3m,
            _ => amount
        };
    }

    /// <summary>
    /// Converts quantity to metric tons based on unit
    /// </summary>
    public static decimal ConvertToMetricTons(decimal quantity, string unit, decimal tonBarrelRatio = 7.6m)
    {
        return unit.ToUpper() switch
        {
            "MT" => quantity,
            "BBL" => quantity / tonBarrelRatio,
            _ => quantity
        };
    }

    #endregion
}