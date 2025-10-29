using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.DTOs;

public class SalesContractDto
{
    public Guid Id { get; set; }
    public ContractNumberDto ContractNumber { get; set; } = new();
    public string? ExternalContractNumber { get; set; }
    public ContractType ContractType { get; set; }
    public ContractStatus Status { get; set; }
    
    // Nested Customer Information
    public CustomerDto Customer { get; set; } = new();
    
    // Nested Product Information
    public ContractProductDto Product { get; set; } = new();
    
    // Trader Information
    public Guid TraderId { get; set; }
    public string TraderName { get; set; } = string.Empty;
    
    // Linked Purchase Contract
    public Guid? LinkedPurchaseContractId { get; set; }
    public string? LinkedPurchaseContractNumber { get; set; }
    
    // Quantity Information
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; }
    public decimal TonBarrelRatio { get; set; }
    
    // Price Benchmark Information  
    public Guid? PriceBenchmarkId { get; set; }
    public string? PriceBenchmarkName { get; set; }
    
    // Pricing Information
    public string? PricingFormula { get; set; }
    public decimal? ContractValue { get; set; }
    public string? ContractValueCurrency { get; set; }
    public decimal? ProfitMargin { get; set; }
    public DateTime? PricingPeriodStart { get; set; }
    public DateTime? PricingPeriodEnd { get; set; }
    public bool IsPriceFinalized { get; set; }
    public decimal? Premium { get; set; }
    public decimal? Discount { get; set; }
    
    // Delivery Information
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public DeliveryTerms DeliveryTerms { get; set; }
    
    // Payment Information
    public string? PaymentTerms { get; set; }
    public int? CreditPeriodDays { get; set; }
    public ContractPaymentMethod SettlementType { get; set; }
    public decimal? PrepaymentPercentage { get; set; }
    
    // Additional Information
    public string? Incoterms { get; set; }
    public string? QualitySpecifications { get; set; }
    public string? InspectionAgency { get; set; }
    public string? Notes { get; set; }
    
    // Business Metrics - NEW for frontend alignment
    public decimal? EstimatedProfit { get; set; }
    public decimal? Margin { get; set; }
    public RiskMetricsDto? RiskMetrics { get; set; }
    
    // Shipping Operations
    public ICollection<ShippingOperationSummaryDto> ShippingOperations { get; set; } = new List<ShippingOperationSummaryDto>();
    
    // Pricing Events
    public ICollection<PricingEventSummaryDto> PricingEvents { get; set; } = new List<PricingEventSummaryDto>();
    
    // Audit Information
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class SalesContractSummaryDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string? ExternalContractNumber { get; set; }
    public ContractStatus Status { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; }
    public decimal? ContractValue { get; set; }
    public decimal? EstimatedProfit { get; set; }
    public decimal? Margin { get; set; }
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateSalesContractDto
{
    public string? ExternalContractNumber { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public Guid TraderId { get; set; }
    public Guid? LinkedPurchaseContractId { get; set; }
    public Guid? PriceBenchmarkId { get; set; }
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = "MT";
    public decimal TonBarrelRatio { get; set; } = 7.6m;
    public string PricingType { get; set; } = string.Empty;
    public decimal? FixedPrice { get; set; }
    public string? PricingFormula { get; set; }
    public DateTime? PricingPeriodStart { get; set; }
    public DateTime? PricingPeriodEnd { get; set; }
    public string DeliveryTerms { get; set; } = "FOB";
    public DateTime LaycanStart { get; set; }
    public DateTime LaycanEnd { get; set; }
    public string LoadPort { get; set; } = string.Empty;
    public string DischargePort { get; set; } = string.Empty;
    public string SettlementType { get; set; } = "TT";
    public int CreditPeriodDays { get; set; } = 15;
    public decimal? PrepaymentPercentage { get; set; }
    public string? PaymentTerms { get; set; }
    public string? QualitySpecifications { get; set; }
    public string? InspectionAgency { get; set; }
    public string? Notes { get; set; }
}

public class UpdateSalesContractDto
{
    public string? ExternalContractNumber { get; set; }
    public Guid? PriceBenchmarkId { get; set; }
    public decimal? Quantity { get; set; }
    public string? QuantityUnit { get; set; }
    public decimal? TonBarrelRatio { get; set; }
    public string? PricingType { get; set; }
    public decimal? FixedPrice { get; set; }
    public string? PricingFormula { get; set; }
    public DateTime? PricingPeriodStart { get; set; }
    public DateTime? PricingPeriodEnd { get; set; }
    public string? DeliveryTerms { get; set; }
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public string? SettlementType { get; set; }
    public int? CreditPeriodDays { get; set; }
    public decimal? PrepaymentPercentage { get; set; }
    public string? PaymentTerms { get; set; }
    public string? QualitySpecifications { get; set; }
    public string? InspectionAgency { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Sales contract summary statistics DTO
/// </summary>
public class SalesContractsSummaryDto
{
    public int TotalContracts { get; set; }
    public decimal TotalValue { get; set; }
    public decimal EstimatedProfit { get; set; }
    public ICollection<ContractStatusSummaryDto> ContractsByStatus { get; set; } = new List<ContractStatusSummaryDto>();
    public ICollection<TopCustomerDto> TopCustomers { get; set; } = new List<TopCustomerDto>();
    public ICollection<MonthlyBreakdownDto> MonthlyBreakdown { get; set; } = new List<MonthlyBreakdownDto>();
}

public class ContractStatusSummaryDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Value { get; set; }
}

public class TopCustomerDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ContractCount { get; set; }
    public decimal TotalValue { get; set; }
}

public class MonthlyBreakdownDto
{
    public string Month { get; set; } = string.Empty;
    public int Contracts { get; set; }
    public decimal Value { get; set; }
    public decimal Profit { get; set; }
}

// New DTOs for frontend alignment
public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class RiskMetricsDto
{
    public decimal Var95 { get; set; }
    public decimal Exposure { get; set; }
}

// Approval/Rejection DTOs for workflow endpoints
public class ApproveSalesContractDto
{
    public string? Comments { get; set; }
}

public class RejectSalesContractDto
{
    public string Reason { get; set; } = string.Empty;
    public string? Comments { get; set; }
}