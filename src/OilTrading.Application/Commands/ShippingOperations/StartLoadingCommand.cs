using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.ShippingOperations;

public class StartLoadingCommand : IRequest<Unit>
{
    public Guid ShippingOperationId { get; set; }
    public DateTime LoadPortATA { get; set; }
    public DateTime? NoticeOfReadinessDate { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class StartLoadingCommandValidator : AbstractValidator<StartLoadingCommand>
{
    public StartLoadingCommandValidator()
    {
        RuleFor(x => x.ShippingOperationId)
            .NotEmpty()
            .WithMessage("Shipping operation ID is required");

        RuleFor(x => x.LoadPortATA)
            .NotEmpty()
            .WithMessage("Load port ATA is required");

        RuleFor(x => x.NoticeOfReadinessDate)
            .LessThanOrEqualTo(x => x.LoadPortATA)
            .When(x => x.NoticeOfReadinessDate.HasValue)
            .WithMessage("Notice of readiness date must be on or before load port ATA");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("Updated by is required");
    }
}