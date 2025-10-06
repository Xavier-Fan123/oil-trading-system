using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Events;

namespace OilTrading.Core.Entities;

public class PricingEvent : BaseEntity
{
    private PricingEvent() { } // For EF Core

    public PricingEvent(
        Guid contractId,
        PricingEventType eventType,
        DateTime eventDate,
        int beforeDays = 0,
        int afterDays = 0,
        bool hasIndexOnEventDay = true,
        string? notes = null)
    {
        ContractId = contractId;
        EventType = eventType;
        EventDate = eventDate;
        BeforeDays = beforeDays >= 0 ? beforeDays : throw new DomainException("Before days cannot be negative");
        AfterDays = afterDays >= 0 ? afterDays : throw new DomainException("After days cannot be negative");
        HasIndexOnEventDay = hasIndexOnEventDay;
        Notes = notes;
        
        CalculatePricingPeriod();
    }

    public Guid ContractId { get; private set; }
    public PricingEventType EventType { get; private set; }
    public DateTime EventDate { get; private set; }
    public int BeforeDays { get; private set; }
    public int AfterDays { get; private set; }
    public bool HasIndexOnEventDay { get; private set; }
    public DateTime PricingPeriodStart { get; private set; }
    public DateTime PricingPeriodEnd { get; private set; }
    public int TotalPricingDays { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? ActualEventDate { get; private set; }
    public bool IsEventConfirmed { get; private set; }

    // Navigation Properties
    public PurchaseContract? PurchaseContract { get; private set; }
    public SalesContract? SalesContract { get; private set; }

    public void UpdateEventDate(DateTime newEventDate, string updatedBy)
    {
        if (IsEventConfirmed)
            throw new DomainException("Cannot update confirmed event date");

        EventDate = newEventDate;
        CalculatePricingPeriod();
        SetUpdatedBy(updatedBy);
    }

    public void ConfirmEvent(DateTime actualEventDate, string confirmedBy)
    {
        if (IsEventConfirmed)
            throw new DomainException("Event is already confirmed");

        ActualEventDate = actualEventDate;
        IsEventConfirmed = true;
        
        // Recalculate pricing period based on actual event date
        var originalEventDate = EventDate;
        EventDate = actualEventDate;
        CalculatePricingPeriod();
        
        SetUpdatedBy(confirmedBy);
        
        AddDomainEvent(new PricingEventConfirmedEvent(
            Id, 
            ContractId, 
            EventType, 
            originalEventDate, 
            actualEventDate));
    }

    public void UpdatePricingWindow(int beforeDays, int afterDays, bool hasIndexOnEventDay, string updatedBy)
    {
        if (IsEventConfirmed)
            throw new DomainException("Cannot update pricing window for confirmed event");

        if (beforeDays < 0 || afterDays < 0)
            throw new DomainException("Pricing window days cannot be negative");

        BeforeDays = beforeDays;
        AfterDays = afterDays;
        HasIndexOnEventDay = hasIndexOnEventDay;
        
        CalculatePricingPeriod();
        SetUpdatedBy(updatedBy);
    }

    private void CalculatePricingPeriod()
    {
        PricingPeriodStart = EventDate.AddDays(-BeforeDays);
        PricingPeriodEnd = EventDate.AddDays(AfterDays);
        
        // Calculate business days only (excluding weekends)
        TotalPricingDays = CalculateBusinessDays(PricingPeriodStart, PricingPeriodEnd, HasIndexOnEventDay);
    }

    private int CalculateBusinessDays(DateTime startDate, DateTime endDate, bool includeEventDay)
    {
        var days = 0;
        var current = startDate;

        while (current <= endDate)
        {
            // Include business days (Monday to Friday)
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                // Skip event day if not included
                if (!includeEventDay && current.Date == EventDate.Date)
                {
                    current = current.AddDays(1);
                    continue;
                }
                
                days++;
            }
            current = current.AddDays(1);
        }

        return days;
    }

    public List<DateTime> GetPricingDates()
    {
        var dates = new List<DateTime>();
        var current = PricingPeriodStart;

        while (current <= PricingPeriodEnd)
        {
            // Include business days only
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                // Skip event day if not included
                if (!HasIndexOnEventDay && current.Date == EventDate.Date)
                {
                    current = current.AddDays(1);
                    continue;
                }
                
                dates.Add(current);
            }
            current = current.AddDays(1);
        }

        return dates;
    }

    public bool IsInPricingPeriod(DateTime date)
    {
        return date.Date >= PricingPeriodStart.Date && 
               date.Date <= PricingPeriodEnd.Date &&
               date.DayOfWeek != DayOfWeek.Saturday &&
               date.DayOfWeek != DayOfWeek.Sunday &&
               (HasIndexOnEventDay || date.Date != EventDate.Date);
    }

    public string GetEventDescription()
    {
        return EventType switch
        {
            PricingEventType.BL => $"Bill of Lading date: {EventDate:yyyy-MM-dd}",
            PricingEventType.NOR => $"Notice of Readiness date: {EventDate:yyyy-MM-dd}",
            PricingEventType.COD => $"Certificate of Discharge date: {EventDate:yyyy-MM-dd}",
            _ => $"Unknown event type: {EventDate:yyyy-MM-dd}"
        };
    }

    public override string ToString()
    {
        var period = $"Pricing period: {BeforeDays} days before";
        if (AfterDays > 0)
            period += $" + {AfterDays} days after";
        
        period += HasIndexOnEventDay ? " (including event day)" : " (excluding event day)";
        
        return $"{GetEventDescription()} - {period}";
    }
}