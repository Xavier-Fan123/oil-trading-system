using MediatR;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// CQRS Command to create a purchase settlement
/// Creates a new settlement for a purchase contract with document information
/// </summary>
public class CreatePurchaseSettlementCommand : IRequest<Guid>
{
    public Guid PurchaseContractId { get; set; }
    public string ExternalContractNumber { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public DateTime DocumentDate { get; set; }
    public decimal ActualQuantityMT { get; set; }
    public decimal ActualQuantityBBL { get; set; }
    public string? Notes { get; set; }
    public string SettlementCurrency { get; set; } = "USD";
    public bool AutoCalculatePrices { get; set; }
    public bool AutoTransitionStatus { get; set; }
    public string CreatedBy { get; set; } = "System";
}
