using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Events;

public class PricingEventConfirmedEvent : DomainEvent
{
    public Guid PricingEventId { get; }
    public Guid ContractId { get; }
    public PricingEventType EventType { get; }
    public DateTime OriginalEventDate { get; }
    public DateTime ActualEventDate { get; }

    public PricingEventConfirmedEvent(
        Guid pricingEventId,
        Guid contractId,
        PricingEventType eventType,
        DateTime originalEventDate,
        DateTime actualEventDate)
    {
        PricingEventId = pricingEventId;
        ContractId = contractId;
        EventType = eventType;
        OriginalEventDate = originalEventDate;
        ActualEventDate = actualEventDate;
    }
}