namespace OilTrading.Core.Events;

public class SalesContractPricingUpdatedEvent : DomainEvent
{
    public Guid ContractId { get; }
    public string PriceFormula { get; }
    public decimal ContractValue { get; }

    public SalesContractPricingUpdatedEvent(Guid contractId, string priceFormula, decimal contractValue)
    {
        ContractId = contractId;
        PriceFormula = priceFormula;
        ContractValue = contractValue;
    }
}