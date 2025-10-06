using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class PricingEventRepository : Repository<PricingEvent>, IPricingEventRepository
{
    public PricingEventRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<PricingEvent>> GetByContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.ContractId == contractId)
            .OrderBy(x => x.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PricingEvent>> GetByEventTypeAsync(PricingEventType eventType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.EventType == eventType)
            .OrderByDescending(x => x.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PricingEvent>> GetByEventDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.EventDate >= startDate && x.EventDate <= endDate)
            .OrderBy(x => x.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PricingEvent>> GetUnconfirmedEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => !x.IsEventConfirmed)
            .OrderBy(x => x.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PricingEvent>> GetConfirmedEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.IsEventConfirmed && x.ActualEventDate.HasValue)
            .OrderByDescending(x => x.ActualEventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PricingEvent>> GetPendingPricingEventsAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.IsEventConfirmed && 
                       x.ActualEventDate.HasValue &&
                       x.PricingPeriodStart <= asOfDate &&
                       x.PricingPeriodEnd >= asOfDate)
            .OrderBy(x => x.ActualEventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PricingEvent>> GetEventsInPricingPeriodAsync(DateTime pricingDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.PurchaseContract)
            .Include(x => x.SalesContract)
            .Where(x => x.PricingPeriodStart <= pricingDate && x.PricingPeriodEnd >= pricingDate)
            .OrderBy(x => x.EventDate)
            .ToListAsync(cancellationToken);
    }
}