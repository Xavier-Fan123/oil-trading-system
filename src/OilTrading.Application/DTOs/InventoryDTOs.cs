using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.DTOs;

public class InventoryAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LocationId { get; set; }
    public Quantity AllocatedQuantity { get; set; } = null!;
    public DateTime AllocationDate { get; set; } = DateTime.UtcNow;
    public InventoryAllocationStatus Status { get; set; } = InventoryAllocationStatus.Allocated;
    public string? ReservationReference { get; set; }
    public Dictionary<string, object> MetaData { get; set; } = new();
}

public class InventoryMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public Guid ProductId { get; set; }
    public Quantity Quantity { get; set; } = null!;
    public InventoryMovementType MovementType { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public DateTime? PlannedDate { get; set; }
    public InventoryMovementStatus Status { get; set; }
    
    public string MovementReference { get; set; } = string.Empty;
    public string? TransportMode { get; set; }
    public string? VesselName { get; set; }
    public string? TransportReference { get; set; }
    
    // Cost tracking
    public Money? TransportCost { get; set; }
    public Money? HandlingCost { get; set; }
    public Money? TotalCost { get; set; }
    
    public string? InitiatedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }
    
    // Related contracts or orders
    public Guid? PurchaseContractId { get; set; }
    public Guid? SalesContractId { get; set; }
    public Guid? ShippingOperationId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum InventoryMovementType
{
    Receipt = 1,
    Shipment = 2,
    Transfer = 3,
    Blending = 4,
    Loss = 5,
    Adjustment = 6
}

public enum InventoryMovementStatus
{
    Planned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Failed = 5
}

public enum InventoryAllocationStatus
{
    Pending = 1,
    Allocated = 2,
    Reserved = 3,
    Fulfilled = 4,
    Cancelled = 5
}

// Additional inventory operation DTOs
public class InventoryReservationRequest
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public Quantity RequestedQuantity { get; set; } = null!;
    public Guid ContractId { get; set; }
    public DateTime RequiredByDate { get; set; }
    public string? Reference { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public Quantity Quantity { get; set; } = null!;
    public string? ReservationReference { get; set; }
    public string? Notes { get; set; }
}

public class InventoryReleaseRequest
{
    public Guid ReservationId { get; set; }
    public Quantity? PartialQuantity { get; set; }
    public string? Reason { get; set; }
    public string? ReservationReference { get; set; }
    public Quantity Quantity { get; set; } = null!;
}

public class InventoryReservationResult
{
    public bool IsSuccessful { get; set; }
    public Guid ReservationId { get; set; }
    public Quantity ReservedQuantity { get; set; } = null!;
    public string? ErrorMessage { get; set; }
    public List<ReservationDetail> ReservationDetails { get; set; } = new();
}

public class ReservationDetail
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public Quantity Quantity { get; set; } = null!;
}

public class InventoryMovementResult
{
    public bool IsSuccessful { get; set; }
    public Guid MovementId { get; set; }
    public string? ErrorMessage { get; set; }
}