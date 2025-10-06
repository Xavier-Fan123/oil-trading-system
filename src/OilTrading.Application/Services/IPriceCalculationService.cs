namespace OilTrading.Application.Services;

public interface IPriceCalculationService
{
    Task<decimal> CalculateContractPriceAsync(Guid contractId);
    Task<decimal> CalculatePeriodAveragePriceAsync(string benchmarkName, DateTime startDate, DateTime endDate);
    Task<bool> FinalizePriceAsync(Guid contractId, string finalizedBy);
    Task<int> CalculateBusinessDaysAsync(DateTime startDate, DateTime endDate);
    Task<decimal[]> GetDailyPricesAsync(string benchmarkName, DateTime startDate, DateTime endDate);
    
    // Enhanced methods with basis calculation support
    Task<PriceCalculationResult> CalculateContractPriceWithDetailsAsync(Guid contractId);
    Task<decimal> CalculateBasisAdjustedPriceAsync(decimal futuresPrice, string productType, DateTime valuationDate, string futuresContract);
    Task<decimal> CalculateSpreadAdjustedPriceAsync(string baseBenchmark, string spreadBenchmark, DateTime startDate, DateTime endDate);
}

public class PriceCalculationResult
{
    public decimal FinalPrice { get; set; }
    public decimal[] DailyPrices { get; set; } = Array.Empty<decimal>();
    public int BusinessDaysUsed { get; set; }
    public string CalculationMethod { get; set; } = string.Empty;
    public string BenchmarkUsed { get; set; } = string.Empty;
    public DateTime CalculationDate { get; set; }
    public Dictionary<string, object> CalculationDetails { get; set; } = new Dictionary<string, object>();
}