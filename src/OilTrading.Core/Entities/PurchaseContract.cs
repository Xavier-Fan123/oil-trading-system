using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Events;

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
        // 设置价格基准物 - Set price benchmark for pricing reference
        PriceBenchmarkId = priceBenchmarkId;
        // 设置外部合同编号 - Set external contract number for official records
        ExternalContractNumber = string.IsNullOrWhiteSpace(externalContractNumber) ? null : externalContractNumber.Trim();
        Status = ContractStatus.Draft;
        
        AddDomainEvent(new PurchaseContractCreatedEvent(Id, contractNumber.Value));
    }

    public ContractNumber ContractNumber { get; private set; } = null!; // 系统内部合同编号 - System internal contract number
    public string? ExternalContractNumber { get; private set; } // 外部/手动合同编号 - External/Manual contract number for official records
    public ContractType ContractType { get; private set; }
    public Guid TradingPartnerId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid TraderId { get; private set; }
    
    // Business-specific property aliases for better domain clarity
    /// <summary>
    /// Supplier ID - Alias for TradingPartnerId in purchase context
    /// </summary>
    public Guid SupplierId => TradingPartnerId;
    
    // Benchmark Information - 基准物信息，用于价格结算
    // Purpose: 在油品交易中，产品价格通常与特定的市场基准物挂钩（如Brent、WTI等）
    // 这个字段存储与合同关联的基准物ID，用于后续的价格计算和结算
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
    
    // Additional Fields
    public string? Incoterms { get; private set; }
    public string? QualitySpecifications { get; private set; }
    public string? InspectionAgency { get; private set; }
    public string? Notes { get; private set; }
    
    // Trade Group Association - 交易组关联
    /// <summary>
    /// 交易组ID - Trade Group ID for multi-leg strategies
    /// </summary>
    public Guid? TradeGroupId { get; private set; }

    /// <summary>
    /// 交易组导航属性 - Trade Group navigation property
    /// </summary>
    public TradeGroup? TradeGroup { get; private set; }

    // Navigation Properties
    public TradingPartner TradingPartner { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public User Trader { get; private set; } = null!;
    public PurchaseContract? BenchmarkContract { get; private set; }
    // Price Benchmark Navigation - 基准物导航属性
    // Purpose: 关联到PriceBenchmark实体，提供基准物的详细信息（名称、类型、货币等）
    public PriceBenchmark? PriceBenchmark { get; private set; }
    public ICollection<SalesContract> LinkedSalesContracts { get; private set; } = new List<SalesContract>();
    public ICollection<ShippingOperation> ShippingOperations { get; private set; } = new List<ShippingOperation>();
    public ICollection<PricingEvent> PricingEvents { get; private set; } = new List<PricingEvent>();
    public ICollection<ContractTag> ContractTags { get; private set; } = new List<ContractTag>();
    public ICollection<ContractMatching> ContractMatchings { get; private set; } = new List<ContractMatching>();

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
        
        AddDomainEvent(new PurchaseContractPriceFinalizedEvent(Id, finalContractValue.Amount, finalContractValue.Currency));
    }

    public void LinkBenchmarkContract(Guid benchmarkContractId)
    {
        if (Status != ContractStatus.Draft)
            throw new DomainException($"Cannot link benchmark contract when contract is in {Status} status");

        BenchmarkContractId = benchmarkContractId;
    }

    // Purpose: 设置价格基准物，用于确定合同的结算价格基准
    // Logic: 只允许在草稿状态下设置，确保合同定价的一致性
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

    // Purpose: 设置外部合同编号，用于与交易对手的正式合同关联
    // Logic: 这是用于对账和查询的主要标识符，必须是唯一的
    public void SetExternalContractNumber(string externalContractNumber, string updatedBy = "")
    {
        if (string.IsNullOrWhiteSpace(externalContractNumber))
            throw new DomainException("External contract number cannot be empty");

        // 允许在活跃状态下更新外部合同编号，因为正式合同号可能在后期确定
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
        
        if (laycanStart < DateTime.UtcNow.Date)
            throw new DomainException("Laycan start cannot be in the past");

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
    /// 分配到交易组 - Assign to trade group
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
    /// 从交易组移除 - Remove from trade group
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
    /// 获取合同的所有标签
    /// </summary>
    public IEnumerable<Tag> GetTags()
    {
        return ContractTags.Select(ct => ct.Tag);
    }

    /// <summary>
    /// 检查合同是否有指定标签
    /// </summary>
    public bool HasTag(Guid tagId)
    {
        return ContractTags.Any(ct => ct.TagId == tagId);
    }

    /// <summary>
    /// 检查合同是否有指定名称的标签
    /// </summary>
    public bool HasTag(string tagName)
    {
        return ContractTags.Any(ct => ct.Tag.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取指定分类的标签
    /// </summary>
    public IEnumerable<Tag> GetTagsByCategory(ValueObjects.TagCategory category)
    {
        return ContractTags.Where(ct => ct.Tag.Category == category).Select(ct => ct.Tag);
    }

    /// <summary>
    /// 获取合同的风险等级标签
    /// </summary>
    public Tag? GetRiskLevelTag()
    {
        return GetTagsByCategory(ValueObjects.TagCategory.RiskLevel).FirstOrDefault();
    }

    /// <summary>
    /// 获取合同的优先级标签
    /// </summary>
    public Tag? GetPriorityTag()
    {
        return GetTagsByCategory(ValueObjects.TagCategory.Priority).FirstOrDefault();
    }

    /// <summary>
    /// 检查合同是否为高风险
    /// </summary>
    public bool IsHighRisk()
    {
        return HasTag("High Risk") || HasTag("Critical");
    }

    /// <summary>
    /// 检查合同是否为紧急优先级
    /// </summary>
    public bool IsUrgent()
    {
        return HasTag("Urgent");
    }

    /// <summary>
    /// 获取合同标签的显示文本
    /// </summary>
    public string GetTagsDisplayText()
    {
        var tags = GetTags().OrderBy(t => t.Category).ThenBy(t => t.Priority).ThenBy(t => t.Name);
        return string.Join(", ", tags.Select(t => t.Name));
    }
}

public enum ContractStatus
{
    Draft = 0,
    PendingApproval = 1,
    Active = 2,
    Completed = 3,
    Cancelled = 4,
    Suspended = 5
}