namespace OilTrading.Core.Events;

public class PurchaseContractCompletedEvent : DomainEvent
{
    public Guid ContractId { get; }
    public string ContractNumber { get; }

    public PurchaseContractCompletedEvent(Guid contractId, string contractNumber)
    {
        ContractId = contractId;
        ContractNumber = contractNumber;
    }
}