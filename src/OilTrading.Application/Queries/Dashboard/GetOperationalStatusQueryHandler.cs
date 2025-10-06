using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;

namespace OilTrading.Application.Queries.Dashboard;

public class GetOperationalStatusQueryHandler : IRequestHandler<GetOperationalStatusQuery, OperationalStatusDto>
{
    private readonly IDashboardService _dashboardService;

    public GetOperationalStatusQueryHandler(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<OperationalStatusDto> Handle(GetOperationalStatusQuery request, CancellationToken cancellationToken)
    {
        return await _dashboardService.GetOperationalStatusAsync(cancellationToken);
    }
}