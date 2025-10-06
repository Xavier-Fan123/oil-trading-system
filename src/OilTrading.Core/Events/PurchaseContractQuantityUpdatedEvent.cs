using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Events;

public class PurchaseContractQuantityUpdatedEvent : DomainEvent
{
    public Guid ContractId { get; }
    public Quantity OldQuantity { get; }
    public Quantity NewQuantity { get; }

    public PurchaseContractQuantityUpdatedEvent(Guid contractId, Quantity oldQuantity, Quantity newQuantity)
    {
        ContractId = contractId;
        OldQuantity = oldQuantity;
        NewQuantity = newQuantity;
    }
}