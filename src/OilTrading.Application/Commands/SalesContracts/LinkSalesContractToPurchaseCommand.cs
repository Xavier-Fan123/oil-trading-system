using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.SalesContracts;

public class LinkSalesContractToPurchaseCommand : IRequest<Unit>
{
    public Guid SalesContractId { get; set; }
    public Guid PurchaseContractId { get; set; }
}

public class LinkSalesContractToPurchaseCommandValidator : AbstractValidator<LinkSalesContractToPurchaseCommand>
{
    public LinkSalesContractToPurchaseCommandValidator()
    {
        RuleFor(x => x.SalesContractId)
            .NotEmpty()
            .WithMessage("Sales contract ID is required");

        RuleFor(x => x.PurchaseContractId)
            .NotEmpty()
            .WithMessage("Purchase contract ID is required");
    }
}