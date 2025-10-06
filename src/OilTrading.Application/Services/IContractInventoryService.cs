using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Services;

/// <summary>
/// Service responsible for managing the relationship between contracts and inventory
/// </summary>
public interface IContractInventoryService
{
    // Contract Activation and Inventory Reservation
    Task<ContractInventoryResult> ReserveInventoryForContractAsync(Guid contractId, string contractType);
    Task<ContractInventoryResult> ReleaseInventoryReservationAsync(Guid contractId, string reason);
    Task<ContractInventoryResult> PartialReleaseInventoryAsync(Guid contractId, Quantity releaseQuantity, string reason);

    // Inventory Availability Checks
    Task<InventoryAvailabilityResult> CheckInventoryAvailabilityAsync(
        string productCode, 
        string locationCode, 
        Quantity requiredQuantity);
    
    Task<InventoryAvailabilityResult> CheckInventoryAvailabilityForContractAsync(Guid contractId);

    // Contract Execution and Inventory Movement
    Task<ContractInventoryResult> ExecuteInventoryMovementAsync(Guid contractId, Quantity actualQuantity, string movementType);
    Task<ContractInventoryResult> ProcessContractDeliveryAsync(Guid contractId, Quantity deliveredQuantity, string deliveryReference);
    Task<ContractInventoryResult> ProcessContractReceiptAsync(Guid contractId, Quantity receivedQuantity, string receiptReference);

    // Reservation Management
    Task<List<InventoryReservation>> GetActiveReservationsAsync();
    Task<List<InventoryReservation>> GetReservationsByContractAsync(Guid contractId);
    Task<List<InventoryReservation>> GetReservationsByProductAsync(string productCode);
    Task<List<InventoryReservation>> GetReservationsByLocationAsync(string locationCode);
    Task<List<InventoryReservation>> GetExpiredReservationsAsync();

    // Reservation Operations
    Task<ContractInventoryResult> ExtendReservationAsync(Guid reservationId, DateTime newExpiryDate, string reason);
    Task<ContractInventoryResult> CancelReservationAsync(Guid reservationId, string reason);
    Task<ContractInventoryResult> ProcessExpiredReservationsAsync();

    // Inventory Allocation and Optimization
    Task<ContractInventoryAllocationResult> AllocateInventoryOptimallyAsync(
        List<ContractInventoryRequest> requests);
    
    Task<InventoryRebalanceResult> RebalanceInventoryAsync(
        List<InventoryRebalanceRequest> rebalanceRequests);

    // Reporting and Analytics
    Task<InventoryReservationSummary> GetReservationSummaryAsync(DateTime? asOfDate = null);
    Task<InventoryUtilizationReport> GetInventoryUtilizationReportAsync(
        DateTime startDate, 
        DateTime endDate, 
        string? productCode = null, 
        string? locationCode = null);
    
    Task<List<InventoryAlert>> GetInventoryAlertsAsync();

    // Validation and Business Rules
    Task<ValidationResult> ValidateContractInventoryRequirementsAsync(Guid contractId);
    Task<ValidationResult> ValidateInventoryMovementAsync(Guid contractId, Quantity quantity, string movementType);

    // Integration with Contract Lifecycle
    Task<ContractInventoryResult> OnContractActivatedAsync(Guid contractId, string contractType);
    Task<ContractInventoryResult> OnContractCancelledAsync(Guid contractId, string reason);
    Task<ContractInventoryResult> OnContractCompletedAsync(Guid contractId);
    Task<ContractInventoryResult> OnContractModifiedAsync(Guid contractId, Quantity oldQuantity, Quantity newQuantity);
}

// Result and Request Classes
public class ContractInventoryResult
{
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public Guid? ReservationId { get; set; }
    public Quantity? ReservedQuantity { get; set; }
    public Quantity? AvailableQuantity { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class InventoryAvailabilityResult
{
    public bool IsAvailable { get; set; }
    public Quantity AvailableQuantity { get; set; } = null!;
    public Quantity RequestedQuantity { get; set; } = null!;
    public Quantity ShortfallQuantity { get; set; } = null!;
    public List<InventoryLocation> AvailableLocations { get; set; } = new();
    public List<AlternativeProduct> AlternativeProducts { get; set; } = new();
    public DateTime? EarliestAvailabilityDate { get; set; }
}

public class InventoryLocation
{
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public Quantity AvailableQuantity { get; set; } = null!;
    public decimal TransferCost { get; set; }
    public TimeSpan TransferTime { get; set; }
}

public class AlternativeProduct
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Quantity AvailableQuantity { get; set; } = null!;
    public decimal PriceDifferential { get; set; }
    public string QualityDifference { get; set; } = string.Empty;
}

public class ContractInventoryRequest
{
    public Guid ContractId { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string PreferredLocationCode { get; set; } = string.Empty;
    public Quantity RequiredQuantity { get; set; } = null!;
    public DateTime RequiredDate { get; set; }
    public int Priority { get; set; } = 1; // 1 = highest, 10 = lowest
    public List<string> AcceptableLocations { get; set; } = new();
    public List<string> AcceptableProducts { get; set; } = new();
}

public class ContractInventoryAllocationResult
{
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ContractAllocation> Allocations { get; set; } = new();
    public List<ContractInventoryRequest> UnallocatedRequests { get; set; } = new();
    public decimal OptimizationScore { get; set; } // Higher is better
}

public class ContractAllocation
{
    public Guid ContractId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public Quantity AllocatedQuantity { get; set; } = null!;
    public Guid ReservationId { get; set; }
    public decimal AllocationCost { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class InventoryRebalanceRequest
{
    public string ProductCode { get; set; } = string.Empty;
    public string FromLocationCode { get; set; } = string.Empty;
    public string ToLocationCode { get; set; } = string.Empty;
    public Quantity Quantity { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public DateTime? ScheduledDate { get; set; }
}

public class InventoryRebalanceResult
{
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public List<InventoryMovement> Movements { get; set; } = new();
    public decimal TotalCost { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
}

public class InventoryMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProductCode { get; set; } = string.Empty;
    public string FromLocationCode { get; set; } = string.Empty;
    public string ToLocationCode { get; set; } = string.Empty;
    public Quantity Quantity { get; set; } = null!;
    public string MovementType { get; set; } = string.Empty; // Transfer, Receipt, Delivery, Adjustment
    public string Reference { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string Status { get; set; } = "Scheduled";
}

public class InventoryReservationSummary
{
    public DateTime AsOfDate { get; set; }
    public int TotalActiveReservations { get; set; }
    public Quantity TotalReservedQuantity { get; set; } = null!;
    public Dictionary<string, Quantity> ReservationsByProduct { get; set; } = new();
    public Dictionary<string, Quantity> ReservationsByLocation { get; set; } = new();
    public Dictionary<string, int> ReservationsByContractType { get; set; } = new();
    public List<TopReservation> TopReservationsByAmount { get; set; } = new();
    public int ExpiredReservations { get; set; }
    public int ExpiringWithin7Days { get; set; }
}

public class TopReservation
{
    public Guid ReservationId { get; set; }
    public Guid ContractId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public Quantity Quantity { get; set; } = null!;
    public DateTime ReservationDate { get; set; }
    public string ContractType { get; set; } = string.Empty;
}

public class InventoryUtilizationReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ProductCode { get; set; }
    public string? LocationCode { get; set; }
    public decimal AverageUtilization { get; set; }
    public decimal PeakUtilization { get; set; }
    public decimal LowestUtilization { get; set; }
    public List<InventoryUtilizationPeriod> UtilizationByPeriod { get; set; } = new();
    public List<InventoryMetric> KeyMetrics { get; set; } = new();
}

public class InventoryUtilizationPeriod
{
    public DateTime Date { get; set; }
    public Quantity TotalInventory { get; set; } = null!;
    public Quantity ReservedInventory { get; set; } = null!;
    public decimal UtilizationPercentage { get; set; }
    public int ActiveReservations { get; set; }
}

public class InventoryMetric
{
    public string MetricName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class InventoryAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public InventoryAlertType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public InventoryAlertSeverity Severity { get; set; }
    public string? ProductCode { get; set; }
    public string? LocationCode { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? ReservationId { get; set; }
    public DateTime TriggeredDate { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}



// Enums
public enum InventoryAlertType
{
    LowInventory = 1,
    ExpiredReservation = 2,
    ExpiringReservation = 3,
    OverReservation = 4,
    UnallocatedReservation = 5,
    InventoryShortfall = 6,
    UnusualActivity = 7
}

public enum InventoryAlertSeverity
{
    Info = 1,
    Warning = 2,
    High = 3,
    Critical = 4
}