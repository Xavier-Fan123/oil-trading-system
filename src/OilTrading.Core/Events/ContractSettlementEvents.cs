using OilTrading.Core.Entities;

namespace OilTrading.Core.Events;

public class ContractSettlementCreatedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public Guid ContractId { get; }
    public string ExternalContractNumber { get; }

    public ContractSettlementCreatedEvent(Guid settlementId, Guid contractId, string externalContractNumber)
    {
        SettlementId = settlementId;
        ContractId = contractId;
        ExternalContractNumber = externalContractNumber;
    }
}

public class ContractSettlementQuantitiesUpdatedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public decimal ActualQuantityMT { get; }
    public decimal ActualQuantityBBL { get; }

    public ContractSettlementQuantitiesUpdatedEvent(Guid settlementId, decimal actualQuantityMT, decimal actualQuantityBBL)
    {
        SettlementId = settlementId;
        ActualQuantityMT = actualQuantityMT;
        ActualQuantityBBL = actualQuantityBBL;
    }
}

public class ContractSettlementCalculationQuantitiesUpdatedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public decimal CalculationQuantityMT { get; }
    public decimal CalculationQuantityBBL { get; }
    public string CalculationNote { get; }

    public ContractSettlementCalculationQuantitiesUpdatedEvent(
        Guid settlementId, 
        decimal calculationQuantityMT, 
        decimal calculationQuantityBBL, 
        string calculationNote)
    {
        SettlementId = settlementId;
        CalculationQuantityMT = calculationQuantityMT;
        CalculationQuantityBBL = calculationQuantityBBL;
        CalculationNote = calculationNote;
    }
}

public class ContractSettlementBenchmarkPriceUpdatedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public decimal BenchmarkPrice { get; }
    public string PriceFormula { get; }

    public ContractSettlementBenchmarkPriceUpdatedEvent(Guid settlementId, decimal benchmarkPrice, string priceFormula)
    {
        SettlementId = settlementId;
        BenchmarkPrice = benchmarkPrice;
        PriceFormula = priceFormula;
    }
}

public class ContractSettlementRecalculatedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public decimal TotalAmount { get; }

    public ContractSettlementRecalculatedEvent(Guid settlementId, decimal totalAmount)
    {
        SettlementId = settlementId;
        TotalAmount = totalAmount;
    }
}

public class ContractSettlementAmountsUpdatedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public decimal TotalSettlementAmount { get; }

    public ContractSettlementAmountsUpdatedEvent(Guid settlementId, decimal totalSettlementAmount)
    {
        SettlementId = settlementId;
        TotalSettlementAmount = totalSettlementAmount;
    }
}

public class ContractSettlementChargeAddedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public Guid ChargeId { get; }
    public ChargeType ChargeType { get; }
    public decimal Amount { get; }

    public ContractSettlementChargeAddedEvent(Guid settlementId, Guid chargeId, ChargeType chargeType, decimal amount)
    {
        SettlementId = settlementId;
        ChargeId = chargeId;
        ChargeType = chargeType;
        Amount = amount;
    }
}

public class ContractSettlementChargeRemovedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public Guid ChargeId { get; }

    public ContractSettlementChargeRemovedEvent(Guid settlementId, Guid chargeId)
    {
        SettlementId = settlementId;
        ChargeId = chargeId;
    }
}

public class ContractSettlementStatusChangedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public ContractSettlementStatus PreviousStatus { get; }
    public ContractSettlementStatus NewStatus { get; }
    public string ChangedBy { get; }

    public ContractSettlementStatusChangedEvent(
        Guid settlementId, 
        ContractSettlementStatus previousStatus, 
        ContractSettlementStatus newStatus, 
        string changedBy)
    {
        SettlementId = settlementId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
    }
}

public class ContractSettlementFinalizedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public decimal TotalAmount { get; }
    public DateTime FinalizedDate { get; }

    public ContractSettlementFinalizedEvent(Guid settlementId, decimal totalAmount, DateTime finalizedDate)
    {
        SettlementId = settlementId;
        TotalAmount = totalAmount;
        FinalizedDate = finalizedDate;
    }
}

public class ContractSettlementExchangeRateUpdatedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public decimal ExchangeRate { get; }
    public string Note { get; }

    public ContractSettlementExchangeRateUpdatedEvent(Guid settlementId, decimal exchangeRate, string note)
    {
        SettlementId = settlementId;
        ExchangeRate = exchangeRate;
        Note = note;
    }
}