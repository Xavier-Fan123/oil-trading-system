namespace OilTrading.Application.DTOs;

public class PricingEventDto
{
    public Guid Id { get; set; }
    public Guid ShippingOperationId { get; set; }
    public string ShippingOperationVessel { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime PricingStartDate { get; set; }
    public DateTime PricingEndDate { get; set; }
    public bool IsConfirmed { get; set; }
    public string? ConfirmedBy { get; set; }
    public string? Notes { get; set; }
    public int PricingDays { get; set; }
    
    // Audit Information
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class PricingEventSummaryDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime PricingStartDate { get; set; }
    public DateTime PricingEndDate { get; set; }
    public bool IsConfirmed { get; set; }
    public int PricingDays { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePricingEventDto
{
    public Guid ShippingOperationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime PricingStartDate { get; set; }
    public DateTime PricingEndDate { get; set; }
    public string? Notes { get; set; }
}

public class ConfirmPricingEventDto
{
    public Guid Id { get; set; }
    public string? Notes { get; set; }
    public string ConfirmedBy { get; set; } = string.Empty;
}

public class ContractPricingEventDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime PricingStartDate { get; set; }
    public DateTime PricingEndDate { get; set; }
    public decimal? AveragePrice { get; set; }
    public bool IsFinalized { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? PricingBenchmark { get; set; }
    public int? PricingDaysCount { get; set; }
    
    // Audit Information
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}