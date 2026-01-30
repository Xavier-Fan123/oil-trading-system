using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.MarketData;

public class GetPriceHistoryQuery : IRequest<IEnumerable<MarketPriceDto>>
{
    public string ProductCode { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? PriceType { get; set; }  // Optional: filter by Spot, FuturesSettlement, etc.
    public string? ContractMonth { get; set; }  // Optional: filter by contract month (JAN25, FEB25, etc.)
    public string? Region { get; set; }  // Optional: filter by region (Singapore, Dubai, etc.) for spot prices
}