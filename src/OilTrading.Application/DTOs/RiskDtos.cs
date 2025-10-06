namespace OilTrading.Application.DTOs;

public class ContractRiskImpact
{
    public decimal VaRImpact { get; set; }
    public decimal ExposureImpact { get; set; }
    public decimal ConcentrationImpact { get; set; }
    public decimal TotalRiskImpact { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public List<string> Warnings { get; set; } = new();
}

public class RiskLimitViolation
{
    public string LimitName { get; set; } = string.Empty;
    public string LimitType { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal LimitValue { get; set; }
    public decimal ExcessAmount { get; set; }
    public string Severity { get; set; } = "Low";
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}