using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Events;

namespace OilTrading.Core.Entities;

public class ContractSettlement : BaseEntity
{
    private ContractSettlement() { } // For EF Core

    public ContractSettlement(
        Guid contractId,
        string contractNumber,
        string externalContractNumber,
        string? documentNumber = null,
        DocumentType documentType = DocumentType.BillOfLading,
        DateTime? documentDate = null,
        string createdBy = "System")
    {
        ContractId = contractId;
        ContractNumber = contractNumber ?? throw new ArgumentNullException(nameof(contractNumber));
        ExternalContractNumber = externalContractNumber ?? throw new ArgumentNullException(nameof(externalContractNumber));
        DocumentNumber = documentNumber;
        DocumentType = documentType;
        DocumentDate = documentDate ?? DateTime.UtcNow;
        Status = ContractSettlementStatus.Draft;
        CreatedDate = DateTime.UtcNow;
        CreatedBy = createdBy;
        
        // Initialize collection
        Charges = new List<SettlementCharge>();
        
        AddDomainEvent(new ContractSettlementCreatedEvent(Id, ContractId, externalContractNumber));
    }

    // Contract reference
    public Guid ContractId { get; private set; }
    public string ContractNumber { get; private set; } = string.Empty;
    public string ExternalContractNumber { get; private set; } = string.Empty;
    
    // Document information (B/L or CQ)
    public string? DocumentNumber { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public DateTime DocumentDate { get; private set; }
    
    // Actual quantities from B/L or CQ
    public decimal ActualQuantityMT { get; private set; }
    public decimal ActualQuantityBBL { get; private set; }
    
    // Calculation quantities (may differ based on calculation mode)
    public decimal CalculationQuantityMT { get; private set; }
    public decimal CalculationQuantityBBL { get; private set; }
    public string? QuantityCalculationNote { get; private set; }
    
    // Price information (from market data)
    public decimal BenchmarkPrice { get; private set; }
    public string? BenchmarkPriceFormula { get; private set; }
    public DateTime? PricingStartDate { get; private set; }
    public DateTime? PricingEndDate { get; private set; }
    public string BenchmarkPriceCurrency { get; private set; } = "USD";
    
    // Calculation results
    public decimal BenchmarkAmount { get; private set; }    // Benchmark price calculation
    public decimal AdjustmentAmount { get; private set; }   // Adjustment price calculation
    public decimal CargoValue { get; private set; }         // Subtotal: benchmark + adjustment
    public decimal TotalCharges { get; private set; }       // Sum of all charges
    public decimal TotalSettlementAmount { get; private set; } // Final settlement amount
    public string SettlementCurrency { get; private set; } = "USD";
    
    // Exchange rate handling
    public decimal? ExchangeRate { get; private set; }
    public string? ExchangeRateNote { get; private set; }
    
    // Status management
    public ContractSettlementStatus Status { get; private set; }
    public bool IsFinalized { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? LastModifiedDate { get; private set; }
    public new string CreatedBy { get; private set; } = string.Empty;
    public string? LastModifiedBy { get; private set; }
    public DateTime? FinalizedDate { get; private set; }
    public string? FinalizedBy { get; private set; }
    
    // Navigation properties
    public PurchaseContract? PurchaseContract { get; private set; }
    public SalesContract? SalesContract { get; private set; }
    public ICollection<SettlementCharge> Charges { get; private set; } = new List<SettlementCharge>();

    // Business methods
    public void UpdateActualQuantities(decimal actualMT, decimal actualBBL, string updatedBy)
    {
        if (IsFinalized)
            throw new DomainException("Cannot update quantities for finalized settlement");

        if (actualMT < 0 || actualBBL < 0)
            throw new DomainException("Quantities cannot be negative");

        ActualQuantityMT = actualMT;
        ActualQuantityBBL = actualBBL;
        LastModifiedDate = DateTime.UtcNow;
        LastModifiedBy = updatedBy;
        
        AddDomainEvent(new ContractSettlementQuantitiesUpdatedEvent(Id, actualMT, actualBBL));
    }

    public void SetCalculationQuantities(
        decimal calculationMT, 
        decimal calculationBBL, 
        string calculationNote,
        string updatedBy)
    {
        if (IsFinalized)
            throw new DomainException("Cannot update calculation quantities for finalized settlement");

        CalculationQuantityMT = calculationMT;
        CalculationQuantityBBL = calculationBBL;
        QuantityCalculationNote = calculationNote;
        LastModifiedDate = DateTime.UtcNow;
        LastModifiedBy = updatedBy;
        
        AddDomainEvent(new ContractSettlementCalculationQuantitiesUpdatedEvent(Id, calculationMT, calculationBBL, calculationNote));
    }

    public void UpdateBenchmarkPrice(
        decimal benchmarkPrice,
        string priceFormula,
        DateTime pricingStart,
        DateTime pricingEnd,
        string currency,
        string updatedBy)
    {
        if (IsFinalized)
            throw new DomainException("Cannot update benchmark price for finalized settlement");

        if (benchmarkPrice < 0)
            throw new DomainException("Benchmark price cannot be negative");

        BenchmarkPrice = benchmarkPrice;
        BenchmarkPriceFormula = priceFormula;
        PricingStartDate = pricingStart;
        PricingEndDate = pricingEnd;
        BenchmarkPriceCurrency = currency;
        LastModifiedDate = DateTime.UtcNow;
        LastModifiedBy = updatedBy;
        
        // Trigger recalculation
        RecalculateAmounts();
        
        AddDomainEvent(new ContractSettlementBenchmarkPriceUpdatedEvent(Id, benchmarkPrice, priceFormula));
    }

    public void RecalculateAmounts()
    {
        // This will be implemented by the settlement calculation service
        // For now, just update the modification timestamp
        LastModifiedDate = DateTime.UtcNow;
        
        AddDomainEvent(new ContractSettlementRecalculatedEvent(Id, TotalSettlementAmount));
    }

    public void UpdateCalculationResults(
        decimal benchmarkAmount,
        decimal adjustmentAmount,
        decimal cargoValue,
        string updatedBy)
    {
        if (IsFinalized)
            throw new DomainException("Cannot update calculation results for finalized settlement");

        BenchmarkAmount = benchmarkAmount;
        AdjustmentAmount = adjustmentAmount;
        CargoValue = cargoValue;
        
        // Recalculate total charges
        TotalCharges = Charges.Sum(c => c.Amount);
        TotalSettlementAmount = CargoValue + TotalCharges;
        
        LastModifiedDate = DateTime.UtcNow;
        LastModifiedBy = updatedBy;
        
        AddDomainEvent(new ContractSettlementAmountsUpdatedEvent(Id, TotalSettlementAmount));
    }

    public SettlementCharge AddCharge(
        ChargeType chargeType,
        string description,
        decimal amount,
        string currency = "USD",
        DateTime? incurredDate = null,
        string? referenceDocument = null,
        string? notes = null,
        string addedBy = "System")
    {
        if (IsFinalized)
            throw new DomainException("Cannot add charges to finalized settlement");

        if (amount < 0)
            throw new DomainException("Charge amount cannot be negative");

        var charge = new SettlementCharge(
            Id,
            chargeType,
            description,
            amount,
            currency,
            incurredDate,
            referenceDocument,
            notes,
            addedBy);

        Charges.Add(charge);
        
        // Recalculate totals
        TotalCharges = Charges.Sum(c => c.Amount);
        TotalSettlementAmount = CargoValue + TotalCharges;
        
        LastModifiedDate = DateTime.UtcNow;
        LastModifiedBy = addedBy;
        
        AddDomainEvent(new ContractSettlementChargeAddedEvent(Id, charge.Id, chargeType, amount));
        
        return charge;
    }

    public void RemoveCharge(Guid chargeId, string removedBy)
    {
        if (IsFinalized)
            throw new DomainException("Cannot remove charges from finalized settlement");

        var charge = Charges.FirstOrDefault(c => c.Id == chargeId);
        if (charge == null)
            throw new DomainException($"Charge with ID {chargeId} not found");

        Charges.Remove(charge);
        
        // Recalculate totals
        TotalCharges = Charges.Sum(c => c.Amount);
        TotalSettlementAmount = CargoValue + TotalCharges;
        
        LastModifiedDate = DateTime.UtcNow;
        LastModifiedBy = removedBy;
        
        AddDomainEvent(new ContractSettlementChargeRemovedEvent(Id, chargeId));
    }

    public void UpdateStatus(ContractSettlementStatus newStatus, string updatedBy)
    {
        var previousStatus = Status;
        Status = newStatus;
        LastModifiedDate = DateTime.UtcNow;
        LastModifiedBy = updatedBy;
        
        AddDomainEvent(new ContractSettlementStatusChangedEvent(Id, previousStatus, newStatus, updatedBy));
    }

    public void Finalize(string finalizedBy)
    {
        if (IsFinalized)
            throw new DomainException("Settlement is already finalized");

        if (Status != ContractSettlementStatus.Calculated)
            throw new DomainException($"Cannot finalize settlement in {Status} status. Must be in Calculated status.");

        if (TotalSettlementAmount == 0)
            throw new DomainException("Cannot finalize settlement with zero amount");

        IsFinalized = true;
        Status = ContractSettlementStatus.Finalized;
        FinalizedDate = DateTime.UtcNow;
        FinalizedBy = finalizedBy;
        LastModifiedDate = DateTime.UtcNow;
        LastModifiedBy = finalizedBy;
        
        AddDomainEvent(new ContractSettlementFinalizedEvent(Id, TotalSettlementAmount, FinalizedDate.Value));
    }

    public void SetExchangeRate(decimal exchangeRate, string note, string updatedBy)
    {
        if (IsFinalized)
            throw new DomainException("Cannot update exchange rate for finalized settlement");

        if (exchangeRate <= 0)
            throw new DomainException("Exchange rate must be greater than zero");

        ExchangeRate = exchangeRate;
        ExchangeRateNote = note;
        LastModifiedDate = DateTime.UtcNow;
        LastModifiedBy = updatedBy;
        
        AddDomainEvent(new ContractSettlementExchangeRateUpdatedEvent(Id, exchangeRate, note));
    }

    public bool CanBeModified() => !IsFinalized && Status != ContractSettlementStatus.Finalized;

    public bool RequiresRecalculation() => 
        Status == ContractSettlementStatus.Draft && 
        (BenchmarkAmount == 0 || CalculationQuantityMT == 0);
}

public class SettlementCharge : BaseEntity
{
    private SettlementCharge() { } // For EF Core

    public SettlementCharge(
        Guid settlementId,
        ChargeType chargeType,
        string description,
        decimal amount,
        string currency = "USD",
        DateTime? incurredDate = null,
        string? referenceDocument = null,
        string? notes = null,
        string createdBy = "System")
    {
        SettlementId = settlementId;
        ChargeType = chargeType;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Amount = amount;
        Currency = currency;
        IncurredDate = incurredDate;
        ReferenceDocument = referenceDocument;
        Notes = notes;
        CreatedDate = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid SettlementId { get; private set; }
    public ChargeType ChargeType { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public DateTime? IncurredDate { get; private set; }
    public string? ReferenceDocument { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public new string CreatedBy { get; private set; } = string.Empty;

    // Navigation property
    public ContractSettlement Settlement { get; private set; } = null!;

    public void UpdateAmount(decimal newAmount, string updatedBy)
    {
        if (newAmount < 0)
            throw new DomainException("Charge amount cannot be negative");

        Amount = newAmount;
        SetUpdatedBy(updatedBy);
    }

    public void UpdateDescription(string newDescription, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(newDescription))
            throw new DomainException("Description cannot be empty");

        Description = newDescription.Trim();
        SetUpdatedBy(updatedBy);
    }
}

// Enums
public enum DocumentType
{
    BillOfLading = 1,
    QuantityCertificate = 2,
    QualityCertificate = 3,
    Other = 99
}

public enum ContractSettlementStatus
{
    Draft = 1,
    DataEntered = 2,
    Calculated = 3,
    Reviewed = 4,
    Approved = 5,
    Finalized = 6,
    Cancelled = 7
}

public enum ChargeType
{
    Demurrage = 1,        // 滞期费
    Despatch = 2,         // 速遣费
    InspectionFee = 3,    // 检验费
    PortCharges = 4,      // 港口费
    FreightCost = 5,      // 运费
    InsurancePremium = 6, // 保险费
    BankCharges = 7,      // 银行手续费
    StorageFee = 8,       // 仓储费
    AgencyFee = 9,        // 代理费
    Other = 99            // 其他费用
}