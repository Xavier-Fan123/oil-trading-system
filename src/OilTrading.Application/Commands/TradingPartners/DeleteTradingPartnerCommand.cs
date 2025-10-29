using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.TradingPartners;

public class DeleteTradingPartnerCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}

public class DeleteTradingPartnerCommandValidator : AbstractValidator<DeleteTradingPartnerCommand>
{
    public DeleteTradingPartnerCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Trading partner ID is required");
    }
}
