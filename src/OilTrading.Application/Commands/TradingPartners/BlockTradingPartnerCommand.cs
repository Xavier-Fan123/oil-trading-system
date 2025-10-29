using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.TradingPartners;

public class BlockTradingPartnerCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class BlockTradingPartnerCommandValidator : AbstractValidator<BlockTradingPartnerCommand>
{
    public BlockTradingPartnerCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Trading partner ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Block reason is required")
            .MaximumLength(500).WithMessage("Block reason must not exceed 500 characters");
    }
}
