using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Events;

namespace OilTrading.Core.Entities;

public class SalesContract : BaseEntity
{
    private SalesContract() { } // For EF Core

    public SalesContract(
        ContractNumber contractNumber,
        ContractType contractType,
        Guid tradingPartnerId,
        Guid productId,
        Guid traderId,
        Quantity contractQuantity,
        decimal tonBarrelRatio = 7.6m,
        Guid? linkedPurchaseContractId = null,
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
        LinkedPurchaseContractId = linkedPurchaseContractId;
        // 设置价格基准物 - Set price benchmark for pricing reference
        PriceBenchmarkId = priceBenchmarkId;
        // 设置外部合同编号 - Set external contract number for official records
        ExternalContractNumber = string.IsNullOrWhiteSpace(externalContractNumber) ? null : externalContractNumber.Trim();
        Status = ContractStatus.Draft;
        
        AddDomainEvent(new SalesContractCreatedEvent(Id, contractNumber.Value));
    }

    public ContractNumber ContractNumber { get; private set; } = null!; // 系统内部合同编号 - System internal contract number
    public string? ExternalContractNumber { get; private set; } // 外部/手动合同编号 - External/Manual contract number for official records
    public ContractType ContractType { get; private set; }
    public Guid TradingPartnerId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid TraderId { get; private set; }
    public Guid? LinkedPurchaseContractId { get; private set; }
    
    // Business-specific property aliases for better domain clarity
    /// <summary>
    /// Customer ID - Alias for TradingPartnerId in sales context
    /// </summary>
    public Guid CustomerId => TradingPartnerId;
    
    // Benchmark Information - 基准物信息，用于价格结算
    // Purpose: 在油品交易中，产品价格通常与特定的市场基准物挂钩（如Brent、WTI等）
    // 这个字段存储与合同关联的基准物ID，用于后续的价格计算和结算
    public Guid? PriceBenchmarkId { get; private set; }
    
    // Quantity
    public Quantity ContractQuantity { get; private set; } = null!;
    public decimal TonBarrelRatio { get; private set; }
    
    // Pricing
    public PriceFormula? PriceFormula { get; private set; }
    public Money? ContractValue { get; private set; }
    public Money? ProfitMargin { get; private set; }
    public DateTime? PricingPeriodStart { get; private set; }
    public DateTime? PricingPeriodEnd { get; private set; }
    public bool IsPriceFinalized { get; private set; }
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
    public PurchaseContract? LinkedPurchaseContract { get; private set; }
    // Price Benchmark Navigation - 基准物导航属性
    // Purpose: 关联到PriceBenchmark实体，提供基准物的详细信息（名称、类型、货币等）
    public PriceBenchmark? PriceBenchmark { get; private set; }
    public ICollection<ShippingOperation> ShippingOperations { get; private set; } = new List<ShippingOperation>();
    public ICollection<PricingEvent> PricingEvents { get; private set; } = new List<PricingEvent>();
    public ICollection<ContractTag> ContractTags { get; private set; } = new List<ContractTag>();
    public ICollection<ContractMatching> ContractMatchings { get; private set; } = new List<ContractMatching>();

    // Business Methods
    public void LinkToPurchaseContract(Guid purchaseContractId)
    {
        if (Status != ContractStatus.Draft)
            throw new DomainException($"Cannot link purchase contract when sales contract is in {Status} status");
        
        LinkedPurchaseContractId = purchaseContractId;
        AddDomainEvent(new SalesContractLinkedToPurchaseEvent(Id, purchaseContractId));
    }

    public void UnlinkFromPurchaseContract()
    {
        if (Status != ContractStatus.Draft)
            throw new DomainException($"Cannot unlink purchase contract when sales contract is in {Status} status");
        
        if (!LinkedPurchaseContractId.HasValue)
            throw new DomainException("Sales contract is not linked to any purchase contract");
        
        var previousLinkedId = LinkedPurchaseContractId.Value;
        LinkedPurchaseContractId = null;
        AddDomainEvent(new SalesContractUnlinkedFromPurchaseEvent(Id, previousLinkedId));
    }

    public void UpdatePricing(PriceFormula priceFormula, Money contractValue, Money? profitMargin = null)
    {
        if (Status == ContractStatus.Completed || Status == ContractStatus.Cancelled)
            throw new DomainException($"Cannot update pricing for contract in {Status} status");

        PriceFormula = priceFormula ?? throw new ArgumentNullException(nameof(priceFormula));
        ContractValue = contractValue ?? throw new ArgumentNullException(nameof(contractValue));
        ProfitMargin = profitMargin;
        
        AddDomainEvent(new SalesContractPricingUpdatedEvent(Id, priceFormula.Formula, contractValue.Amount));
    }

    public void UpdateLaycan(DateTime laycanStart, DateTime laycanEnd)
    {
        if (laycanStart >= laycanEnd)
            throw new DomainException("Laycan start must be before laycan end");
        
        if (laycanStart < DateTime.UtcNow.Date)
            throw new DomainException("Laycan start cannot be in the past");

        LaycanStart = laycanStart;
        LaycanEnd = laycanEnd;
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

    public void Activate()
    {
        if (Status != ContractStatus.Draft && Status != ContractStatus.PendingApproval)
            throw new DomainException($"Cannot activate contract from {Status} status");
        
        ValidateForActivation();
        
        Status = ContractStatus.Active;
        AddDomainEvent(new SalesContractActivatedEvent(Id, ContractNumber.Value));
    }

    public void Complete()
    {
        if (Status != ContractStatus.Active)
            throw new DomainException($"Cannot complete contract from {Status} status");
        
        Status = ContractStatus.Completed;
        AddDomainEvent(new SalesContractCompletedEvent(Id, ContractNumber.Value));
    }

    public void Cancel(string reason)
    {
        if (Status == ContractStatus.Completed)
            throw new DomainException("Cannot cancel completed contract");
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason is required");
        
        Status = ContractStatus.Cancelled;
        Notes = $"Cancelled: {reason}";
        
        AddDomainEvent(new SalesContractCancelledEvent(Id, ContractNumber.Value, reason));
    }

    public void Reject(string reason)
    {
        if (Status != ContractStatus.PendingApproval)
            throw new DomainException($"Cannot reject contract from {Status} status");
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required");
        
        Status = ContractStatus.Draft;
        var rejectionNote = $"REJECTED: {reason} - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        Notes = string.IsNullOrEmpty(Notes) ? rejectionNote : $"{Notes}\n\n{rejectionNote}";
    }

    public void SubmitForApproval()
    {
        if (Status != ContractStatus.Draft)
            throw new DomainException($"Cannot submit contract for approval from {Status} status");
        
        ValidateForSubmission();
        Status = ContractStatus.PendingApproval;
    }

    private void ValidateForSubmission()
    {
        if (TradingPartnerId == Guid.Empty)
            throw new DomainException("Customer is required for submission");
        if (ProductId == Guid.Empty)
            throw new DomainException("Product is required for submission");
        if (TraderId == Guid.Empty)
            throw new DomainException("Trader is required for submission");
        if (ContractQuantity == null || ContractQuantity.Value <= 0)
            throw new DomainException("Valid quantity is required for submission");
        if (!LaycanStart.HasValue || !LaycanEnd.HasValue)
            throw new DomainException("Laycan dates are required for submission");
        if (string.IsNullOrWhiteSpace(LoadPort))
            throw new DomainException("Load port is required for submission");
        if (string.IsNullOrWhiteSpace(DischargePort))
            throw new DomainException("Discharge port is required for submission");
    }

    public Money? CalculateProfitMargin()
    {
        if (LinkedPurchaseContract?.ContractValue == null || ContractValue == null)
            return null;
        
        // Simple profit calculation - sales value minus purchase value
        return ContractValue.Subtract(LinkedPurchaseContract.ContractValue);
    }

    public decimal? CalculateProfitMarginPercentage()
    {
        var profit = CalculateProfitMargin();
        if (profit == null || LinkedPurchaseContract?.ContractValue == null || LinkedPurchaseContract.ContractValue.IsZero())
            return null;
        
        return (profit.Amount / LinkedPurchaseContract.ContractValue.Amount) * 100;
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
                previousGroupId.Value, Id, "SalesContract", "Previous Group"));
        }

        AddDomainEvent(new ContractAddedToTradeGroupEvent(
            tradeGroupId, Id, "SalesContract", "New Group"));
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
                removedGroupId, Id, "SalesContract", "Removed Group"));
        }
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