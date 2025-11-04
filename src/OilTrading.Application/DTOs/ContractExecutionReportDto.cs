namespace OilTrading.Application.DTOs;

public class ContractExecutionReportDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public DateTime ReportGeneratedDate { get; set; }

    // Contract Basic Information
    public Guid? TradingPartnerId { get; set; }
    public string TradingPartnerName { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public string ContractStatus { get; set; } = string.Empty;

    // Execution Metrics
    public decimal? ContractValue { get; set; }
    public string? Currency { get; set; }
    public decimal? ExecutedQuantity { get; set; }
    public decimal ExecutionPercentage { get; set; }

    // Dates
    public DateTime? CreatedDate { get; set; }
    public DateTime? ActivatedDate { get; set; }
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public DateTime? SettlementDate { get; set; }
    public DateTime? CompletionDate { get; set; }

    // Settlement Information
    public int SettlementCount { get; set; }
    public decimal TotalSettledAmount { get; set; }
    public decimal PaidSettledAmount { get; set; }
    public decimal UnpaidSettledAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;

    // Shipping/Logistics Information
    public int ShippingOperationCount { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public string? DeliveryTerms { get; set; }

    // Performance Indicators
    public int DaysToActivation { get; set; }
    public int DaysToCompletion { get; set; }
    public bool IsOnSchedule { get; set; }
    public string ExecutionStatus { get; set; } = string.Empty;

    // Pricing Information
    public decimal? BenchmarkPrice { get; set; }
    public decimal? AdjustmentPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public bool IsPriceFinalized { get; set; }

    // Risk & Compliance
    public bool HasRiskViolations { get; set; }
    public bool IsCompliant { get; set; }

    // Metadata
    public string? Notes { get; set; }
    public DateTime LastUpdatedDate { get; set; }
}
