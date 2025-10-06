using OilTrading.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace OilTrading.Application.DTOs;

/// <summary>
/// DTO for adding a new charge to a contract settlement.
/// Used for adding various types of charges such as demurrage,
/// despatch, inspection fees, port charges, etc.
/// </summary>
public class AddChargeDto
{
    /// <summary>
    /// Settlement ID to add the charge to
    /// </summary>
    [Required]
    public Guid SettlementId { get; set; }

    /// <summary>
    /// Type of charge to add
    /// </summary>
    [Required]
    public ChargeType ChargeType { get; set; }

    /// <summary>
    /// Description of the charge
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Description must be between 1 and 500 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Charge amount (can be positive for costs or negative for credits)
    /// </summary>
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency for the charge amount
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-character ISO code")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Date when the charge was incurred (optional)
    /// </summary>
    public DateTime? IncurredDate { get; set; }

    /// <summary>
    /// Reference document number for the charge (e.g., invoice number, notice number)
    /// </summary>
    [StringLength(100)]
    public string? ReferenceDocument { get; set; }

    /// <summary>
    /// Additional notes about the charge
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// User adding the charge
    /// </summary>
    public string AddedBy { get; set; } = "System";

    /// <summary>
    /// Whether to automatically recalculate settlement totals after adding the charge
    /// </summary>
    public bool AutoRecalculate { get; set; } = true;
}

/// <summary>
/// DTO for updating an existing charge in a contract settlement
/// </summary>
public class UpdateChargeDto
{
    /// <summary>
    /// ID of the charge to update
    /// </summary>
    [Required]
    public Guid ChargeId { get; set; }

    /// <summary>
    /// Settlement ID (for validation)
    /// </summary>
    [Required]
    public Guid SettlementId { get; set; }

    /// <summary>
    /// Updated charge type (optional - if not provided, keeps existing)
    /// </summary>
    public ChargeType? ChargeType { get; set; }

    /// <summary>
    /// Updated description (optional - if not provided, keeps existing)
    /// </summary>
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Description must be between 1 and 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Updated charge amount (optional - if not provided, keeps existing)
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Updated currency (optional - if not provided, keeps existing)
    /// </summary>
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-character ISO code")]
    public string? Currency { get; set; }

    /// <summary>
    /// Updated incurred date (optional - if not provided, keeps existing)
    /// </summary>
    public DateTime? IncurredDate { get; set; }

    /// <summary>
    /// Updated reference document (optional - if not provided, keeps existing)
    /// </summary>
    [StringLength(100)]
    public string? ReferenceDocument { get; set; }

    /// <summary>
    /// Updated notes (optional - if not provided, keeps existing)
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// User updating the charge
    /// </summary>
    public string UpdatedBy { get; set; } = "System";

    /// <summary>
    /// Whether to automatically recalculate settlement totals after updating the charge
    /// </summary>
    public bool AutoRecalculate { get; set; } = true;
}

/// <summary>
/// DTO for bulk charge operations (add multiple charges at once)
/// </summary>
public class BulkAddChargesDto
{
    /// <summary>
    /// Settlement ID to add charges to
    /// </summary>
    [Required]
    public Guid SettlementId { get; set; }

    /// <summary>
    /// List of charges to add
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one charge must be provided")]
    public ICollection<BulkChargeItemDto> Charges { get; set; } = new List<BulkChargeItemDto>();

    /// <summary>
    /// User adding the charges
    /// </summary>
    public string AddedBy { get; set; } = "System";

    /// <summary>
    /// Whether to automatically recalculate settlement totals after adding all charges
    /// </summary>
    public bool AutoRecalculate { get; set; } = true;

    /// <summary>
    /// Whether to continue adding remaining charges if one fails
    /// </summary>
    public bool ContinueOnError { get; set; } = false;
}

/// <summary>
/// Individual charge item for bulk operations
/// </summary>
public class BulkChargeItemDto
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
    [StringLength(500, MinimumLength = 1)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Charge amount
    /// </summary>
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency (defaults to USD)
    /// </summary>
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Date when charge was incurred
    /// </summary>
    public DateTime? IncurredDate { get; set; }

    /// <summary>
    /// Reference document
    /// </summary>
    [StringLength(100)]
    public string? ReferenceDocument { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>
/// Result DTO for charge operations
/// </summary>
public class ChargeOperationResultDto
{
    public bool IsSuccessful { get; set; }
    public Guid? ChargeId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
    public SettlementChargeDto? Charge { get; set; }
    
    /// <summary>
    /// Updated settlement totals after the charge operation
    /// </summary>
    public SettlementTotalsDto? UpdatedTotals { get; set; }
}

/// <summary>
/// Result DTO for bulk charge operations
/// </summary>
public class BulkChargeOperationResultDto
{
    public bool IsSuccessful { get; set; }
    public int SuccessfulCharges { get; set; }
    public int FailedCharges { get; set; }
    public int TotalCharges { get; set; }
    public List<ChargeOperationResultDto> Results { get; set; } = new();
    public List<string> GeneralErrors { get; set; } = new();
    public SettlementTotalsDto? FinalTotals { get; set; }
}

/// <summary>
/// DTO containing settlement totals after charge operations
/// </summary>
public class SettlementTotalsDto
{
    public decimal CargoValue { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal TotalSettlementAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public int ChargesCount { get; set; }
    
    public string FormattedCargoValue => $"{CargoValue:N2} {Currency}";
    public string FormattedTotalCharges => $"{TotalCharges:N2} {Currency}";
    public string FormattedTotalSettlement => $"{TotalSettlementAmount:N2} {Currency}";
}