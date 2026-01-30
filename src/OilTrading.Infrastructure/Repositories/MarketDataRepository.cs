using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class MarketDataRepository : Repository<MarketPrice>, IMarketDataRepository
{
    public MarketDataRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<MarketPrice?> GetByProductAndDateAsync(
        string productCode,
        DateTime priceDate,
        CancellationToken cancellationToken = default)
    {
        // CRITICAL: Must check against UNIQUE index constraint (ProductCode, ContractMonth, PriceDate, PriceType)
        // For Spot prices: ContractMonth is null, PriceType is Spot
        // For Futures: ContractMonth varies (AUG25, SEP25, etc), PriceType is FuturesSettlement
        // This query only works correctly when ContractMonth is null (i.e., Spot prices)
        // For futures prices with contract months, use GetByProductCodeAndContractMonthAsync instead
        return await _dbSet
            .FirstOrDefaultAsync(p =>
                p.ProductCode == productCode &&
                p.PriceDate.Date == priceDate.Date &&
                string.IsNullOrEmpty(p.ContractMonth), // Spot prices only (no contract month)
                cancellationToken);
    }

    /// <summary>
    /// Get spot price for a product by ProductCode, Date.
    /// Spot prices have no ContractMonth and PriceType = Spot.
    /// Uses the optimized index: (ProductCode, ContractMonth, PriceDate, PriceType)
    /// where ContractMonth is null and PriceType is Spot.
    /// </summary>
    public async Task<MarketPrice?> GetSpotPriceAsync(
        string productCode,
        DateTime priceDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p =>
                p.ProductCode == productCode &&
                p.PriceDate.Date == priceDate.Date &&
                string.IsNullOrEmpty(p.ContractMonth) &&
                p.PriceType == MarketPriceType.Spot,
                cancellationToken);
    }

    public async Task<IEnumerable<MarketPrice>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.PriceDate.Date == date.Date)
            .OrderBy(p => p.ProductCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketPrice>> GetByProductAsync(
        string productCode,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        // Support both exact match and base product code prefix match
        // Examples:
        //   - "ICE_BRENT" will match "ICE_BRENT", "ICE_BRENT_JAN25", "ICE_BRENT_FEB25", etc.
        //   - "MOPS_380" will match "MOPS_380" (exact match for spot prices)
        // This allows frontend to query with base product code and get all contract months
        return await _dbSet
            .Where(p => (p.ProductCode == productCode || p.ProductCode.StartsWith(productCode + "_"))
                && p.PriceDate >= startDate
                && p.PriceDate <= endDate)
            .OrderBy(p => p.PriceDate)
            .ThenBy(p => p.ContractMonth)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get prices for products matching a prefix pattern (with space).
    /// Supports backward compatibility with legacy data format: "SG380 2511", "GO 10ppm 2601", etc.
    /// Used when querying clean base codes ("SG380") against legacy embedded format ("SG380 2511").
    /// </summary>
    public async Task<IEnumerable<MarketPrice>> GetByProductPrefixAsync(
        string productPrefix,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(p => p.ProductCode.StartsWith(productPrefix))
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(p => p.PriceDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(p => p.PriceDate <= endDate.Value);

        return await query
            .OrderBy(p => p.PriceDate)
            .ThenBy(p => p.ContractMonth)
            .ToListAsync(cancellationToken);
    }

    public async Task<MarketPrice?> GetLatestPriceAsync(string productCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ProductCode == productCode)
            .OrderByDescending(p => p.PriceDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MarketPrice?> GetLatestPriceAsync(string productCode, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ProductCode == productCode && p.PriceDate <= asOfDate)
            .OrderByDescending(p => p.PriceDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<MarketPrice>> GetHistoricalPricesAsync(string productCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ProductCode == productCode 
                && p.PriceDate >= startDate 
                && p.PriceDate <= endDate)
            .OrderBy(p => p.PriceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketPrice>> GetLatestPricesAsync(CancellationToken cancellationToken = default)
    {
        // Group by ProductCode, PriceType, and ContractMonth to properly separate:
        // - Spot prices (no ContractMonth)
        // - Futures prices (with ContractMonth)
        // - Forward prices (with ContractMonth)
        // This ensures futures contracts don't hide spot prices and vice versa
        var latestPrices = await _dbSet
            .GroupBy(p => new { p.ProductCode, p.PriceType, p.ContractMonth })
            .Select(g => g.OrderByDescending(p => p.PriceDate).First())
            .ToListAsync(cancellationToken);

        return latestPrices;
    }

    // Note: AddAsync is inherited from Repository<T> base class which now includes
    // systemic BaseEntity property tracking fix that applies to all entities

    public override async Task UpdateAsync(MarketPrice marketPrice, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(marketPrice);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
            _dbSet.Remove(entity);
    }

    public async Task<IEnumerable<Core.Entities.TimeSeries.MarketData>> GetHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate)
    {
        // Convert MarketPrice to TimeSeries.MarketData for compatibility
        var marketPrices = await _dbSet
            .Where(p => p.ProductCode == symbol && p.PriceDate >= startDate && p.PriceDate <= endDate)
            .OrderBy(p => p.PriceDate)
            .ToListAsync();

        return marketPrices.Select(mp => new Core.Entities.TimeSeries.MarketData
        {
            Timestamp = mp.PriceDate,
            Symbol = mp.ProductCode,
            Exchange = mp.Source ?? "Unknown",
            Price = mp.Price,
            Volume = 0, // Not available in MarketPrice
            High = mp.Price,
            Low = mp.Price,
            Open = mp.Price,
            Close = mp.Price,
            Currency = mp.Currency,
            DataSource = mp.DataSource ?? "Historical"
        });
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await _dbSet.ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> DeleteByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.PriceDate >= startDate && p.PriceDate <= endDate)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> DeleteByProductCodeAsync(string productCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ProductCode == productCode)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Get price for a specific product by ProductCode, ContractMonth, Date, and PriceType.
    /// Supports the futures/derivatives architecture with monthly contracts.
    /// Uses the optimized index: (ProductCode, ContractMonth, PriceDate, PriceType)
    /// </summary>
    public async Task<MarketPrice?> GetByProductCodeAndContractMonthAsync(
        string productCode,
        string contractMonth,
        DateTime priceDate,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ProductCode == productCode
                && p.ContractMonth == contractMonth
                && p.PriceDate.Date == priceDate.Date
                && p.PriceType == priceType)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Get historical price range for a product by ProductCode, ContractMonth, and DateRange.
    /// Used for volatility calculations and risk management.
    /// </summary>
    public async Task<IEnumerable<MarketPrice>> GetByProductCodeAndContractMonthRangeAsync(
        string productCode,
        string contractMonth,
        DateTime startDate,
        DateTime endDate,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ProductCode == productCode
                && p.ContractMonth == contractMonth
                && p.PriceDate.Date >= startDate.Date
                && p.PriceDate.Date <= endDate.Date
                && p.PriceType == priceType)
            .OrderBy(p => p.PriceDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get the latest available price for a product by ProductCode and ContractMonth.
    /// Used for real-time position valuation and dashboard display.
    /// </summary>
    public async Task<MarketPrice?> GetLatestByProductCodeAndContractMonthAsync(
        string productCode,
        string contractMonth,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ProductCode == productCode
                && p.ContractMonth == contractMonth
                && p.PriceType == priceType)
            .OrderByDescending(p => p.PriceDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Get all distinct contract months available for a product.
    /// Used for UI dropdowns and forward curve visualization.
    /// </summary>
    public async Task<IEnumerable<string>> GetDistinctContractMonthsByProductCodeAsync(
        string productCode,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ProductCode == productCode && !string.IsNullOrEmpty(p.ContractMonth))
            .Select(p => p.ContractMonth!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get all prices for a product and contract month (no date filter).
    /// Used for statistics calculation (min, max, avg, std dev).
    /// </summary>
    public async Task<IEnumerable<MarketPrice>> GetAllByProductCodeAndContractMonthAsync(
        string productCode,
        string contractMonth,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ProductCode == productCode
                && p.ContractMonth == contractMonth
                && p.PriceType == priceType)
            .OrderBy(p => p.PriceDate)
            .ToListAsync(cancellationToken);
    }
}