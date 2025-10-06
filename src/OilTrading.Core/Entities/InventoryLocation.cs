using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

public class InventoryLocation : BaseEntity
{
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public InventoryLocationType LocationType { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Coordinates { get; set; } // GPS coordinates
    public bool IsActive { get; set; } = true;
    public string? OperatorName { get; set; }
    public string? ContactInfo { get; set; }
    
    // Capacity information
    public Quantity TotalCapacity { get; set; } = new(0, QuantityUnit.MT);
    public Quantity AvailableCapacity { get; set; } = new(0, QuantityUnit.MT);
    public Quantity UsedCapacity { get; set; } = new(0, QuantityUnit.MT);
    
    // Operational details
    public string[]? SupportedProducts { get; set; }
    public string[]? HandlingServices { get; set; }
    public bool HasRailAccess { get; set; }
    public bool HasRoadAccess { get; set; }
    public bool HasSeaAccess { get; set; }
    public bool HasPipelineAccess { get; set; }
    
    // Navigation properties
    public ICollection<InventoryPosition> Inventories { get; set; } = new List<InventoryPosition>();
    public ICollection<InventoryMovement> Movements { get; set; } = new List<InventoryMovement>();
}

public class InventoryPosition : BaseEntity
{
    public Guid LocationId { get; set; }
    public InventoryLocation Location { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Quantity Quantity { get; set; } = new(0, QuantityUnit.MT);
    public Money AverageCost { get; set; } = new(0, "USD");
    public Money TotalValue => new(Quantity.Value * AverageCost.Amount, AverageCost.Currency);
    public DateTime LastUpdated { get; set; }
    public string? Grade { get; set; }
    public string? BatchReference { get; set; }
    
    // Quality specifications
    public decimal? Sulfur { get; set; } // Sulfur content %
    public decimal? API { get; set; } // API gravity
    public decimal? Viscosity { get; set; }
    public string? QualityNotes { get; set; }
    
    // Aging and status
    public DateTime? ReceivedDate { get; set; }
    public InventoryStatus Status { get; set; }
    public string? StatusNotes { get; set; }
    
    // Navigation properties
    public ICollection<InventoryMovement> IncomingMovements { get; set; } = new List<InventoryMovement>();
    public ICollection<InventoryMovement> OutgoingMovements { get; set; } = new List<InventoryMovement>();
}

public class InventoryMovement : BaseEntity
{
    public Guid FromLocationId { get; set; }
    public InventoryLocation FromLocation { get; set; } = null!;
    public Guid ToLocationId { get; set; }
    public InventoryLocation ToLocation { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    public Quantity Quantity { get; set; } = new(0, QuantityUnit.MT);
    public InventoryMovementType MovementType { get; set; }
    public DateTime MovementDate { get; set; }
    public DateTime? PlannedDate { get; set; }
    public InventoryMovementStatus Status { get; set; }
    
    public string MovementReference { get; set; } = string.Empty;
    public string? TransportMode { get; set; } // Vessel, Truck, Rail, Pipeline
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
    public PurchaseContract? PurchaseContract { get; set; }
    public Guid? SalesContractId { get; set; }
    public SalesContract? SalesContract { get; set; }
    public Guid? ShippingOperationId { get; set; }
    public ShippingOperation? ShippingOperation { get; set; }
}

public enum InventoryLocationType
{
    Terminal = 1,
    Tank = 2,
    Refinery = 3,
    Port = 4,
    Pipeline = 5,
    Storage = 6,
    Floating = 7 // Floating storage vessel
}

public enum InventoryStatus
{
    Available = 1,
    Reserved = 2,
    InTransit = 3,
    Quality = 4, // Under quality check
    Blocked = 5,
    Contaminated = 6,
    Aged = 7 // Old inventory
}

public enum InventoryMovementType
{
    Receipt = 1,    // Receiving inventory
    Shipment = 2,   // Shipping out inventory
    Transfer = 3,   // Internal transfer
    Blending = 4,   // Blending operation
    Loss = 5,       // Inventory loss
    Adjustment = 6  // Inventory adjustment
}

public enum InventoryMovementStatus
{
    Planned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Failed = 5
}