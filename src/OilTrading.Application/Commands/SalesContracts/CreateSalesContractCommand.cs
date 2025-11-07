using MediatR;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Enums;

namespace OilTrading.Application.Commands.SalesContracts;

public class CreateSalesContractCommand : IRequest<Guid>
{
    public string? ExternalContractNumber { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public Guid TraderId { get; set; }
    public Guid? LinkedPurchaseContractId { get; set; }
    public Guid? PriceBenchmarkId { get; set; }
    
    // Quantity Information
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = "MT";
    public decimal TonBarrelRatio { get; set; } = 7.6m;
    
    // Pricing Information
    public string PricingType { get; set; } = string.Empty;
    public decimal? FixedPrice { get; set; }
    public string? PricingFormula { get; set; }
    public DateTime? PricingPeriodStart { get; set; }
    public DateTime? PricingPeriodEnd { get; set; }
    
    // Delivery Information
    public string DeliveryTerms { get; set; } = "FOB";
    public DateTime LaycanStart { get; set; }
    public DateTime LaycanEnd { get; set; }
    public string LoadPort { get; set; } = string.Empty;
    public string DischargePort { get; set; } = string.Empty;
    
    // Payment Information
    public string SettlementType { get; set; } = "TT";
    public int CreditPeriodDays { get; set; } = 15;
    public decimal? PrepaymentPercentage { get; set; }
    public string? PaymentTerms { get; set; }
    
    // Additional Information
    public string? QualitySpecifications { get; set; }
    public string? InspectionAgency { get; set; }
    public string? Notes { get; set; }
    
    // Audit Information
    public string CreatedBy { get; set; } = string.Empty;
}