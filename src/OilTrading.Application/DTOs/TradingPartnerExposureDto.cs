using OilTrading.Core.Entities;

namespace OilTrading.Application.DTOs;

/// <summary>
/// DTO for displaying trading partner credit exposure and risk information
/// </summary>
public class TradingPartnerExposureDto
{
    public Guid TradingPartnerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public TradingPartnerType PartnerType { get; set; }

    // Credit Management
    public decimal CreditLimit { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal CurrentExposure { get; set; }
    public decimal CreditUtilizationPercentage { get; set; }

    // Outstanding Amounts
    public decimal OutstandingApAmount { get; set; }  // Accounts Payable (we owe)
    public decimal OutstandingArAmount { get; set; }  // Accounts Receivable (they owe us)
    public decimal NetExposure { get; set; }  // OutstandingAP - OutstandingAR

    // Overdue Information
    public decimal OverdueApAmount { get; set; }
    public decimal OverdueArAmount { get; set; }
    public int OverdueSettlementCount { get; set; }

    // Settlement Statistics
    public int TotalUnpaidSettlements { get; set; }
    public int SettlementsDueIn30Days { get; set; }

    // Risk Assessment
    public RiskLevel RiskLevel { get; set; }
    public string RiskLevelDescription { get; set; } = string.Empty;
    public bool IsOverLimit { get; set; }
    public bool IsCreditExpired { get; set; }

    // Status
    public bool IsActive { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }

    // Timestamps
    public DateTime CreditLimitValidUntil { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public DateTime ExposureCalculatedDate { get; set; }
}

/// <summary>
/// Risk level assessment for trading partners
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Credit utilization < 60%, no overdue amounts
    /// </summary>
    Low = 1,

    /// <summary>
    /// Credit utilization 60-85%, minimal overdue amounts
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Credit utilization 85-100%, moderate overdue amounts
    /// </summary>
    High = 3,

    /// <summary>
    /// Credit utilization > 100%, significant overdue amounts, or credit expired
    /// </summary>
    Critical = 4
}

/// <summary>
/// Settlement exposure details for a specific trading partner
/// </summary>
public class SettlementExposureDetailDto
{
    public Guid SettlementId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string? ExternalContractNumber { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string SettlementStatus { get; set; } = string.Empty;

    // Payment Information
    public DateTime? DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public bool IsOverdue => DaysOverdue > 0;
    public string PaymentStatus { get; set; } = string.Empty;

    // Contract Information
    public string ContractType { get; set; } = string.Empty;  // Purchase or Sales
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Summary of AP (Accounts Payable) and AR (Accounts Receivable) by trading partner
/// </summary>
public class PartnerSettlementSummaryDto
{
    public Guid TradingPartnerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    // AP (Purchase Contracts - we owe)
    public decimal TotalApAmount { get; set; }
    public decimal PaidApAmount { get; set; }
    public decimal UnpaidApAmount { get; set; }
    public int ApSettlementCount { get; set; }

    // AR (Sales Contracts - they owe us)
    public decimal TotalArAmount { get; set; }
    public decimal PaidArAmount { get; set; }
    public decimal UnpaidArAmount { get; set; }
    public int ArSettlementCount { get; set; }

    // Net Position
    public decimal NetAmount { get; set; }  // UnpaidAP - UnpaidAR
    public string NetDirection { get; set; } = string.Empty;  // "We Owe", "They Owe Us", "Balanced"
}

/// <summary>
/// Request DTO for getting trading partner exposure details
/// </summary>
public class GetTradingPartnerExposureRequest
{
    /// <summary>
    /// Include detailed settlement list (can be expensive for large partners)
    /// </summary>
    public bool IncludeDetails { get; set; } = false;

    /// <summary>
    /// Only include unpaid settlements
    /// </summary>
    public bool OnlyUnpaid { get; set; } = true;

    /// <summary>
    /// Filter by settlement type (null = both)
    /// </summary>
    public string? SettlementType { get; set; }  // "Purchase", "Sales", or null for both
}
