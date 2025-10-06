using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class FuturesDealRepository : Repository<FuturesDeal>, IFuturesDealRepository
{
    public FuturesDealRepository(ApplicationDbContext context) : base(context)
    {
    }
    
    public async Task<FuturesDeal?> GetByDealNumberAsync(string dealNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(d => d.DealNumber == dealNumber, cancellationToken);
    }
    
    public async Task<IEnumerable<FuturesDeal>> GetByTradeDateAsync(DateTime tradeDate, CancellationToken cancellationToken = default)
    {
        var startOfDay = tradeDate.Date;
        var endOfDay = startOfDay.AddDays(1);
        
        return await _dbSet
            .Where(d => d.TradeDate >= startOfDay && d.TradeDate < endOfDay)
            .OrderBy(d => d.DealNumber)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<FuturesDeal>> GetByProductAsync(string productCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.ProductCode == productCode)
            .OrderByDescending(d => d.TradeDate)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<FuturesDeal>> GetByContractMonthAsync(string contractMonth, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.ContractMonth == contractMonth)
            .OrderBy(d => d.TradeDate)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<FuturesDeal>> GetOpenPositionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.Status == DealStatus.Executed && d.RealizedPnL == null)
            .OrderBy(d => d.ContractMonth)
            .ThenBy(d => d.ProductCode)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<FuturesDeal>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.TradeDate >= startDate && d.TradeDate <= endDate)
            .OrderBy(d => d.TradeDate)
            .ThenBy(d => d.DealNumber)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<decimal> GetNetPositionAsync(string productCode, string contractMonth, CancellationToken cancellationToken = default)
    {
        var deals = await _dbSet
            .Where(d => d.ProductCode == productCode && 
                       d.ContractMonth == contractMonth &&
                       d.Status == DealStatus.Executed)
            .ToListAsync(cancellationToken);
        
        var buyQuantity = deals
            .Where(d => d.Direction == DealDirection.Buy)
            .Sum(d => d.Quantity);
            
        var sellQuantity = deals
            .Where(d => d.Direction == DealDirection.Sell)
            .Sum(d => d.Quantity);
            
        return buyQuantity - sellQuantity;
    }
    
    public async Task<Dictionary<string, decimal>> GetPositionsByProductAsync(CancellationToken cancellationToken = default)
    {
        var deals = await _dbSet
            .Where(d => d.Status == DealStatus.Executed)
            .GroupBy(d => new { d.ProductCode, d.ContractMonth })
            .Select(g => new
            {
                Key = $"{g.Key.ProductCode}_{g.Key.ContractMonth}",
                BuyQuantity = g.Where(d => d.Direction == DealDirection.Buy).Sum(d => d.Quantity),
                SellQuantity = g.Where(d => d.Direction == DealDirection.Sell).Sum(d => d.Quantity)
            })
            .ToListAsync(cancellationToken);
        
        return deals.ToDictionary(
            d => d.Key,
            d => d.BuyQuantity - d.SellQuantity
        );
    }
    
    public async Task<bool> DealExistsAsync(string dealNumber, DateTime tradeDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(d => d.DealNumber == dealNumber && 
                          d.TradeDate.Date == tradeDate.Date, 
                      cancellationToken);
    }
    
    public new async Task AddRangeAsync(IEnumerable<FuturesDeal> deals, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(deals, cancellationToken);
    }
    
    public async Task UpdateRangeAsync(IEnumerable<FuturesDeal> deals, CancellationToken cancellationToken = default)
    {
        _dbSet.UpdateRange(deals);
        await Task.CompletedTask;
    }
}