using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Commands.PaperContracts;

public class CreatePaperContractCommand : IRequest<PaperContractDto>
{
    public string ContractMonth { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal LotSize { get; set; } = 1000;
    public decimal EntryPrice { get; set; }
    public DateTime TradeDate { get; set; }
    public string? TradeReference { get; set; }
    public string? CounterpartyName { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class CreatePaperContractCommandValidator : AbstractValidator<CreatePaperContractCommand>
{
    public CreatePaperContractCommandValidator()
    {
        RuleFor(x => x.ContractMonth)
            .NotEmpty()
            .WithMessage("Contract month is required")
            .Matches(@"^[A-Z]{3}\d{2}$")
            .WithMessage("Contract month must be in format 'MMMyy' (e.g., AUG25)");

        RuleFor(x => x.ProductType)
            .NotEmpty()
            .WithMessage("Product type is required");

        RuleFor(x => x.Position)
            .NotEmpty()
            .Must(BeValidPosition)
            .WithMessage("Position must be 'Long' or 'Short'");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.LotSize)
            .GreaterThan(0)
            .WithMessage("Lot size must be greater than 0");

        RuleFor(x => x.EntryPrice)
            .GreaterThan(0)
            .WithMessage("Entry price must be greater than 0");

        RuleFor(x => x.TradeDate)
            .NotEmpty()
            .WithMessage("Trade date is required");

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("Created by is required");
    }

    private static bool BeValidPosition(string position)
    {
        return new[] { "Long", "Short" }.Contains(position);
    }
}