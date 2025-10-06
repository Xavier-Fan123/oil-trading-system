using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Commands.PhysicalContracts;

public class CreatePhysicalContractCommand : IRequest<PhysicalContractDto>
{
    public string ContractType { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    public Guid TradingPartnerId { get; set; }
    
    // Product Details
    public string ProductType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; } = QuantityUnit.MT;
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
    public int CreditDays { get; set; } = 30;
    
    // Agency Trade
    public bool IsAgencyTrade { get; set; } = false;
    public string? PrincipalName { get; set; }
    public decimal? AgencyFee { get; set; }
    
    public string? Notes { get; set; }
}

public class CreatePhysicalContractCommandValidator : AbstractValidator<CreatePhysicalContractCommand>
{
    public CreatePhysicalContractCommandValidator()
    {
        RuleFor(x => x.ContractType)
            .NotEmpty().WithMessage("Contract type is required")
            .Must(BeValidContractType).WithMessage("Contract type must be Purchase or Sales");

        RuleFor(x => x.ContractDate)
            .NotEmpty().WithMessage("Contract date is required");

        RuleFor(x => x.TradingPartnerId)
            .NotEmpty().WithMessage("Trading partner is required");

        RuleFor(x => x.ProductType)
            .NotEmpty().WithMessage("Product type is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.PricingType)
            .NotEmpty().WithMessage("Pricing type is required")
            .Must(BeValidPricingType).WithMessage("Invalid pricing type");

        RuleFor(x => x.FixedPrice)
            .GreaterThan(0).When(x => x.PricingType == "Fixed")
            .WithMessage("Fixed price is required for fixed pricing");

        RuleFor(x => x.PricingFormula)
            .NotEmpty().When(x => x.PricingType != "Fixed")
            .WithMessage("Pricing formula is required for non-fixed pricing");

        RuleFor(x => x.LoadPort)
            .NotEmpty().WithMessage("Load port is required");

        RuleFor(x => x.DischargePort)
            .NotEmpty().WithMessage("Discharge port is required");

        RuleFor(x => x.LaycanEnd)
            .GreaterThan(x => x.LaycanStart)
            .WithMessage("Laycan end must be after laycan start");
    }

    private static bool BeValidContractType(string contractType)
    {
        return new[] { "Purchase", "Sales" }.Contains(contractType);
    }

    private static bool BeValidPricingType(string pricingType)
    {
        return new[] { "Fixed", "Floating", "Formula" }.Contains(pricingType);
    }
}