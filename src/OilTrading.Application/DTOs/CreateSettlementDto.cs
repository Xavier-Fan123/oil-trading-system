using OilTrading.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace OilTrading.Application.DTOs;

/// <summary>
/// DTO for creating a new contract settlement.
/// Contains the essential information needed to initialize a settlement
/// from Bill of Lading or Certificate of Quantity data.
/// </summary>
public class CreateSettlementDto
{
    /// <summary>
    /// Contract ID (Purchase or Sales contract)
    /// </summary>
    [Required]
    public Guid ContractId { get; set; }

    /// <summary>
    /// Document number (B/L number, CQ number, etc.)
    /// </summary>
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// Type of document (BillOfLading, QuantityCertificate, QualityCertificate, Other)
    /// </summary>
    [Required]
    public DocumentType DocumentType { get; set; } = DocumentType.BillOfLading;

    /// <summary>
    /// Date of the document
    /// </summary>
    [Required]
    public DateTime DocumentDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Actual quantity in metric tons from the document
    /// </summary>
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Actual quantity in MT must be greater than or equal to zero")]
    public decimal ActualQuantityMT { get; set; }

    /// <summary>
    /// Actual quantity in barrels from the document
    /// </summary>
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Actual quantity in BBL must be greater than or equal to zero")]
    public decimal ActualQuantityBBL { get; set; }

    /// <summary>
    /// User creating the settlement
    /// </summary>
    public string CreatedBy { get; set; } = "System";

    /// <summary>
    /// Optional notes for the settlement creation
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Optional initial exchange rate if different from default
    /// </summary>
    [Range(0.0001, double.MaxValue, ErrorMessage = "Exchange rate must be greater than zero")]
    public decimal? ExchangeRate { get; set; }

    /// <summary>
    /// Note about the exchange rate
    /// </summary>
    public string? ExchangeRateNote { get; set; }

    /// <summary>
    /// Settlement currency (defaults to USD)
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-character ISO code")]
    public string SettlementCurrency { get; set; } = "USD";

    /// <summary>
    /// Override calculation quantities if different from actual quantities
    /// </summary>
    public decimal? OverrideCalculationQuantityMT { get; set; }

    /// <summary>
    /// Override calculation quantities if different from actual quantities
    /// </summary>
    public decimal? OverrideCalculationQuantityBBL { get; set; }

    /// <summary>
    /// Note explaining quantity calculation override
    /// </summary>
    public string? QuantityCalculationNote { get; set; }

    /// <summary>
    /// Whether to automatically calculate benchmark prices during creation
    /// </summary>
    public bool AutoCalculatePrices { get; set; } = true;

    /// <summary>
    /// Whether to automatically transition to DataEntered status after creation
    /// </summary>
    public bool AutoTransitionStatus { get; set; } = false;
}

/// <summary>
/// DTO for creating a settlement with additional context information
/// </summary>
public class CreateSettlementWithContextDto : CreateSettlementDto
{
    /// <summary>
    /// External contract number for validation
    /// </summary>
    public string? ExternalContractNumber { get; set; }

    /// <summary>
    /// Contract number for validation
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Expected contract type (Purchase/Sales) for validation
    /// </summary>
    public string? ExpectedContractType { get; set; }

    /// <summary>
    /// Trading partner information for validation
    /// </summary>
    public Guid? TradingPartnerId { get; set; }

    /// <summary>
    /// Product information for validation
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Initial charges to add during settlement creation
    /// </summary>
    public ICollection<CreateInitialChargeDto> InitialCharges { get; set; } = new List<CreateInitialChargeDto>();
}

/// <summary>
/// DTO for creating an initial charge during settlement creation
/// </summary>
public class CreateInitialChargeDto
{
    /// <summary>
    /// Type of charge
    /// </summary>
    [Required]
    public ChargeType ChargeType { get; set; }

    /// <summary>
    /// Charge description
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Description must be between 1 and 500 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Charge amount (can be positive or negative)
    /// </summary>
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Charge currency (defaults to settlement currency)
    /// </summary>
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-character ISO code")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Date when the charge was incurred
    /// </summary>
    public DateTime? IncurredDate { get; set; }

    /// <summary>
    /// Reference document for the charge
    /// </summary>
    [StringLength(100)]
    public string? ReferenceDocument { get; set; }

    /// <summary>
    /// Additional notes about the charge
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>
/// Result DTO for settlement creation operations
/// </summary>
public class CreateSettlementResultDto
{
    public bool IsSuccessful { get; set; }
    public Guid? SettlementId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
    public ContractSettlementDto? Settlement { get; set; }
    
    /// <summary>
    /// Information about what was calculated during creation
    /// </summary>
    public SettlementCalculationSummaryDto? CalculationSummary { get; set; }
}

/// <summary>
/// Summary of calculations performed during settlement creation
/// </summary>
public class SettlementCalculationSummaryDto
{
    public bool PricesCalculated { get; set; }
    public bool QuantitiesCalculated { get; set; }
    public bool ChargesCalculated { get; set; }
    public decimal BenchmarkPrice { get; set; }
    public string? BenchmarkPriceFormula { get; set; }
    public DateTime? PricingPeriodStart { get; set; }
    public DateTime? PricingPeriodEnd { get; set; }
    public decimal CalculationQuantityMT { get; set; }
    public decimal CalculationQuantityBBL { get; set; }
    public string? QuantityCalculationNote { get; set; }
    public int InitialChargesAdded { get; set; }
    public decimal TotalInitialCharges { get; set; }
    public string Currency { get; set; } = "USD";
}