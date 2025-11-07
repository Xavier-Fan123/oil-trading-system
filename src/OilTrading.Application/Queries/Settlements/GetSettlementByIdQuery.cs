using MediatR;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Queries.Settlements;

/// <summary>
/// CQRS Query to retrieve a specific settlement by ID
/// Works for both purchase and sales settlements
/// </summary>
public class GetSettlementByIdQuery : IRequest<SettlementDto?>
{
    public Guid SettlementId { get; set; }
    public bool IsPurchaseSettlement { get; set; } = true;
}

/// <summary>
/// DTO for settlement query responses
/// Combines key settlement data for API responses
/// </summary>
public class SettlementDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ExternalContractNumber { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public DocumentType DocumentType { get; set; }
    public DateTime DocumentDate { get; set; }
    public decimal ActualQuantityMT { get; set; }
    public decimal ActualQuantityBBL { get; set; }
    public decimal CalculationQuantityMT { get; set; }
    public decimal CalculationQuantityBBL { get; set; }
    public decimal BenchmarkPrice { get; set; }
    public decimal BenchmarkAmount { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public decimal CargoValue { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal TotalSettlementAmount { get; set; }
    public string SettlementCurrency { get; set; } = "USD";
    public ContractSettlementStatus Status { get; set; }
    public bool IsFinalized { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? LastModifiedDate { get; set; }
    public string? LastModifiedBy { get; set; }
    public int ChargeCount { get; set; }
}
