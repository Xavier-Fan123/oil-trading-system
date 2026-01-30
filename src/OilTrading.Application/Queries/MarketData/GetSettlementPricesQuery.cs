using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Queries.MarketData;

/// <summary>
/// Query to retrieve market prices for settlement calculation
/// Returns all prices for a specific product/contract month within a date range
/// Used for benchmark price calculation (SimpleAverage, WeightedAverage, etc.)
/// </summary>
public class GetSettlementPricesQuery : IRequest<IEnumerable<MarketPriceDto>>
{
    /// <summary>
    /// Product code (e.g., BRENT, WTI)
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Contract month identifier (e.g., OCT25, NOV25)
    /// Used to filter prices for specific futures contract month
    /// </summary>
    public string ContractMonth { get; set; } = string.Empty;

    /// <summary>
    /// Start date for benchmark period (defaults to 30 days ago)
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date for benchmark period (defaults to today)
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Price type filter: 0=Spot, 1=FuturesSettlement, 2=ForwardCurve
    /// </summary>
    public MarketPriceType PriceType { get; set; } = MarketPriceType.Spot;
}
