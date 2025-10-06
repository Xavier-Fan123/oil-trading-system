using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;

namespace OilTrading.Application.Queries.Dashboard;

public class GetDashboardOverviewQueryHandler : IRequestHandler<GetDashboardOverviewQuery, DashboardOverviewDto>
{
    private readonly IDashboardService _dashboardService;

    public GetDashboardOverviewQueryHandler(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<DashboardOverviewDto> Handle(GetDashboardOverviewQuery request, CancellationToken cancellationToken)
    {
        return await _dashboardService.GetDashboardOverviewAsync(cancellationToken);
    }
}