using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class ShippingOperationRepository : Repository<ShippingOperation>, IShippingOperationRepository
{
    public ShippingOperationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<ShippingOperation?> GetByShippingNumberAsync(string shippingNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Include(x => x.PricingEvents)
            .FirstOrDefaultAsync(x => x.ShippingNumber == shippingNumber.ToUpper(), cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingOperation>> GetByContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.ContractId == contractId)
            .OrderBy(x => x.LoadPortETA)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingOperation>> GetByVesselAsync(string vesselName, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.VesselName.Contains(vesselName))
            .OrderByDescending(x => x.LoadPortETA)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingOperation>> GetByStatusAsync(ShippingStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.Status == status)
            .OrderBy(x => x.LoadPortETA)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingOperation>> GetByScheduleRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.LoadPortETA >= startDate && x.LoadPortETA <= endDate)
            .OrderBy(x => x.LoadPortETA)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingOperation>> GetActiveShipmentsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.Status == ShippingStatus.Planned || 
                       x.Status == ShippingStatus.Loading || 
                       x.Status == ShippingStatus.InTransit)
            .OrderBy(x => x.LoadPortETA)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingOperation>> GetDelayedShipmentsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => (x.Status == ShippingStatus.Planned && x.LoadPortETA.Date < today) ||
                       (x.Status == ShippingStatus.Loading && x.LoadPortATA.HasValue && x.LoadPortATA.Value.Date > x.LoadPortETA.Date) ||
                       (x.Status == ShippingStatus.InTransit && x.DischargePortATA.HasValue && x.DischargePortATA.Value.Date > x.DischargePortETA.Date))
            .OrderBy(x => x.LoadPortETA)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingOperation>> GetByLoadPortAsync(string loadPort, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.LoadPort != null && x.LoadPort.Contains(loadPort))
            .OrderByDescending(x => x.LoadPortETA)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingOperation>> GetByDischargePortAsync(string dischargePort, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.DischargePort != null && x.DischargePort.Contains(dischargePort))
            .OrderByDescending(x => x.DischargePortETA)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ShippingNumberExistsAsync(string shippingNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(x => x.ShippingNumber == shippingNumber.ToUpper(), cancellationToken);
    }

    public async Task<decimal> GetTotalQuantityByContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.ContractId == contractId && 
                       x.Status != ShippingStatus.Cancelled &&
                       x.ActualQuantity != null)
            .SumAsync(x => x.ActualQuantity!.Value, cancellationToken);
    }

    public async Task<ShippingOperation?> GetLastShippingByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.ShippingNumber.StartsWith($"SHIP-{year}-"))
            .OrderByDescending(x => x.ShippingNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ShippingOperation?> GetByIdWithIncludesAsync(Guid id, string[] includeProperties, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        foreach (var includeProperty in includeProperties)
        {
            query = query.Include(includeProperty);
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<ShippingOperation>> GetPagedAsync(
        System.Linq.Expressions.Expression<Func<ShippingOperation, bool>>? filter = null,
        System.Linq.Expressions.Expression<Func<ShippingOperation, object>>? orderBy = null,
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

        // Get total count before paging
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply ordering
        if (orderBy != null)
        {
            query = orderByDescending 
                ? query.OrderByDescending(orderBy) 
                : query.OrderBy(orderBy);
        }
        else
        {
            // Default ordering by LoadPortETA
            query = query.OrderBy(x => x.LoadPortETA);
        }

        // Apply paging
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ShippingOperation>(items, totalCount, page, pageSize);
    }
}