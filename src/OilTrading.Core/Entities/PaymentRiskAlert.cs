using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

/// <summary>
/// Represents a payment risk alert for a trading partner
/// Used to track and manage credit and payment-related risks
/// </summary>
public class PaymentRiskAlert : BaseEntity
{
    /// <summary>
    /// Foreign key reference to the trading partner this alert is for
    /// </summary>
    public Guid TradingPartnerId { get; set; }

    /// <summary>
    /// Navigation property to the trading partner
    /// </summary>
    public TradingPartner? TradingPartner { get; set; }

    /// <summary>
    /// Type of alert (e.g., OverduePayment, CreditLimitExceeded)
    /// </summary>
    public AlertType AlertType { get; set; }

    /// <summary>
    /// Severity level of the alert
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Alert title/subject
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the alert
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Amount involved in the alert (in USD)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Due date related to the alert (optional)
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Date when the alert was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the alert was resolved (if applicable)
    /// </summary>
    public DateTime? ResolvedDate { get; set; }

    /// <summary>
    /// Whether the alert has been resolved
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// Number of days overdue (if applicable)
    /// </summary>
    public int? DaysOverdue { get; set; }

    /// <summary>
    /// Number of days until due (if applicable)
    /// </summary>
    public int? DaysUntilDue { get; set; }
}

/// <summary>
/// Alert type enumeration
/// Defines different types of payment-related alerts
/// </summary>
public enum AlertType
{
    OverduePayment = 1,
    UpcomingDueDate = 2,
    CreditLimitApproaching = 3,
    CreditLimitExceeded = 4,
    CreditExpired = 5,
    LargeOutstandingAmount = 6,
    FrequentLatePayment = 7
}

/// <summary>
/// Alert severity level enumeration
/// Defines priority/urgency of alerts
/// </summary>
public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}
