using MediatR;
using FluentValidation;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.FinancialReports;

public class GetFinancialReportsByTradingPartnerQuery : IRequest<IReadOnlyList<FinancialReportDto>>
{
    public Guid TradingPartnerId { get; set; }
    public int? Year { get; set; }
    public bool IncludeGrowthMetrics { get; set; } = true;

    public GetFinancialReportsByTradingPartnerQuery(Guid tradingPartnerId, int? year = null, bool includeGrowthMetrics = true)
    {
        TradingPartnerId = tradingPartnerId;
        Year = year;
        IncludeGrowthMetrics = includeGrowthMetrics;
    }
}

public class GetFinancialReportsByTradingPartnerQueryValidator : AbstractValidator<GetFinancialReportsByTradingPartnerQuery>
{
    public GetFinancialReportsByTradingPartnerQueryValidator()
    {
        RuleFor(x => x.TradingPartnerId)
            .NotEmpty()
            .WithMessage("Trading partner ID is required");

        RuleFor(x => x.Year)
            .GreaterThan(1900)
            .LessThanOrEqualTo(DateTime.UtcNow.Year + 1)
            .When(x => x.Year.HasValue)
            .WithMessage("Year must be between 1900 and next year");
    }
}