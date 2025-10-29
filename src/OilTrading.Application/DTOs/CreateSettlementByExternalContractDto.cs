using OilTrading.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace OilTrading.Application.DTOs;

/// <summary>
/// DTO for creating a settlement by specifying the external contract number
/// instead of the internal contract GUID
/// </summary>
public class CreateSettlementByExternalContractDto
{
    /// <summary>
    /// The external contract number (as provided by the trading partner)
    /// </summary>
    [Required(ErrorMessage = "External contract number is required")]
    [StringLength(100, ErrorMessage = "External contract number must not exceed 100 characters")]
    public string ExternalContractNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Contract type to help disambiguate (Purchase or Sales)
    /// </summary>
    [StringLength(50, ErrorMessage = "Expected contract type must not exceed 50 characters")]
    public string? ExpectedContractType { get; set; }

    /// <summary>
    /// Optional: Trading partner ID to help disambiguate
    /// </summary>
    public Guid? TradingPartnerId { get; set; }

    /// <summary>
    /// Optional: Product ID to help disambiguate
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Document number (Bill of Lading, Certificate of Quantity, etc.)
    /// </summary>
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// Type of document (BillOfLading, QuantityCertificate, QualityCertificate, Other)
    /// </summary>
    [Required(ErrorMessage = "Document type is required")]
    public DocumentType DocumentType { get; set; } = DocumentType.BillOfLading;

    /// <summary>
    /// Date of the document
    /// </summary>
    [Required(ErrorMessage = "Document date is required")]
    public DateTime DocumentDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Actual quantity in metric tons from the document
    /// </summary>
    [Required(ErrorMessage = "Actual quantity in MT is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Actual quantity in MT must be greater than or equal to zero")]
    public decimal ActualQuantityMT { get; set; }

    /// <summary>
    /// Actual quantity in barrels from the document
    /// </summary>
    [Required(ErrorMessage = "Actual quantity in BBL is required")]
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
    /// Settlement currency (defaults to USD)
    /// </summary>
    [Required(ErrorMessage = "Settlement currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-character ISO code")]
    public string SettlementCurrency { get; set; } = "USD";

    /// <summary>
    /// Whether to automatically calculate benchmark prices during creation
    /// </summary>
    public bool AutoCalculatePrices { get; set; } = true;

    /// <summary>
    /// Whether to automatically transition to DataEntered status after creation
    /// </summary>
    public bool AutoTransitionStatus { get; set; } = false;
}
