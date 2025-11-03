using OilTrading.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace OilTrading.Application.DTOs;

/// <summary>
/// Request DTO for creating a purchase settlement
/// </summary>
public class CreatePurchaseSettlementRequest
{
    [Required(ErrorMessage = "Purchase contract ID is required")]
    public Guid PurchaseContractId { get; set; }

    [StringLength(100, ErrorMessage = "External contract number cannot exceed 100 characters")]
    public string? ExternalContractNumber { get; set; }

    [Required(ErrorMessage = "Document number is required")]
    [StringLength(100, ErrorMessage = "Document number cannot exceed 100 characters")]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Document type is required")]
    public DocumentType DocumentType { get; set; }

    [Required(ErrorMessage = "Document date is required")]
    public DateTime DocumentDate { get; set; }
}

/// <summary>
/// Request DTO for creating a sales settlement
/// </summary>
public class CreateSalesSettlementRequest
{
    [Required(ErrorMessage = "Sales contract ID is required")]
    public Guid SalesContractId { get; set; }

    [StringLength(100, ErrorMessage = "External contract number cannot exceed 100 characters")]
    public string? ExternalContractNumber { get; set; }

    [Required(ErrorMessage = "Document number is required")]
    [StringLength(100, ErrorMessage = "Document number cannot exceed 100 characters")]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Document type is required")]
    public DocumentType DocumentType { get; set; }

    [Required(ErrorMessage = "Document date is required")]
    public DateTime DocumentDate { get; set; }
}

/// <summary>
/// Request DTO for calculating a settlement
/// Shared between purchase and sales settlements
/// </summary>
public class CalculateSettlementRequest
{
    [Range(0, double.MaxValue, ErrorMessage = "Calculation quantity MT must be non-negative")]
    public decimal CalculationQuantityMT { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Calculation quantity BBL must be non-negative")]
    public decimal CalculationQuantityBBL { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Benchmark amount must be non-negative")]
    public decimal BenchmarkAmount { get; set; }

    [Range(double.MinValue, double.MaxValue, ErrorMessage = "Adjustment amount must be a valid number")]
    public decimal AdjustmentAmount { get; set; }

    [StringLength(500, ErrorMessage = "Calculation note cannot exceed 500 characters")]
    public string? CalculationNote { get; set; }
}

/// <summary>
/// Response DTO for purchase settlement creation
/// </summary>
public class CreatePurchaseSettlementResponse
{
    public Guid SettlementId { get; set; }
}

/// <summary>
/// Response DTO for sales settlement creation
/// </summary>
public class CreateSalesSettlementResponse
{
    public Guid SettlementId { get; set; }
}
