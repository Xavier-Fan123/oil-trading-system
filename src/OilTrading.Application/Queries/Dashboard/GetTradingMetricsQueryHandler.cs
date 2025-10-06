using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;

namespace OilTrading.Application.Queries.Dashboard;

public class GetTradingMetricsQueryHandler : IRequestHandler<GetTradingMetricsQuery, TradingMetricsDto>
{
    private readonly IDashboardService _dashboardService;

    public GetTradingMetricsQueryHandler(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<TradingMetricsDto> Handle(GetTradingMetricsQuery request, CancellationToken cancellationToken)
    {
        return await _dashboardService.GetTradingMetricsAsync(request.StartDate, request.EndDate, cancellationToken);
    }
}