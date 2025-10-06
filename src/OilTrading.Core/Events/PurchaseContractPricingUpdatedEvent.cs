namespace OilTrading.Core.Events;

public class PurchaseContractPricingUpdatedEvent : DomainEvent
{
    public Guid ContractId { get; }
    public string PriceFormula { get; }
    public decimal ContractValue { get; }

    public PurchaseContractPricingUpdatedEvent(Guid contractId, string priceFormula, decimal contractValue)
    {
        ContractId = contractId;
        PriceFormula = priceFormula;
        ContractValue = contractValue;
    }
}