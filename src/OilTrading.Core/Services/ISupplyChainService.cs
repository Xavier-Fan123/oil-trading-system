using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Services;

public interface ISupplyChainService
{
    // Inventory Management
    Task<IEnumerable<InventoryPosition>> GetInventoryPositionsAsync(int? locationId = null, int? productId = null);
    Task<InventoryPosition?> GetInventoryPositionAsync(int locationId, int productId);
    Task<InventoryPosition> UpdateInventoryAsync(int locationId, int productId, decimal quantity, string updatedBy);
    Task<InventoryMovement> CreateInventoryMovementAsync(CreateInventoryMovementRequest request);
    Task<IEnumerable<InventoryMovement>> GetInventoryMovementsAsync(InventoryMovementFilter filter);
    Task<InventoryLocationSummary> GetLocationInventorySummaryAsync(int locationId);
    
    // Port and Vessel Management
    Task<IEnumerable<Port>> GetPortsAsync(string? country = null, PortType? portType = null);
    Task<Port?> GetPortByCodeAsync(string portCode);
    Task<IEnumerable<PortBerth>> GetAvailableBerthsAsync(int portId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<VesselCall> CreateVesselCallAsync(CreateVesselCallRequest request);
    Task<VesselCall> UpdateVesselCallAsync(int vesselCallId, UpdateVesselCallRequest request);
    Task<IEnumerable<VesselCall>> GetVesselCallsAsync(VesselCallFilter filter);
    Task<PortOperationsReport> GetPortOperationsReportAsync(int portId, DateTime startDate, DateTime endDate);
    
    // Documentation Management
    Task<BillOfLading> CreateBillOfLadingAsync(CreateBillOfLadingRequest request);
    Task<BillOfLading> UpdateBillOfLadingAsync(int blId, UpdateBillOfLadingRequest request);
    Task<BillOfLading?> GetBillOfLadingAsync(int blId);
    Task<IEnumerable<BillOfLading>> GetBillsOfLadingAsync(BillOfLadingFilter filter);
    Task<QuantityCertificate> CreateQuantityCertificateAsync(CreateQuantityCertificateRequest request);
    Task<IEnumerable<QuantityCertificate>> GetQuantityCertificatesAsync(int? billOfLadingId = null);
    
    // Supply Chain Analytics
    Task<SupplyChainMetrics> GetSupplyChainMetricsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<InventoryAging>> GetInventoryAgingReportAsync();
    Task<LogisticsOptimizationResult> OptimizeLogisticsAsync(LogisticsOptimizationRequest request);
}

public class CreateInventoryMovementRequest
{
    public int FromLocationId { get; set; }
    public int ToLocationId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; } = QuantityUnit.MT;
    public InventoryMovementType MovementType { get; set; }
    public DateTime PlannedDate { get; set; }
    public string? TransportMode { get; set; }
    public string? VesselName { get; set; }
    public string? TransportReference { get; set; }
    public decimal? TransportCost { get; set; }
    public string? InitiatedBy { get; set; }
    public string? Notes { get; set; }
    public int? PurchaseContractId { get; set; }
    public int? SalesContractId { get; set; }
    public int? ShippingOperationId { get; set; }
}

public class InventoryMovementFilter
{
    public int? LocationId { get; set; }
    public int? ProductId { get; set; }
    public InventoryMovementType? MovementType { get; set; }
    public InventoryMovementStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateVesselCallRequest
{
    public string VesselName { get; set; } = string.Empty;
    public string? IMONumber { get; set; }
    public int PortId { get; set; }
    public int? BerthId { get; set; }
    public VesselCallPurpose Purpose { get; set; }
    public DateTime? ETA { get; set; }
    public DateTime? ETD { get; set; }
    public int? ProductId { get; set; }
    public decimal? CargoQuantity { get; set; }
    public string? CargoGrade { get; set; }
    public int? TradingPartnerId { get; set; }
    public string? CharterParty { get; set; }
    public int? PurchaseContractId { get; set; }
    public int? SalesContractId { get; set; }
    public int? ShippingOperationId { get; set; }
    public string? Agent { get; set; }
    public string? Notes { get; set; }
}

public class UpdateVesselCallRequest
{
    public VesselCallStatus? Status { get; set; }
    public DateTime? ETA { get; set; }
    public DateTime? ETD { get; set; }
    public DateTime? ATA { get; set; }
    public DateTime? ATD { get; set; }
    public int? BerthId { get; set; }
    public DateTime? LoadingStart { get; set; }
    public DateTime? LoadingEnd { get; set; }
    public DateTime? DischargingStart { get; set; }
    public DateTime? DischargingEnd { get; set; }
    public decimal? ActualLoadedQuantity { get; set; }
    public decimal? ActualDischargedQuantity { get; set; }
    public string? Notes { get; set; }
}

public class VesselCallFilter
{
    public int? PortId { get; set; }
    public VesselCallStatus? Status { get; set; }
    public VesselCallPurpose? Purpose { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? VesselName { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateBillOfLadingRequest
{
    public string BLNumber { get; set; } = string.Empty;
    public BillOfLadingType BLType { get; set; }
    public string IssuedBy { get; set; } = string.Empty;
    public string IssuerCompany { get; set; } = string.Empty;
    public string Shipper { get; set; } = string.Empty;
    public string ShipperAddress { get; set; } = string.Empty;
    public string Consignee { get; set; } = string.Empty;
    public string ConsigneeAddress { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public string PortOfLoading { get; set; } = string.Empty;
    public string PortOfDischarge { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string CargoDescription { get; set; } = string.Empty;
    public decimal BLQuantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; } = QuantityUnit.MT;
    public string? Grade { get; set; }
    public DateTime? OnBoardDate { get; set; }
    public string FreightTerms { get; set; } = string.Empty;
    public int? PurchaseContractId { get; set; }
    public int? SalesContractId { get; set; }
    public int? ShippingOperationId { get; set; }
    public int? VesselCallId { get; set; }
}

public class UpdateBillOfLadingRequest
{
    public BillOfLadingStatus? Status { get; set; }
    public decimal? OutturnQuantity { get; set; }
    public DateTime? DischargingDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? Remarks { get; set; }
}

public class BillOfLadingFilter
{
    public BillOfLadingStatus? Status { get; set; }
    public string? VesselName { get; set; }
    public string? PortOfLoading { get; set; }
    public string? PortOfDischarge { get; set; }
    public int? ProductId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateQuantityCertificateRequest
{
    public string CertificateNumber { get; set; } = string.Empty;
    public QuantityCertificateType CertificateType { get; set; }
    public string IssuedBy { get; set; } = string.Empty;
    public string IssuerCompany { get; set; } = string.Empty;
    public int? BillOfLadingId { get; set; }
    public int ProductId { get; set; }
    public decimal CertifiedQuantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; } = QuantityUnit.MT;
    public string MeasurementMethod { get; set; } = string.Empty;
    public decimal? Density { get; set; }
    public decimal? Temperature { get; set; }
    public DateTime MeasurementDate { get; set; }
    public string MeasuredBy { get; set; } = string.Empty;
    public int? VesselCallId { get; set; }
    public int? ShippingOperationId { get; set; }
}

public class InventoryLocationSummary
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public decimal TotalCapacity { get; set; }
    public decimal UsedCapacity { get; set; }
    public decimal AvailableCapacity { get; set; }
    public decimal UtilizationPercentage { get; set; }
    public int ProductCount { get; set; }
    public decimal TotalValue { get; set; }
    public IEnumerable<ProductInventorySummary> ProductSummaries { get; set; } = new List<ProductInventorySummary>();
}

public class ProductInventorySummary
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public decimal AverageCost { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class PortOperationsReport
{
    public int PortId { get; set; }
    public string PortName { get; set; } = string.Empty;
    public DateTime ReportStart { get; set; }
    public DateTime ReportEnd { get; set; }
    public int TotalVesselCalls { get; set; }
    public int CompletedCalls { get; set; }
    public decimal TotalCargoHandled { get; set; }
    public decimal AveragePortStay { get; set; } // Days
    public decimal BerthUtilization { get; set; } // Percentage
    public IEnumerable<CargoTypeSummary> CargoByType { get; set; } = new List<CargoTypeSummary>();
}

public class CargoTypeSummary
{
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public int VesselCount { get; set; }
    public decimal AverageParcelSize { get; set; }
}

public class SupplyChainMetrics
{
    public DateTime AsOf { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int ActiveLocations { get; set; }
    public int ActiveVesselCalls { get; set; }
    public int PendingMovements { get; set; }
    public decimal AverageInventoryTurnover { get; set; }
    public decimal StorageUtilization { get; set; }
    public decimal TotalTransportCosts { get; set; }
    public IEnumerable<LocationMetrics> LocationMetrics { get; set; } = new List<LocationMetrics>();
}

public class LocationMetrics
{
    public string LocationName { get; set; } = string.Empty;
    public decimal InventoryValue { get; set; }
    public decimal Utilization { get; set; }
    public int MovementsCount { get; set; }
    public decimal TurnoverRate { get; set; }
}

public class InventoryAging
{
    public string LocationName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime ReceivedDate { get; set; }
    public int AgeDays { get; set; }
    public decimal Value { get; set; }
    public string? QualityStatus { get; set; }
}

public class LogisticsOptimizationRequest
{
    public int[] SourceLocationIds { get; set; } = Array.Empty<int>();
    public int[] DestinationLocationIds { get; set; } = Array.Empty<int>();
    public int ProductId { get; set; }
    public decimal RequiredQuantity { get; set; }
    public DateTime RequiredDate { get; set; }
    public string[] PreferredTransportModes { get; set; } = Array.Empty<string>();
    public decimal MaxTransportCost { get; set; }
}

public class LogisticsOptimizationResult
{
    public bool IsOptimal { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalTime { get; set; } // Hours
    public IEnumerable<OptimizedMovement> RecommendedMovements { get; set; } = new List<OptimizedMovement>();
    public string? OptimizationNotes { get; set; }
}

public class OptimizedMovement
{
    public int FromLocationId { get; set; }
    public string FromLocationName { get; set; } = string.Empty;
    public int ToLocationId { get; set; }
    public string ToLocationName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string TransportMode { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal TimeHours { get; set; }
    public int Priority { get; set; }
}