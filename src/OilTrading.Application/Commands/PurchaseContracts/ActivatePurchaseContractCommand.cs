using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.PurchaseContracts;

public class ActivatePurchaseContractCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string ActivatedBy { get; set; } = string.Empty;
}

public class ActivatePurchaseContractCommandValidator : AbstractValidator<ActivatePurchaseContractCommand>
{
    public ActivatePurchaseContractCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Contract ID is required");

        RuleFor(x => x.ActivatedBy)
            .NotEmpty()
            .WithMessage("Activated by is required");
    }
}