namespace OilTrading.Core.Events;

public class PurchaseContractCancelledEvent : DomainEvent
{
    public Guid ContractId { get; }
    public string ContractNumber { get; }
    public string Reason { get; }

    public PurchaseContractCancelledEvent(Guid contractId, string contractNumber, string reason)
    {
        ContractId = contractId;
        ContractNumber = contractNumber;
        Reason = reason;
    }
}