using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.TradingPartners;

public class UpdateTradingPartnerCommand : IRequest<TradingPartnerDto>
{
    public Guid Id { get; set; }
    public string? CompanyName { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public decimal? CreditLimit { get; set; }
    public DateTime? CreditLimitValidUntil { get; set; }
    public int? PaymentTermDays { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsBlocked { get; set; }
    public string? BlockReason { get; set; }
}

public class UpdateTradingPartnerCommandValidator : AbstractValidator<UpdateTradingPartnerCommand>
{
    public UpdateTradingPartnerCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Trading partner ID is required");

        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.CompanyName));

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Credit limit must be non-negative")
            .When(x => x.CreditLimit.HasValue);

        RuleFor(x => x.CreditLimitValidUntil)
            .GreaterThan(DateTime.Now).WithMessage("Credit limit validity must be in the future")
            .When(x => x.CreditLimitValidUntil.HasValue);

        RuleFor(x => x.PaymentTermDays)
            .InclusiveBetween(0, 365).WithMessage("Payment term must be between 0 and 365 days")
            .When(x => x.PaymentTermDays.HasValue);

        RuleFor(x => x.ContactEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("Invalid email format");
    }
}
