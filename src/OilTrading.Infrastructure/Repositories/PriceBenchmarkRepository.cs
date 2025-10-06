using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class PriceBenchmarkRepository : Repository<PriceBenchmark>, IPriceBenchmarkRepository
{
    public PriceBenchmarkRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PriceBenchmark?> GetByNameAsync(string benchmarkName)
    {
        return await _context.PriceBenchmarks
            .FirstOrDefaultAsync(p => p.BenchmarkName == benchmarkName);
    }

    public async Task<List<PriceBenchmark>> GetActiveAsync()
    {
        return await _context.PriceBenchmarks
            .Where(p => p.IsActive)
            .OrderBy(p => p.BenchmarkName)
            .ToListAsync();
    }

    public async Task<List<PriceBenchmark>> GetByProductTypeAsync(string productType)
    {
        return await _context.PriceBenchmarks
            .Where(p => p.ProductCategory == productType && p.IsActive)
            .OrderBy(p => p.BenchmarkName)
            .ToListAsync();
    }

    public async Task<PriceBenchmark?> GetLatestPriceAsync(string benchmarkName, DateTime date)
    {
        return await _context.PriceBenchmarks
            .Where(p => p.BenchmarkName == benchmarkName && p.CreatedAt <= date)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<PriceBenchmark>> GetPriceHistoryAsync(string benchmarkName, DateTime startDate, DateTime endDate)
    {
        return await _context.PriceBenchmarks
            .Where(p => p.BenchmarkName == benchmarkName && 
                       p.CreatedAt >= startDate && 
                       p.CreatedAt <= endDate)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<string, decimal>> GetBenchmarkPricesAsync(DateTime date)
    {
        var prices = await _context.Set<DailyPrice>()
            .Include(dp => dp.Benchmark)
            .Where(dp => dp.PriceDate.Date == date.Date && dp.Benchmark.IsActive)
            .Select(dp => new 
            { 
                BenchmarkName = dp.Benchmark.BenchmarkName, 
                Price = dp.Price 
            })
            .ToListAsync();

        return prices.ToDictionary(p => p.BenchmarkName, p => p.Price);
    }
}