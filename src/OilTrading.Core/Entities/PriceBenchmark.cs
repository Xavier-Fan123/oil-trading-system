using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

public class PriceBenchmark : BaseEntity
{
    private PriceBenchmark() { } // For EF Core

    public PriceBenchmark(
        string benchmarkName,
        BenchmarkType benchmarkType,
        string productCategory,
        string currency = "USD",
        string unit = "BBL")
    {
        if (string.IsNullOrWhiteSpace(benchmarkName))
            throw new DomainException("Benchmark name cannot be empty");

        if (string.IsNullOrWhiteSpace(productCategory))
            throw new DomainException("Product category cannot be empty");

        BenchmarkName = benchmarkName.Trim().ToUpper();
        BenchmarkType = benchmarkType;
        ProductCategory = productCategory.Trim();
        Currency = currency.ToUpper();
        Unit = unit.ToUpper();
        IsActive = true;
    }

    public string BenchmarkName { get; private set; } = string.Empty;
    public BenchmarkType BenchmarkType { get; private set; }
    public string ProductCategory { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public string? Description { get; private set; }
    public string? DataSource { get; private set; }

    // Navigation Properties
    public ICollection<DailyPrice> DailyPrices { get; private set; } = new List<DailyPrice>();

    public void UpdateDetails(string? description, string? dataSource, string updatedBy)
    {
        Description = description?.Trim();
        DataSource = dataSource?.Trim();
        SetUpdatedBy(updatedBy);
    }

    public void Deactivate(string updatedBy)
    {
        IsActive = false;
        SetUpdatedBy(updatedBy);
    }

    public void Activate(string updatedBy)
    {
        IsActive = true;
        SetUpdatedBy(updatedBy);
    }

    public override string ToString()
    {
        return $"{BenchmarkName} ({BenchmarkType})";
    }
}

public enum BenchmarkType
{
    MOPS = 1,      // Mean of Platts Singapore
    PLATTS = 2,    // S&P Global Platts
    ARGUS = 3,     // Argus Media
    ICE = 4,       // Intercontinental Exchange
    NYMEX = 5,     // New York Mercantile Exchange
    IPE = 6,       // International Petroleum Exchange
    BRENT = 7,     // Brent Crude
    WTI = 8,       // West Texas Intermediate
    DUBAI = 9,     // Dubai Crude
    DME = 10       // Dubai Mercantile Exchange
}