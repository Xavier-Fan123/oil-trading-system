using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.SalesContracts;

public class ActivateSalesContractCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string ActivatedBy { get; set; } = string.Empty;
}

public class ActivateSalesContractCommandValidator : AbstractValidator<ActivateSalesContractCommand>
{
    public ActivateSalesContractCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Contract ID is required");

        RuleFor(x => x.ActivatedBy)
            .NotEmpty()
            .WithMessage("Activated by is required");
    }
}