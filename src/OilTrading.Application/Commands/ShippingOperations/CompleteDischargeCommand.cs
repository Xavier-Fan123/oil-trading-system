using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.ShippingOperations;

public class CompleteDischargeCommand : IRequest<Unit>
{
    public Guid ShippingOperationId { get; set; }
    public DateTime DischargePortATA { get; set; }
    public DateTime CertificateOfDischargeDate { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class CompleteDischargeCommandValidator : AbstractValidator<CompleteDischargeCommand>
{
    public CompleteDischargeCommandValidator()
    {
        RuleFor(x => x.ShippingOperationId)
            .NotEmpty()
            .WithMessage("Shipping operation ID is required");

        RuleFor(x => x.DischargePortATA)
            .NotEmpty()
            .WithMessage("Discharge port ATA is required");

        RuleFor(x => x.CertificateOfDischargeDate)
            .NotEmpty()
            .WithMessage("Certificate of discharge date is required")
            .GreaterThanOrEqualTo(x => x.DischargePortATA)
            .WithMessage("Certificate of discharge date must be on or after discharge port ATA");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("Updated by is required");
    }
}