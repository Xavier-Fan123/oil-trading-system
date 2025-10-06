using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Events;

public class PurchaseContractCreatedEvent : DomainEvent
{
    public Guid ContractId { get; }
    public string ContractNumber { get; }

    public PurchaseContractCreatedEvent(Guid contractId, string contractNumber)
    {
        ContractId = contractId;
        ContractNumber = contractNumber;
    }
}