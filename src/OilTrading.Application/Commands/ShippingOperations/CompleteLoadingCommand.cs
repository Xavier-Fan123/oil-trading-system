using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.ShippingOperations;

public class CompleteLoadingCommand : IRequest<Unit>
{
    public Guid ShippingOperationId { get; set; }
    public DateTime BillOfLadingDate { get; set; }
    public decimal ActualQuantity { get; set; }
    public string ActualQuantityUnit { get; set; } = "MT";
    public string UpdatedBy { get; set; } = string.Empty;
}

public class CompleteLoadingCommandValidator : AbstractValidator<CompleteLoadingCommand>
{
    public CompleteLoadingCommandValidator()
    {
        RuleFor(x => x.ShippingOperationId)
            .NotEmpty()
            .WithMessage("Shipping operation ID is required");

        RuleFor(x => x.BillOfLadingDate)
            .NotEmpty()
            .WithMessage("Bill of lading date is required");

        RuleFor(x => x.ActualQuantity)
            .GreaterThan(0)
            .WithMessage("Actual quantity must be greater than zero");

        RuleFor(x => x.ActualQuantityUnit)
            .NotEmpty()
            .WithMessage("Actual quantity unit is required")
            .Must(unit => unit == "MT" || unit == "BBL")
            .WithMessage("Quantity unit must be either MT or BBL");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("Updated by is required");
    }
}