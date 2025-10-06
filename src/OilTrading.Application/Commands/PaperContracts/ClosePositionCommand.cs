using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.PaperContracts;

public class ClosePositionCommand : IRequest<PaperContractDto?>
{
    public Guid ContractId { get; set; }
    public decimal ClosingPrice { get; set; }
    public DateTime CloseDate { get; set; }
    public string ClosedBy { get; set; } = string.Empty;
}

public class ClosePositionCommandValidator : AbstractValidator<ClosePositionCommand>
{
    public ClosePositionCommandValidator()
    {
        RuleFor(x => x.ContractId)
            .NotEmpty()
            .WithMessage("Contract ID is required");

        RuleFor(x => x.ClosingPrice)
            .GreaterThan(0)
            .WithMessage("Closing price must be greater than 0");

        RuleFor(x => x.CloseDate)
            .NotEmpty()
            .WithMessage("Close date is required");

        RuleFor(x => x.ClosedBy)
            .NotEmpty()
            .WithMessage("Closed by is required");
    }
}