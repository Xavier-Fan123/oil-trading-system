using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

public interface IPriceBenchmarkRepository : IRepository<PriceBenchmark>
{
    Task<PriceBenchmark?> GetByNameAsync(string benchmarkName);
    Task<List<PriceBenchmark>> GetActiveAsync();
    Task<List<PriceBenchmark>> GetByProductTypeAsync(string productType);
    Task<PriceBenchmark?> GetLatestPriceAsync(string benchmarkName, DateTime date);
    Task<List<PriceBenchmark>> GetPriceHistoryAsync(string benchmarkName, DateTime startDate, DateTime endDate);
    Task<Dictionary<string, decimal>> GetBenchmarkPricesAsync(DateTime date);
}