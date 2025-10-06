using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.Risk;

public class CalculateRiskQuery : IRequest<RiskCalculationResultDto>
{
    public DateTime CalculationDate { get; set; }
    public int HistoricalDays { get; set; } = 252; // 1 year of trading days
    public bool IncludeStressTests { get; set; } = true;
}

public class GetPortfolioRiskSummaryQuery : IRequest<PortfolioRiskSummaryDto>
{
}

public class GetProductRiskQuery : IRequest<ProductRiskDto?>
{
    public string ProductType { get; set; } = string.Empty;
}

public class RunBacktestQuery : IRequest<BacktestResultDto>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int LookbackDays { get; set; } = 252;
}