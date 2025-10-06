using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.FinancialReports;

public class GetTradingPartnerAnalysisQuery : IRequest<TradingPartnerAnalysisDto?>
{
    public Guid TradingPartnerId { get; set; }
    public bool IncludeCooperationVolume { get; set; } = true;
    public bool IncludeFinancialHistory { get; set; } = true;
    public bool IncludeRiskAssessment { get; set; } = true;
    public int? MaxReportsCount { get; set; } = 10; // Limit number of reports returned

    public GetTradingPartnerAnalysisQuery(
        Guid tradingPartnerId, 
        bool includeCooperationVolume = true,
        bool includeFinancialHistory = true,
        bool includeRiskAssessment = true,
        int? maxReportsCount = 10)
    {
        TradingPartnerId = tradingPartnerId;
        IncludeCooperationVolume = includeCooperationVolume;
        IncludeFinancialHistory = includeFinancialHistory;
        IncludeRiskAssessment = includeRiskAssessment;
        MaxReportsCount = maxReportsCount;
    }
}

public class GetTradingPartnerAnalysisQueryValidator : AbstractValidator<GetTradingPartnerAnalysisQuery>
{
    public GetTradingPartnerAnalysisQueryValidator()
    {
        RuleFor(x => x.TradingPartnerId)
            .NotEmpty()
            .WithMessage("Trading partner ID is required");

        RuleFor(x => x.MaxReportsCount)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .When(x => x.MaxReportsCount.HasValue)
            .WithMessage("Max reports count must be between 1 and 100");
    }
}