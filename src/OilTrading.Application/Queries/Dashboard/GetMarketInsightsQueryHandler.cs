using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;

namespace OilTrading.Application.Queries.Dashboard;

public class GetMarketInsightsQueryHandler : IRequestHandler<GetMarketInsightsQuery, MarketInsightsDto>
{
    private readonly IDashboardService _dashboardService;

    public GetMarketInsightsQueryHandler(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<MarketInsightsDto> Handle(GetMarketInsightsQuery request, CancellationToken cancellationToken)
    {
        return await _dashboardService.GetMarketInsightsAsync(cancellationToken);
    }
}