namespace OilTrading.Core.ValueObjects;

/// <summary>
/// Price statistics value object for risk and analytics calculations.
/// Computed on-demand from historical price data, not persisted to database.
/// </summary>
public class PriceStatistics
{
    /// <summary>
    /// Minimum price in the period
    /// </summary>
    public decimal MinPrice { get; set; }

    /// <summary>
    /// Maximum price in the period
    /// </summary>
    public decimal MaxPrice { get; set; }

    /// <summary>
    /// Average price in the period
    /// </summary>
    public decimal AveragePrice { get; set; }

    /// <summary>
    /// Standard deviation of prices (volatility measure)
    /// </summary>
    public decimal StandardDeviation { get; set; }

    /// <summary>
    /// Number of price observations in the period
    /// </summary>
    public int DataPointCount { get; set; }

    /// <summary>
    /// Date range of the statistics
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Date range of the statistics
    /// </summary>
    public DateTime? EndDate { get; set; }
}
