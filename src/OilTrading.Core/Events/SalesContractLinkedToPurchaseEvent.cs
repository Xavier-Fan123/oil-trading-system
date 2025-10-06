namespace OilTrading.Core.Events;

public class SalesContractLinkedToPurchaseEvent : DomainEvent
{
    public Guid SalesContractId { get; }
    public Guid PurchaseContractId { get; }

    public SalesContractLinkedToPurchaseEvent(Guid salesContractId, Guid purchaseContractId)
    {
        SalesContractId = salesContractId;
        PurchaseContractId = purchaseContractId;
    }
}