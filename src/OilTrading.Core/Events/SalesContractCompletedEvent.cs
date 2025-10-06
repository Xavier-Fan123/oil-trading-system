namespace OilTrading.Core.Events;

public class SalesContractCompletedEvent : DomainEvent
{
    public Guid ContractId { get; }
    public string ContractNumber { get; }

    public SalesContractCompletedEvent(Guid contractId, string contractNumber)
    {
        ContractId = contractId;
        ContractNumber = contractNumber;
    }
}