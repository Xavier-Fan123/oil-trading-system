namespace OilTrading.Core.Events;

public class PurchaseContractLaycanUpdatedEvent : DomainEvent
{
    public Guid ContractId { get; }
    public DateTime LaycanStart { get; }
    public DateTime LaycanEnd { get; }

    public PurchaseContractLaycanUpdatedEvent(Guid contractId, DateTime laycanStart, DateTime laycanEnd)
    {
        ContractId = contractId;
        LaycanStart = laycanStart;
        LaycanEnd = laycanEnd;
    }
}