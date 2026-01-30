using MediatR;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Queries.MarketData;

/// <summary>
/// Query to retrieve price statistics for settlement analysis
/// Calculates min, max, average, and standard deviation of prices
/// Used to validate pricing calculations and analyze volatility
/// </summary>
public class GetPriceStatisticsQuery : IRequest<PriceStatistics?>
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
