using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.Dashboard;

public class GetMarketInsightsQuery : IRequest<MarketInsightsDto>
{
}