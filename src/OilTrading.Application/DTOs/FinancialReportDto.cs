namespace OilTrading.Application.DTOs;

public class FinancialReportDto
{
    public Guid Id { get; set; }
    public Guid TradingPartnerId { get; set; }
    public DateTime ReportStartDate { get; set; }
    public DateTime ReportEndDate { get; set; }
    
    // Financial Position Data
    public decimal? TotalAssets { get; set; }
    public decimal? TotalLiabilities { get; set; }
    public decimal? NetAssets { get; set; }
    public decimal? CurrentAssets { get; set; }
    public decimal? CurrentLiabilities { get; set; }
    
    // Performance Data
    public decimal? Revenue { get; set; }
    public decimal? NetProfit { get; set; }
    public decimal? OperatingCashFlow { get; set; }
    
    // Calculated Ratios
    public decimal? CurrentRatio { get; set; }
    public decimal? DebtToAssetRatio { get; set; }
    public decimal? ROE { get; set; }
    public decimal? ROA { get; set; }
    
    // Year-over-Year Growth Percentages
    public decimal? RevenueGrowth { get; set; }
    public decimal? NetProfitGrowth { get; set; }
    public decimal? TotalAssetsGrowth { get; set; }
    
    // Meta Information
    public int ReportYear { get; set; }
    public bool IsAnnualReport { get; set; }
    
    // Audit Information
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class CreateFinancialReportDto
{
    public Guid TradingPartnerId { get; set; }
    public DateTime ReportStartDate { get; set; }
    public DateTime ReportEndDate { get; set; }
    
    // Financial Position Data
    public decimal? TotalAssets { get; set; }
    public decimal? TotalLiabilities { get; set; }
    public decimal? NetAssets { get; set; }
    public decimal? CurrentAssets { get; set; }
    public decimal? CurrentLiabilities { get; set; }
    
    // Performance Data
    public decimal? Revenue { get; set; }
    public decimal? NetProfit { get; set; }
    public decimal? OperatingCashFlow { get; set; }
}

public class UpdateFinancialReportDto
{
    public DateTime ReportStartDate { get; set; }
    public DateTime ReportEndDate { get; set; }
    
    // Financial Position Data
    public decimal? TotalAssets { get; set; }
    public decimal? TotalLiabilities { get; set; }
    public decimal? NetAssets { get; set; }
    public decimal? CurrentAssets { get; set; }
    public decimal? CurrentLiabilities { get; set; }
    
    // Performance Data
    public decimal? Revenue { get; set; }
    public decimal? NetProfit { get; set; }
    public decimal? OperatingCashFlow { get; set; }
}

public class FinancialReportListDto
{
    public Guid Id { get; set; }
    public Guid TradingPartnerId { get; set; }
    public DateTime ReportStartDate { get; set; }
    public DateTime ReportEndDate { get; set; }
    public int ReportYear { get; set; }
    public bool IsAnnualReport { get; set; }
    
    // Key Financial Metrics for List View
    public decimal? Revenue { get; set; }
    public decimal? NetProfit { get; set; }
    public decimal? TotalAssets { get; set; }
    public decimal? CurrentRatio { get; set; }
    
    // Growth Indicators
    public decimal? RevenueGrowth { get; set; }
    public decimal? NetProfitGrowth { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

public class TradingPartnerAnalysisDto
{
    public Guid TradingPartnerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    
    // Credit Management
    public decimal CreditLimit { get; set; }
    public decimal CurrentExposure { get; set; }
    public decimal CreditUtilization { get; set; }
    
    // Cooperation Volume
    public decimal TotalCooperationAmount { get; set; }
    public decimal TotalCooperationQuantity { get; set; }
    
    // Financial Reports History (sorted by latest first)
    public List<FinancialReportDto> FinancialReports { get; set; } = [];
    
    // Current Financial Health Indicators (from latest report)
    public decimal? CurrentRatio { get; set; }
    public decimal? DebtToAssetRatio { get; set; }
    public decimal? ROE { get; set; }
    public decimal? ROA { get; set; }
    
    // Financial Health Status
    public string FinancialHealthStatus { get; set; } = "Unknown";
    public List<string> RiskIndicators { get; set; } = [];
}

public class CooperationVolumeDto
{
    public decimal TotalCooperationAmount { get; set; }
    public decimal TotalCooperationQuantity { get; set; }
    public int TotalTransactions { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    
    // Breakdown by contract type
    public decimal PurchaseAmount { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal PurchaseQuantity { get; set; }
    public decimal SalesQuantity { get; set; }
}