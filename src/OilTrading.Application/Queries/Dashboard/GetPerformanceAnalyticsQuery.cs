using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.Dashboard;

public class GetPerformanceAnalyticsQuery : IRequest<PerformanceAnalyticsDto>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}