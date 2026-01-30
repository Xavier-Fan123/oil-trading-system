using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Enums;

namespace OilTrading.Application.DTOs;

public class PurchaseContractDto
{
    public Guid Id { get; set; }
    public ContractNumberDto ContractNumber { get; set; } = new();
    public string? ExternalContractNumber { get; set; } // 外部/手动合同编号 - External/Manual contract number
    public ContractType ContractType { get; set; }
    public ContractStatus Status { get; set; }
    
    // Nested Trading Partner Information
    public SupplierDto Supplier { get; set; } = new();
    
    // Nested Product Information
    public ContractProductDto Product { get; set; } = new();
    
    // Price Benchmark Information - 基准物信息
    // Purpose: 显示合同关联的价格基准物，用于价格计算和结算参考
    public Guid? PriceBenchmarkId { get; set; }
    public string? PriceBenchmarkName { get; set; }
    public string? PriceBenchmarkType { get; set; }
    
    // Trader Information
    public Guid TraderId { get; set; }
    public string TraderName { get; set; } = string.Empty;
    public string TraderEmail { get; set; } = string.Empty;
    
    // Quantity Information
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; }
    public decimal TonBarrelRatio { get; set; }
    
    // Pricing Information
    public string? PricingFormula { get; set; }
    public decimal? ContractValue { get; set; }
    public string? ContractValueCurrency { get; set; }
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

    // Payment Status Information - NEW
    public ContractPaymentStatus? PaymentStatus { get; set; }
    public DateTime? EstimatedPaymentDate { get; set; }
    public decimal TotalSettledAmount { get; set; }
    public decimal PaidSettledAmount { get; set; }
    public decimal UnpaidSettledAmount { get; set; }

    // Additional Information
    public string? Incoterms { get; set; }
    public string? QualitySpecifications { get; set; }
    public string? InspectionAgency { get; set; }
    public string? Notes { get; set; }
    
    // Linked Contracts
    public Guid? BenchmarkContractId { get; set; }
    public string? BenchmarkContractNumber { get; set; }
    public ICollection<SalesContractSummaryDto> LinkedSalesContracts { get; set; } = new List<SalesContractSummaryDto>();
    
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

public class PurchaseContractListDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty; // 系统内部合同编号
    public string? ExternalContractNumber { get; set; } // 外部/手动合同编号
    public ContractType ContractType { get; set; }
    public ContractStatus Status { get; set; }

    // Include IDs for frontend filtering/referencing
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string TraderName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; }
    public decimal? ContractValue { get; set; }
    public string? ContractValueCurrency { get; set; }
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public bool IsPriceFinalized { get; set; }

    // Payment Status Information - NEW
    public ContractPaymentStatus? PaymentStatus { get; set; }
    public decimal UnpaidSettledAmount { get; set; }

    // Pricing Status Information (Data Lineage Enhancement v2.18.0)
    public string PricingStatus { get; set; } = "Unpriced";
    public decimal FixedPercentage { get; set; }
    public decimal FixedQuantity { get; set; }

    public int ShippingOperationsCount { get; set; }
    public int LinkedSalesContractsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PurchaseContractSummaryDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public ContractStatus Status { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; }
    public decimal? ContractValue { get; set; }
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePurchaseContractDto
{
    // External Contract Number - 外部合同编号
    // Purpose: 用户手动输入的正式合同编号，用于与交易对手的合同关联和对账
    public string? ExternalContractNumber { get; set; }
    public ContractType ContractType { get; set; }
    public Guid SupplierId { get; set; }
    public Guid ProductId { get; set; }
    public Guid TraderId { get; set; }
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; } = QuantityUnit.MT;
    public decimal TonBarrelRatio { get; set; } = 7.6m;
    
    // Price Benchmark Selection - 基准物选择
    // Purpose: 允许用户在创建合同时选择价格基准物，用于后续价格计算
    // 这是油品交易的关键字段，决定了最终结算价格的基准
    public Guid? PriceBenchmarkId { get; set; }
    public PricingType PricingType { get; set; }
    public decimal? FixedPrice { get; set; }
    public string? PricingFormula { get; set; }
    public DateTime? PricingPeriodStart { get; set; }
    public DateTime? PricingPeriodEnd { get; set; }
    public DeliveryTerms DeliveryTerms { get; set; } = DeliveryTerms.FOB;
    public DateTime LaycanStart { get; set; }
    public DateTime LaycanEnd { get; set; }
    public string LoadPort { get; set; } = string.Empty;
    public string DischargePort { get; set; } = string.Empty;
    public ContractPaymentMethod SettlementType { get; set; } = ContractPaymentMethod.TT;
    public int CreditPeriodDays { get; set; } = 30;
    public decimal? PrepaymentPercentage { get; set; }
    public string? PaymentTerms { get; set; }
    public string? QualitySpecifications { get; set; }
    public string? InspectionAgency { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePurchaseContractDto
{
    // External Contract Number Update - 外部合同编号更新
    public string? ExternalContractNumber { get; set; }
    public decimal? Quantity { get; set; }
    public QuantityUnit? QuantityUnit { get; set; }
    public decimal? TonBarrelRatio { get; set; }
    
    // Price Benchmark Update - 基准物更新
    // Purpose: 允许在更新合同时修改价格基准物
    public Guid? PriceBenchmarkId { get; set; }
    public PricingType? PricingType { get; set; }
    public decimal? FixedPrice { get; set; }
    public string? PricingFormula { get; set; }
    public DateTime? PricingPeriodStart { get; set; }
    public DateTime? PricingPeriodEnd { get; set; }
    public DeliveryTerms? DeliveryTerms { get; set; }
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public ContractPaymentMethod? SettlementType { get; set; }
    public int? CreditPeriodDays { get; set; }
    public decimal? PrepaymentPercentage { get; set; }
    public string? PaymentTerms { get; set; }
    public string? QualitySpecifications { get; set; }
    public string? InspectionAgency { get; set; }
    public string? Notes { get; set; }
}

public class ContractPricingDto
{
    public Guid ContractId { get; set; }
    public string? PricingFormula { get; set; }
    public decimal? ContractValue { get; set; }
    public string? ContractValueCurrency { get; set; }
    public DateTime? PricingPeriodStart { get; set; }
    public DateTime? PricingPeriodEnd { get; set; }
    public bool IsPriceFinalized { get; set; }
    public decimal? Premium { get; set; }
    public decimal? Discount { get; set; }
    public string? BenchmarkName { get; set; }
    public decimal? AveragePrice { get; set; }
    public int? BusinessDaysCount { get; set; }
}

// Nested DTOs for frontend compatibility
public class ContractNumberDto
{
    public string Value { get; set; } = string.Empty;
}

public class SupplierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ContractProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}