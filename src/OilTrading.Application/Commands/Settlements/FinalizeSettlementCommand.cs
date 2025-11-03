using MediatR;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// CQRS Command to finalize a settlement
/// Locks settlement and transitions to Finalized status
/// </summary>
public class FinalizeSettlementCommand : IRequest<Unit>
{
    public Guid SettlementId { get; set; }
    public bool IsPurchaseSettlement { get; set; } = true;
    public string FinalizedBy { get; set; } = "System";
}
