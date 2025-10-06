using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.PaperContracts;

public class UpdateMTMCommand : IRequest<IEnumerable<MTMUpdateDto>>
{
    public DateTime MTMDate { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public List<Guid>? ContractIds { get; set; } // Optional: specify contracts, otherwise update all open positions
}

public class UpdateMTMCommandValidator : AbstractValidator<UpdateMTMCommand>
{
    public UpdateMTMCommandValidator()
    {
        RuleFor(x => x.MTMDate)
            .NotEmpty()
            .WithMessage("MTM date is required");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("Updated by is required");
    }
}