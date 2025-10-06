using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.ShippingOperations;

public class CancelShippingOperationCommand : IRequest<Unit>
{
    public Guid ShippingOperationId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public class CancelShippingOperationCommandValidator : AbstractValidator<CancelShippingOperationCommand>
{
    public CancelShippingOperationCommandValidator()
    {
        RuleFor(x => x.ShippingOperationId)
            .NotEmpty()
            .WithMessage("Shipping operation ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("Updated by is required");
    }
}