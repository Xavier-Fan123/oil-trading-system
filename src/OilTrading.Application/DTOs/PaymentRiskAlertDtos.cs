namespace OilTrading.Application.DTOs;

/// <summary>
/// Enum for alert severity levels
/// </summary>
public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3,
}

/// <summary>
/// Enum for alert types
/// </summary>
public enum AlertType
{
    OverduePayment = 1,
    UpcomingDueDate = 2,
    CreditLimitApproaching = 3,
    CreditLimitExceeded = 4,
    CreditExpired = 5,
    LargeOutstandingAmount = 6,
    FrequentLatePayment = 7,
}

/// <summary>
/// DTO for payment risk alert
/// </summary>
public class PaymentRiskAlertDto
{
    public Guid AlertId { get; set; }
    public Guid TradingPartnerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;

    // Alert Details
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Financial Details
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    // Dates
    public DateTime? DueDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public bool IsResolved { get; set; }

    // Additional Context
    public string? SettlementId { get; set; }
    public string? ContractNumber { get; set; }
    public decimal? CreditUtilizationPercentage { get; set; }
    public decimal? CreditLimit { get; set; }
    public int? DaysOverdue { get; set; }
    public int? DaysUntilDue { get; set; }
}

/// <summary>
/// Request DTO for creating a payment risk alert
/// </summary>
public class CreatePaymentRiskAlertRequest
{
    public Guid TradingPartnerId { get; set; }
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime? DueDate { get; set; }
    public string? SettlementId { get; set; }
    public string? ContractNumber { get; set; }
}

/// <summary>
/// Request DTO for resolving a payment risk alert
/// </summary>
public class ResolvePaymentRiskAlertRequest
{
    public Guid AlertId { get; set; }
    public string ResolutionNotes { get; set; } = string.Empty;
}

/// <summary>
/// Summary statistics for payment risk alerts
/// </summary>
public class PaymentRiskAlertSummaryDto
{
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int WarningAlerts { get; set; }
    public int InfoAlerts { get; set; }
    public int UnresolvedAlerts { get; set; }
    public int ResolvedAlerts { get; set; }

    // Financial Summary
    public decimal TotalAmountAtRisk { get; set; }
    public decimal CriticalAmountAtRisk { get; set; }
    public decimal WarningAmountAtRisk { get; set; }

    // Alert Type Breakdown
    public int OverduePaymentCount { get; set; }
    public int UpcomingDueDateCount { get; set; }
    public int CreditLimitExceededCount { get; set; }
    public int CreditLimitApproachingCount { get; set; }
    public int CreditExpiredCount { get; set; }
    public int LargeOutstandingAmountCount { get; set; }
    public int FrequentLatePaymentCount { get; set; }

    // Partner Breakdown
    public int TradingPartnersWithAlerts { get; set; }
    public int TradingPartnersWithCriticalAlerts { get; set; }
}

/// <summary>
/// Request DTO for filtering alerts
/// </summary>
public class PaymentRiskAlertFilterRequest
{
    public Guid? TradingPartnerId { get; set; }
    public AlertType? AlertType { get; set; }
    public AlertSeverity? Severity { get; set; }
    public bool? OnlyUnresolved { get; set; } = true;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
