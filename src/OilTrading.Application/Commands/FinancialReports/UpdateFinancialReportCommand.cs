using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.FinancialReports;

public class UpdateFinancialReportCommand : IRequest<FinancialReportDto>
{
    public Guid Id { get; set; }
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
    
    public string UpdatedBy { get; set; } = string.Empty;
}

public class UpdateFinancialReportCommandValidator : AbstractValidator<UpdateFinancialReportCommand>
{
    public UpdateFinancialReportCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Financial report ID is required");

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
        RuleFor(x => x)
            .Must(x => !x.TotalAssets.HasValue || !x.CurrentAssets.HasValue || x.CurrentAssets.Value <= x.TotalAssets.Value)
            .WithMessage("Current assets cannot exceed total assets")
            .Must(x => !x.TotalLiabilities.HasValue || !x.CurrentLiabilities.HasValue || x.CurrentLiabilities.Value <= x.TotalLiabilities.Value)
            .WithMessage("Current liabilities cannot exceed total liabilities");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Updated by is required and must not exceed 100 characters");
    }
}