namespace OilTrading.Core.Events;

public class PurchaseContractPriceFinalizedEvent : DomainEvent
{
    public Guid ContractId { get; }
    public decimal FinalPrice { get; }
    public string Currency { get; }

    public PurchaseContractPriceFinalizedEvent(Guid contractId, decimal finalPrice, string currency)
    {
        ContractId = contractId;
        FinalPrice = finalPrice;
        Currency = currency;
    }
}