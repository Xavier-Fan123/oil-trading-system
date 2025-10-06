using OilTrading.Application.Commands.FinancialReports;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Tests.Builders;

/// <summary>
/// Builder class for creating FinancialReport test data using the Builder pattern.
/// Provides fluent interface for constructing test objects with sensible defaults.
/// </summary>
public class FinancialReportTestDataBuilder
{
    private Guid _tradingPartnerId = Guid.NewGuid();
    private DateTime _reportStartDate = DateTime.UtcNow.AddDays(-90);
    private DateTime _reportEndDate = DateTime.UtcNow.AddDays(-1);
    private decimal? _totalAssets = 10000m;
    private decimal? _totalLiabilities = 5000m;
    private decimal? _netAssets = 5000m;
    private decimal? _currentAssets = 6000m;
    private decimal? _currentLiabilities = 3000m;
    private decimal? _revenue = 15000m;
    private decimal? _netProfit = 1500m;
    private decimal? _operatingCashFlow = 2000m;
    private string _createdBy = "test.user";
    private string? _updatedBy;
    private DateTime _createdAt = DateTime.UtcNow.AddDays(-1);
    private DateTime? _updatedAt;

    public FinancialReportTestDataBuilder WithTradingPartner(Guid tradingPartnerId)
    {
        _tradingPartnerId = tradingPartnerId;
        return this;
    }

    public FinancialReportTestDataBuilder WithReportPeriod(DateTime startDate, DateTime endDate)
    {
        _reportStartDate = startDate;
        _reportEndDate = endDate;
        return this;
    }

    public FinancialReportTestDataBuilder WithQuarterlyPeriod(int year, int quarter)
    {
        var startMonth = (quarter - 1) * 3 + 1;
        _reportStartDate = new DateTime(year, startMonth, 1);
        _reportEndDate = _reportStartDate.AddMonths(3).AddDays(-1);
        return this;
    }

    public FinancialReportTestDataBuilder WithAnnualPeriod(int year)
    {
        _reportStartDate = new DateTime(year, 1, 1);
        _reportEndDate = new DateTime(year, 12, 31);
        return this;
    }

    public FinancialReportTestDataBuilder WithFinancialPosition(
        decimal? totalAssets = null,
        decimal? totalLiabilities = null,
        decimal? netAssets = null,
        decimal? currentAssets = null,
        decimal? currentLiabilities = null)
    {
        if (totalAssets.HasValue) _totalAssets = totalAssets;
        if (totalLiabilities.HasValue) _totalLiabilities = totalLiabilities;
        if (netAssets.HasValue) _netAssets = netAssets;
        if (currentAssets.HasValue) _currentAssets = currentAssets;
        if (currentLiabilities.HasValue) _currentLiabilities = currentLiabilities;
        return this;
    }

    public FinancialReportTestDataBuilder WithPerformanceData(
        decimal? revenue = null,
        decimal? netProfit = null,
        decimal? operatingCashFlow = null)
    {
        if (revenue.HasValue) _revenue = revenue;
        if (netProfit.HasValue) _netProfit = netProfit;
        if (operatingCashFlow.HasValue) _operatingCashFlow = operatingCashFlow;
        return this;
    }

    public FinancialReportTestDataBuilder WithHealthyRatios()
    {
        return WithFinancialPosition(
            totalAssets: 10000m,
            totalLiabilities: 4000m,
            netAssets: 6000m,
            currentAssets: 6000m,
            currentLiabilities: 2000m)
            .WithPerformanceData(
                revenue: 20000m,
                netProfit: 2000m,
                operatingCashFlow: 2500m);
    }

    public FinancialReportTestDataBuilder WithPoorRatios()
    {
        return WithFinancialPosition(
            totalAssets: 10000m,
            totalLiabilities: 9000m,
            netAssets: 1000m,
            currentAssets: 3000m,
            currentLiabilities: 4000m)
            .WithPerformanceData(
                revenue: 8000m,
                netProfit: -500m,
                operatingCashFlow: -200m);
    }

    public FinancialReportTestDataBuilder WithNullValues()
    {
        _totalAssets = null;
        _totalLiabilities = null;
        _netAssets = null;
        _currentAssets = null;
        _currentLiabilities = null;
        _revenue = null;
        _netProfit = null;
        _operatingCashFlow = null;
        return this;
    }

    public FinancialReportTestDataBuilder WithCreatedBy(string createdBy)
    {
        _createdBy = createdBy;
        return this;
    }

    public FinancialReportTestDataBuilder WithUpdatedBy(string updatedBy)
    {
        _updatedBy = updatedBy;
        _updatedAt = DateTime.UtcNow;
        return this;
    }

    public FinancialReportTestDataBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public FinancialReport BuildEntity()
    {
        var entity = new FinancialReport(_tradingPartnerId, _reportStartDate, _reportEndDate);
        
        entity.UpdateFinancialPosition(_totalAssets, _totalLiabilities, _netAssets, _currentAssets, _currentLiabilities);
        entity.UpdatePerformanceData(_revenue, _netProfit, _operatingCashFlow);
        
        entity.SetCreated(_createdBy);
        
        if (_updatedBy != null)
        {
            entity.SetUpdatedBy(_updatedBy);
        }

        return entity;
    }

    public FinancialReport BuildEntityWithId(Guid id)
    {
        var entity = BuildEntity();
        SetEntityId(entity, id);
        return entity;
    }

    public CreateFinancialReportCommand BuildCreateCommand()
    {
        return new CreateFinancialReportCommand
        {
            TradingPartnerId = _tradingPartnerId,
            ReportStartDate = _reportStartDate,
            ReportEndDate = _reportEndDate,
            TotalAssets = _totalAssets,
            TotalLiabilities = _totalLiabilities,
            NetAssets = _netAssets,
            CurrentAssets = _currentAssets,
            CurrentLiabilities = _currentLiabilities,
            Revenue = _revenue,
            NetProfit = _netProfit,
            OperatingCashFlow = _operatingCashFlow,
            CreatedBy = _createdBy
        };
    }

    public UpdateFinancialReportCommand BuildUpdateCommand(Guid id)
    {
        return new UpdateFinancialReportCommand
        {
            Id = id,
            ReportStartDate = _reportStartDate,
            ReportEndDate = _reportEndDate,
            TotalAssets = _totalAssets,
            TotalLiabilities = _totalLiabilities,
            NetAssets = _netAssets,
            CurrentAssets = _currentAssets,
            CurrentLiabilities = _currentLiabilities,
            Revenue = _revenue,
            NetProfit = _netProfit,
            OperatingCashFlow = _operatingCashFlow,
            UpdatedBy = _updatedBy ?? "test.updater"
        };
    }

    public FinancialReportDto BuildDto(Guid? id = null)
    {
        var entity = BuildEntity();
        var reportId = id ?? Guid.NewGuid();
        
        return new FinancialReportDto
        {
            Id = reportId,
            TradingPartnerId = _tradingPartnerId,
            ReportStartDate = _reportStartDate,
            ReportEndDate = _reportEndDate,
            TotalAssets = _totalAssets,
            TotalLiabilities = _totalLiabilities,
            NetAssets = _netAssets,
            CurrentAssets = _currentAssets,
            CurrentLiabilities = _currentLiabilities,
            Revenue = _revenue,
            NetProfit = _netProfit,
            OperatingCashFlow = _operatingCashFlow,
            CurrentRatio = entity.CurrentRatio,
            DebtToAssetRatio = entity.DebtToAssetRatio,
            ROE = entity.ROE,
            ROA = entity.ROA,
            ReportYear = entity.ReportYear,
            IsAnnualReport = entity.IsAnnualReport,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            CreatedBy = _createdBy,
            UpdatedBy = _updatedBy
        };
    }

    /// <summary>
    /// Creates a collection of financial reports for multiple years with growth patterns
    /// </summary>
    public static List<FinancialReport> BuildMultiYearReports(Guid tradingPartnerId, int startYear, int yearCount)
    {
        var reports = new List<FinancialReport>();
        var baseAssets = 8000m;
        var baseRevenue = 10000m;
        var baseProfit = 800m;

        for (int i = 0; i < yearCount; i++)
        {
            var year = startYear + i;
            var growthFactor = 1 + (i * 0.15m); // 15% growth per year

            var report = new FinancialReportTestDataBuilder()
                .WithTradingPartner(tradingPartnerId)
                .WithAnnualPeriod(year)
                .WithFinancialPosition(
                    totalAssets: baseAssets * growthFactor,
                    totalLiabilities: baseAssets * growthFactor * 0.5m,
                    netAssets: baseAssets * growthFactor * 0.5m,
                    currentAssets: baseAssets * growthFactor * 0.6m,
                    currentLiabilities: baseAssets * growthFactor * 0.3m)
                .WithPerformanceData(
                    revenue: baseRevenue * growthFactor,
                    netProfit: baseProfit * growthFactor,
                    operatingCashFlow: baseProfit * growthFactor * 1.2m)
                .WithCreatedBy($"test.user.{year}")
                .BuildEntity();

            reports.Add(report);
        }

        return reports;
    }

    /// <summary>
    /// Creates reports with different risk profiles for testing risk assessment
    /// </summary>
    public static Dictionary<string, FinancialReport> BuildRiskProfileReports(Guid tradingPartnerId)
    {
        var reports = new Dictionary<string, FinancialReport>();

        // Excellent financial health
        reports["Excellent"] = new FinancialReportTestDataBuilder()
            .WithTradingPartner(tradingPartnerId)
            .WithAnnualPeriod(2024)
            .WithFinancialPosition(
                totalAssets: 20000m,
                totalLiabilities: 8000m,
                netAssets: 12000m,
                currentAssets: 14000m,
                currentLiabilities: 4000m)
            .WithPerformanceData(
                revenue: 50000m,
                netProfit: 6000m,
                operatingCashFlow: 7000m)
            .BuildEntity();

        // Poor financial health
        reports["Poor"] = new FinancialReportTestDataBuilder()
            .WithTradingPartner(tradingPartnerId)
            .WithAnnualPeriod(2024)
            .WithFinancialPosition(
                totalAssets: 10000m,
                totalLiabilities: 9500m,
                netAssets: 500m,
                currentAssets: 3000m,
                currentLiabilities: 4000m)
            .WithPerformanceData(
                revenue: 8000m,
                netProfit: -1000m,
                operatingCashFlow: -500m)
            .BuildEntity();

        // Average financial health
        reports["Fair"] = new FinancialReportTestDataBuilder()
            .WithTradingPartner(tradingPartnerId)
            .WithAnnualPeriod(2024)
            .WithFinancialPosition(
                totalAssets: 15000m,
                totalLiabilities: 10000m,
                netAssets: 5000m,
                currentAssets: 8000m,
                currentLiabilities: 6000m)
            .WithPerformanceData(
                revenue: 20000m,
                netProfit: 1000m,
                operatingCashFlow: 1500m)
            .BuildEntity();

        return reports;
    }

    private static void SetEntityId(BaseEntity entity, Guid id)
    {
        var idProperty = typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id));
        idProperty?.SetValue(entity, id);
    }

    /// <summary>
    /// Creates a new builder instance (factory method)
    /// </summary>
    public static FinancialReportTestDataBuilder Create() => new();

    /// <summary>
    /// Creates a builder with default healthy company data
    /// </summary>
    public static FinancialReportTestDataBuilder CreateHealthy() => new FinancialReportTestDataBuilder().WithHealthyRatios();

    /// <summary>
    /// Creates a builder with default poor company data
    /// </summary>
    public static FinancialReportTestDataBuilder CreatePoor() => new FinancialReportTestDataBuilder().WithPoorRatios();
}