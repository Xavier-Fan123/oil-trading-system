using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Queries.MarketData;

/// <summary>
/// Query to retrieve the latest price for a specific product and contract month
/// Used for real-time settlement price reference and position valuation
/// </summary>
public class GetLatestContractMonthPriceQuery : IRequest<MarketPriceDto?>
{
    /// <summary>
    /// Product code (e.g., BRENT, WTI)
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Contract month identifier (e.g., OCT25, NOV25)
    /// </summary>
    public string ContractMonth { get; set; } = string.Empty;

    /// <summary>
    /// Price type filter: 0=Spot, 1=FuturesSettlement, 2=ForwardCurve
    /// </summary>
    public MarketPriceType PriceType { get; set; } = MarketPriceType.Spot;
}
