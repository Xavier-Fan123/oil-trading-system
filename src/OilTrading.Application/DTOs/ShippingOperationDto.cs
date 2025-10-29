namespace OilTrading.Application.DTOs;

public class ShippingOperationDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public string? ImoNumber { get; set; }
    public decimal PlannedQuantity { get; set; }
    public string PlannedQuantityUnit { get; set; } = string.Empty;
    public decimal? ActualQuantity { get; set; }
    public string? ActualQuantityUnit { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public DateTime? NorDate { get; set; }
    public DateTime? BillOfLadingDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public string? Notes { get; set; }
    
    // Related pricing events
    public ICollection<PricingEventSummaryDto> PricingEvents { get; set; } = new List<PricingEventSummaryDto>();
    
    // Audit Information
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class ShippingOperationSummaryDto
{
    public Guid Id { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public string PlannedQuantityUnit { get; set; } = string.Empty;
    public decimal? ActualQuantity { get; set; }
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public DateTime? BillOfLadingDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateShippingOperationDto
{
    public Guid ContractId { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string? ImoNumber { get; set; }
    public string? ChartererName { get; set; }
    public decimal? VesselCapacity { get; set; }
    public string? ShippingAgent { get; set; }
    public decimal PlannedQuantity { get; set; }
    public string PlannedQuantityUnit { get; set; } = "MT";
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public string? Notes { get; set; }
}

public class UpdateShippingOperationDto
{
    public string? VesselName { get; set; }
    public string? ImoNumber { get; set; }
    public decimal? PlannedQuantity { get; set; }
    public string? PlannedQuantityUnit { get; set; }
    public decimal? ActualQuantity { get; set; }
    public string? ActualQuantityUnit { get; set; }
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public DateTime? NorDate { get; set; }
    public DateTime? BillOfLadingDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public string? Notes { get; set; }
}

public class RecordLiftingOperationDto
{
    public Guid ShippingOperationId { get; set; }
    public DateTime? NorDate { get; set; }
    public DateTime? BillOfLadingDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public decimal? ActualQuantity { get; set; }
    public string? ActualQuantityUnit { get; set; }
    public string? Notes { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}