using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Infrastructure.Data;
using System.Linq.Expressions;

namespace OilTrading.Infrastructure.Repositories;

public class PurchaseContractRepository : Repository<PurchaseContract>, IPurchaseContractRepository
{
    public PurchaseContractRepository(ApplicationDbContext context) : base(context) { }

    public async Task<PurchaseContract?> GetByContractNumberAsync(ContractNumber contractNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .Include(x => x.Product)
            .Include(x => x.Trader)
            .FirstOrDefaultAsync(x => x.ContractNumber.Value == contractNumber.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseContract>> GetByTradingPartnerAsync(Guid tradingPartnerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .Include(x => x.Product)
            .Include(x => x.Trader)
            .Where(x => x.TradingPartnerId == tradingPartnerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseContract>> GetByTraderAsync(Guid traderId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .Include(x => x.Product)
            .Include(x => x.Trader)
            .Where(x => x.TraderId == traderId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseContract>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .Include(x => x.Product)
            .Include(x => x.Trader)
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseContract>> GetByStatusAsync(ContractStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .Include(x => x.Product)
            .Include(x => x.Trader)
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseContract>> GetByLaycanDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .Include(x => x.Product)
            .Include(x => x.Trader)
            .Where(x => x.LaycanStart.HasValue && x.LaycanEnd.HasValue &&
                       x.LaycanStart >= startDate && x.LaycanEnd <= endDate)
            .OrderBy(x => x.LaycanStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseContract>> GetActiveContractsAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync(ContractStatus.Active, cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseContract>> GetContractsWithAvailableQuantityAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .Include(x => x.Product)
            .Include(x => x.Trader)
            .Include(x => x.LinkedSalesContracts.Where(sc => sc.Status == ContractStatus.Active))
            .Where(x => x.Status == ContractStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ContractNumberExistsAsync(ContractNumber contractNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(x => x.ContractNumber.Value == contractNumber.Value, cancellationToken);
    }

    public async Task<decimal> GetTotalContractValueByTraderAsync(Guid traderId, string currency = "USD", CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.TraderId == traderId && 
                       x.Status == ContractStatus.Active &&
                       x.ContractValue != null &&
                       x.ContractValue.Currency == currency)
            .SumAsync(x => x.ContractValue!.Amount, cancellationToken);
    }

    public async Task<ContractNumber> GetNextContractNumberAsync(int year, ContractType contractType, CancellationToken cancellationToken = default)
    {
        var typeString = contractType.ToString().ToUpper();
        var prefix = $"ITGR-{year}-{typeString}-B";
        
        var lastContract = await _dbSet
            .Where(x => x.ContractNumber.Value.StartsWith(prefix))
            .OrderByDescending(x => x.ContractNumber.Value)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSerial = 1;
        if (lastContract != null)
        {
            nextSerial = lastContract.ContractNumber.SerialNumber + 1;
        }

        return ContractNumber.Create(year, contractType, nextSerial);
    }

    public async Task<PurchaseContract?> GetByIdWithIncludesAsync(Guid id, string[] includeProperties, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (includeProperties != null)
        {
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<PurchaseContract>> GetPagedAsync(
        Expression<Func<PurchaseContract, bool>>? filter = null,
        Expression<Func<PurchaseContract, object>>? orderBy = null,
        bool orderByDescending = false,
        int page = 1,
        int pageSize = 20,
        string[]? includeProperties = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Apply includes
        if (includeProperties != null)
        {
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }
        }

        // Apply filter
        if (filter != null)
        {
            query = query.Where(filter);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        if (orderBy != null)
        {
            query = orderByDescending 
                ? query.OrderByDescending(orderBy) 
                : query.OrderBy(orderBy);
        }
        else
        {
            query = query.OrderByDescending(x => x.CreatedAt);
        }

        // Apply paging
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseContract>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<PurchaseContract>> GetContractsByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31, 23, 59, 59);

        return await _dbSet
            .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate)
            .OrderBy(x => x.ContractNumber.Value)
            .ToListAsync(cancellationToken);
    }
}