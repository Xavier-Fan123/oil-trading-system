namespace OilTrading.Core.Events;

public class SalesContractActivatedEvent : DomainEvent
{
    public Guid ContractId { get; }
    public string ContractNumber { get; }

    public SalesContractActivatedEvent(Guid contractId, string contractNumber)
    {
        ContractId = contractId;
        ContractNumber = contractNumber;
    }
}