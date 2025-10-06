using OilTrading.Core.ValueObjects;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Services;

public interface IRealTimeInventoryService
{
    // Real-time inventory tracking
    Task<InventorySnapshot> GetRealTimeInventoryAsync(Guid? productId = null, Guid? locationId = null);
    Task<List<OilTrading.Application.DTOs.InventoryMovement>> GetInventoryMovementsAsync(DateTime startDate, DateTime endDate, Guid? productId = null);
    Task<InventoryBalance> GetProductBalanceAsync(Guid productId, Guid? locationId = null);
    
    // Inventory operations
    Task<InventoryOperationResult> ReceiveInventoryAsync(InventoryReceiptRequest request);
    Task<InventoryOperationResult> DeliverInventoryAsync(InventoryDeliveryRequest request);
    Task<InventoryOperationResult> TransferInventoryAsync(InventoryTransferRequest request);
    Task<InventoryOperationResult> AdjustInventoryAsync(InventoryAdjustmentRequest request);
    
    // Real-time monitoring
    Task<List<OilTrading.Application.DTOs.InventoryAlert>> GetActiveInventoryAlertsAsync();
    Task<InventoryMetrics> GetInventoryMetricsAsync();
    Task ConfigureInventoryThresholdsAsync(Guid productId, Guid locationId, InventoryThresholds thresholds);
    
    // Inventory forecasting
    Task<InventoryForecast> ForecastInventoryAsync(Guid productId, Guid locationId, int forecastDays);
    Task<List<InventoryRecommendation>> GetInventoryRecommendationsAsync();
    
    // Integration with contracts
    Task<InventoryAvailabilityCheck> CheckAvailabilityForContractAsync(Guid contractId);
    Task<InventoryAllocationResult> AllocateInventoryForContractAsync(Guid contractId, InventoryAllocationRequest request);
    
    // Inventory reservation methods  
    Task<InventoryReservationResult> ReserveInventoryAsync(InventoryReservationRequest request);
    Task<InventoryOperationResult> ReleaseReservationAsync(InventoryReleaseRequest request);
    
    // Real-time inventory access with string-based product/location codes
    Task<InventorySnapshot> GetRealTimeInventoryAsync(string productCode, string locationCode);
    
    // Inventory optimization
    Task<InventoryOptimizationResult> OptimizeInventoryDistributionAsync(InventoryOptimizationRequest request);
    Task<List<InventoryRebalanceRecommendation>> GetRebalanceRecommendationsAsync();
}

public class InventorySnapshot
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<InventoryPosition> Positions { get; set; } = new();
    public Dictionary<Guid, decimal> TotalsByProduct { get; set; } = new();
    public Dictionary<Guid, decimal> TotalsByLocation { get; set; } = new();
    public InventoryValuation TotalValuation { get; set; } = new();
    public Quantity? AvailableQuantity { get; set; }
}

public class InventoryPosition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public Quantity AvailableQuantity { get; set; } = null!;
    public Quantity ReservedQuantity { get; set; } = null!;
    public Quantity TotalQuantity { get; set; } = null!;
    public DateTime LastUpdated { get; set; }
    public decimal AverageCost { get; set; }
    public InventoryStatus Status { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class InventoryBalance
{
    public Guid ProductId { get; set; }
    public Guid? LocationId { get; set; }
    public Quantity OpeningBalance { get; set; } = null!;
    public Quantity ClosingBalance { get; set; } = null!;
    public Quantity TotalReceipts { get; set; } = null!;
    public Quantity TotalDeliveries { get; set; } = null!;
    public Quantity TotalTransfersIn { get; set; } = null!;
    public Quantity TotalTransfersOut { get; set; } = null!;
    public Quantity NetMovement { get; set; } = null!;
    public DateTime BalanceDate { get; set; }
}

public class InventoryReceiptRequest
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public Quantity ReceivedQuantity { get; set; } = null!;
    public decimal UnitCost { get; set; }
    public string? Reference { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class InventoryDeliveryRequest
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public Quantity DeliveredQuantity { get; set; } = null!;
    public string? Reference { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime DeliveryDate { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class InventoryTransferRequest
{
    public Guid ProductId { get; set; }
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public Quantity TransferQuantity { get; set; } = null!;
    public string? Reference { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public decimal? TransferCost { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class InventoryAdjustmentRequest
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public Quantity AdjustmentQuantity { get; set; } = null!; // Can be positive or negative
    public InventoryAdjustmentReason Reason { get; set; }
    public string? Comments { get; set; }
    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;
    public Guid AdjustedBy { get; set; }
}

public class InventoryOperationResult
{
    public bool IsSuccessful { get; set; }
    public Guid OperationId { get; set; } = Guid.NewGuid();
    public string? ErrorMessage { get; set; }
    public InventoryPosition? UpdatedPosition { get; set; }
    public List<OilTrading.Application.DTOs.InventoryMovement> GeneratedMovements { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}



public class InventoryMetrics
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TotalProducts { get; set; }
    public int TotalLocations { get; set; }
    public InventoryValuation TotalValue { get; set; } = new();
    public Dictionary<string, decimal> ValueByProduct { get; set; } = new();
    public Dictionary<string, decimal> ValueByLocation { get; set; } = new();
    public InventoryTurnoverMetrics Turnover { get; set; } = new();
    public List<OilTrading.Application.DTOs.InventoryAlert> ActiveAlerts { get; set; } = new();
}

public class InventoryValuation
{
    public decimal TotalValue { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime ValuationDate { get; set; } = DateTime.UtcNow;
    public InventoryValuationMethod Method { get; set; } = InventoryValuationMethod.WeightedAverage;
}

public class InventoryTurnoverMetrics
{
    public decimal InventoryTurnoverRatio { get; set; }
    public TimeSpan AverageDaysOnHand { get; set; }
    public Dictionary<Guid, decimal> TurnoverByProduct { get; set; } = new();
    public Dictionary<Guid, TimeSpan> DaysOnHandByProduct { get; set; } = new();
}

public class InventoryThresholds
{
    public Quantity MinimumLevel { get; set; } = null!;
    public Quantity MaximumLevel { get; set; } = null!;
    public Quantity ReorderLevel { get; set; } = null!;
    public Quantity SafetyStock { get; set; } = null!;
    public bool EnableAlerts { get; set; } = true;
}

public class InventoryForecast
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public DateTime ForecastDate { get; set; }
    public List<InventoryForecastPeriod> Periods { get; set; } = new();
    public InventoryForecastAccuracy Accuracy { get; set; } = new();
}

public class InventoryForecastPeriod
{
    public DateTime Date { get; set; }
    public Quantity PredictedLevel { get; set; } = null!;
    public Quantity PredictedDemand { get; set; } = null!;
    public Quantity PredictedSupply { get; set; } = null!;
    public decimal ConfidenceLevel { get; set; }
}

public class InventoryForecastAccuracy
{
    public decimal MeanAbsoluteError { get; set; }
    public decimal MeanAbsolutePercentageError { get; set; }
    public decimal RootMeanSquareError { get; set; }
}

public class InventoryRecommendation
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public InventoryRecommendationType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Quantity RecommendedQuantity { get; set; } = null!;
    public DateTime RecommendedDate { get; set; }
    public InventoryRecommendationPriority Priority { get; set; }
    public decimal EstimatedImpact { get; set; }
}

public class InventoryAvailabilityCheck
{
    public Guid ContractId { get; set; }
    public bool IsAvailable { get; set; }
    public Quantity RequiredQuantity { get; set; } = null!;
    public Quantity AvailableQuantity { get; set; } = null!;
    public Quantity Shortfall { get; set; } = null!;
    public List<InventoryAllocationOption> AllocationOptions { get; set; } = new();
}

public class InventoryAllocationOption
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public Quantity AvailableQuantity { get; set; } = null!;
    public decimal AllocationScore { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class InventoryAllocationRequest
{
    public Guid ContractId { get; set; }
    public Quantity RequestedQuantity { get; set; } = null!;
    public List<Guid> PreferredLocations { get; set; } = new();
    public InventoryAllocationStrategy Strategy { get; set; } = InventoryAllocationStrategy.FirstAvailable;
    public Dictionary<string, object> Criteria { get; set; } = new();
}


public class InventoryAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LocationId { get; set; }
    public Quantity AllocatedQuantity { get; set; } = null!;
    public DateTime AllocationDate { get; set; } = DateTime.UtcNow;
    public InventoryAllocationStatus Status { get; set; } = InventoryAllocationStatus.Allocated;
}

public class InventoryOptimizationRequest
{
    public List<Guid> ProductIds { get; set; } = new();
    public List<Guid> LocationIds { get; set; } = new();
    public InventoryOptimizationGoal Goal { get; set; } = InventoryOptimizationGoal.MinimizeCost;
    public Dictionary<string, object> Constraints { get; set; } = new();
    public DateTime OptimizationDate { get; set; } = DateTime.UtcNow;
}

public class InventoryOptimizationResult
{
    public bool IsSuccessful { get; set; }
    public List<InventoryOptimizationRecommendation> Recommendations { get; set; } = new();
    public decimal EstimatedSavings { get; set; }
    public string? ErrorMessage { get; set; }
}

public class InventoryOptimizationRecommendation
{
    public Guid ProductId { get; set; }
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public Quantity RecommendedQuantity { get; set; } = null!;
    public string Rationale { get; set; } = string.Empty;
    public decimal EstimatedCostSaving { get; set; }
    public InventoryRecommendationPriority Priority { get; set; }
}

public class InventoryRebalanceRecommendation
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public List<InventoryRebalanceAction> Actions { get; set; } = new();
    public decimal EstimatedBenefit { get; set; }
    public InventoryRecommendationPriority Priority { get; set; }
}

public class InventoryRebalanceAction
{
    public InventoryRebalanceActionType ActionType { get; set; }
    public Guid? SourceLocationId { get; set; }
    public Guid? TargetLocationId { get; set; }
    public Quantity Quantity { get; set; } = null!;
    public string Rationale { get; set; } = string.Empty;
}

// Enums
public enum InventoryStatus
{
    Available,
    Reserved,
    InTransit,
    Blocked,
    Expired
}

public enum InventoryMovementType
{
    Receipt,
    Delivery,
    Transfer,
    Adjustment,
    Return,
    Disposal
}

public enum InventoryAdjustmentReason
{
    PhysicalCount,
    Shrinkage,
    Damage,
    Evaporation,
    QualityIssue,
    SystemCorrection,
    Other
}


public enum InventoryValuationMethod
{
    FIFO,
    LIFO,
    WeightedAverage,
    StandardCost
}

public enum InventoryRecommendationType
{
    Reorder,
    Transfer,
    Dispose,
    Reserve,
    Adjust
}

public enum InventoryRecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum InventoryAllocationStrategy
{
    FirstAvailable,
    ClosestLocation,
    LowestCost,
    HighestQuality,
    Balanced
}

public enum InventoryAllocationStatus
{
    Pending,
    Allocated,
    Reserved,
    Fulfilled,
    Cancelled
}

public enum InventoryOptimizationGoal
{
    MinimizeCost,
    MaximizeEfficiency,
    BalanceDistribution,
    MinimizeTransfers
}

public enum InventoryRebalanceActionType
{
    Transfer,
    Reorder,
    Dispose,
    Hold
}