using OilTrading.Core.Entities;

namespace OilTrading.Application.DTOs;

/// <summary>
/// Data Transfer Object for ContractSettlement entity.
/// Contains all settlement information including contract references,
/// document details, quantities, pricing, charges, and calculation results.
/// </summary>
public class ContractSettlementDto
{
    public Guid Id { get; set; }
    
    // Contract reference information
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ExternalContractNumber { get; set; } = string.Empty;
    
    // Document information (B/L or CQ)
    public string? DocumentNumber { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    
    // Actual quantities from B/L or CQ
    public decimal ActualQuantityMT { get; set; }
    public decimal ActualQuantityBBL { get; set; }
    
    // Calculation quantities (may differ based on calculation mode)
    public decimal CalculationQuantityMT { get; set; }
    public decimal CalculationQuantityBBL { get; set; }
    public string? QuantityCalculationNote { get; set; }
    
    // Price information (from market data)
    public decimal BenchmarkPrice { get; set; }
    public string? BenchmarkPriceFormula { get; set; }
    public DateTime? PricingStartDate { get; set; }
    public DateTime? PricingEndDate { get; set; }
    public string BenchmarkPriceCurrency { get; set; } = "USD";
    
    // Calculation results
    public decimal BenchmarkAmount { get; set; }    // Benchmark price calculation
    public decimal AdjustmentAmount { get; set; }   // Adjustment price calculation
    public decimal CargoValue { get; set; }         // Subtotal: benchmark + adjustment
    public decimal TotalCharges { get; set; }       // Sum of all charges
    public decimal TotalSettlementAmount { get; set; } // Final settlement amount
    public string SettlementCurrency { get; set; } = "USD";
    
    // Exchange rate handling
    public decimal? ExchangeRate { get; set; }
    public string? ExchangeRateNote { get; set; }
    
    // Status management
    public string Status { get; set; } = string.Empty;
    public bool IsFinalized { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? LastModifiedBy { get; set; }
    public DateTime? FinalizedDate { get; set; }
    public string? FinalizedBy { get; set; }

    // Data Lineage - Deal Reference ID & Amendment Chain
    public string? DealReferenceId { get; set; }
    public Guid? PreviousSettlementId { get; set; }
    public Guid? OriginalSettlementId { get; set; }
    public int SettlementSequence { get; set; } = 1;
    public string AmendmentType { get; set; } = "Initial";
    public string? AmendmentReason { get; set; }
    public bool IsLatestVersion { get; set; } = true;
    public DateTime? SupersededDate { get; set; }
    
    // Navigation properties
    public PurchaseContractSummaryDto? PurchaseContract { get; set; }
    public SalesContractSummaryDto? SalesContract { get; set; }
    public ICollection<SettlementChargeDto> Charges { get; set; } = new List<SettlementChargeDto>();
    
    // Computed properties for UI
    public bool CanBeModified => !IsFinalized && Status != "Finalized";
    public bool RequiresRecalculation => Status == "Draft" && (BenchmarkAmount == 0 || CalculationQuantityMT == 0);
    public decimal NetCharges => Charges.Sum(c => c.Amount);
    public string DisplayStatus => IsFinalized ? "Finalized" : Status;
    public string FormattedTotalAmount => $"{TotalSettlementAmount:N2} {SettlementCurrency}";
    public string FormattedCargoValue => $"{CargoValue:N2} {SettlementCurrency}";
    public string FormattedTotalCharges => $"{TotalCharges:N2} {SettlementCurrency}";
}

/// <summary>
/// Simplified DTO for ContractSettlement listings and summaries
/// </summary>
public class ContractSettlementListDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ExternalContractNumber { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public decimal ActualQuantityMT { get; set; }
    public decimal ActualQuantityBBL { get; set; }
    public decimal TotalSettlementAmount { get; set; }
    public string SettlementCurrency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public bool IsFinalized { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int ChargesCount { get; set; }
    public string FormattedAmount => $"{TotalSettlementAmount:N2} {SettlementCurrency}";
    public string DisplayStatus => IsFinalized ? "Finalized" : Status;
}

/// <summary>
/// Summary DTO for ContractSettlement for use in other entities
/// </summary>
public class ContractSettlementSummaryDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public decimal TotalSettlementAmount { get; set; }
    public string SettlementCurrency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public bool IsFinalized { get; set; }
    public DateTime DocumentDate { get; set; }
    public string FormattedAmount => $"{TotalSettlementAmount:N2} {SettlementCurrency}";
}