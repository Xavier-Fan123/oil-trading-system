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

    /// <summary>
    /// Contract month in YYMM format (e.g., "2511" for Nov 2025, "2512" for Dec 2025)
    /// Provides machine-readable month specification for API consumption and risk aggregation
    /// Distinct from Month field which is human-readable format (e.g., "Nov25")
    /// </summary>
    public string? ContractMonth { get; set; }

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

    // Settlement-adjusted quantities
    public decimal SettledPurchaseQuantity { get; set; }
    public decimal SettledSalesQuantity { get; set; }

    // Contract matching (natural hedge)
    public decimal MatchedQuantity { get; set; }
    public decimal AdjustedNetExposure { get; set; }

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

// ═══════════════════════════════════════════════════════════════════════════
// HEDGE LINKING DTOs (Data Lineage Enhancement)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO for positions with explicit hedge linkage information
/// </summary>
public class HedgedPositionDto
{
    public string ProductType { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;

    // Physical position details
    public decimal PhysicalQuantity { get; set; }
    public string PhysicalPositionType { get; set; } = string.Empty; // "Long" or "Short"

    // Linked hedge details
    public decimal HedgedQuantity { get; set; }
    public decimal UnhedgedQuantity { get; set; }
    public decimal HedgeRatio { get; set; } // Percentage of physical position hedged

    // Paper hedges linked to this position
    public ICollection<LinkedHedgeDto> LinkedHedges { get; set; } = new List<LinkedHedgeDto>();

    // Exposure metrics
    public decimal GrossExposure { get; set; }
    public decimal NetExposure { get; set; }
    public decimal MarketPrice { get; set; }

    // Calculated metrics
    public bool IsFullyHedged => HedgeRatio >= 1.0m;
    public bool IsPartiallyHedged => HedgeRatio > 0 && HedgeRatio < 1.0m;
    public bool IsUnhedged => HedgeRatio == 0;
}

/// <summary>
/// DTO for a paper hedge linked to a physical contract
/// </summary>
public class LinkedHedgeDto
{
    public Guid PaperContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Position { get; set; } = string.Empty; // "Long" or "Short"
    public decimal HedgeRatio { get; set; }
    public decimal HedgeEffectiveness { get; set; }
    public string? DealReferenceId { get; set; }
    public DateTime DesignationDate { get; set; }
}

/// <summary>
/// DTO for hedge effectiveness metrics
/// </summary>
public class HedgeEffectivenessDto
{
    public Guid PhysicalContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public decimal PhysicalQuantity { get; set; }
    public string PhysicalPosition { get; set; } = string.Empty;

    // Aggregate hedge metrics
    public decimal TotalHedgedQuantity { get; set; }
    public decimal OverallHedgeRatio { get; set; }
    public decimal WeightedAverageEffectiveness { get; set; }

    // Individual hedges
    public ICollection<LinkedHedgeDto> Hedges { get; set; } = new List<LinkedHedgeDto>();

    // Effectiveness assessment
    public string EffectivenessStatus { get; set; } = string.Empty; // "Highly Effective", "Effective", "Ineffective"
    public bool MeetsAccountingThreshold { get; set; } // 80-125% for IFRS 9

    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// DTO for unhedged physical positions
/// </summary>
public class UnhedgedPositionDto
{
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty; // "Purchase" or "Sales"
    public string ProductType { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal MarketPrice { get; set; }
    public decimal Exposure { get; set; }
    public string? DealReferenceId { get; set; }
    public DateTime ContractDate { get; set; }

    // Risk metrics
    public decimal PotentialLoss { get; set; } // Based on VaR or stress test
    public string RiskLevel { get; set; } = string.Empty; // "Low", "Medium", "High"
}