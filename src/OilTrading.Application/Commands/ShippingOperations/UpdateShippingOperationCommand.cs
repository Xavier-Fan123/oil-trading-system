using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.ShippingOperations;

public class UpdateShippingOperationCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string? VesselName { get; set; }
    public string? IMONumber { get; set; }
    public string? ChartererName { get; set; }
    public decimal? VesselCapacity { get; set; }
    public string? ShippingAgent { get; set; }
    public decimal? PlannedQuantity { get; set; }
    public string? PlannedQuantityUnit { get; set; }
    public DateTime? LoadPortETA { get; set; }
    public DateTime? DischargePortETA { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public string? Notes { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class UpdateShippingOperationCommandValidator : AbstractValidator<UpdateShippingOperationCommand>
{
    public UpdateShippingOperationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Shipping operation ID is required");

        RuleFor(x => x.VesselName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.VesselName))
            .WithMessage("Vessel name cannot exceed 100 characters");

        RuleFor(x => x.IMONumber)
            .Matches(@"^[0-9]{7}$")
            .When(x => !string.IsNullOrEmpty(x.IMONumber))
            .WithMessage("IMO number must be 7 digits");

        RuleFor(x => x.PlannedQuantity)
            .GreaterThan(0)
            .When(x => x.PlannedQuantity.HasValue)
            .WithMessage("Planned quantity must be greater than zero");

        RuleFor(x => x.PlannedQuantityUnit)
            .Must(unit => unit == "MT" || unit == "BBL")
            .When(x => !string.IsNullOrEmpty(x.PlannedQuantityUnit))
            .WithMessage("Quantity unit must be either MT or BBL");

        RuleFor(x => x.LoadPortETA)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.LoadPortETA.HasValue)
            .WithMessage("Load port ETA must be in the future");

        RuleFor(x => x.DischargePortETA)
            .GreaterThan(x => x.LoadPortETA ?? DateTime.UtcNow)
            .When(x => x.DischargePortETA.HasValue)
            .WithMessage("Discharge port ETA must be after load port ETA");

        RuleFor(x => x.VesselCapacity)
            .GreaterThan(0)
            .When(x => x.VesselCapacity.HasValue)
            .WithMessage("Vessel capacity must be greater than zero");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("Updated by is required");
    }
}