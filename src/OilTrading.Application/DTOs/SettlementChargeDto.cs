using OilTrading.Core.Entities;

namespace OilTrading.Application.DTOs;

/// <summary>
/// Data Transfer Object for SettlementCharge entity.
/// Represents charges associated with contract settlements such as
/// demurrage, despatch, inspection fees, port charges, etc.
/// </summary>
public class SettlementChargeDto
{
    public Guid Id { get; set; }
    public Guid SettlementId { get; set; }
    public string ChargeType { get; set; } = string.Empty;
    public string ChargeTypeDisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime? IncurredDate { get; set; }
    public string? ReferenceDocument { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    // Computed properties for UI
    public string FormattedAmount => $"{Amount:N2} {Currency}";
    public string FormattedIncurredDate => IncurredDate?.ToString("yyyy-MM-dd") ?? "Not specified";
    public bool IsNegativeCharge => Amount < 0;
    public bool IsPositiveCharge => Amount > 0;
}

/// <summary>
/// Simplified DTO for SettlementCharge listings
/// </summary>
public class SettlementChargeListDto
{
    public Guid Id { get; set; }
    public Guid SettlementId { get; set; }
    public string ChargeType { get; set; } = string.Empty;
    public string ChargeTypeDisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime? IncurredDate { get; set; }
    public string? ReferenceDocument { get; set; }
    public DateTime CreatedDate { get; set; }
    public string FormattedAmount => $"{Amount:N2} {Currency}";
    public string ChargeTypeCode => ChargeType;
}

/// <summary>
/// Summary DTO for SettlementCharge for aggregations
/// </summary>
public class SettlementChargeSummaryDto
{
    public Guid Id { get; set; }
    public string ChargeType { get; set; } = string.Empty;
    public string ChargeTypeDisplayName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public string FormattedAmount => $"{Amount:N2} {Currency}";
}

/// <summary>
/// DTO for charge type breakdown and totals
/// </summary>
public class ChargeTypeBreakdownDto
{
    public string ChargeType { get; set; } = string.Empty;
    public string ChargeTypeDisplayName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public int Count { get; set; }
    public decimal AverageAmount { get; set; }
    public string FormattedTotalAmount => $"{TotalAmount:N2} {Currency}";
    public string FormattedAverageAmount => $"{AverageAmount:N2} {Currency}";
}

/// <summary>
/// DTO for charge statistics within a settlement
/// </summary>
public class SettlementChargeStatisticsDto
{
    public Guid SettlementId { get; set; }
    public decimal TotalChargesAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public int TotalChargesCount { get; set; }
    public decimal PositiveChargesTotal { get; set; }
    public decimal NegativeChargesTotal { get; set; }
    public int PositiveChargesCount { get; set; }
    public int NegativeChargesCount { get; set; }
    public ICollection<ChargeTypeBreakdownDto> ChargeTypeBreakdown { get; set; } = new List<ChargeTypeBreakdownDto>();
    
    public string FormattedTotalAmount => $"{TotalChargesAmount:N2} {Currency}";
    public string FormattedPositiveTotal => $"{PositiveChargesTotal:N2} {Currency}";
    public string FormattedNegativeTotal => $"{NegativeChargesTotal:N2} {Currency}";
    public decimal NetCharges => PositiveChargesTotal + NegativeChargesTotal;
    public string FormattedNetCharges => $"{NetCharges:N2} {Currency}";
}