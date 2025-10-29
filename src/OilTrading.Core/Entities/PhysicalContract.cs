using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

public class PhysicalContract : BaseEntity
{
    public string ContractNumber { get; set; } = string.Empty;
    public PhysicalContractType ContractType { get; set; }
    public DateTime ContractDate { get; set; }
    
    // Trading Partner
    public Guid TradingPartnerId { get; set; }
    public TradingPartner TradingPartner { get; set; } = null!;
    
    // Product Details
    public string ProductType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; } = QuantityUnit.MT;
    public string ProductSpec { get; set; } = string.Empty;
    
    // Pricing
    public PricingType PricingType { get; set; }
    public decimal? FixedPrice { get; set; }
    public string? PricingFormula { get; set; }
    public string? PricingBasis { get; set; }
    public decimal? Premium { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal? ContractValue { get; set; }
    
    // Delivery Terms
    public string DeliveryTerms { get; set; } = "FOB";
    public string LoadPort { get; set; } = string.Empty;
    public string DischargePort { get; set; } = string.Empty;
    public DateTime LaycanStart { get; set; }
    public DateTime LaycanEnd { get; set; }
    
    // Payment Terms
    public string PaymentTerms { get; set; } = "TT";
    public decimal? PrepaymentPercentage { get; set; }
    public int CreditDays { get; set; } = 30;
    public DateTime? PaymentDueDate { get; set; }
    
    // Agency Trade
    public bool IsAgencyTrade { get; set; } = false;
    public string? PrincipalName { get; set; }
    public decimal? AgencyFee { get; set; }
    
    // Status and Settlement
    public PhysicalContractStatus Status { get; set; } = PhysicalContractStatus.Active;
    public decimal? DeliveredQuantity { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public decimal? InvoicedAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public decimal? OutstandingAmount { get; set; }
    public bool IsFullySettled { get; set; } = false;
    
    // Invoice
    public string? ProformaInvoiceNumber { get; set; }
    public DateTime? ProformaInvoiceDate { get; set; }
    public string? CommercialInvoiceNumber { get; set; }
    public DateTime? CommercialInvoiceDate { get; set; }
    
    // Notes
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
}

public enum PhysicalContractType
{
    Purchase = 1,
    Sales = 2
}

public enum PricingType
{
    Fixed = 1,
    Floating = 2,
    Formula = 3
}

public enum PhysicalContractStatus
{
    Draft = 1,
    Active = 2,
    Delivered = 3,
    Cancelled = 4,
    Completed = 5
}