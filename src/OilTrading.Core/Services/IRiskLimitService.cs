using OilTrading.Core.Entities;

namespace OilTrading.Core.Services;

public interface IRiskLimitService
{
    Task<IEnumerable<RiskLimit>> GetActiveRiskLimitsAsync();
    Task<RiskLimit?> GetRiskLimitByIdAsync(int limitId);
    Task<RiskLimit> CreateRiskLimitAsync(CreateRiskLimitRequest request);
    Task<RiskLimit> UpdateRiskLimitAsync(int limitId, UpdateRiskLimitRequest request);
    Task<bool> DeleteRiskLimitAsync(int limitId);
    
    Task<RiskLimitMonitoringResult> MonitorLimitsAsync();
    Task<IEnumerable<RiskLimitBreach>> GetActiveBreachesAsync();
    Task<RiskLimitBreach> CreateBreachAsync(int limitId, decimal breachAmount);
    Task<bool> ResolveBreachAsync(int breachId, string resolvedBy, string resolution);
    
    Task<IEnumerable<RiskLimit>> GetLimitsByTypeAsync(string limitType);
    Task<IEnumerable<RiskLimit>> GetLimitsByScopeAsync(string limitScope, string scopeValue);
    Task<RiskLimitUtilizationSummary> GetLimitUtilizationSummaryAsync();
}

public class CreateRiskLimitRequest
{
    public string LimitType { get; set; } = string.Empty;
    public string LimitScope { get; set; } = string.Empty;
    public string ScopeValue { get; set; } = string.Empty;
    public decimal MaxLimitAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal WarningThresholdPercentage { get; set; } = 80;
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class UpdateRiskLimitRequest
{
    public decimal? MaxLimitAmount { get; set; }
    public decimal? WarningThresholdPercentage { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public RiskLimitStatus? Status { get; set; }
    public string? Notes { get; set; }
}

public class RiskLimitMonitoringResult
{
    public DateTime MonitoringTime { get; set; }
    public int TotalLimitsChecked { get; set; }
    public int NewBreaches { get; set; }
    public int ResolvedBreaches { get; set; }
    public int ActiveBreaches { get; set; }
    public IEnumerable<RiskLimitBreach> NewBreachDetails { get; set; } = new List<RiskLimitBreach>();
    public IEnumerable<LimitUtilization> HighUtilizationLimits { get; set; } = new List<LimitUtilization>();
}

public class LimitUtilization
{
    public int LimitId { get; set; }
    public string LimitType { get; set; } = string.Empty;
    public string LimitScope { get; set; } = string.Empty;
    public string ScopeValue { get; set; } = string.Empty;
    public decimal MaxLimit { get; set; }
    public decimal CurrentExposure { get; set; }
    public decimal UtilizationPercentage { get; set; }
    public RiskBreachSeverity? BreachLevel { get; set; }
}

public class RiskLimitUtilizationSummary
{
    public DateTime AsOf { get; set; }
    public int TotalLimits { get; set; }
    public int LimitsInWarning { get; set; }
    public int LimitsBreached { get; set; }
    public decimal AverageUtilization { get; set; }
    public decimal HighestUtilization { get; set; }
    public IEnumerable<LimitUtilization> TopUtilizations { get; set; } = new List<LimitUtilization>();
    public IEnumerable<LimitUtilizationByType> UtilizationByType { get; set; } = new List<LimitUtilizationByType>();
}

public class LimitUtilizationByType
{
    public string LimitType { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal AverageUtilization { get; set; }
    public decimal TotalLimit { get; set; }
    public decimal TotalExposure { get; set; }
}