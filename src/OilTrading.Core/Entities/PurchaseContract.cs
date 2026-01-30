using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Events;
using OilTrading.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OilTrading.Core.Entities;

public class PurchaseContract : BaseEntity
{
    private PurchaseContract() { } // For EF Core

    public PurchaseContract(
        ContractNumber contractNumber,
        ContractType contractType,
        Guid tradingPartnerId,
        Guid productId,
        Guid traderId,
        Quantity contractQuantity,
        decimal tonBarrelRatio = 7.6m,
        Guid? priceBenchmarkId = null,
        string? externalContractNumber = null)
    {
        ContractNumber = contractNumber ?? throw new ArgumentNullException(nameof(contractNumber));
        ContractType = contractType;
        TradingPartnerId = tradingPartnerId;
        ProductId = productId;
        TraderId = traderId;
        ContractQuantity = contractQuantity ?? throw new ArgumentNullException(nameof(contractQuantity));
        TonBarrelRatio = tonBarrelRatio;
        // Set price benchmark for pricing reference
        PriceBenchmarkId = priceBenchmarkId;
        // Set external contract number for official records
        ExternalContractNumber = string.IsNullOrWhiteSpace(externalContractNumber) ? null : externalContractNumber.Trim();
        Status = ContractStatus.Draft;
        
        AddDomainEvent(new PurchaseContractCreatedEvent(Id, contractNumber.Value));
    }

    public ContractNumber ContractNumber { get; private set; } = null!; // System internal contract number
    public string? ExternalContractNumber { get; private set; } // External/Manual contract number for official records
    public ContractType ContractType { get; private set; }

    // ═══════════════════════════════════════════════════════════════════════════
    // DATA LINEAGE ENHANCEMENT - Deal Reference ID
    // Purpose: Business-meaningful identifier that flows through entire transaction lifecycle
    // Format: "DEAL-{YYYY}-{NNNNNN}" e.g., "DEAL-2025-000001"
    // This ID is inherited by: ShippingOperation, Settlement, and linked SalesContract
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Deal Reference ID - Lifecycle-spanning business identifier
    /// Generated when contract is created, inherited by all downstream entities
    /// </summary>
    public string? DealReferenceId { get; private set; }
    public Guid TradingPartnerId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid TraderId { get; private set; }
    
    // Business-specific property aliases for better domain clarity
    /// <summary>
    /// Supplier ID - Alias for TradingPartnerId in purchase context
    /// </summary>
    public Guid SupplierId => TradingPartnerId;
    
    // Benchmark Information for pricing settlement
    // Purpose: In oil trading, product prices are typically linked to specific market benchmarks (such as Brent, WTI, etc.)
    // This field stores the benchmark ID associated with the contract for subsequent price calculation and settlement
    public Guid? PriceBenchmarkId { get; private set; }
    
    // Quantity
    public Quantity ContractQuantity { get; private set; } = null!;
    public decimal MatchedQuantity { get; private set; } = 0;
    public decimal TonBarrelRatio { get; private set; }
    
    // Pricing
    public PriceFormula? PriceFormula { get; private set; }
    public Money? ContractValue { get; private set; }
    public DateTime? PricingPeriodStart { get; private set; }
    public DateTime? PricingPeriodEnd { get; private set; }
    public bool IsPriceFinalized { get; private set; }
    public Guid? BenchmarkContractId { get; private set; }
    public Money? Premium { get; private set; }
    public Money? Discount { get; private set; }

    // ═══════════════════════════════════════════════════════════════════════════
    // DATA LINEAGE ENHANCEMENT - Explicit Pricing Status
    // Purpose: Replace implicit pricing status determination with explicit state
    // Solves: Risk calculations using wrong exposure values due to implicit state
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Explicit Pricing Status - Unpriced, PartiallyPriced, or FullyPriced
    /// Replaces implicit determination from IsPriceFinalized boolean
    /// </summary>
    public ContractPricingStatus PricingStatus { get; private set; } = ContractPricingStatus.Unpriced;

    /// <summary>
    /// Fixed Quantity - Amount of contract quantity that has been price-fixed
    /// Used with UnfixedQuantity to track partial pricing progress
    /// </summary>
    public decimal FixedQuantity { get; private set; } = 0;

    /// <summary>
    /// Unfixed Quantity - Amount of contract quantity pending pricing
    /// Calculated: ContractQuantity - FixedQuantity
    /// </summary>
    public decimal UnfixedQuantity { get; private set; } = 0;

    /// <summary>
    /// Fixed Percentage - Percentage of contract quantity that has been priced
    /// Range: 0-100, calculated: (FixedQuantity / ContractQuantity) * 100
    /// </summary>
    public decimal FixedPercentage { get; private set; } = 0;

    /// <summary>
    /// Last Pricing Date - When the most recent pricing action occurred
    /// Useful for aging reports and pricing activity tracking
    /// </summary>
    public DateTime? LastPricingDate { get; private set; }

    /// <summary>
    /// Price Source - How the price was determined (Manual, MarketData, Formula, Estimate)
    /// Enables auditing of price origin for compliance
    /// </summary>
    public PriceSourceType? PriceSource { get; private set; }
    
    // Contract Details
    public ContractStatus Status { get; private set; }
    public DateTime? LaycanStart { get; private set; }
    public DateTime? LaycanEnd { get; private set; }
    public string? LoadPort { get; private set; }
    public string? DischargePort { get; private set; }
    public DeliveryTerms DeliveryTerms { get; private set; } = DeliveryTerms.FOB;
    
    // Payment Terms
    public string? PaymentTerms { get; private set; }
    public int? CreditPeriodDays { get; private set; }
    public SettlementType SettlementType { get; private set; } = SettlementType.ContractPayment;
    public decimal? PrepaymentPercentage { get; private set; }

    // Payment Dates - Three-tier date tracking system
    /// <summary>
    /// Estimated payment date - filled by user when creating the contract
    /// Based on Payment Terms and business agreement
    /// </summary>
    public DateTime? EstimatedPaymentDate { get; private set; }

    /// <summary>
    /// Payment status - dynamically calculated based on ActualPayableDueDate and ActualPaymentDate
    /// from related settlements. Not persisted to database.
    /// </summary>
    [NotMapped]
    public ContractPaymentStatus? PaymentStatus { get; set; }

    // Additional Fields
    public string? Incoterms { get; private set; }
    public string? QualitySpecifications { get; private set; }
    public string? InspectionAgency { get; private set; }
    public string? Notes { get; private set; }
    
    // Trade Group Association
    /// <summary>
    /// Trade Group ID for multi-leg strategies
    /// </summary>
    public Guid? TradeGroupId { get; private set; }

    /// <summary>
    /// Trade Group navigation property
    /// </summary>
    public TradeGroup? TradeGroup { get; private set; }

    // Navigation Properties
    public TradingPartner TradingPartner { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public User Trader { get; private set; } = null!;
    public PurchaseContract? BenchmarkContract { get; private set; }
    // Price Benchmark Navigation property
    // Purpose: Links to PriceBenchmark entity, providing detailed benchmark information (name, type, currency, etc.)
    public PriceBenchmark? PriceBenchmark { get; private set; }
    public ICollection<SalesContract> LinkedSalesContracts { get; private set; } = new List<SalesContract>();
    public ICollection<ShippingOperation> ShippingOperations { get; private set; } = new List<ShippingOperation>();
    public ICollection<PricingEvent> PricingEvents { get; private set; } = new List<PricingEvent>();
    public ICollection<ContractTag> ContractTags { get; private set; } = new List<ContractTag>();
    public ICollection<ContractMatching> ContractMatchings { get; private set; } = new List<ContractMatching>();

    // Settlement relationships (one-to-many)
    // One PurchaseContract can have multiple PurchaseSettlements
    // Supporting term contracts with multiple delivery periods and partial shipments
    public ICollection<PurchaseSettlement> PurchaseSettlements { get; private set; } = new List<PurchaseSettlement>();

    // Business Methods
    public void UpdatePricing(PriceFormula priceFormula, Money contractValue, Money? premium = null, Money? discount = null)
    {
        if (Status == ContractStatus.Completed || Status == ContractStatus.Cancelled)
            throw new DomainException($"Cannot update pricing for contract in {Status} status");

        if (IsPriceFinalized)
            throw new DomainException("Cannot update pricing for finalized contract");

        PriceFormula = priceFormula ?? throw new ArgumentNullException(nameof(priceFormula));
        ContractValue = contractValue ?? throw new ArgumentNullException(nameof(contractValue));
        Premium = premium;
        Discount = discount;
        
        // Set pricing period if floating price
        if (priceFormula.IsFloatingPrice() && priceFormula.RequiresPricingPeriod())
        {
            SetPricingPeriodFromLaycan();
        }
        
        AddDomainEvent(new PurchaseContractPricingUpdatedEvent(Id, priceFormula.Formula, contractValue.Amount));
    }

    public void SetPricingPeriod(DateTime startDate, DateTime endDate)
    {
        if (IsPriceFinalized)
            throw new DomainException("Cannot set pricing period for finalized contract");

        if (startDate >= endDate)
            throw new DomainException("Pricing period start must be before end date");

        PricingPeriodStart = startDate;
        PricingPeriodEnd = endDate;
    }

    public void FinalizePrice(Money finalContractValue, string finalizedBy)
    {
        if (IsPriceFinalized)
            throw new DomainException("Price is already finalized");

        if (PriceFormula?.IsFloatingPrice() != true)
            throw new DomainException("Only floating price contracts can be finalized");

        ContractValue = finalContractValue;
        IsPriceFinalized = true;
        SetUpdatedBy(finalizedBy);

        // Update explicit pricing status fields
        UpdatePricingStatus(ContractQuantity?.Value ?? 0, PriceSourceType.Formula);

        AddDomainEvent(new PurchaseContractPriceFinalizedEvent(Id, finalContractValue.Amount, finalContractValue.Currency));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DATA LINEAGE METHODS - Deal Reference ID Management
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Set the Deal Reference ID for this contract
    /// Should be called once during contract creation or activation
    /// </summary>
    public void SetDealReferenceId(string dealReferenceId, string updatedBy = "")
    {
        if (string.IsNullOrWhiteSpace(dealReferenceId))
            throw new DomainException("Deal Reference ID cannot be empty");

        if (!string.IsNullOrEmpty(DealReferenceId))
            throw new DomainException("Deal Reference ID has already been set and cannot be changed");

        DealReferenceId = dealReferenceId.Trim().ToUpper();
        if (!string.IsNullOrEmpty(updatedBy))
        {
            SetUpdatedBy(updatedBy);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DATA LINEAGE METHODS - Explicit Pricing Status Management
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Update the pricing status based on fixed quantity
    /// Call this method whenever pricing is applied to the contract
    /// </summary>
    public void UpdatePricingStatus(decimal newFixedQuantity, PriceSourceType priceSource, string updatedBy = "")
    {
        if (newFixedQuantity < 0)
            throw new DomainException("Fixed quantity cannot be negative");

        var totalQuantity = ContractQuantity?.Value ?? 0;
        if (totalQuantity <= 0)
            throw new DomainException("Contract quantity must be set before updating pricing status");

        if (newFixedQuantity > totalQuantity)
            throw new DomainException("Fixed quantity cannot exceed contract quantity");

        FixedQuantity = newFixedQuantity;
        UnfixedQuantity = totalQuantity - newFixedQuantity;
        FixedPercentage = Math.Round((newFixedQuantity / totalQuantity) * 100, 2);
        PriceSource = priceSource;
        LastPricingDate = DateTime.UtcNow;

        // Determine pricing status based on percentage
        if (FixedPercentage == 0)
        {
            PricingStatus = ContractPricingStatus.Unpriced;
        }
        else if (FixedPercentage >= 100)
        {
            PricingStatus = ContractPricingStatus.FullyPriced;
        }
        else
        {
            PricingStatus = ContractPricingStatus.PartiallyPriced;
        }

        if (!string.IsNullOrEmpty(updatedBy))
        {
            SetUpdatedBy(updatedBy);
        }
    }

    /// <summary>
    /// Add fixed quantity incrementally (for partial pricing scenarios)
    /// </summary>
    public void AddFixedQuantity(decimal additionalFixedQuantity, PriceSourceType priceSource, string updatedBy = "")
    {
        if (additionalFixedQuantity <= 0)
            throw new DomainException("Additional fixed quantity must be greater than zero");

        var newFixedQuantity = FixedQuantity + additionalFixedQuantity;
        UpdatePricingStatus(newFixedQuantity, priceSource, updatedBy);
    }

    /// <summary>
    /// Reset pricing status to Unpriced (e.g., when price formula changes)
    /// </summary>
    public void ResetPricingStatus(string updatedBy = "")
    {
        FixedQuantity = 0;
        UnfixedQuantity = ContractQuantity?.Value ?? 0;
        FixedPercentage = 0;
        PricingStatus = ContractPricingStatus.Unpriced;
        PriceSource = null;
        LastPricingDate = null;

        if (!string.IsNullOrEmpty(updatedBy))
        {
            SetUpdatedBy(updatedBy);
        }
    }

    public void LinkBenchmarkContract(Guid benchmarkContractId)
    {
        if (Status != ContractStatus.Draft)
            throw new DomainException($"Cannot link benchmark contract when contract is in {Status} status");

        BenchmarkContractId = benchmarkContractId;
    }

    // Purpose: Set price benchmark to determine contract settlement price reference
    // Logic: Only allowed in Draft/PendingApproval status to ensure contract pricing consistency
    public void SetPriceBenchmark(Guid? priceBenchmarkId, string updatedBy = "")
    {
        if (Status != ContractStatus.Draft && Status != ContractStatus.PendingApproval)
            throw new DomainException($"Cannot set price benchmark when contract is in {Status} status");

        PriceBenchmarkId = priceBenchmarkId;
        if (!string.IsNullOrEmpty(updatedBy))
        {
            SetUpdatedBy(updatedBy);
        }
    }

    // Purpose: Set external contract number to associate with counterparty's official contract
    // Logic: This is the primary identifier for reconciliation and queries, must be unique
    public void SetExternalContractNumber(string externalContractNumber, string updatedBy = "")
    {
        if (string.IsNullOrWhiteSpace(externalContractNumber))
            throw new DomainException("External contract number cannot be empty");

        // Allow updating in Active status as official contract number may be determined later
        if (Status == ContractStatus.Completed || Status == ContractStatus.Cancelled)
            throw new DomainException($"Cannot set external contract number when contract is in {Status} status");

        ExternalContractNumber = externalContractNumber.Trim();
        if (!string.IsNullOrEmpty(updatedBy))
        {
            SetUpdatedBy(updatedBy);
        }
    }

    public void UpdateDeliveryTerms(DeliveryTerms deliveryTerms, string updatedBy)
    {
        if (Status == ContractStatus.Completed || Status == ContractStatus.Cancelled)
            throw new DomainException($"Cannot update delivery terms for contract in {Status} status");

        DeliveryTerms = deliveryTerms;
        SetUpdatedBy(updatedBy);
    }

    public void UpdateSettlementTerms(SettlementType settlementType, decimal? prepaymentPercentage = null, string updatedBy = "")
    {
        if (Status == ContractStatus.Completed || Status == ContractStatus.Cancelled)
            throw new DomainException($"Cannot update settlement terms for contract in {Status} status");

        if (prepaymentPercentage.HasValue && (prepaymentPercentage < 0 || prepaymentPercentage > 100))
            throw new DomainException("Prepayment percentage must be between 0 and 100");

        SettlementType = settlementType;
        PrepaymentPercentage = prepaymentPercentage;
        SetUpdatedBy(updatedBy);
    }

    private void SetPricingPeriodFromLaycan()
    {
        if (LaycanStart.HasValue && LaycanEnd.HasValue)
        {
            // Default pricing period: 5 days before laycan start to laycan end
            PricingPeriodStart = LaycanStart.Value.AddDays(-5);
            PricingPeriodEnd = LaycanEnd.Value;
        }
    }

    public void UpdateLaycan(DateTime laycanStart, DateTime laycanEnd)
    {
        if (laycanStart >= laycanEnd)
            throw new DomainException("Laycan start must be before laycan end");

        // NOTE: Past date validation temporarily disabled for historical data import/seeding
        // Original validation:
        // if (laycanStart < DateTime.UtcNow.Date)
        //     throw new DomainException("Laycan start cannot be in the past");

        LaycanStart = laycanStart;
        LaycanEnd = laycanEnd;

        AddDomainEvent(new PurchaseContractLaycanUpdatedEvent(Id, laycanStart, laycanEnd));
    }

    public void UpdatePorts(string loadPort, string dischargePort)
    {
        if (string.IsNullOrWhiteSpace(loadPort))
            throw new DomainException("Load port cannot be empty");
        
        if (string.IsNullOrWhiteSpace(dischargePort))
            throw new DomainException("Discharge port cannot be empty");

        LoadPort = loadPort.Trim();
        DischargePort = dischargePort.Trim();
    }

    public void UpdatePaymentTerms(string paymentTerms, int? creditPeriodDays = null)
    {
        if (string.IsNullOrWhiteSpace(paymentTerms))
            throw new DomainException("Payment terms cannot be empty");
        
        if (creditPeriodDays.HasValue && creditPeriodDays.Value < 0)
            throw new DomainException("Credit period cannot be negative");

        PaymentTerms = paymentTerms.Trim();
        CreditPeriodDays = creditPeriodDays;
    }

    public void UpdateTonBarrelRatio(decimal tonBarrelRatio)
    {
        if (tonBarrelRatio <= 0)
            throw new DomainException("Ton/Barrel ratio must be greater than 0");

        TonBarrelRatio = tonBarrelRatio;
    }

    public void UpdateDeliveryTerms(DeliveryTerms deliveryTerms)
    {
        if (Status == ContractStatus.Completed || Status == ContractStatus.Cancelled)
            throw new DomainException($"Cannot update delivery terms for contract in {Status} status");

        DeliveryTerms = deliveryTerms;
    }

    public void UpdateSettlementType(SettlementType settlementType)
    {
        if (Status == ContractStatus.Completed || Status == ContractStatus.Cancelled)
            throw new DomainException($"Cannot update settlement type for contract in {Status} status");

        SettlementType = settlementType;
    }

    /// <summary>
    /// Update the estimated payment date for the contract
    /// Used when creating or editing a purchase contract to set the expected payment date
    /// </summary>
    public void SetEstimatedPaymentDate(DateTime estimatedPaymentDate, string updatedBy = "")
    {
        if (Status == ContractStatus.Completed || Status == ContractStatus.Cancelled)
            throw new DomainException($"Cannot update estimated payment date for contract in {Status} status");

        EstimatedPaymentDate = estimatedPaymentDate;
        if (!string.IsNullOrEmpty(updatedBy))
        {
            SetUpdatedBy(updatedBy);
        }
    }

    public void SetPrepaymentPercentage(decimal prepaymentPercentage)
    {
        if (prepaymentPercentage < 0 || prepaymentPercentage > 100)
            throw new DomainException("Prepayment percentage must be between 0 and 100");

        PrepaymentPercentage = prepaymentPercentage;
    }

    public void UpdateQualitySpecifications(string qualitySpecifications)
    {
        QualitySpecifications = qualitySpecifications?.Trim();
    }

    public void UpdateInspectionAgency(string inspectionAgency)
    {
        InspectionAgency = inspectionAgency?.Trim();
    }

    public void AddNotes(string notes)
    {
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) 
                ? notes.Trim() 
                : $"{Notes}\n{notes.Trim()}";
        }
    }

    public void Activate()
    {
        if (Status != ContractStatus.Draft && Status != ContractStatus.PendingApproval)
            throw new DomainException($"Cannot activate contract from {Status} status");
        
        ValidateForActivation();
        
        Status = ContractStatus.Active;
        AddDomainEvent(new PurchaseContractActivatedEvent(Id, ContractNumber.Value));
    }

    public void Complete()
    {
        if (Status != ContractStatus.Active)
            throw new DomainException($"Cannot complete contract from {Status} status");
        
        Status = ContractStatus.Completed;
        AddDomainEvent(new PurchaseContractCompletedEvent(Id, ContractNumber.Value));
    }

    public void Cancel(string reason)
    {
        if (Status == ContractStatus.Completed)
            throw new DomainException("Cannot cancel completed contract");
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason is required");
        
        Status = ContractStatus.Cancelled;
        Notes = $"Cancelled: {reason}";
        
        AddDomainEvent(new PurchaseContractCancelledEvent(Id, ContractNumber.Value, reason));
    }

    public void SubmitForApproval()
    {
        if (Status != ContractStatus.Draft)
            throw new DomainException($"Cannot submit contract for approval from {Status} status");
        
        ValidateForActivation();
        
        Status = ContractStatus.PendingApproval;
        AddDomainEvent(new PurchaseContractSubmittedForApprovalEvent(Id, ContractNumber.Value));
    }

    public bool CanBeLinkedToSalesContract()
    {
        return Status == ContractStatus.Active && 
               ContractQuantity != null && 
               GetAvailableQuantity().Value > 0;
    }

    public Quantity GetAvailableQuantity()
    {
        var linkedQuantity = LinkedSalesContracts
            .Where(sc => sc.Status == ContractStatus.Active)
            .Sum(sc => sc.ContractQuantity.Value);
        
        var availableValue = ContractQuantity.Value - linkedQuantity;
        return new Quantity(Math.Max(0, availableValue), ContractQuantity.Unit);
    }

    private void ValidateForActivation()
    {
        var errors = new List<string>();

        if (PriceFormula == null || !PriceFormula.IsValid())
            errors.Add("Valid price formula is required");
        
        if (ContractValue == null || ContractValue.IsZero())
            errors.Add("Contract value is required");
        
        if (LaycanStart == null || LaycanEnd == null)
            errors.Add("Laycan dates are required");
        
        if (string.IsNullOrWhiteSpace(LoadPort))
            errors.Add("Load port is required");
        
        if (string.IsNullOrWhiteSpace(DischargePort))
            errors.Add("Discharge port is required");
        
        if (string.IsNullOrWhiteSpace(PaymentTerms))
            errors.Add("Payment terms are required");

        if (errors.Any())
            throw new DomainException($"Contract validation failed: {string.Join(", ", errors)}");
    }

    public void UpdateQuantity(Quantity newQuantity, string updatedBy)
    {
        if (Status != ContractStatus.Draft && Status != ContractStatus.PendingApproval)
            throw new DomainException($"Cannot update quantity for contract in {Status} status");
        
        if (newQuantity == null)
            throw new ArgumentNullException(nameof(newQuantity));
        
        if (newQuantity.IsZero())
            throw new DomainException("Contract quantity cannot be zero");

        var oldQuantity = ContractQuantity;
        ContractQuantity = newQuantity;
        SetUpdatedBy(updatedBy);
        
        AddDomainEvent(new PurchaseContractQuantityUpdatedEvent(Id, oldQuantity, newQuantity));
    }

    public void UpdateMatchedQuantity(decimal matchedQuantity)
    {
        if (matchedQuantity < 0)
            throw new DomainException("Matched quantity cannot be negative");
        
        if (matchedQuantity > ContractQuantity.Value)
            throw new DomainException("Matched quantity cannot exceed contract quantity");

        MatchedQuantity = matchedQuantity;
    }

    /// <summary>
    /// Assign to trade group
    /// </summary>
    public void AssignToTradeGroup(Guid tradeGroupId, string updatedBy = "System")
    {
        if (Status == ContractStatus.Completed || Status == ContractStatus.Cancelled)
            throw new DomainException("Cannot assign completed or cancelled contract to trade group");

        var previousGroupId = TradeGroupId;
        TradeGroupId = tradeGroupId;
        SetUpdatedBy(updatedBy);

        if (previousGroupId.HasValue)
        {
            AddDomainEvent(new ContractRemovedFromTradeGroupEvent(
                previousGroupId.Value, Id, "PurchaseContract", "Previous Group"));
        }

        AddDomainEvent(new ContractAddedToTradeGroupEvent(
            tradeGroupId, Id, "PurchaseContract", "New Group"));
    }

    /// <summary>
    /// Remove from trade group
    /// </summary>
    public void RemoveFromTradeGroup(string updatedBy = "System")
    {
        if (TradeGroupId.HasValue)
        {
            var removedGroupId = TradeGroupId.Value;
            TradeGroupId = null;
            SetUpdatedBy(updatedBy);

            AddDomainEvent(new ContractRemovedFromTradeGroupEvent(
                removedGroupId, Id, "PurchaseContract", "Removed Group"));
        }
    }

    /// <summary>
    /// Get all tags for the contract
    /// </summary>
    public IEnumerable<Tag> GetTags()
    {
        return ContractTags.Select(ct => ct.Tag);
    }

    /// <summary>
    /// Check if contract has the specified tag
    /// </summary>
    public bool HasTag(Guid tagId)
    {
        return ContractTags.Any(ct => ct.TagId == tagId);
    }

    /// <summary>
    /// Check if contract has a tag with the specified name
    /// </summary>
    public bool HasTag(string tagName)
    {
        return ContractTags.Any(ct => ct.Tag.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get tags by category
    /// </summary>
    public IEnumerable<Tag> GetTagsByCategory(ValueObjects.TagCategory category)
    {
        return ContractTags.Where(ct => ct.Tag.Category == category).Select(ct => ct.Tag);
    }

    /// <summary>
    /// Get the risk level tag for the contract
    /// </summary>
    public Tag? GetRiskLevelTag()
    {
        return GetTagsByCategory(ValueObjects.TagCategory.RiskLevel).FirstOrDefault();
    }

    /// <summary>
    /// Get the priority tag for the contract
    /// </summary>
    public Tag? GetPriorityTag()
    {
        return GetTagsByCategory(ValueObjects.TagCategory.Priority).FirstOrDefault();
    }

    /// <summary>
    /// Check if contract is high risk
    /// </summary>
    public bool IsHighRisk()
    {
        return HasTag("High Risk") || HasTag("Critical");
    }

    /// <summary>
    /// Check if contract has urgent priority
    /// </summary>
    public bool IsUrgent()
    {
        return HasTag("Urgent");
    }

    /// <summary>
    /// Get display text of contract tags
    /// </summary>
    public string GetTagsDisplayText()
    {
        var tags = GetTags().OrderBy(t => t.Category).ThenBy(t => t.Priority).ThenBy(t => t.Name);
        return string.Join(", ", tags.Select(t => t.Name));
    }
}

public enum ContractStatus
{
    Draft = 1,
    PendingApproval = 2,
    Active = 3,
    PartiallySettled = 4,  // Added: for contracts with multiple settlements where some are finalized
    Completed = 5,         // Updated: moved from 4 to 5
    Cancelled = 6,         // Updated: moved from 5 to 6
    Suspended = 7          // Updated: moved from 6 to 7
}

/// <summary>
/// Contract Payment Status enumeration - tracks the payment status of a contract
/// Used to indicate whether a contract has been paid, partially paid, is overdue, etc.
/// Calculated dynamically based on ActualPayableDueDate and ActualPaymentDate
/// Note: Different from Payment.PaymentStatus which tracks individual payment workflow
/// </summary>
public enum ContractPaymentStatus
{
    NotDue = 1,          // Payment/collection not yet due
    Due = 2,             // Payment/collection due but not yet received
    PartiallyPaid = 3,   // Partially paid (e.g., down payment, partial settlement)
    Paid = 4,            // Fully paid/collected
    Overdue = 5          // Payment/collection is overdue
}