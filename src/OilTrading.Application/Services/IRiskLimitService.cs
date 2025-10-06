using OilTrading.Application.DTOs;

namespace OilTrading.Application.Services;

public interface IRiskLimitService
{
    Task<bool> CheckRiskLimitsAsync(Guid contractId, decimal contractValue);
    Task<List<RiskLimitViolation>> GetRiskLimitViolationsAsync(Guid contractId, decimal contractValue);
    Task<RiskLimitCheckResult> ValidateRiskLimitsAsync(RiskLimitRequest request);
}

public class RiskLimitCheckResult
{
    public bool IsWithinLimits { get; set; }
    public List<RiskLimitViolation> Violations { get; set; } = new();
    public decimal TotalExposure { get; set; }
    public decimal RemainingLimit { get; set; }
    public string? ApprovalRequired { get; set; }
}

public class RiskLimitRequest
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public decimal ExposureAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime ValuationDate { get; set; }
}