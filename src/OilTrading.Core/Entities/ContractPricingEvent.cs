using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

public class ContractPricingEvent : BaseEntity
{
    private ContractPricingEvent() { } // For EF Core

    public ContractPricingEvent(
        Guid contractId,
        PricingEventType eventType,
        DateTime eventDate,
        DateTime pricingStartDate,
        DateTime pricingEndDate)
    {
        if (pricingStartDate >= pricingEndDate)
            throw new DomainException("Pricing start date must be before end date");

        ContractId = contractId;
        EventType = eventType;
        EventDate = eventDate.Date;
        PricingStartDate = pricingStartDate.Date;
        PricingEndDate = pricingEndDate.Date;
        IsFinalized = false;
        Status = PricingEventStatus.Pending;
    }

    public Guid ContractId { get; private set; }
    public PricingEventType EventType { get; private set; }
    public DateTime EventDate { get; private set; }
    public DateTime PricingStartDate { get; private set; }
    public DateTime PricingEndDate { get; private set; }
    public decimal? AveragePrice { get; private set; }
    public bool IsFinalized { get; private set; }
    public PricingEventStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string? PricingBenchmark { get; private set; }
    public int? PricingDaysCount { get; private set; }

    // Navigation Properties
    public PurchaseContract? PurchaseContract { get; private set; }
    public SalesContract? SalesContract { get; private set; }

    public void SetPricingBenchmark(string benchmarkName, string updatedBy)
    {
        if (IsFinalized)
            throw new DomainException("Cannot update pricing benchmark for finalized event");

        PricingBenchmark = benchmarkName?.Trim();
        SetUpdatedBy(updatedBy);
    }

    public void CalculateAveragePrice(decimal[] prices, int pricingDaysCount, string updatedBy)
    {
        if (IsFinalized)
            throw new DomainException("Cannot update average price for finalized event");

        if (prices == null || prices.Length == 0)
            throw new DomainException("Prices array cannot be null or empty");

        AveragePrice = prices.Average();
        PricingDaysCount = pricingDaysCount;
        Status = PricingEventStatus.Calculated;
        SetUpdatedBy(updatedBy);
    }

    public void Finalize(string finalizedBy, string? notes = null)
    {
        if (IsFinalized)
            throw new DomainException("Pricing event is already finalized");

        if (!AveragePrice.HasValue)
            throw new DomainException("Cannot finalize without calculated average price");

        IsFinalized = true;
        Status = PricingEventStatus.Finalized;
        Notes = notes?.Trim();
        SetUpdatedBy(finalizedBy);
    }

    public void Reject(string reason, string updatedBy)
    {
        if (IsFinalized)
            throw new DomainException("Cannot reject finalized pricing event");

        Status = PricingEventStatus.Rejected;
        Notes = $"Rejected: {reason}";
        SetUpdatedBy(updatedBy);
    }

    public int GetPricingPeriodDays()
    {
        return (int)(PricingEndDate - PricingStartDate).TotalDays + 1;
    }

    public bool IsInPricingPeriod(DateTime date)
    {
        var checkDate = date.Date;
        return checkDate >= PricingStartDate && checkDate <= PricingEndDate;
    }

    public override string ToString()
    {
        var priceInfo = AveragePrice.HasValue ? $"Avg: {AveragePrice:F2}" : "Not calculated";
        return $"{EventType} {EventDate:yyyy-MM-dd} - {priceInfo} ({Status})";
    }
}

public enum PricingEventStatus
{
    Pending = 1,
    Calculated = 2,
    Finalized = 3,
    Rejected = 4
}