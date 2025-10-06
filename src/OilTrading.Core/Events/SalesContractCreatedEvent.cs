namespace OilTrading.Core.Events;

public class SalesContractCreatedEvent : DomainEvent
{
    public Guid ContractId { get; }
    public string ContractNumber { get; }

    public SalesContractCreatedEvent(Guid contractId, string contractNumber)
    {
        ContractId = contractId;
        ContractNumber = contractNumber;
    }
}