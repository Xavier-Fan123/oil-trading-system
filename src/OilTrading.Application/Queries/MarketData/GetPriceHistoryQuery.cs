using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.MarketData;

public class GetPriceHistoryQuery : IRequest<IEnumerable<MarketPriceDto>>
{
    public string ProductCode { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}