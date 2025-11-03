using MediatR;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// CQRS Command to approve a settlement
/// Transitions settlement from Calculated to Approved status
/// </summary>
public class ApproveSettlementCommand : IRequest<Unit>
{
    public Guid SettlementId { get; set; }
    public bool IsPurchaseSettlement { get; set; } = true;
    public string ApprovedBy { get; set; } = "System";
}
