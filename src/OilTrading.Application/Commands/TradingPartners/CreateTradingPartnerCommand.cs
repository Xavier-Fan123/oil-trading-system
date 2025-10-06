using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.TradingPartners;

public class CreateTradingPartnerCommand : IRequest<TradingPartnerDto>
{
    public string CompanyName { get; set; } = string.Empty;
    public string PartnerType { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public decimal CreditLimit { get; set; }
    public DateTime CreditLimitValidUntil { get; set; }
    public int PaymentTermDays { get; set; } = 30;
}

public class CreateTradingPartnerCommandValidator : AbstractValidator<CreateTradingPartnerCommand>
{
    public CreateTradingPartnerCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters");

        RuleFor(x => x.PartnerType)
            .NotEmpty().WithMessage("Partner type is required")
            .Must(BeValidPartnerType).WithMessage("Partner type must be Trader, EndUser, or Both");

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Credit limit must be non-negative");

        RuleFor(x => x.CreditLimitValidUntil)
            .GreaterThan(DateTime.Now).WithMessage("Credit limit validity must be in the future");

        RuleFor(x => x.PaymentTermDays)
            .InclusiveBetween(0, 365).WithMessage("Payment term must be between 0 and 365 days");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("Invalid email format");
    }

    private static bool BeValidPartnerType(string partnerType)
    {
        return new[] { "Trader", "EndUser", "Both", "Supplier", "Customer" }.Contains(partnerType);
    }
}