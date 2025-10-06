using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

/// <summary>
/// Represents a complete trade chain from purchase to sales with full tracking
/// </summary>
public class TradeChain : BaseEntity
{
    public string ChainId { get; private set; } = string.Empty;
    public string ChainName { get; private set; } = string.Empty;
    public TradeChainStatus Status { get; private set; }
    public TradeChainType Type { get; private set; }
    
    // Core contract links
    public Guid? PurchaseContractId { get; private set; }
    public Guid? SalesContractId { get; private set; }
    
    // Physical operations
    public List<TradeChainOperation> Operations { get; private set; } = new();
    public List<TradeChainEvent> Events { get; private set; } = new();
    
    // Financial tracking
    public Money? PurchaseValue { get; private set; }
    public Money? SalesValue { get; private set; }
    public Money? RealizedPnL { get; private set; }
    public Money? UnrealizedPnL { get; private set; }
    
    // Quantity tracking
    public Quantity? PurchaseQuantity { get; private set; }
    public Quantity? SalesQuantity { get; private set; }
    public Quantity? RemainingQuantity { get; private set; }
    
    // Timing
    public DateTime? TradeDate { get; private set; }
    public DateTime? ExpectedDeliveryStart { get; private set; }
    public DateTime? ExpectedDeliveryEnd { get; private set; }
    public DateTime? ActualDeliveryStart { get; private set; }
    public DateTime? ActualDeliveryEnd { get; private set; }
    
    // Parties
    public Guid? SupplierId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? ProductId { get; private set; }
    
    // Metadata
    public new string CreatedBy { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();

    // Private constructor for EF Core
    private TradeChain() { }

    public TradeChain(
        string chainId,
        string chainName,
        TradeChainType type,
        string createdBy)
    {
        ChainId = chainId;
        ChainName = chainName;
        Type = type;
        Status = TradeChainStatus.Initiated;
        CreatedBy = createdBy;
        TradeDate = DateTime.UtcNow;
        
        AddEvent(TradeChainEventType.ChainInitiated, "Trade chain created", createdBy);
    }

    public void LinkPurchaseContract(
        Guid purchaseContractId, 
        Guid supplierId, 
        Guid productId,
        Quantity quantity, 
        Money value,
        DateTime expectedDeliveryStart,
        DateTime expectedDeliveryEnd,
        string linkedBy)
    {
        if (PurchaseContractId.HasValue)
            throw new InvalidOperationException("Purchase contract already linked to this trade chain");

        PurchaseContractId = purchaseContractId;
        SupplierId = supplierId;
        ProductId = productId;
        PurchaseQuantity = quantity;
        PurchaseValue = value;
        ExpectedDeliveryStart = expectedDeliveryStart;
        ExpectedDeliveryEnd = expectedDeliveryEnd;
        
        RecalculateRemainingQuantity();
        UpdateStatus();
        
        AddEvent(TradeChainEventType.PurchaseLinked, 
            $"Purchase contract {purchaseContractId} linked", linkedBy);
    }

    public void LinkSalesContract(
        Guid salesContractId,
        Guid customerId,
        Quantity quantity,
        Money value,
        string linkedBy)
    {
        if (!PurchaseContractId.HasValue)
            throw new InvalidOperationException("Cannot link sales contract without purchase contract");

        if (quantity.Value > RemainingQuantity?.Value)
            throw new InvalidOperationException("Sales quantity exceeds remaining available quantity");

        SalesContractId = salesContractId;
        CustomerId = customerId;
        SalesQuantity = quantity;
        SalesValue = value;
        
        RecalculateRemainingQuantity();
        RecalculatePnL();
        UpdateStatus();
        
        AddEvent(TradeChainEventType.SalesLinked,
            $"Sales contract {salesContractId} linked", linkedBy);
    }

    public void AddOperation(TradeChainOperationType operationType, string description, string performedBy, object? data = null)
    {
        var operation = new TradeChainOperation
        {
            Id = Guid.NewGuid(),
            OperationType = operationType,
            Description = description,
            PerformedBy = performedBy,
            PerformedAt = DateTime.UtcNow,
            Data = data
        };

        Operations.Add(operation);
        
        AddEvent(TradeChainEventType.OperationAdded,
            $"Operation {operationType}: {description}", performedBy);
        
        UpdateStatus();
    }

    public void UpdateDeliveryActuals(DateTime? actualStart, DateTime? actualEnd, string updatedBy)
    {
        ActualDeliveryStart = actualStart;
        ActualDeliveryEnd = actualEnd;
        
        AddEvent(TradeChainEventType.DeliveryUpdated,
            $"Delivery actuals updated: {actualStart:yyyy-MM-dd} to {actualEnd:yyyy-MM-dd}", updatedBy);
        
        UpdateStatus();
    }

    public void MarkCompleted(string completedBy, string? completionNotes = null)
    {
        if (Status == TradeChainStatus.Completed)
            return;

        Status = TradeChainStatus.Completed;
        Notes = completionNotes;
        
        // Final PnL calculation
        RecalculatePnL();
        
        AddEvent(TradeChainEventType.ChainCompleted,
            $"Trade chain completed. {completionNotes}", completedBy);
    }

    public void Cancel(string reason, string cancelledBy)
    {
        if (Status == TradeChainStatus.Completed)
            throw new InvalidOperationException("Cannot cancel completed trade chain");

        Status = TradeChainStatus.Cancelled;
        Notes = reason;
        
        AddEvent(TradeChainEventType.ChainCancelled,
            $"Trade chain cancelled: {reason}", cancelledBy);
    }

    public void AddMetadata(string key, object value, string addedBy)
    {
        Metadata[key] = value;
        
        AddEvent(TradeChainEventType.MetadataUpdated,
            $"Metadata updated: {key}", addedBy);
    }

    public TradeChainSummary GetSummary()
    {
        return new TradeChainSummary
        {
            ChainId = ChainId,
            ChainName = ChainName,
            Status = Status,
            Type = Type,
            TradeDate = TradeDate,
            PurchaseValue = PurchaseValue,
            SalesValue = SalesValue,
            RealizedPnL = RealizedPnL,
            UnrealizedPnL = UnrealizedPnL,
            PurchaseQuantity = PurchaseQuantity,
            SalesQuantity = SalesQuantity,
            RemainingQuantity = RemainingQuantity,
            OperationCount = Operations.Count,
            EventCount = Events.Count,
            CompletionPercentage = CalculateCompletionPercentage()
        };
    }

    public List<TradeChainOperation> GetOperationsByType(TradeChainOperationType operationType)
    {
        return Operations.Where(op => op.OperationType == operationType).ToList();
    }

    public List<TradeChainEvent> GetEventsByType(TradeChainEventType eventType)
    {
        return Events.Where(e => e.EventType == eventType).ToList();
    }

    public TradeChainPerformanceMetrics CalculatePerformanceMetrics()
    {
        var totalDuration = DateTime.UtcNow - CreatedAt;
        var deliveryDuration = ActualDeliveryEnd - ActualDeliveryStart;
        var plannedDuration = ExpectedDeliveryEnd - ExpectedDeliveryStart;
        
        return new TradeChainPerformanceMetrics
        {
            TotalDurationDays = (int)totalDuration.TotalDays,
            DeliveryDurationDays = deliveryDuration?.TotalDays,
            PlannedDurationDays = plannedDuration?.TotalDays,
            DeliveryOnTime = deliveryDuration <= plannedDuration,
            ProfitMargin = CalculateProfitMargin(),
            OperationEfficiency = CalculateOperationEfficiency(),
            RiskAdjustedReturn = CalculateRiskAdjustedReturn()
        };
    }

    private void RecalculateRemainingQuantity()
    {
        if (PurchaseQuantity == null)
        {
            RemainingQuantity = null;
            return;
        }

        var soldQuantity = SalesQuantity?.Value ?? 0;
        var remainingValue = PurchaseQuantity.Value - soldQuantity;
        
        RemainingQuantity = new Quantity(remainingValue, PurchaseQuantity.Unit);
    }

    private void RecalculatePnL()
    {
        if (PurchaseValue == null || SalesValue == null)
        {
            RealizedPnL = null;
            UnrealizedPnL = null;
            return;
        }

        // Calculate realized PnL based on sold quantity
        var soldRatio = SalesQuantity?.Value / PurchaseQuantity?.Value ?? 0;
        var allocatedPurchaseCost = new Money(PurchaseValue.Amount * (decimal)soldRatio, PurchaseValue.Currency);
        
        RealizedPnL = new Money(SalesValue.Amount - allocatedPurchaseCost.Amount, SalesValue.Currency);
        
        // Calculate unrealized PnL for remaining quantity
        var remainingRatio = 1 - soldRatio;
        var remainingCost = new Money(PurchaseValue.Amount * (decimal)remainingRatio, PurchaseValue.Currency);
        
        // For unrealized PnL, we'd need current market price - simplified here
        UnrealizedPnL = remainingCost; // Placeholder - would use mark-to-market
    }

    private void UpdateStatus()
    {
        if (Status == TradeChainStatus.Completed || Status == TradeChainStatus.Cancelled)
            return;

        if (PurchaseContractId.HasValue && SalesContractId.HasValue)
        {
            if (ActualDeliveryEnd.HasValue)
                Status = TradeChainStatus.Delivered;
            else if (ActualDeliveryStart.HasValue)
                Status = TradeChainStatus.InDelivery;
            else
                Status = TradeChainStatus.Contracted;
        }
        else if (PurchaseContractId.HasValue)
        {
            Status = TradeChainStatus.PurchaseOnly;
        }
    }

    private void AddEvent(TradeChainEventType eventType, string description, string performedBy)
    {
        Events.Add(new TradeChainEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Description = description,
            PerformedBy = performedBy,
            PerformedAt = DateTime.UtcNow
        });
    }

    private decimal CalculateCompletionPercentage()
    {
        var totalSteps = 10; // Simplified calculation
        var completedSteps = 0;

        if (PurchaseContractId.HasValue) completedSteps += 3;
        if (SalesContractId.HasValue) completedSteps += 3;
        if (ActualDeliveryStart.HasValue) completedSteps += 2;
        if (ActualDeliveryEnd.HasValue) completedSteps += 2;

        return (decimal)completedSteps / totalSteps * 100;
    }

    private decimal? CalculateProfitMargin()
    {
        if (PurchaseValue == null || SalesValue == null)
            return null;

        return (SalesValue.Amount - PurchaseValue.Amount) / SalesValue.Amount * 100;
    }

    private decimal CalculateOperationEfficiency()
    {
        // Simplified efficiency calculation based on operation count and duration
        var expectedOperations = 5; // Baseline
        var actualOperations = Operations.Count;
        
        return Math.Max(0, (decimal)(expectedOperations - Math.Max(0, actualOperations - expectedOperations)) / expectedOperations * 100);
    }

    private decimal? CalculateRiskAdjustedReturn()
    {
        // Simplified risk-adjusted return calculation
        var profitMargin = CalculateProfitMargin();
        if (!profitMargin.HasValue)
            return null;

        var riskFactor = Type switch
        {
            TradeChainType.BackToBack => 0.9m,
            TradeChainType.Speculative => 0.6m,
            TradeChainType.Storage => 0.8m,
            TradeChainType.Processing => 0.7m,
            _ => 0.8m
        };

        return profitMargin * riskFactor;
    }
}

/// <summary>
/// Individual operation within a trade chain
/// </summary>
public class TradeChainOperation
{
    public Guid Id { get; set; }
    public TradeChainOperationType OperationType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// Events that occur within a trade chain
/// </summary>
public class TradeChainEvent
{
    public Guid Id { get; set; }
    public TradeChainEventType EventType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
}

/// <summary>
/// Summary information for a trade chain
/// </summary>
public class TradeChainSummary
{
    public string ChainId { get; set; } = string.Empty;
    public string ChainName { get; set; } = string.Empty;
    public TradeChainStatus Status { get; set; }
    public TradeChainType Type { get; set; }
    public DateTime? TradeDate { get; set; }
    public Money? PurchaseValue { get; set; }
    public Money? SalesValue { get; set; }
    public Money? RealizedPnL { get; set; }
    public Money? UnrealizedPnL { get; set; }
    public Quantity? PurchaseQuantity { get; set; }
    public Quantity? SalesQuantity { get; set; }
    public Quantity? RemainingQuantity { get; set; }
    public int OperationCount { get; set; }
    public int EventCount { get; set; }
    public decimal CompletionPercentage { get; set; }
}

/// <summary>
/// Performance metrics for a trade chain
/// </summary>
public class TradeChainPerformanceMetrics
{
    public int TotalDurationDays { get; set; }
    public double? DeliveryDurationDays { get; set; }
    public double? PlannedDurationDays { get; set; }
    public bool? DeliveryOnTime { get; set; }
    public decimal? ProfitMargin { get; set; }
    public decimal OperationEfficiency { get; set; }
    public decimal? RiskAdjustedReturn { get; set; }
}

/// <summary>
/// Trade chain status enumeration
/// </summary>
public enum TradeChainStatus
{
    Initiated = 1,
    PurchaseOnly = 2,
    Contracted = 3,
    InDelivery = 4,
    Delivered = 5,
    Completed = 6,
    Cancelled = 7
}

/// <summary>
/// Trade chain type enumeration
/// </summary>
public enum TradeChainType
{
    BackToBack = 1,     // Direct purchase-to-sales
    Speculative = 2,    // Purchase without immediate sales
    Storage = 3,        // Purchase with storage period
    Processing = 4,     // Purchase with processing/blending
    Transit = 5         // In-transit trading
}

/// <summary>
/// Operation type enumeration
/// </summary>
public enum TradeChainOperationType
{
    ContractExecution = 1,
    QualityInspection = 2,
    ShippingArrangement = 3,
    Documentation = 4,
    PaymentProcessing = 5,
    RiskManagement = 6,
    ComplianceCheck = 7,
    InventoryMovement = 8,
    PriceFixing = 9,
    Delivery = 10
}

/// <summary>
/// Event type enumeration
/// </summary>
public enum TradeChainEventType
{
    ChainInitiated = 1,
    PurchaseLinked = 2,
    SalesLinked = 3,
    OperationAdded = 4,
    DeliveryUpdated = 5,
    ChainCompleted = 6,
    ChainCancelled = 7,
    MetadataUpdated = 8,
    StatusChanged = 9,
    AlertTriggered = 10
}