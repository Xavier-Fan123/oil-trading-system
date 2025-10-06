using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

public class DailyPrice : BaseEntity
{
    private DailyPrice() { } // For EF Core

    public DailyPrice(
        Guid benchmarkId,
        DateTime priceDate,
        decimal price,
        decimal premium = 0m,
        bool isHoliday = false)
    {
        BenchmarkId = benchmarkId;
        PriceDate = priceDate.Date; // Ensure date only
        Price = price >= 0 ? price : throw new DomainException("Price cannot be negative");
        Premium = premium;
        IsHoliday = isHoliday;
    }

    public Guid BenchmarkId { get; private set; }
    public DateTime PriceDate { get; private set; }
    public decimal Price { get; private set; }
    public decimal OpenPrice { get; private set; }
    public decimal HighPrice { get; private set; }
    public decimal LowPrice { get; private set; }
    public decimal ClosePrice { get; private set; }
    public decimal Volume { get; private set; }
    public decimal Premium { get; private set; }
    public decimal? Discount { get; private set; }
    public bool IsHoliday { get; private set; }
    public bool IsPublished { get; private set; } = true;
    public string? Notes { get; private set; }
    public string? DataSource { get; private set; }
    public string? DataQuality { get; private set; }

    // Navigation Properties
    public PriceBenchmark Benchmark { get; private set; } = null!;

    public void UpdatePrice(decimal newPrice, decimal premium = 0m, string updatedBy = "", string? notes = null)
    {
        if (newPrice < 0)
            throw new DomainException("Price cannot be negative");

        Price = newPrice;
        Premium = premium;
        Notes = notes?.Trim();
        SetUpdatedBy(updatedBy);
    }

    public void MarkAsHoliday(string updatedBy)
    {
        IsHoliday = true;
        SetUpdatedBy(updatedBy);
    }

    public decimal GetEffectivePrice()
    {
        return Price + Premium;
    }

    public Money GetPriceAsMoney(string currency)
    {
        return new Money(GetEffectivePrice(), currency);
    }

    public bool IsPriceStale(int maxDaysOld = 2)
    {
        return DateTime.UtcNow.Date.Subtract(PriceDate).TotalDays > maxDaysOld;
    }

    public override string ToString()
    {
        var effectivePrice = GetEffectivePrice();
        return $"{PriceDate:yyyy-MM-dd}: {effectivePrice:F2}" + (IsHoliday ? " (Holiday)" : "");
    }
}