using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.TradingPartners;

public class UnblockTradingPartnerCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}

public class UnblockTradingPartnerCommandValidator : AbstractValidator<UnblockTradingPartnerCommand>
{
    public UnblockTradingPartnerCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Trading partner ID is required");
    }
}
