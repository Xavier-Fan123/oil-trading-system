using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

public interface IFuturesDealRepository : IRepository<FuturesDeal>
{
    Task<FuturesDeal?> GetByDealNumberAsync(string dealNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<FuturesDeal>> GetByTradeDateAsync(DateTime tradeDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<FuturesDeal>> GetByProductAsync(string productCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<FuturesDeal>> GetByContractMonthAsync(string contractMonth, CancellationToken cancellationToken = default);
    Task<IEnumerable<FuturesDeal>> GetOpenPositionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<FuturesDeal>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    // Position aggregation
    Task<decimal> GetNetPositionAsync(string productCode, string contractMonth, CancellationToken cancellationToken = default);
    Task<Dictionary<string, decimal>> GetPositionsByProductAsync(CancellationToken cancellationToken = default);
    
    // Duplicate check
    Task<bool> DealExistsAsync(string dealNumber, DateTime tradeDate, CancellationToken cancellationToken = default);
    
    // Batch operations for performance
    new Task AddRangeAsync(IEnumerable<FuturesDeal> deals, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<FuturesDeal> deals, CancellationToken cancellationToken = default);
}