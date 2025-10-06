using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.FinancialReports;

public class DeleteFinancialReportCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
    public string? DeletionReason { get; set; }
}

public class DeleteFinancialReportCommandValidator : AbstractValidator<DeleteFinancialReportCommand>
{
    public DeleteFinancialReportCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Financial report ID is required");

        RuleFor(x => x.DeletedBy)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Deleted by is required and must not exceed 100 characters");

        RuleFor(x => x.DeletionReason)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.DeletionReason))
            .WithMessage("Deletion reason must not exceed 500 characters");
    }
}