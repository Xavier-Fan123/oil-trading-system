using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

public class RiskLimit : BaseEntity
{
    public string LimitType { get; set; } = string.Empty; // VaR, Position, Exposure, etc.
    public string LimitScope { get; set; } = string.Empty; // Portfolio, Product, TradingPartner, Trader
    public string ScopeValue { get; set; } = string.Empty; // Specific ID or ALL
    public Money MaxLimit { get; set; } = new(0, "USD");
    public Money WarningThreshold { get; set; } = new(0, "USD"); // 80% of limit
    public decimal UtilizationPercentage { get; set; }
    public Money CurrentExposure { get; set; } = new(0, "USD");
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public RiskLimitStatus Status { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime LastCalculatedAt { get; set; }

    // Navigation properties
    public ICollection<RiskLimitBreach> Breaches { get; set; } = new List<RiskLimitBreach>();
}

public class RiskLimitBreach : BaseEntity
{
    public int RiskLimitId { get; set; }
    public RiskLimit RiskLimit { get; set; } = null!;
    public Money BreachAmount { get; set; } = new(0, "USD");
    public decimal BreachPercentage { get; set; }
    public RiskBreachSeverity Severity { get; set; }
    public DateTime BreachTime { get; set; }
    public DateTime? ResolvedTime { get; set; }
    public string? ResolvedBy { get; set; }
    public string? Resolution { get; set; }
    public RiskBreachStatus Status { get; set; }
    public string? NotifiedTo { get; set; }
    public DateTime? NotificationSent { get; set; }
}

public enum RiskLimitStatus
{
    Active = 1,
    Suspended = 2,
    Expired = 3,
    PendingApproval = 4
}

public enum RiskBreachSeverity
{
    Warning = 1,    // 80-100% of limit
    Minor = 2,      // 100-120% of limit
    Major = 3,      // 120-150% of limit
    Critical = 4    // >150% of limit
}

public enum RiskBreachStatus
{
    Open = 1,
    Acknowledged = 2,
    Resolved = 3,
    Escalated = 4
}