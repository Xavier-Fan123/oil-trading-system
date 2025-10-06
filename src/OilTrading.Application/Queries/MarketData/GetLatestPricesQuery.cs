using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.MarketData;

public class GetLatestPricesQuery : IRequest<LatestPricesDto>
{
}