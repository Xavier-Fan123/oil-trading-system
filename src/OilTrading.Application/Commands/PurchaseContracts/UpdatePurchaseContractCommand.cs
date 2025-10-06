using MediatR;
using FluentValidation;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Commands.PurchaseContracts;

public class UpdatePurchaseContractCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    // External Contract Number - 外部合同编号更新
    public string? ExternalContractNumber { get; set; }
    // Price Benchmark Update - 基准物更新
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
    public string UpdatedBy { get; set; } = string.Empty;
}

public class UpdatePurchaseContractCommandValidator : AbstractValidator<UpdatePurchaseContractCommand>
{
    public UpdatePurchaseContractCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Contract ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .When(x => x.Quantity.HasValue)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.QuantityUnit)
            .Must(BeValidQuantityUnit)
            .When(x => !string.IsNullOrEmpty(x.QuantityUnit))
            .WithMessage("Quantity unit must be MT, BBL, or GAL");

        RuleFor(x => x.TonBarrelRatio)
            .GreaterThan(0)
            .When(x => x.TonBarrelRatio.HasValue)
            .WithMessage("Ton/Barrel ratio must be greater than 0");

        RuleFor(x => x.PricingType)
            .Must(BeValidPricingType)
            .When(x => !string.IsNullOrEmpty(x.PricingType))
            .WithMessage("Pricing type must be Fixed, IndexAverage, IndexPoint, or EventBased");

        RuleFor(x => x.FixedPrice)
            .GreaterThan(0)
            .When(x => x.FixedPrice.HasValue && x.PricingType == "Fixed")
            .WithMessage("Fixed price must be greater than 0 for Fixed pricing");

        RuleFor(x => x.LaycanEnd)
            .GreaterThan(x => x.LaycanStart)
            .When(x => x.LaycanStart.HasValue && x.LaycanEnd.HasValue)
            .WithMessage("Laycan end must be after laycan start");

        RuleFor(x => x.SettlementType)
            .Must(BeValidSettlementType)
            .When(x => !string.IsNullOrEmpty(x.SettlementType))
            .WithMessage("Settlement type must be TT, LC, SBLC, DP, or CAD");

        RuleFor(x => x.CreditPeriodDays)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(365)
            .When(x => x.CreditPeriodDays.HasValue)
            .WithMessage("Credit period must be between 0 and 365 days");

        RuleFor(x => x.PrepaymentPercentage)
            .InclusiveBetween(0, 100)
            .When(x => x.PrepaymentPercentage.HasValue)
            .WithMessage("Prepayment percentage must be between 0 and 100");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("Updated by is required");

        When(x => x.PricingPeriodStart.HasValue && x.PricingPeriodEnd.HasValue, () =>
        {
            RuleFor(x => x.PricingPeriodEnd)
                .GreaterThan(x => x.PricingPeriodStart)
                .WithMessage("Pricing period end must be after pricing period start");
        });
    }

    private static bool BeValidQuantityUnit(string? unit)
    {
        if (string.IsNullOrEmpty(unit)) return false;
        return Enum.TryParse<QuantityUnit>(unit, true, out _);
    }

    private static bool BeValidPricingType(string? pricingType)
    {
        if (string.IsNullOrEmpty(pricingType)) return false;
        return new[] { "Fixed", "IndexAverage", "IndexPoint", "EventBased" }.Contains(pricingType);
    }

    private static bool BeValidSettlementType(string? settlementType)
    {
        if (string.IsNullOrEmpty(settlementType)) return false;
        return new[] { "TT", "LC", "SBLC", "DP", "CAD" }.Contains(settlementType.ToUpper());
    }
}