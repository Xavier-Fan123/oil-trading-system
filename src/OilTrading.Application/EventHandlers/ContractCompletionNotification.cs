using MediatR;

namespace OilTrading.Application.EventHandlers;

/// <summary>
/// MediatR notification wrapper for purchase contract completion
/// Bridges domain events to MediatR notification system for auto-settlement
/// </summary>
public class PurchaseContractCompletionNotification : INotification
{
    public Guid ContractId { get; }
    public string ContractNumber { get; }

    public PurchaseContractCompletionNotification(Guid contractId, string contractNumber)
    {
        ContractId = contractId;
        ContractNumber = contractNumber;
    }
}

/// <summary>
/// MediatR notification wrapper for sales contract completion
/// Bridges domain events to MediatR notification system for auto-settlement
/// </summary>
public class SalesContractCompletionNotification : INotification
{
    public Guid ContractId { get; }
    public string ContractNumber { get; }

    public SalesContractCompletionNotification(Guid contractId, string contractNumber)
    {
        ContractId = contractId;
        ContractNumber = contractNumber;
    }
}
