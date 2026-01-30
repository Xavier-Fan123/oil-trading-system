using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Repositories;

public interface IMarketDataRepository
{
    Task<MarketPrice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MarketPrice?> GetByProductAndDateAsync(string productCode, DateTime priceDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get spot price for a product by ProductCode and Date.
    /// Spot prices have no ContractMonth and PriceType = Spot.
    /// Uses the optimized UNIQUE index: (ProductCode, ContractMonth, PriceDate, PriceType)
    /// where ContractMonth is null and PriceType is Spot.
    /// This is the preferred method for spot price lookups to avoid UNIQUE constraint violations.
    /// </summary>
    Task<MarketPrice?> GetSpotPriceAsync(string productCode, DateTime priceDate, CancellationToken cancellationToken = default);

    Task<IEnumerable<MarketPrice>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketPrice>> GetByProductAsync(string productCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get prices for products matching a prefix pattern.
    /// Used for backward compatibility with legacy data that has embedded contract months in ProductCode.
    /// Example: "SG380 " matches "SG380 2511", "SG380 2512", etc.
    /// </summary>
    Task<IEnumerable<MarketPrice>> GetByProductPrefixAsync(
        string productPrefix,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<MarketPrice?> GetLatestPriceAsync(string productCode, CancellationToken cancellationToken = default);
    Task<MarketPrice?> GetLatestPriceAsync(string productCode, DateTime asOfDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketPrice>> GetLatestPricesAsync(CancellationToken cancellationToken = default);
    Task<List<MarketPrice>> GetHistoricalPricesAsync(string productCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Core.Entities.TimeSeries.MarketData>> GetHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate);
    Task<MarketPrice> AddAsync(MarketPrice marketPrice, CancellationToken cancellationToken = default);
    Task UpdateAsync(MarketPrice marketPrice, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAllAsync(CancellationToken cancellationToken = default);
    Task<int> DeleteByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<int> DeleteByProductCodeAsync(string productCode, CancellationToken cancellationToken = default);
    Task<int> CountAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get price for a specific product by ProductCode, ContractMonth, Date, and PriceType.
    /// Supports the new futures/derivatives architecture with monthly contracts.
    /// Uses the optimized index: (ProductCode, ContractMonth, PriceDate, PriceType)
    /// </summary>
    Task<MarketPrice?> GetByProductCodeAndContractMonthAsync(
        string productCode,
        string contractMonth,
        DateTime priceDate,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get historical price range for a product by ProductCode, ContractMonth, and DateRange.
    /// Used for volatility calculations and risk management.
    /// </summary>
    Task<IEnumerable<MarketPrice>> GetByProductCodeAndContractMonthRangeAsync(
        string productCode,
        string contractMonth,
        DateTime startDate,
        DateTime endDate,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest available price for a product by ProductCode and ContractMonth.
    /// Used for real-time position valuation and dashboard display.
    /// </summary>
    Task<MarketPrice?> GetLatestByProductCodeAndContractMonthAsync(
        string productCode,
        string contractMonth,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all distinct contract months available for a product.
    /// Used for UI dropdowns and forward curve visualization.
    /// </summary>
    Task<IEnumerable<string>> GetDistinctContractMonthsByProductCodeAsync(
        string productCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all prices for a product and contract month (no date filter).
    /// Used for statistics calculation (min, max, avg, std dev).
    /// </summary>
    Task<IEnumerable<MarketPrice>> GetAllByProductCodeAndContractMonthAsync(
        string productCode,
        string contractMonth,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default);
}