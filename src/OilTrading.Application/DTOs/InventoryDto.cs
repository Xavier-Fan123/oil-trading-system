using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.DTOs;

public class InventoryLocationDto
{
    public Guid Id { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Coordinates { get; set; }
    public bool IsActive { get; set; }
    public string? OperatorName { get; set; }
    public string? ContactInfo { get; set; }
    
    // Capacity information
    public decimal TotalCapacity { get; set; }
    public decimal AvailableCapacity { get; set; }
    public decimal UsedCapacity { get; set; }
    public string CapacityUnit { get; set; } = "MT";
    
    // Operational details
    public string[]? SupportedProducts { get; set; }
    public string[]? HandlingServices { get; set; }
    public bool HasRailAccess { get; set; }
    public bool HasRoadAccess { get; set; }
    public bool HasSeaAccess { get; set; }
    public bool HasPipelineAccess { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Summary information
    public int InventoryPositionsCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
}

public class InventoryPositionDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = "MT";
    public decimal AverageCost { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal TotalValue { get; set; }
    
    public DateTime LastUpdated { get; set; }
    public string? Grade { get; set; }
    public string? BatchReference { get; set; }
    
    // Quality specifications
    public decimal? Sulfur { get; set; }
    public decimal? API { get; set; }
    public decimal? Viscosity { get; set; }
    public string? QualityNotes { get; set; }
    
    // Aging and status
    public DateTime? ReceivedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? StatusNotes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class InventoryMovementDto
{
    public Guid Id { get; set; }
    public Guid FromLocationId { get; set; }
    public string FromLocationName { get; set; } = string.Empty;
    public string FromLocationCode { get; set; } = string.Empty;
    public Guid ToLocationId { get; set; }
    public string ToLocationName { get; set; } = string.Empty;
    public string ToLocationCode { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = "MT";
    public string MovementType { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; }
    public DateTime? PlannedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    
    public string MovementReference { get; set; } = string.Empty;
    public string? TransportMode { get; set; }
    public string? VesselName { get; set; }
    public string? TransportReference { get; set; }
    
    // Cost tracking
    public decimal? TransportCost { get; set; }
    public decimal? HandlingCost { get; set; }
    public decimal? TotalCost { get; set; }
    public string? CostCurrency { get; set; }
    
    public string? InitiatedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }
    
    // Related contracts
    public Guid? PurchaseContractId { get; set; }
    public string? PurchaseContractNumber { get; set; }
    public Guid? SalesContractId { get; set; }
    public string? SalesContractNumber { get; set; }
    public Guid? ShippingOperationId { get; set; }
    public string? ShippingOperationReference { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Request DTOs for creating/updating
public class CreateInventoryLocationRequest
{
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public InventoryLocationType LocationType { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Coordinates { get; set; }
    public string? OperatorName { get; set; }
    public string? ContactInfo { get; set; }
    
    public decimal TotalCapacity { get; set; }
    public QuantityUnit CapacityUnit { get; set; } = QuantityUnit.MT;
    
    public string[]? SupportedProducts { get; set; }
    public string[]? HandlingServices { get; set; }
    public bool HasRailAccess { get; set; }
    public bool HasRoadAccess { get; set; }
    public bool HasSeaAccess { get; set; }
    public bool HasPipelineAccess { get; set; }
}

public class UpdateInventoryLocationRequest : CreateInventoryLocationRequest
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateInventoryPositionRequest
{
    public Guid LocationId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; } = QuantityUnit.MT;
    public decimal AverageCost { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Grade { get; set; }
    public string? BatchReference { get; set; }
    
    // Quality specifications
    public decimal? Sulfur { get; set; }
    public decimal? API { get; set; }
    public decimal? Viscosity { get; set; }
    public string? QualityNotes { get; set; }
    
    public DateTime? ReceivedDate { get; set; }
    public InventoryStatus Status { get; set; } = InventoryStatus.Available;
    public string? StatusNotes { get; set; }
}

public class UpdateInventoryPositionRequest : CreateInventoryPositionRequest
{
    public Guid Id { get; set; }
}

public class CreateInventoryMovementRequest
{
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; } = QuantityUnit.MT;
    public OilTrading.Application.DTOs.InventoryMovementType MovementType { get; set; }
    public DateTime MovementDate { get; set; }
    public DateTime? PlannedDate { get; set; }
    
    public string? TransportMode { get; set; }
    public string? VesselName { get; set; }
    public string? TransportReference { get; set; }
    
    public decimal? TransportCost { get; set; }
    public decimal? HandlingCost { get; set; }
    public string? CostCurrency { get; set; }
    
    public string? Notes { get; set; }
    public Guid? PurchaseContractId { get; set; }
    public Guid? SalesContractId { get; set; }
    public Guid? ShippingOperationId { get; set; }
}

public class UpdateInventoryMovementRequest : CreateInventoryMovementRequest
{
    public Guid Id { get; set; }
    public OilTrading.Application.DTOs.InventoryMovementStatus Status { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

// Summary DTOs
public class InventorySummaryDto
{
    public int TotalLocations { get; set; }
    public int ActiveLocations { get; set; }
    public int TotalProducts { get; set; }
    public decimal TotalInventoryQuantity { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public string Currency { get; set; } = "USD";
    public int PendingMovements { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class LocationSummaryDto
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public decimal UtilizationPercentage { get; set; }
    public int ProductCount { get; set; }
    public decimal TotalValue { get; set; }
    public string Currency { get; set; } = "USD";
}