using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.SalesContracts;

public class UnlinkSalesContractFromPurchaseCommand : IRequest<Unit>
{
    public Guid SalesContractId { get; set; }
}

public class UnlinkSalesContractFromPurchaseCommandValidator : AbstractValidator<UnlinkSalesContractFromPurchaseCommand>
{
    public UnlinkSalesContractFromPurchaseCommandValidator()
    {
        RuleFor(x => x.SalesContractId)
            .NotEmpty()
            .WithMessage("Sales contract ID is required");
    }
}