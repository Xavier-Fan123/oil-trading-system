using MediatR;
using FluentValidation;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;

namespace OilTrading.Application.Commands.PurchaseContracts;

public class CreatePurchaseContractCommand : IRequest<Guid>
{
    // External Contract Number - 外部合同编号
    // Purpose: 用户手动输入的正式合同编号，用于与交易对手的合同关联和对账
    public string? ExternalContractNumber { get; set; }
    public ContractType ContractType { get; set; }
    public Guid SupplierId { get; set; }
    public Guid ProductId { get; set; }
    public Guid TraderId { get; set; }
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; } = QuantityUnit.MT; // MT, BBL
    public decimal TonBarrelRatio { get; set; } = 7.6m;
    
    // Price Benchmark Selection - 基准物选择
    // Purpose: 选择价格基准物用于价格计算，这是油品交易中的关键定价参数
    public Guid? PriceBenchmarkId { get; set; }
    
    public PricingType PricingType { get; set; } // Fixed, Floating, Formula
    public decimal? FixedPrice { get; set; }
    public string? PricingFormula { get; set; }
    public DateTime? PricingPeriodStart { get; set; }
    public DateTime? PricingPeriodEnd { get; set; }
    public DeliveryTerms DeliveryTerms { get; set; } = DeliveryTerms.FOB; // FOB, CFR, CIF, etc.
    public DateTime LaycanStart { get; set; }
    public DateTime LaycanEnd { get; set; }
    public string LoadPort { get; set; } = string.Empty;
    public string DischargePort { get; set; } = string.Empty;
    public ContractPaymentMethod SettlementType { get; set; } = ContractPaymentMethod.TT; // TT, LC, SBLC
    public int CreditPeriodDays { get; set; } = 30;
    public decimal? PrepaymentPercentage { get; set; }
    public string? PaymentTerms { get; set; }
    public string? QualitySpecifications { get; set; }
    public string? InspectionAgency { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class CreatePurchaseContractCommandValidator : AbstractValidator<CreatePurchaseContractCommand>
{
    public CreatePurchaseContractCommandValidator()
    {
        RuleFor(x => x.ContractType)
            .IsInEnum()
            .WithMessage("Contract type must be a valid enum value");

        RuleFor(x => x.SupplierId)
            .NotEmpty()
            .WithMessage("Supplier is required");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product is required");

        RuleFor(x => x.TraderId)
            .NotEmpty()
            .WithMessage("Trader is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.QuantityUnit)
            .IsInEnum()
            .WithMessage("Quantity unit must be a valid enum value");

        RuleFor(x => x.TonBarrelRatio)
            .GreaterThan(0)
            .WithMessage("Ton/Barrel ratio must be greater than 0");

        RuleFor(x => x.PricingType)
            .IsInEnum()
            .WithMessage("Pricing type must be a valid enum value");

        RuleFor(x => x.FixedPrice)
            .GreaterThan(0)
            .When(x => x.PricingType == PricingType.Fixed)
            .WithMessage("Fixed price is required and must be greater than 0 for Fixed pricing");

        RuleFor(x => x.PricingFormula)
            .NotEmpty()
            .When(x => x.PricingType != PricingType.Fixed)
            .WithMessage("Pricing formula is required for non-fixed pricing");

        RuleFor(x => x.LaycanStart)
            .GreaterThanOrEqualTo(DateTime.Now.Date)
            .WithMessage("Laycan start must be today or in the future");

        RuleFor(x => x.LaycanEnd)
            .GreaterThan(x => x.LaycanStart)
            .WithMessage("Laycan end must be after laycan start");

        RuleFor(x => x.LoadPort)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Load port is required and must not exceed 100 characters");

        RuleFor(x => x.DischargePort)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Discharge port is required and must not exceed 100 characters");

        RuleFor(x => x.SettlementType)
            .IsInEnum()
            .WithMessage("Settlement type must be a valid enum value");

        RuleFor(x => x.CreditPeriodDays)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(365)
            .WithMessage("Credit period must be between 0 and 365 days");

        RuleFor(x => x.PrepaymentPercentage)
            .InclusiveBetween(0, 100)
            .When(x => x.PrepaymentPercentage.HasValue)
            .WithMessage("Prepayment percentage must be between 0 and 100");

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("Created by is required");

        When(x => x.PricingPeriodStart.HasValue && x.PricingPeriodEnd.HasValue, () =>
        {
            RuleFor(x => x.PricingPeriodEnd)
                .GreaterThan(x => x.PricingPeriodStart)
                .WithMessage("Pricing period end must be after pricing period start");
        });
    }

}