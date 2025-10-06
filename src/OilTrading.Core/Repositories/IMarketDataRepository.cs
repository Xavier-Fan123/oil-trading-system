using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

public interface IMarketDataRepository
{
    Task<MarketPrice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MarketPrice?> GetByProductAndDateAsync(string productCode, DateTime priceDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketPrice>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketPrice>> GetByProductAsync(string productCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
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
}