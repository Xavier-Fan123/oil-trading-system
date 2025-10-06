using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
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
        return await _dbSet
            .FirstOrDefaultAsync(p => p.ProductCode == productCode && p.PriceDate.Date == priceDate.Date, cancellationToken);
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
        return await _dbSet
            .Where(p => p.ProductCode == productCode 
                && p.PriceDate >= startDate 
                && p.PriceDate <= endDate)
            .OrderBy(p => p.PriceDate)
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
        var latestPrices = await _dbSet
            .GroupBy(p => p.ProductCode)
            .Select(g => g.OrderByDescending(p => p.PriceDate).First())
            .ToListAsync(cancellationToken);
        
        return latestPrices;
    }

    public override async Task<MarketPrice> AddAsync(MarketPrice marketPrice, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(marketPrice, cancellationToken);
        return marketPrice;
    }

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
}