using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.FinancialReports;

public class CreateFinancialReportCommand : IRequest<FinancialReportDto>
{
    public Guid TradingPartnerId { get; set; }
    public DateTime ReportStartDate { get; set; }
    public DateTime ReportEndDate { get; set; }
    
    // Financial Position Data
    public decimal? TotalAssets { get; set; }
    public decimal? TotalLiabilities { get; set; }
    public decimal? NetAssets { get; set; }
    public decimal? CurrentAssets { get; set; }
    public decimal? CurrentLiabilities { get; set; }
    
    // Performance Data
    public decimal? Revenue { get; set; }
    public decimal? NetProfit { get; set; }
    public decimal? OperatingCashFlow { get; set; }
    
    public string CreatedBy { get; set; } = string.Empty;
}

public class CreateFinancialReportCommandValidator : AbstractValidator<CreateFinancialReportCommand>
{
    public CreateFinancialReportCommandValidator()
    {
        RuleFor(x => x.TradingPartnerId)
            .NotEmpty()
            .WithMessage("Trading partner is required");

        RuleFor(x => x.ReportStartDate)
            .NotEmpty()
            .WithMessage("Report start date is required")
            .LessThan(x => x.ReportEndDate)
            .WithMessage("Report start date must be before end date");

        RuleFor(x => x.ReportEndDate)
            .NotEmpty()
            .WithMessage("Report end date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Report end date cannot be in the future");

        // Validate report period length
        RuleFor(x => x)
            .Must(x => (x.ReportEndDate - x.ReportStartDate).Days <= 366)
            .WithMessage("Report period cannot exceed 366 days")
            .Must(x => (x.ReportEndDate - x.ReportStartDate).Days >= 1)
            .WithMessage("Report period must be at least 1 day");

        // Financial Position Data Validation
        RuleFor(x => x.TotalAssets)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TotalAssets.HasValue)
            .WithMessage("Total assets cannot be negative");

        RuleFor(x => x.TotalLiabilities)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TotalLiabilities.HasValue)
            .WithMessage("Total liabilities cannot be negative");

        RuleFor(x => x.CurrentAssets)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CurrentAssets.HasValue)
            .WithMessage("Current assets cannot be negative");

        RuleFor(x => x.CurrentLiabilities)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CurrentLiabilities.HasValue)
            .WithMessage("Current liabilities cannot be negative");

        // Logical relationships validation
        RuleFor(x => x.CurrentAssets)
            .Must((command, currentAssets) => !currentAssets.HasValue || !command.TotalAssets.HasValue || currentAssets.Value <= command.TotalAssets.Value)
            .When(x => x.CurrentAssets.HasValue && x.TotalAssets.HasValue)
            .WithMessage("Current assets cannot exceed total assets");

        RuleFor(x => x.CurrentLiabilities)
            .Must((command, currentLiabilities) => !currentLiabilities.HasValue || !command.TotalLiabilities.HasValue || currentLiabilities.Value <= command.TotalLiabilities.Value)
            .When(x => x.CurrentLiabilities.HasValue && x.TotalLiabilities.HasValue)
            .WithMessage("Current liabilities cannot exceed total liabilities");

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Created by is required and must not exceed 100 characters");
    }
}