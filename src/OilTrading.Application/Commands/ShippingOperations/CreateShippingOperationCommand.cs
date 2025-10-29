using MediatR;
using FluentValidation;

namespace OilTrading.Application.Commands.ShippingOperations;

public class CreateShippingOperationCommand : IRequest<Guid>
{
    public Guid ContractId { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string? IMONumber { get; set; }
    public string? ChartererName { get; set; }
    public decimal? VesselCapacity { get; set; }
    public string? ShippingAgent { get; set; }
    public decimal PlannedQuantity { get; set; }
    public string PlannedQuantityUnit { get; set; } = "MT";
    public DateTime LoadPortETA { get; set; }
    public DateTime DischargePortETA { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class CreateShippingOperationCommandValidator : AbstractValidator<CreateShippingOperationCommand>
{
    public CreateShippingOperationCommandValidator()
    {
        RuleFor(x => x.ContractId)
            .NotEmpty()
            .WithMessage("Contract ID is required");

        RuleFor(x => x.VesselName)
            .NotEmpty()
            .WithMessage("Vessel name is required")
            .MaximumLength(100)
            .WithMessage("Vessel name cannot exceed 100 characters");

        RuleFor(x => x.IMONumber)
            .Matches(@"^[0-9]{7}$")
            .When(x => !string.IsNullOrEmpty(x.IMONumber))
            .WithMessage("IMO number must be 7 digits");

        RuleFor(x => x.PlannedQuantity)
            .GreaterThan(0)
            .WithMessage("Planned quantity must be greater than zero");

        RuleFor(x => x.PlannedQuantityUnit)
            .NotEmpty()
            .WithMessage("Planned quantity unit is required")
            .Must(unit => unit == "MT" || unit == "BBL")
            .WithMessage("Quantity unit must be either MT or BBL");

        // Note: We allow past dates for LoadPortETA and DischargePortETA
        // Users may enter historical data when recording past shipping operations

        RuleFor(x => x.DischargePortETA)
            .GreaterThan(x => x.LoadPortETA)
            .WithMessage("Discharge port ETA must be after load port ETA");

        RuleFor(x => x.VesselCapacity)
            .GreaterThan(0)
            .When(x => x.VesselCapacity.HasValue)
            .WithMessage("Vessel capacity must be greater than zero");

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("Created by is required");
    }
}