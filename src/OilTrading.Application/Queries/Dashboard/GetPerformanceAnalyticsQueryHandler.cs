using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;

namespace OilTrading.Application.Queries.Dashboard;

public class GetPerformanceAnalyticsQueryHandler : IRequestHandler<GetPerformanceAnalyticsQuery, PerformanceAnalyticsDto>
{
    private readonly IDashboardService _dashboardService;

    public GetPerformanceAnalyticsQueryHandler(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<PerformanceAnalyticsDto> Handle(GetPerformanceAnalyticsQuery request, CancellationToken cancellationToken)
    {
        return await _dashboardService.GetPerformanceAnalyticsAsync(request.StartDate, request.EndDate, cancellationToken);
    }
}