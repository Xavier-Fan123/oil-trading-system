using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

public class FinancialReport : BaseEntity
{
    private FinancialReport() { } // For EF Core

    public FinancialReport(
        Guid tradingPartnerId,
        DateTime reportStartDate,
        DateTime reportEndDate)
    {
        TradingPartnerId = tradingPartnerId;
        ReportStartDate = reportStartDate;
        ReportEndDate = reportEndDate;
        
        ValidateReportPeriod();
    }

    public Guid TradingPartnerId { get; private set; }
    public DateTime ReportStartDate { get; private set; }
    public DateTime ReportEndDate { get; private set; }
    
    // Financial Position Data
    public decimal? TotalAssets { get; private set; }
    public decimal? TotalLiabilities { get; private set; }
    public decimal? NetAssets { get; private set; }
    public decimal? CurrentAssets { get; private set; }
    public decimal? CurrentLiabilities { get; private set; }
    
    // Performance Data  
    public decimal? Revenue { get; private set; }
    public decimal? NetProfit { get; private set; }
    public decimal? OperatingCashFlow { get; private set; }
    
    // Navigation Properties
    public TradingPartner TradingPartner { get; set; } = null!;
    
    // Computed Properties (not stored in database)
    public decimal? CurrentRatio => CurrentLiabilities != 0 && CurrentLiabilities.HasValue && CurrentAssets.HasValue 
        ? CurrentAssets.Value / CurrentLiabilities.Value : null;
    
    public decimal? DebtToAssetRatio => TotalAssets != 0 && TotalAssets.HasValue && TotalLiabilities.HasValue 
        ? TotalLiabilities.Value / TotalAssets.Value : null;
    
    public decimal? ROE => NetAssets != 0 && NetAssets.HasValue && NetProfit.HasValue 
        ? NetProfit.Value / NetAssets.Value : null;
    
    public decimal? ROA => TotalAssets != 0 && TotalAssets.HasValue && NetProfit.HasValue 
        ? NetProfit.Value / TotalAssets.Value : null;

    public int ReportYear => ReportStartDate.Year;
    
    public bool IsAnnualReport => ReportEndDate.Subtract(ReportStartDate).Days > 358;

    // Business Methods
    public void UpdateFinancialPosition(
        decimal? totalAssets,
        decimal? totalLiabilities,
        decimal? netAssets,
        decimal? currentAssets,
        decimal? currentLiabilities)
    {
        TotalAssets = totalAssets;
        TotalLiabilities = totalLiabilities;
        NetAssets = netAssets;
        CurrentAssets = currentAssets;
        CurrentLiabilities = currentLiabilities;
        
        ValidateFinancialData();
    }
    
    public void UpdatePerformanceData(
        decimal? revenue,
        decimal? netProfit,
        decimal? operatingCashFlow)
    {
        Revenue = revenue;
        NetProfit = netProfit;
        OperatingCashFlow = operatingCashFlow;
    }
    
    public void UpdateReportPeriod(DateTime startDate, DateTime endDate)
    {
        ReportStartDate = startDate;
        ReportEndDate = endDate;
        ValidateReportPeriod();
    }

    private void ValidateReportPeriod()
    {
        if (ReportStartDate >= ReportEndDate)
            throw new DomainException("Report start date must be before end date");
        
        if (ReportEndDate > DateTime.UtcNow.Date)
            throw new DomainException("Report end date cannot be in the future");
        
        // Validate that the period represents a reasonable reporting period (not too long)
        var reportDays = (ReportEndDate - ReportStartDate).Days;
        if (reportDays > 366) // Allow for leap year
            throw new DomainException("Report period cannot exceed 366 days");
        
        if (reportDays < 1)
            throw new DomainException("Report period must be at least 1 day");
    }
    
    private void ValidateFinancialData()
    {
        // Validate positive values where applicable
        if (TotalAssets.HasValue && TotalAssets.Value < 0)
            throw new DomainException("Total assets cannot be negative");
            
        if (CurrentAssets.HasValue && CurrentAssets.Value < 0)
            throw new DomainException("Current assets cannot be negative");
            
        // Validate logical relationships
        if (TotalAssets.HasValue && CurrentAssets.HasValue && CurrentAssets.Value > TotalAssets.Value)
            throw new DomainException("Current assets cannot exceed total assets");
            
        if (TotalLiabilities.HasValue && CurrentLiabilities.HasValue && CurrentLiabilities.Value > TotalLiabilities.Value)
            throw new DomainException("Current liabilities cannot exceed total liabilities");
    }
}