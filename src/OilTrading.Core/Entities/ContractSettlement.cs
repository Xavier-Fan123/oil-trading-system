using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Events;
using OilTrading.Core.Enums;

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

    // ═══════════════════════════════════════════════════════════════════════════
    // DATA LINEAGE ENHANCEMENT - Deal Reference ID & Amendment Chain
    // Purpose: Enable full lifecycle traceability and audit trail for settlements
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Deal Reference ID - Inherited from the contract this settlement belongs to
    /// Enables tracing settlement back to original deal
    /// </summary>
    public string? DealReferenceId { get; private set; }

    /// <summary>
    /// Previous Settlement ID - Links to the prior version of this settlement (amendment chain)
    /// Null for initial settlements
    /// </summary>
    public Guid? PreviousSettlementId { get; private set; }

    /// <summary>
    /// Original Settlement ID - Links to the root of the amendment chain
    /// For initial settlements, this equals Id. For amendments, points to first settlement.
    /// </summary>
    public Guid? OriginalSettlementId { get; private set; }

    /// <summary>
    /// Settlement Sequence - Version number in the amendment chain (1 = initial, 2+ = amendments)
    /// </summary>
    public int SettlementSequence { get; private set; } = 1;

    /// <summary>
    /// Amendment Type - Why this settlement was created (Initial, Amendment, Correction, etc.)
    /// </summary>
    public SettlementAmendmentType AmendmentType { get; private set; } = SettlementAmendmentType.Initial;

    /// <summary>
    /// Amendment Reason - Business justification for non-initial settlements
    /// Required for Amendment, Correction, SecondaryPricing types
    /// </summary>
    public string? AmendmentReason { get; private set; }

    /// <summary>
    /// Is Latest Version - Quick filter flag indicating this is the current active version
    /// False for superseded settlements
    /// </summary>
    public bool IsLatestVersion { get; private set; } = true;

    /// <summary>
    /// Superseded Date - When this settlement was replaced by a newer version
    /// Null for the latest version
    /// </summary>
    public DateTime? SupersededDate { get; private set; }

    // Navigation property for amendment chain
    public ContractSettlement? PreviousSettlement { get; private set; }
    public ContractSettlement? OriginalSettlement { get; private set; }
    
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

    // Payment Dates - Three-tier date tracking system
    /// <summary>
    /// Actual payable/receivable due date - filled by user when creating/editing settlement
    /// For purchases: when we need to pay
    /// For sales: when the customer needs to pay us
    /// This is determined after the settlement is created, potentially different from contract estimate
    /// </summary>
    public DateTime? ActualPayableDueDate { get; private set; }

    /// <summary>
    /// Actual payment/collection date - filled by finance department after payment is made
    /// For purchases: when we actually paid the supplier
    /// For sales: when we actually received payment from customer
    /// Will be null until the payment is actually made
    /// </summary>
    public DateTime? ActualPaymentDate { get; private set; }

    // Navigation properties
    // Note: PurchaseContract and SalesContract are not navigation properties in the database
    // Instead, ContractId (Guid) can reference either table. Navigation properties are
    // populated manually in the service layer (GetContractInfoAsync) based on lookup.
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

    /// <summary>
    /// Set the actual payable/receivable due date for the settlement
    /// Filled by user when creating/editing the settlement
    /// For purchases: when we need to pay
    /// For sales: when the customer needs to pay us
    /// </summary>
    public void SetActualPayableDueDate(DateTime actualPayableDueDate, string updatedBy)
    {
        ActualPayableDueDate = actualPayableDueDate;
        LastModifiedDate = DateTime.UtcNow;
        LastModifiedBy = updatedBy;
    }

    /// <summary>
    /// Set the actual payment/collection date for the settlement
    /// Filled by finance department after payment is actually made
    /// Can be updated even after finalization
    /// </summary>
    public void SetActualPaymentDate(DateTime? actualPaymentDate, string updatedBy)
    {
        ActualPaymentDate = actualPaymentDate;
        LastModifiedDate = DateTime.UtcNow;
        LastModifiedBy = updatedBy;
    }

    public bool CanBeModified() => !IsFinalized && Status != ContractSettlementStatus.Finalized;

    public bool RequiresRecalculation() =>
        Status == ContractSettlementStatus.Draft &&
        (BenchmarkAmount == 0 || CalculationQuantityMT == 0);

    // ═══════════════════════════════════════════════════════════════════════════
    // DATA LINEAGE METHODS - Deal Reference ID & Amendment Chain Management
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Set the Deal Reference ID inherited from the contract
    /// Should be called when creating the settlement
    /// </summary>
    public void SetDealReferenceId(string dealReferenceId, string updatedBy = "")
    {
        if (string.IsNullOrWhiteSpace(dealReferenceId))
            throw new DomainException("Deal Reference ID cannot be empty");

        DealReferenceId = dealReferenceId.Trim().ToUpper();
        LastModifiedDate = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(updatedBy))
        {
            LastModifiedBy = updatedBy;
        }
    }

    /// <summary>
    /// Initialize as the first settlement in the chain (no prior version)
    /// OriginalSettlementId will equal this settlement's Id
    /// </summary>
    public void InitializeAsOriginal()
    {
        OriginalSettlementId = Id;
        PreviousSettlementId = null;
        SettlementSequence = 1;
        AmendmentType = SettlementAmendmentType.Initial;
        IsLatestVersion = true;
        SupersededDate = null;
    }

    /// <summary>
    /// Create this settlement as an amendment to a previous settlement
    /// </summary>
    public void InitializeAsAmendment(
        Guid previousSettlementId,
        Guid originalSettlementId,
        int previousSequence,
        SettlementAmendmentType amendmentType,
        string amendmentReason,
        string updatedBy = "")
    {
        if (amendmentType == SettlementAmendmentType.Initial)
            throw new DomainException("Cannot initialize as amendment with Initial type");

        if (string.IsNullOrWhiteSpace(amendmentReason) &&
            (amendmentType == SettlementAmendmentType.Amendment ||
             amendmentType == SettlementAmendmentType.Correction))
            throw new DomainException("Amendment reason is required for Amendment or Correction types");

        PreviousSettlementId = previousSettlementId;
        OriginalSettlementId = originalSettlementId;
        SettlementSequence = previousSequence + 1;
        AmendmentType = amendmentType;
        AmendmentReason = amendmentReason?.Trim();
        IsLatestVersion = true;
        SupersededDate = null;

        LastModifiedDate = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(updatedBy))
        {
            LastModifiedBy = updatedBy;
        }
    }

    /// <summary>
    /// Mark this settlement as superseded by a newer version
    /// Called on the previous settlement when a new amendment is created
    /// </summary>
    public void MarkAsSuperseded(string updatedBy = "")
    {
        IsLatestVersion = false;
        SupersededDate = DateTime.UtcNow;
        LastModifiedDate = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(updatedBy))
        {
            LastModifiedBy = updatedBy;
        }
    }

    /// <summary>
    /// Check if this is the original (first) settlement in the chain
    /// </summary>
    public bool IsOriginalSettlement() =>
        SettlementSequence == 1 && AmendmentType == SettlementAmendmentType.Initial;

    /// <summary>
    /// Check if this settlement has been superseded by a newer version
    /// </summary>
    public bool IsSuperseded() => !IsLatestVersion && SupersededDate.HasValue;
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
    Demurrage = 1,        // Demurrage fee
    Despatch = 2,         // Despatch fee
    InspectionFee = 3,    // Inspection fee
    PortCharges = 4,      // Port charges
    FreightCost = 5,      // Freight cost
    InsurancePremium = 6, // Insurance premium
    BankCharges = 7,      // Bank charges
    StorageFee = 8,       // Storage fee
    AgencyFee = 9,        // Agency fee
    Other = 99            // Other charges
}