namespace OilTrading.Core.Events;

public class PurchaseContractActivatedEvent : DomainEvent
{
    public Guid ContractId { get; }
    public string ContractNumber { get; }

    public PurchaseContractActivatedEvent(Guid contractId, string contractNumber)
    {
        ContractId = contractId;
        ContractNumber = contractNumber;
    }
}