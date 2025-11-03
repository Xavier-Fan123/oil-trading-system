using FluentValidation;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Validators;

/// <summary>
/// Validator for CreatePurchaseSettlementRequest
/// Ensures all required fields are present and valid
/// </summary>
public class CreatePurchaseSettlementRequestValidator : AbstractValidator<CreatePurchaseSettlementRequest>
{
    public CreatePurchaseSettlementRequestValidator()
    {
        RuleFor(x => x.PurchaseContractId)
            .NotEmpty()
            .WithMessage("Purchase contract ID is required");

        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .WithMessage("Document number is required")
            .MaximumLength(100)
            .WithMessage("Document number cannot exceed 100 characters");

        RuleFor(x => x.ExternalContractNumber)
            .MaximumLength(100)
            .WithMessage("External contract number cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.ExternalContractNumber));

        RuleFor(x => x.DocumentType)
            .IsInEnum()
            .WithMessage("Document type must be a valid DocumentType");

        RuleFor(x => x.DocumentDate)
            .NotEmpty()
            .WithMessage("Document date is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Document date cannot be in the future");
    }
}

/// <summary>
/// Validator for CreateSalesSettlementRequest
/// Ensures all required fields are present and valid
/// </summary>
public class CreateSalesSettlementRequestValidator : AbstractValidator<CreateSalesSettlementRequest>
{
    public CreateSalesSettlementRequestValidator()
    {
        RuleFor(x => x.SalesContractId)
            .NotEmpty()
            .WithMessage("Sales contract ID is required");

        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .WithMessage("Document number is required")
            .MaximumLength(100)
            .WithMessage("Document number cannot exceed 100 characters");

        RuleFor(x => x.ExternalContractNumber)
            .MaximumLength(100)
            .WithMessage("External contract number cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.ExternalContractNumber));

        RuleFor(x => x.DocumentType)
            .IsInEnum()
            .WithMessage("Document type must be a valid DocumentType");

        RuleFor(x => x.DocumentDate)
            .NotEmpty()
            .WithMessage("Document date is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Document date cannot be in the future");
    }
}

/// <summary>
/// Validator for CalculateSettlementRequest
/// Ensures quantities and amounts are valid for settlement calculations
/// </summary>
public class CalculateSettlementRequestValidator : AbstractValidator<CalculateSettlementRequest>
{
    public CalculateSettlementRequestValidator()
    {
        RuleFor(x => x.CalculationQuantityMT)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Calculation quantity MT must be non-negative")
            .LessThanOrEqualTo(decimal.MaxValue)
            .WithMessage("Calculation quantity MT exceeds maximum value");

        RuleFor(x => x.CalculationQuantityBBL)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Calculation quantity BBL must be non-negative")
            .LessThanOrEqualTo(decimal.MaxValue)
            .WithMessage("Calculation quantity BBL exceeds maximum value");

        RuleFor(x => x.BenchmarkAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Benchmark amount must be non-negative")
            .LessThanOrEqualTo(decimal.MaxValue)
            .WithMessage("Benchmark amount exceeds maximum value");

        RuleFor(x => x.AdjustmentAmount)
            .GreaterThanOrEqualTo(decimal.MinValue)
            .WithMessage("Adjustment amount must be a valid number")
            .LessThanOrEqualTo(decimal.MaxValue)
            .WithMessage("Adjustment amount exceeds maximum value");

        RuleFor(x => x.CalculationNote)
            .MaximumLength(500)
            .WithMessage("Calculation note cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.CalculationNote));

        // At least one quantity must be specified
        RuleFor(x => x)
            .Must(x => x.CalculationQuantityMT > 0 || x.CalculationQuantityBBL > 0)
            .WithMessage("At least one calculation quantity (MT or BBL) must be greater than zero")
            .WithName("Quantities");

        // Benchmark amount must be specified
        RuleFor(x => x.BenchmarkAmount)
            .GreaterThan(0)
            .WithMessage("Benchmark amount must be greater than zero");
    }
}
