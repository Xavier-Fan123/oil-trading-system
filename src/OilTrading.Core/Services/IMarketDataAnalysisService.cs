using OilTrading.Core.Entities;

namespace OilTrading.Core.Services;

public interface IMarketDataAnalysisService
{
    Task<HistoricalAnalysisResult> AnalyzeHistoricalTrendsAsync(string symbol, int periodDays);
    Task<VolatilityAnalysisResult> CalculateVolatilityAsync(string symbol, int periodDays);
    Task<CorrelationAnalysisResult> AnalyzeCorrelationAsync(string symbol1, string symbol2, int periodDays);
    Task<IEnumerable<MarketAlert>> CheckMarketAlertsAsync();
    Task<SeasonalityAnalysisResult> AnalyzeSeasonalityAsync(string symbol, int years);
}

public class HistoricalAnalysisResult
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal MedianPrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal AnnualizedReturn { get; set; }
    public decimal MaxDrawdown { get; set; }
    public IEnumerable<DailyReturn> DailyReturns { get; set; } = new List<DailyReturn>();
}

public class VolatilityAnalysisResult
{
    public string Symbol { get; set; } = string.Empty;
    public decimal RealizedVolatility { get; set; }
    public decimal AnnualizedVolatility { get; set; }
    public decimal VaR95 { get; set; }
    public decimal VaR99 { get; set; }
    public IEnumerable<VolatilityPoint> HistoricalVolatility { get; set; } = new List<VolatilityPoint>();
}

public class CorrelationAnalysisResult
{
    public string Symbol1 { get; set; } = string.Empty;
    public string Symbol2 { get; set; } = string.Empty;
    public decimal CorrelationCoefficient { get; set; }
    public decimal Beta { get; set; }
    public decimal RSquared { get; set; }
    public int PeriodDays { get; set; }
}

public class MarketAlert
{
    public string Symbol { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal? ThresholdValue { get; set; }
    public DateTime AlertTime { get; set; }
    public MarketAlertSeverity Severity { get; set; }
}

public class SeasonalityAnalysisResult
{
    public string Symbol { get; set; } = string.Empty;
    public IEnumerable<MonthlySeasonality> MonthlyPatterns { get; set; } = new List<MonthlySeasonality>();
    public IEnumerable<WeeklySeasonality> WeeklyPatterns { get; set; } = new List<WeeklySeasonality>();
    public decimal SeasonalityScore { get; set; }
}

public class DailyReturn
{
    public DateTime Date { get; set; }
    public decimal Return { get; set; }
    public decimal CumulativeReturn { get; set; }
}

public class VolatilityPoint
{
    public DateTime Date { get; set; }
    public decimal Volatility { get; set; }
}

public class MonthlySeasonality
{
    public int Month { get; set; }
    public decimal AverageReturn { get; set; }
    public decimal StandardDeviation { get; set; }
    public int ObservationCount { get; set; }
}

public class WeeklySeasonality
{
    public DayOfWeek DayOfWeek { get; set; }
    public decimal AverageReturn { get; set; }
    public decimal StandardDeviation { get; set; }
    public int ObservationCount { get; set; }
}

public enum MarketAlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}