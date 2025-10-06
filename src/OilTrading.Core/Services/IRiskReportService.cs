using OilTrading.Core.Entities;

namespace OilTrading.Core.Services;

public interface IRiskReportService
{
    Task<RiskReport> GenerateRiskReportAsync(RiskReportRequest request);
    Task<IEnumerable<RiskReport>> GetRiskReportsAsync(int pageNumber = 1, int pageSize = 20);
    Task<RiskReport?> GetRiskReportByIdAsync(int reportId);
    Task<byte[]> GetReportContentAsync(int reportId);
    Task<bool> DeleteRiskReportAsync(int reportId);
    
    Task<RiskReport> GenerateDailyRiskReportAsync(DateTime reportDate);
    Task<RiskReport> GenerateWeeklyRiskReportAsync(DateTime weekEndDate);
    Task<RiskReport> GenerateMonthlyRiskReportAsync(DateTime monthEndDate);
    
    Task<bool> ScheduleAutoReportsAsync();
    Task<IEnumerable<RiskReportTemplate>> GetReportTemplatesAsync();
    Task<bool> DistributeReportAsync(int reportId, string[] emailRecipients);
}

public class RiskReportRequest
{
    public string ReportType { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public RiskReportParameters Parameters { get; set; } = new();
    public string[] EmailRecipients { get; set; } = Array.Empty<string>();
    public bool AutoDistribute { get; set; }
}

public class RiskReportParameters
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string[]? ProductTypes { get; set; }
    public string[]? TradingPartners { get; set; }
    public bool IncludePositions { get; set; } = true;
    public bool IncludeVaRCalculations { get; set; } = true;
    public bool IncludeLimitUtilization { get; set; } = true;
    public bool IncludeStressTesting { get; set; } = true;
    public bool IncludeMarketData { get; set; } = true;
    public string ReportFormat { get; set; } = "PDF"; // PDF, Excel, JSON
    public string ConfidenceLevel { get; set; } = "95,99"; // VaR confidence levels
}

public class RiskReportTemplate
{
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
    public RiskReportParameters DefaultParameters { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string[] DefaultRecipients { get; set; } = Array.Empty<string>();
    public string Schedule { get; set; } = string.Empty; // Cron expression
}