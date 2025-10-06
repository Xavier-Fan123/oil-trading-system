using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

public interface IPriceValidationService
{
    /// <summary>
    /// Validate a single price point against historical data
    /// </summary>
    Task<PriceValidationResult> ValidatePriceAsync(string productType, decimal price, DateTime priceDate);
    
    /// <summary>
    /// Validate multiple prices in a series
    /// </summary>
    Task<List<PriceValidationResult>> ValidatePriceSeriesAsync(string productType, Dictionary<DateTime, decimal> prices);
    
    /// <summary>
    /// Detect price anomalies in historical data
    /// </summary>
    Task<List<PriceAnomalyResult>> DetectPriceAnomaliesAsync(string productType, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Get price volatility metrics
    /// </summary>
    Task<PriceVolatilityMetrics> GetVolatilityMetricsAsync(string productType, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Check if price change exceeds threshold
    /// </summary>
    Task<PriceChangeValidation> ValidatePriceChangeAsync(string productType, decimal oldPrice, decimal newPrice, DateTime changeDate);
    
    /// <summary>
    /// Get price validation configuration for a product
    /// </summary>
    Task<PriceValidationConfig> GetValidationConfigAsync(string productType);
    
    /// <summary>
    /// Update price validation thresholds
    /// </summary>
    Task UpdateValidationConfigAsync(string productType, PriceValidationConfig config);
}

public class PriceValidationResult
{
    public bool IsValid { get; set; }
    public decimal Price { get; set; }
    public DateTime PriceDate { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public PriceValidationLevel ValidationLevel { get; set; }
    public List<string> ValidationMessages { get; set; } = new();
    public PriceStatistics Statistics { get; set; } = new();
    public bool RequiresApproval { get; set; }
    public string? ApprovalReason { get; set; }
}

public class PriceAnomalyResult
{
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public AnomalyType AnomalyType { get; set; }
    public decimal Severity { get; set; } // 0-1 scale
    public string Description { get; set; } = string.Empty;
    public decimal ExpectedPrice { get; set; }
    public decimal Deviation { get; set; }
    public decimal ZScore { get; set; }
    public bool IsOutlier { get; set; }
}

public class PriceVolatilityMetrics
{
    public string ProductType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DailyVolatility { get; set; }
    public decimal AnnualizedVolatility { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal PriceRange { get; set; }
    public decimal Skewness { get; set; }
    public decimal Kurtosis { get; set; }
    public int TotalObservations { get; set; }
}

public class PriceChangeValidation
{
    public bool IsValidChange { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercentage { get; set; }
    public DateTime ChangeDate { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public ChangeValidationLevel ValidationLevel { get; set; }
    public string? ValidationMessage { get; set; }
    public bool RequiresApproval { get; set; }
    public decimal ThresholdExceeded { get; set; }
}

public class PriceValidationConfig
{
    public string ProductType { get; set; } = string.Empty;
    public decimal MaxDailyChange { get; set; } = 0.15m; // 15% max daily change
    public decimal VolatilityThreshold { get; set; } = 0.25m; // 25% annualized volatility
    public decimal ZScoreThreshold { get; set; } = 3.0m; // 3 standard deviations
    public int HistoricalDays { get; set; } = 252; // Trading days for analysis
    public bool EnableOutlierDetection { get; set; } = true;
    public bool RequireApprovalForLargeChanges { get; set; } = true;
    public decimal ApprovalThreshold { get; set; } = 0.10m; // 10% change requires approval
    public List<PriceValidationRule> ValidationRules { get; set; } = new();
}

public class PriceValidationRule
{
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public decimal Threshold { get; set; }
    public ValidationAction Action { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class PriceStatistics
{
    public decimal Mean { get; set; }
    public decimal Median { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal ZScore { get; set; }
    public decimal Percentile95 { get; set; }
    public decimal Percentile5 { get; set; }
    public bool IsOutlier => Math.Abs(ZScore) > 3.0m;
}

public enum PriceValidationLevel
{
    Valid = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

public enum AnomalyType
{
    Spike = 1,
    Drop = 2,
    Volatility = 3,
    Trend = 4,
    Outlier = 5
}

public enum ChangeValidationLevel
{
    Normal = 1,
    Elevated = 2,
    High = 3,
    Extreme = 4
}

public enum ValidationAction
{
    Accept = 1,
    Warn = 2,
    Reject = 3,
    RequireApproval = 4
}