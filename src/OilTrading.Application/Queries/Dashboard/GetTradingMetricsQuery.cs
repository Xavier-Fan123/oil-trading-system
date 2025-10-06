using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.Dashboard;

public class GetTradingMetricsQuery : IRequest<TradingMetricsDto>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}