using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Repositories;

public interface IPricingEventRepository : IRepository<PricingEvent>
{
    Task<IReadOnlyList<PricingEvent>> GetByContractAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PricingEvent>> GetByEventTypeAsync(PricingEventType eventType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PricingEvent>> GetByEventDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PricingEvent>> GetUnconfirmedEventsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PricingEvent>> GetConfirmedEventsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PricingEvent>> GetPendingPricingEventsAsync(DateTime asOfDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PricingEvent>> GetEventsInPricingPeriodAsync(DateTime pricingDate, CancellationToken cancellationToken = default);
}