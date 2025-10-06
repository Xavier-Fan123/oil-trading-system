namespace OilTrading.Core.Events;

public class SalesContractCancelledEvent : DomainEvent
{
    public Guid ContractId { get; }
    public string ContractNumber { get; }
    public string Reason { get; }

    public SalesContractCancelledEvent(Guid contractId, string contractNumber, string reason)
    {
        ContractId = contractId;
        ContractNumber = contractNumber;
        Reason = reason;
    }
}