using Microsoft.Extensions.Logging;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Repositories;
using System.Text.Json;

namespace OilTrading.Application.Services;

public class PriceValidationService : IPriceValidationService
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PriceValidationService> _logger;

    // Default validation configurations for different product types
    private readonly Dictionary<string, PriceValidationConfig> _defaultConfigs = new()
    {
        {
            "BRENT", new PriceValidationConfig
            {
                ProductType = "BRENT",
                MaxDailyChange = 0.12m,
                VolatilityThreshold = 0.30m,
                ZScoreThreshold = 2.5m,
                HistoricalDays = 252,
                ApprovalThreshold = 0.08m,
                ValidationRules = new List<PriceValidationRule>
                {
                    new() { RuleName = "DailyChange", Threshold = 0.12m, Action = ValidationAction.RequireApproval },
                    new() { RuleName = "ZScore", Threshold = 2.5m, Action = ValidationAction.Warn },
                    new() { RuleName = "AbsoluteMin", Threshold = 20m, Action = ValidationAction.Reject },
                    new() { RuleName = "AbsoluteMax", Threshold = 200m, Action = ValidationAction.Reject }
                }
            }
        },
        {
            "WTI", new PriceValidationConfig
            {
                ProductType = "WTI",
                MaxDailyChange = 0.12m,
                VolatilityThreshold = 0.32m,
                ZScoreThreshold = 2.5m,
                HistoricalDays = 252,
                ApprovalThreshold = 0.08m,
                ValidationRules = new List<PriceValidationRule>
                {
                    new() { RuleName = "DailyChange", Threshold = 0.12m, Action = ValidationAction.RequireApproval },
                    new() { RuleName = "ZScore", Threshold = 2.5m, Action = ValidationAction.Warn },
                    new() { RuleName = "AbsoluteMin", Threshold = 15m, Action = ValidationAction.Reject },
                    new() { RuleName = "AbsoluteMax", Threshold = 180m, Action = ValidationAction.Reject }
                }
            }
        },
        {
            "MOPS FO 380", new PriceValidationConfig
            {
                ProductType = "MOPS FO 380",
                MaxDailyChange = 0.15m,
                VolatilityThreshold = 0.35m,
                ZScoreThreshold = 3.0m,
                HistoricalDays = 126,
                ApprovalThreshold = 0.10m,
                ValidationRules = new List<PriceValidationRule>
                {
                    new() { RuleName = "DailyChange", Threshold = 0.15m, Action = ValidationAction.RequireApproval },
                    new() { RuleName = "ZScore", Threshold = 3.0m, Action = ValidationAction.Warn },
                    new() { RuleName = "AbsoluteMin", Threshold = 200m, Action = ValidationAction.Reject },
                    new() { RuleName = "AbsoluteMax", Threshold = 800m, Action = ValidationAction.Reject }
                }
            }
        },
        {
            "MOPS MGO", new PriceValidationConfig
            {
                ProductType = "MOPS MGO",
                MaxDailyChange = 0.15m,
                VolatilityThreshold = 0.35m,
                ZScoreThreshold = 3.0m,
                HistoricalDays = 126,
                ApprovalThreshold = 0.10m,
                ValidationRules = new List<PriceValidationRule>
                {
                    new() { RuleName = "DailyChange", Threshold = 0.15m, Action = ValidationAction.RequireApproval },
                    new() { RuleName = "ZScore", Threshold = 3.0m, Action = ValidationAction.Warn },
                    new() { RuleName = "AbsoluteMin", Threshold = 400m, Action = ValidationAction.Reject },
                    new() { RuleName = "AbsoluteMax", Threshold = 1200m, Action = ValidationAction.Reject }
                }
            }
        }
    };

    public PriceValidationService(
        IMarketDataRepository marketDataRepository,
        ICacheService cacheService,
        ILogger<PriceValidationService> logger)
    {
        _marketDataRepository = marketDataRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<PriceValidationResult> ValidatePriceAsync(string productType, decimal price, DateTime priceDate)
    {
        _logger.LogInformation("Validating price {Price} for {ProductType} on {Date}", price, productType, priceDate);

        var config = await GetValidationConfigAsync(productType);
        var result = new PriceValidationResult
        {
            Price = price,
            PriceDate = priceDate,
            ProductType = productType,
            IsValid = true,
            ValidationLevel = PriceValidationLevel.Valid
        };

        try
        {
            // Get historical data for statistical analysis
            var historicalData = await GetHistoricalPricesAsync(productType, 
                priceDate.AddDays(-config.HistoricalDays), priceDate.AddDays(-1));

            if (!historicalData.Any())
            {
                result.ValidationMessages.Add("No historical data available for validation");
                result.ValidationLevel = PriceValidationLevel.Warning;
                return result;
            }

            // Calculate statistics
            result.Statistics = CalculateStatistics(historicalData, price);

            // Apply validation rules
            await ApplyValidationRules(result, config, historicalData);

            // Check for price change validation if we have previous day data
            var previousPrice = historicalData.LastOrDefault();
            if (previousPrice > 0)
            {
                var changeValidation = await ValidatePriceChangeInternalAsync(productType, previousPrice, price, priceDate);
                if (!changeValidation.IsValidChange)
                {
                    result.ValidationLevel = PriceValidationLevel.Error;
                    result.ValidationMessages.Add($"Price change validation failed: {changeValidation.ValidationMessage}");
                    result.RequiresApproval = changeValidation.RequiresApproval;
                    result.ApprovalReason = changeValidation.ValidationMessage;
                }
            }

            _logger.LogInformation("Price validation completed for {ProductType}: {ValidationLevel}", 
                productType, result.ValidationLevel);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating price for {ProductType}", productType);
            result.IsValid = false;
            result.ValidationLevel = PriceValidationLevel.Error;
            result.ValidationMessages.Add($"Validation error: {ex.Message}");
            return result;
        }
    }

    public async Task<List<PriceValidationResult>> ValidatePriceSeriesAsync(string productType, Dictionary<DateTime, decimal> prices)
    {
        var results = new List<PriceValidationResult>();

        foreach (var (date, price) in prices.OrderBy(p => p.Key))
        {
            var validation = await ValidatePriceAsync(productType, price, date);
            results.Add(validation);
        }

        // Additional series-level validations
        await ValidateSeriesConsistency(results);

        return results;
    }

    public async Task<List<PriceAnomalyResult>> DetectPriceAnomaliesAsync(string productType, DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Detecting price anomalies for {ProductType} from {StartDate} to {EndDate}", 
            productType, startDate, endDate);

        var anomalies = new List<PriceAnomalyResult>();
        var prices = await GetHistoricalPricesWithDatesAsync(productType, startDate, endDate);

        if (prices.Count < 10) // Need sufficient data for anomaly detection
        {
            _logger.LogWarning("Insufficient data for anomaly detection: {Count} prices", prices.Count);
            return anomalies;
        }

        var priceValues = prices.Values.ToArray();
        var mean = priceValues.Average();
        var stdDev = CalculateStandardDeviation(priceValues);

        foreach (var (date, price) in prices)
        {
            var zScore = stdDev > 0 ? (price - mean) / stdDev : 0;
            var isOutlier = Math.Abs(zScore) > 3.0m;

            if (isOutlier)
            {
                var anomaly = new PriceAnomalyResult
                {
                    Date = date,
                    Price = price,
                    ProductType = productType,
                    ZScore = zScore,
                    IsOutlier = true,
                    ExpectedPrice = mean,
                    Deviation = Math.Abs(price - mean),
                    Severity = Math.Min(Math.Abs(zScore) / 5.0m, 1.0m)
                };

                // Determine anomaly type
                if (price > mean + (2 * stdDev))
                {
                    anomaly.AnomalyType = AnomalyType.Spike;
                    anomaly.Description = $"Price spike detected: {price:F2} vs expected {mean:F2}";
                }
                else if (price < mean - (2 * stdDev))
                {
                    anomaly.AnomalyType = AnomalyType.Drop;
                    anomaly.Description = $"Price drop detected: {price:F2} vs expected {mean:F2}";
                }
                else
                {
                    anomaly.AnomalyType = AnomalyType.Outlier;
                    anomaly.Description = $"Statistical outlier detected: Z-score {zScore:F2}";
                }

                anomalies.Add(anomaly);
            }
        }

        // Detect volatility anomalies
        await DetectVolatilityAnomalies(anomalies, prices, productType);

        _logger.LogInformation("Detected {Count} anomalies for {ProductType}", anomalies.Count, productType);
        return anomalies.OrderBy(a => a.Date).ToList();
    }

    public async Task<PriceVolatilityMetrics> GetVolatilityMetricsAsync(string productType, DateTime startDate, DateTime endDate)
    {
        var prices = await GetHistoricalPricesAsync(productType, startDate, endDate);
        
        if (!prices.Any())
        {
            throw new NotFoundException($"No price data found for {productType} in the specified period");
        }

        var returns = CalculateReturns(prices);
        var volatility = CalculateStandardDeviation(returns);
        var annualizedVolatility = volatility * (decimal)Math.Sqrt(252); // Annualized

        return new PriceVolatilityMetrics
        {
            ProductType = productType,
            StartDate = startDate,
            EndDate = endDate,
            DailyVolatility = volatility,
            AnnualizedVolatility = annualizedVolatility,
            AveragePrice = prices.Average(),
            StandardDeviation = CalculateStandardDeviation(prices),
            MaxPrice = prices.Max(),
            MinPrice = prices.Min(),
            PriceRange = prices.Max() - prices.Min(),
            Skewness = CalculateSkewness(prices),
            Kurtosis = CalculateKurtosis(prices),
            TotalObservations = prices.Length
        };
    }

    public async Task<PriceChangeValidation> ValidatePriceChangeAsync(string productType, decimal oldPrice, decimal newPrice, DateTime changeDate)
    {
        return await ValidatePriceChangeInternalAsync(productType, oldPrice, newPrice, changeDate);
    }

    public async Task<PriceValidationConfig> GetValidationConfigAsync(string productType)
    {
        var cacheKey = $"price_validation_config:{productType}";
        var cached = await _cacheService.GetAsync<PriceValidationConfig>(cacheKey);
        
        if (cached != null)
        {
            return cached;
        }

        // Get default configuration or custom configuration from storage
        var config = _defaultConfigs.GetValueOrDefault(productType) ?? _defaultConfigs["BRENT"];
        
        // Cache the configuration
        await _cacheService.SetAsync(cacheKey, config, TimeSpan.FromHours(4));
        
        return config;
    }

    public async Task UpdateValidationConfigAsync(string productType, PriceValidationConfig config)
    {
        // In a real implementation, this would persist to database
        // For now, update the cache
        var cacheKey = $"price_validation_config:{productType}";
        await _cacheService.SetAsync(cacheKey, config, TimeSpan.FromHours(4));
        
        _logger.LogInformation("Updated validation configuration for {ProductType}", productType);
    }

    private async Task<decimal[]> GetHistoricalPricesAsync(string productType, DateTime startDate, DateTime endDate)
    {
        // This is a simplified implementation
        // In reality, you would query your market data repository
        var random = new Random(42); // Fixed seed for consistency
        var basePrice = GetBasePrice(productType);
        var prices = new List<decimal>();
        var currentDate = startDate;
        
        while (currentDate <= endDate)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                var variation = (decimal)(random.NextDouble() * 10 - 5); // +/- 5
                prices.Add(Math.Max(0, basePrice + variation));
            }
            currentDate = currentDate.AddDays(1);
        }
        
        return prices.ToArray();
    }

    private async Task<Dictionary<DateTime, decimal>> GetHistoricalPricesWithDatesAsync(string productType, DateTime startDate, DateTime endDate)
    {
        var prices = new Dictionary<DateTime, decimal>();
        var random = new Random(42);
        var basePrice = GetBasePrice(productType);
        var currentDate = startDate;
        
        while (currentDate <= endDate)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                var variation = (decimal)(random.NextDouble() * 10 - 5);
                prices[currentDate] = Math.Max(0, basePrice + variation);
            }
            currentDate = currentDate.AddDays(1);
        }
        
        return prices;
    }

    private PriceStatistics CalculateStatistics(decimal[] historicalPrices, decimal currentPrice)
    {
        var allPrices = historicalPrices.Concat(new[] { currentPrice }).ToArray();
        var mean = historicalPrices.Average();
        var stdDev = CalculateStandardDeviation(historicalPrices);
        var sorted = historicalPrices.OrderBy(p => p).ToArray();
        
        return new PriceStatistics
        {
            Mean = mean,
            Median = sorted.Length % 2 == 0 
                ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2 
                : sorted[sorted.Length / 2],
            StandardDeviation = stdDev,
            ZScore = stdDev > 0 ? (currentPrice - mean) / stdDev : 0,
            Percentile95 = sorted[(int)(sorted.Length * 0.95)],
            Percentile5 = sorted[(int)(sorted.Length * 0.05)]
        };
    }

    private async Task ApplyValidationRules(PriceValidationResult result, PriceValidationConfig config, decimal[] historicalPrices)
    {
        foreach (var rule in config.ValidationRules.Where(r => r.IsEnabled))
        {
            switch (rule.RuleName)
            {
                case "DailyChange":
                    if (historicalPrices.Any())
                    {
                        var lastPrice = historicalPrices.Last();
                        var change = Math.Abs((result.Price - lastPrice) / lastPrice);
                        if (change > rule.Threshold)
                        {
                            ApplyRuleAction(result, rule, $"Daily change {change:P2} exceeds threshold {rule.Threshold:P2}");
                        }
                    }
                    break;

                case "ZScore":
                    if (Math.Abs(result.Statistics.ZScore) > rule.Threshold)
                    {
                        ApplyRuleAction(result, rule, $"Z-score {result.Statistics.ZScore:F2} exceeds threshold {rule.Threshold:F2}");
                    }
                    break;

                case "AbsoluteMin":
                    if (result.Price < rule.Threshold)
                    {
                        ApplyRuleAction(result, rule, $"Price {result.Price:F2} below minimum threshold {rule.Threshold:F2}");
                    }
                    break;

                case "AbsoluteMax":
                    if (result.Price > rule.Threshold)
                    {
                        ApplyRuleAction(result, rule, $"Price {result.Price:F2} above maximum threshold {rule.Threshold:F2}");
                    }
                    break;
            }
        }
    }

    private void ApplyRuleAction(PriceValidationResult result, PriceValidationRule rule, string message)
    {
        result.ValidationMessages.Add($"{rule.RuleName}: {message}");

        switch (rule.Action)
        {
            case ValidationAction.Warn:
                if (result.ValidationLevel < PriceValidationLevel.Warning)
                    result.ValidationLevel = PriceValidationLevel.Warning;
                break;

            case ValidationAction.Reject:
                result.IsValid = false;
                result.ValidationLevel = PriceValidationLevel.Error;
                break;

            case ValidationAction.RequireApproval:
                result.RequiresApproval = true;
                result.ApprovalReason = message;
                if (result.ValidationLevel < PriceValidationLevel.Warning)
                    result.ValidationLevel = PriceValidationLevel.Warning;
                break;
        }
    }

    private async Task<PriceChangeValidation> ValidatePriceChangeInternalAsync(string productType, decimal oldPrice, decimal newPrice, DateTime changeDate)
    {
        var config = await GetValidationConfigAsync(productType);
        var changeAmount = newPrice - oldPrice;
        var changePercentage = oldPrice != 0 ? Math.Abs(changeAmount / oldPrice) : 0;

        var validation = new PriceChangeValidation
        {
            OldPrice = oldPrice,
            NewPrice = newPrice,
            ChangeAmount = changeAmount,
            ChangePercentage = changePercentage,
            ChangeDate = changeDate,
            ProductType = productType,
            IsValidChange = true,
            ValidationLevel = ChangeValidationLevel.Normal
        };

        if (changePercentage > config.MaxDailyChange)
        {
            validation.IsValidChange = false;
            validation.ValidationLevel = ChangeValidationLevel.Extreme;
            validation.ValidationMessage = $"Price change {changePercentage:P2} exceeds maximum allowed {config.MaxDailyChange:P2}";
            validation.ThresholdExceeded = changePercentage - config.MaxDailyChange;
        }
        else if (changePercentage > config.ApprovalThreshold)
        {
            validation.RequiresApproval = true;
            validation.ValidationLevel = ChangeValidationLevel.High;
            validation.ValidationMessage = $"Price change {changePercentage:P2} requires approval (threshold: {config.ApprovalThreshold:P2})";
        }
        else if (changePercentage > config.ApprovalThreshold * 0.7m)
        {
            validation.ValidationLevel = ChangeValidationLevel.Elevated;
            validation.ValidationMessage = $"Elevated price change detected: {changePercentage:P2}";
        }

        return validation;
    }

    private async Task ValidateSeriesConsistency(List<PriceValidationResult> results)
    {
        // Check for consecutive anomalies
        var consecutiveErrors = 0;
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].ValidationLevel >= PriceValidationLevel.Error)
            {
                consecutiveErrors++;
                if (consecutiveErrors >= 3)
                {
                    results[i].ValidationMessages.Add("Multiple consecutive validation errors detected");
                    results[i].ValidationLevel = PriceValidationLevel.Critical;
                }
            }
            else
            {
                consecutiveErrors = 0;
            }
        }
    }

    private async Task DetectVolatilityAnomalies(List<PriceAnomalyResult> anomalies, Dictionary<DateTime, decimal> prices, string productType)
    {
        var returns = CalculateReturns(prices.Values.ToArray());
        var volatility = CalculateStandardDeviation(returns);
        
        // Detect periods of unusually high volatility
        var windowSize = 10;
        var sortedDates = prices.Keys.OrderBy(d => d).ToArray();
        
        for (int i = windowSize; i < sortedDates.Length; i++)
        {
            var windowReturns = new decimal[windowSize];
            for (int j = 0; j < windowSize; j++)
            {
                var currentPrice = prices[sortedDates[i - j]];
                var previousPrice = prices[sortedDates[i - j - 1]];
                windowReturns[j] = previousPrice != 0 ? (currentPrice - previousPrice) / previousPrice : 0;
            }
            
            var windowVolatility = CalculateStandardDeviation(windowReturns);
            if (windowVolatility > volatility * 2) // Volatility spike
            {
                anomalies.Add(new PriceAnomalyResult
                {
                    Date = sortedDates[i],
                    Price = prices[sortedDates[i]],
                    ProductType = productType,
                    AnomalyType = AnomalyType.Volatility,
                    Severity = Math.Min(windowVolatility / volatility / 5, 1.0m),
                    Description = $"High volatility period detected: {windowVolatility:P2} vs normal {volatility:P2}",
                    ExpectedPrice = prices[sortedDates[i]],
                    Deviation = 0,
                    ZScore = 0,
                    IsOutlier = false
                });
            }
        }
    }

    private static decimal CalculateStandardDeviation(decimal[] values)
    {
        if (!values.Any()) return 0;
        var mean = values.Average();
        var squaredDifferences = values.Select(x => (x - mean) * (x - mean));
        var variance = squaredDifferences.Average();
        return (decimal)Math.Sqrt((double)variance);
    }

    private static decimal[] CalculateReturns(decimal[] prices)
    {
        var returns = new List<decimal>();
        for (int i = 1; i < prices.Length; i++)
        {
            var returnValue = prices[i - 1] != 0 ? (prices[i] - prices[i - 1]) / prices[i - 1] : 0;
            returns.Add(returnValue);
        }
        return returns.ToArray();
    }

    private static decimal CalculateSkewness(decimal[] values)
    {
        if (values.Length < 3) return 0;
        
        var mean = values.Average();
        var stdDev = CalculateStandardDeviation(values);
        if (stdDev == 0) return 0;
        
        var skewness = values.Select(x => Math.Pow((double)((x - mean) / stdDev), 3)).Average();
        return (decimal)skewness;
    }

    private static decimal CalculateKurtosis(decimal[] values)
    {
        if (values.Length < 4) return 0;
        
        var mean = values.Average();
        var stdDev = CalculateStandardDeviation(values);
        if (stdDev == 0) return 0;
        
        var kurtosis = values.Select(x => Math.Pow((double)((x - mean) / stdDev), 4)).Average() - 3;
        return (decimal)kurtosis;
    }

    private static decimal GetBasePrice(string productType)
    {
        return productType.ToUpper() switch
        {
            "BRENT" => 80.0m,
            "WTI" => 78.0m,
            "MOPS FO 380" => 420.0m,
            "MOPS MGO" => 650.0m,
            _ => 100.0m
        };
    }
}