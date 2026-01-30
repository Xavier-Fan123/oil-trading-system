using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Services;

/// <summary>
/// Unified pricing service interface for querying market prices across the system.
///
/// This service provides a single entry point for all market price queries used in:
/// - Settlement calculations (linking prices to specific contracts)
/// - Risk management (VaR calculations, volatility, correlation)
/// - Position reporting (market value calculations)
/// - Audit trail (price source tracking for compliance)
///
/// Key Design Decisions:
/// 1. Separated by PriceType (Spot vs Futures) - different calculation rules
/// 2. Grouped by (ProductType, ContractMonth) - preserves temporal structure
/// 3. Includes Product relationship - enables future product-level permissions
/// 4. Returns historical ranges - supports volatility and basis calculations
/// 5. Audit-trail friendly - tracks all price sources
/// </summary>
public interface IPricingService
{
    /// <summary>
    /// Get the latest price for a product on a specific date.
    /// Used for settlement price determination and P&L calculations.
    /// </summary>
    /// <param name="productId">Product identifier (Brent, WTI, MGO, etc.)</param>
    /// <param name="contractMonth">Contract month in YYMM format (e.g., "2511" for Nov 2025)</param>
    /// <param name="priceDate">Date for which to retrieve the price</param>
    /// <param name="priceType">Spot or Futures price type</param>
    /// <returns>Market price on that date, or null if not found</returns>
    Task<MarketPrice?> GetSettlementPriceAsync(
        Guid productId,
        string contractMonth,
        DateTime priceDate,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get historical prices for a product across a date range.
    /// Used for volatility calculations, basis calculations, and price trend analysis.
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="contractMonth">Contract month in YYMM format</param>
    /// <param name="startDate">Start of date range (inclusive)</param>
    /// <param name="endDate">End of date range (inclusive)</param>
    /// <param name="priceType">Spot or Futures price type</param>
    /// <returns>Ordered list of market prices for the period</returns>
    Task<IEnumerable<MarketPrice>> GetHistoricalPricesAsync(
        Guid productId,
        string contractMonth,
        DateTime startDate,
        DateTime endDate,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the most recent available price for a product (across all dates).
    /// Used for real-time position valuation and dashboard display.
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="contractMonth">Contract month in YYMM format</param>
    /// <param name="priceType">Spot or Futures price type</param>
    /// <returns>Most recent market price available, or null if no prices exist</returns>
    Task<MarketPrice?> GetLatestPriceAsync(
        Guid productId,
        string contractMonth,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all available contract months for a product.
    /// Used for UI dropdowns, forward curve visualization, and term structure analysis.
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <returns>Sorted list of contract months (e.g., ["2511", "2512", "2601", ...])</returns>
    Task<IEnumerable<string>> GetAvailableContractMonthsAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get price statistics for volatility and basis calculations.
    /// Returns computed statistics (not stored in DB) for a contract month period.
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="contractMonth">Contract month in YYMM format</param>
    /// <param name="priceType">Spot or Futures price type</param>
    /// <returns>Price statistics including min, max, avg, volatility</returns>
    Task<PriceStatistics> GetPriceStatisticsAsync(
        Guid productId,
        string contractMonth,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that a price exists for settlement purposes.
    /// Separated method for explicit validation (vs. returning null) to avoid null-reference errors.
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="contractMonth">Contract month in YYMM format</param>
    /// <param name="priceDate">Date for which to check price existence</param>
    /// <param name="priceType">Spot or Futures price type</param>
    /// <returns>True if price exists, false otherwise</returns>
    Task<bool> PriceExistsAsync(
        Guid productId,
        string contractMonth,
        DateTime priceDate,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default);
}
