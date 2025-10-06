namespace OilTrading.Application.DTOs;

public class PhysicalContractDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    
    // Trading Partner
    public Guid TradingPartnerId { get; set; }
    public string TradingPartnerName { get; set; } = string.Empty;
    public string TradingPartnerCode { get; set; } = string.Empty;
    
    // Product Details
    public string ProductType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public string ProductSpec { get; set; } = string.Empty;
    
    // Pricing
    public string PricingType { get; set; } = string.Empty;
    public decimal? FixedPrice { get; set; }
    public string? PricingFormula { get; set; }
    public string? PricingBasis { get; set; }
    public decimal? Premium { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal? ContractValue { get; set; }
    
    // Delivery Terms
    public string DeliveryTerms { get; set; } = string.Empty;
    public string LoadPort { get; set; } = string.Empty;
    public string DischargePort { get; set; } = string.Empty;
    public DateTime LaycanStart { get; set; }
    public DateTime LaycanEnd { get; set; }
    
    // Payment Terms
    public string PaymentTerms { get; set; } = string.Empty;
    public decimal? PrepaymentPercentage { get; set; }
    public int CreditDays { get; set; }
    public DateTime? PaymentDueDate { get; set; }
    
    // Agency Trade
    public bool IsAgencyTrade { get; set; }
    public string? PrincipalName { get; set; }
    public decimal? AgencyFee { get; set; }
    
    // Status
    public string Status { get; set; } = string.Empty;
    public decimal? DeliveredQuantity { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public decimal? InvoicedAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public decimal? OutstandingAmount { get; set; }
    public bool IsFullySettled { get; set; }
    
    // Invoice
    public string? ProformaInvoiceNumber { get; set; }
    public DateTime? ProformaInvoiceDate { get; set; }
    
    // Notes
    public string? Notes { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class CreatePhysicalContractDto
{
    public string ContractType { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    public Guid TradingPartnerId { get; set; }
    
    // Product Details
    public string ProductType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = "MT";
    public string? ProductSpec { get; set; }
    
    // Pricing
    public string PricingType { get; set; } = string.Empty;
    public decimal? FixedPrice { get; set; }
    public string? PricingFormula { get; set; }
    public string? PricingBasis { get; set; }
    public decimal? Premium { get; set; }
    
    // Delivery Terms
    public string DeliveryTerms { get; set; } = "FOB";
    public string LoadPort { get; set; } = string.Empty;
    public string DischargePort { get; set; } = string.Empty;
    public DateTime LaycanStart { get; set; }
    public DateTime LaycanEnd { get; set; }
    
    // Payment Terms
    public string PaymentTerms { get; set; } = "TT";
    public decimal? PrepaymentPercentage { get; set; }
    public int? CreditDays { get; set; }
    
    // Agency Trade
    public bool IsAgencyTrade { get; set; } = false;
    public string? PrincipalName { get; set; }
    public decimal? AgencyFee { get; set; }
    
    public string? Notes { get; set; }
}

public class UpdatePhysicalContractDto
{
    // Product Details
    public decimal? Quantity { get; set; }
    public string? ProductSpec { get; set; }
    
    // Pricing
    public decimal? FixedPrice { get; set; }
    public string? PricingFormula { get; set; }
    public decimal? Premium { get; set; }
    
    // Delivery Terms
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    
    // Settlement
    public decimal? DeliveredQuantity { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public decimal? InvoicedAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    
    // Status
    public string? Status { get; set; }
    public string? Notes { get; set; }
}

public class PhysicalContractListDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    public string TradingPartnerName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public decimal? ContractValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LaycanStart { get; set; }
    public DateTime LaycanEnd { get; set; }
    public decimal? OutstandingAmount { get; set; }
}

public class NetPositionDto
{
    public string ProductType { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    
    // Physical Contracts (Legacy)
    public decimal PhysicalPurchases { get; set; }
    public decimal PhysicalSales { get; set; }
    public decimal PhysicalNetPosition { get; set; }
    
    // Purchase & Sales Contracts (New Main System)
    public decimal PurchaseContractQuantity { get; set; }
    public decimal SalesContractQuantity { get; set; }
    public decimal ContractNetPosition { get; set; }
    
    // Paper Contracts (from existing system)
    public decimal PaperLongPosition { get; set; }
    public decimal PaperShortPosition { get; set; }
    public decimal PaperNetPosition { get; set; }
    
    // Combined
    public decimal TotalNetPosition { get; set; }
    public string PositionStatus { get; set; } = string.Empty;
    public decimal ExposureValue { get; set; }
    public decimal MarketPrice { get; set; }
}

public class PositionSummaryDto
{
    public int TotalContracts { get; set; }
    public decimal TotalExposure { get; set; }
    public int LongPositions { get; set; }
    public int ShortPositions { get; set; }
    public int FlatPositions { get; set; }
    public decimal LargestExposure { get; set; }
    public decimal SmallestExposure { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class ExposureDto
{
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Product", "Counterparty", "Region"
    public decimal TotalQuantity { get; set; }
    public decimal TotalExposure { get; set; }
    public decimal LongQuantity { get; set; }
    public decimal ShortQuantity { get; set; }
    public int ContractCount { get; set; }
    public decimal AveragePrice { get; set; }
    public string Unit { get; set; } = "MT";
}

public class PnLDto
{
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ContractPrice { get; set; }
    public decimal MarketPrice { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal RealizedPnL { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime AsOfDate { get; set; }
}