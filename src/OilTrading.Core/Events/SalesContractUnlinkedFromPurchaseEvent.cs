namespace OilTrading.Core.Events;

public class SalesContractUnlinkedFromPurchaseEvent : DomainEvent
{
    public Guid SalesContractId { get; }
    public Guid PurchaseContractId { get; }

    public SalesContractUnlinkedFromPurchaseEvent(Guid salesContractId, Guid purchaseContractId)
    {
        SalesContractId = salesContractId;
        PurchaseContractId = purchaseContractId;
    }
}