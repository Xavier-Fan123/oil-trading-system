using Microsoft.Extensions.Logging;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

public class BasisCalculationService : IBasisCalculationService
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IPriceBenchmarkRepository _priceBenchmarkRepository;
    private readonly ILogger<BasisCalculationService> _logger;

    // Basis configuration for different products
    private readonly Dictionary<string, BasisConfig> _basisConfigs = new()
    {
        {
            "BRENT", new BasisConfig
            {
                ExpectedBasisRange = (-2.0m, 2.0m),
                WarningThreshold = 1.5m,
                CriticalThreshold = 3.0m,
                HistoricalDays = 252
            }
        },
        {
            "WTI", new BasisConfig
            {
                ExpectedBasisRange = (-1.5m, 1.5m),
                WarningThreshold = 1.0m,
                CriticalThreshold = 2.5m,
                HistoricalDays = 252
            }
        },
        {
            "MOPS FO 380", new BasisConfig
            {
                ExpectedBasisRange = (-10.0m, 10.0m),
                WarningThreshold = 8.0m,
                CriticalThreshold = 15.0m,
                HistoricalDays = 126
            }
        },
        {
            "MOPS MGO", new BasisConfig
            {
                ExpectedBasisRange = (-15.0m, 15.0m),
                WarningThreshold = 12.0m,
                CriticalThreshold = 20.0m,
                HistoricalDays = 126
            }
        }
    };

    public BasisCalculationService(
        IMarketDataRepository marketDataRepository,
        IPriceBenchmarkRepository priceBenchmarkRepository,
        ILogger<BasisCalculationService> logger)
    {
        _marketDataRepository = marketDataRepository;
        _priceBenchmarkRepository = priceBenchmarkRepository;
        _logger = logger;
    }

    public async Task<decimal> CalculateBasisAsync(string productType, DateTime valuationDate, string futuresContract)
    {
        _logger.LogInformation("Calculating basis for {ProductType} vs {FuturesContract} on {Date}",
            productType, futuresContract, valuationDate);

        try
        {
            // Get spot price for the product
            var spotPrice = await GetSpotPriceAsync(productType, valuationDate);
            if (spotPrice == null)
            {
                throw new NotFoundException($"Spot price not found for {productType} on {valuationDate:yyyy-MM-dd}");
            }

            // Get futures price
            var futuresPrice = await GetFuturesPriceAsync(futuresContract, valuationDate);
            if (futuresPrice == null)
            {
                throw new NotFoundException($"Futures price not found for {futuresContract} on {valuationDate:yyyy-MM-dd}");
            }

            // Calculate basis: Spot - Futures
            var basis = spotPrice.Value - futuresPrice.Value;

            _logger.LogInformation("Calculated basis: {Basis} = {SpotPrice} - {FuturesPrice}",
                basis, spotPrice.Value, futuresPrice.Value);

            return basis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating basis for {ProductType} vs {FuturesContract}",
                productType, futuresContract);
            throw;
        }
    }

    public async Task<Dictionary<string, decimal>> CalculateMultipleBasisAsync(string productType, DateTime valuationDate, string[] futuresContracts)
    {
        var results = new Dictionary<string, decimal>();

        // Get spot price once for all calculations
        var spotPrice = await GetSpotPriceAsync(productType, valuationDate);
        if (spotPrice == null)
        {
            throw new NotFoundException($"Spot price not found for {productType} on {valuationDate:yyyy-MM-dd}");
        }

        foreach (var contract in futuresContracts)
        {
            try
            {
                var futuresPrice = await GetFuturesPriceAsync(contract, valuationDate);
                if (futuresPrice != null)
                {
                    results[contract] = spotPrice.Value - futuresPrice.Value;
                }
                else
                {
                    _logger.LogWarning("Futures price not found for {Contract} on {Date}", contract, valuationDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating basis for {Contract}", contract);
            }
        }

        return results;
    }

    public async Task<BasisHistoryDto[]> GetBasisHistoryAsync(string productType, string futuresContract, DateTime startDate, DateTime endDate)
    {
        var history = new List<BasisHistoryDto>();

        // Get spot prices for the period
        var spotPrices = await GetSpotPriceHistoryAsync(productType, startDate, endDate);
        var futuresPrices = await GetFuturesPriceHistoryAsync(futuresContract, startDate, endDate);

        // Join spot and futures prices by date
        var priceJoin = from spot in spotPrices
                       join futures in futuresPrices on spot.Date equals futures.Date
                       select new { spot.Date, SpotPrice = spot.Price, FuturesPrice = futures.Price };

        foreach (var item in priceJoin)
        {
            var basis = item.SpotPrice - item.FuturesPrice;
            var basisPercentage = item.FuturesPrice != 0 ? (basis / item.FuturesPrice) * 100 : 0;

            history.Add(new BasisHistoryDto
            {
                Date = item.Date,
                ProductType = productType,
                FuturesContract = futuresContract,
                SpotPrice = item.SpotPrice,
                FuturesPrice = item.FuturesPrice,
                Basis = basis,
                BasisPercentage = basisPercentage
            });
        }

        return history.OrderBy(h => h.Date).ToArray();
    }

    public async Task<decimal> CalculateBasisAdjustedPriceAsync(decimal futuresPrice, string productType, DateTime valuationDate, string futuresContract)
    {
        var basis = await CalculateBasisAsync(productType, valuationDate, futuresContract);
        return futuresPrice + basis;
    }

    public async Task<BasisValidationResult> ValidateBasisAsync(string productType, decimal calculatedBasis, DateTime valuationDate)
    {
        var config = _basisConfigs.GetValueOrDefault(productType);
        if (config == null)
        {
            _logger.LogWarning("No basis configuration found for product type: {ProductType}", productType);
            return new BasisValidationResult
            {
                IsValid = true, // Default to valid if no config
                CalculatedBasis = calculatedBasis,
                ValidationMessage = $"No validation rules configured for {productType}",
                RiskLevel = BasisRiskLevel.Low
            };
        }

        // Get historical basis data for validation
        var historicalData = await GetHistoricalBasisDataAsync(productType, valuationDate, config.HistoricalDays);
        var historicalAverage = historicalData.Any() ? historicalData.Average() : 0;
        var standardDeviation = CalculateStandardDeviation(historicalData);

        var result = new BasisValidationResult
        {
            CalculatedBasis = calculatedBasis,
            ExpectedBasisMin = config.ExpectedBasisRange.Min,
            ExpectedBasisMax = config.ExpectedBasisRange.Max,
            HistoricalAverage = historicalAverage,
            StandardDeviation = standardDeviation
        };

        // Validate basis against expected range
        var absDeviation = Math.Abs(calculatedBasis - historicalAverage);

        if (calculatedBasis >= config.ExpectedBasisRange.Min && calculatedBasis <= config.ExpectedBasisRange.Max)
        {
            result.IsValid = true;
            result.RiskLevel = BasisRiskLevel.Low;
            result.ValidationMessage = "Basis within expected range";
        }
        else if (absDeviation <= config.WarningThreshold)
        {
            result.IsValid = true;
            result.RiskLevel = BasisRiskLevel.Medium;
            result.ValidationMessage = $"Basis outside normal range but within warning threshold ({config.WarningThreshold})";
        }
        else if (absDeviation <= config.CriticalThreshold)
        {
            result.IsValid = false;
            result.RiskLevel = BasisRiskLevel.High;
            result.ValidationMessage = $"Basis significantly outside expected range. Deviation: {absDeviation:F2}";
        }
        else
        {
            result.IsValid = false;
            result.RiskLevel = BasisRiskLevel.Critical;
            result.ValidationMessage = $"Critical basis deviation detected. Immediate review required. Deviation: {absDeviation:F2}";
        }

        return result;
    }

    private async Task<decimal?> GetSpotPriceAsync(string productType, DateTime date)
    {
        // Try to get the exact date first
        var price = await _marketDataRepository.GetLatestPriceAsync(productType, date);
        if (price != null) return price.Price;

        // If no exact match, get the latest price before the date (within 3 days)
        var startDate = date.AddDays(-3);
        var historicalPrices = await GetSpotPriceHistoryAsync(productType, startDate, date);
        var lastPrice = historicalPrices.LastOrDefault();
        return lastPrice != default ? lastPrice.Price : (decimal?)null;
    }

    private async Task<decimal?> GetFuturesPriceAsync(string futuresContract, DateTime date)
    {
        // In a real implementation, this would query futures price data
        // For now, we'll simulate futures prices based on spot prices with some variation
        var benchmarkName = ExtractBenchmarkFromFuturesContract(futuresContract);
        var spotPrice = await GetSpotPriceAsync(benchmarkName, date);
        
        if (spotPrice == null) return null;

        // Simulate futures premium/discount based on contract month
        var contractMonth = ExtractContractMonth(futuresContract);
        var monthsOut = (contractMonth.Year - date.Year) * 12 + (contractMonth.Month - date.Month);
        
        // Simple contango/backwardation simulation
        var timeDecay = monthsOut * 0.1m; // 0.1 per month
        return spotPrice + timeDecay;
    }

    private async Task<List<(DateTime Date, decimal Price)>> GetSpotPriceHistoryAsync(string productType, DateTime startDate, DateTime endDate)
    {
        var prices = new List<(DateTime Date, decimal Price)>();
        
        // This is a simplified implementation
        // In reality, you would query your market data repository
        var random = new Random(42); // Fixed seed for consistency
        var basePrice = GetBasePrice(productType);
        var currentDate = startDate;
        
        while (currentDate <= endDate)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                var variation = (decimal)(random.NextDouble() * 10 - 5); // +/- 5
                prices.Add((currentDate, Math.Max(0, basePrice + variation)));
            }
            currentDate = currentDate.AddDays(1);
        }
        
        return prices;
    }

    private async Task<List<(DateTime Date, decimal Price)>> GetFuturesPriceHistoryAsync(string futuresContract, DateTime startDate, DateTime endDate)
    {
        var prices = new List<(DateTime Date, decimal Price)>();
        var benchmarkName = ExtractBenchmarkFromFuturesContract(futuresContract);
        var spotHistory = await GetSpotPriceHistoryAsync(benchmarkName, startDate, endDate);
        
        var contractMonth = ExtractContractMonth(futuresContract);
        
        foreach (var (date, spotPrice) in spotHistory)
        {
            var monthsOut = (contractMonth.Year - date.Year) * 12 + (contractMonth.Month - date.Month);
            var timeDecay = monthsOut * 0.1m;
            prices.Add((date, spotPrice + timeDecay));
        }
        
        return prices;
    }

    private async Task<List<decimal>> GetHistoricalBasisDataAsync(string productType, DateTime referenceDate, int days)
    {
        var startDate = referenceDate.AddDays(-days);
        var basisData = new List<decimal>();
        
        // Simulate historical basis data
        var random = new Random(42);
        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                // Simulate basis with some mean reversion
                var basis = (decimal)(random.NextDouble() * 4 - 2); // +/- 2
                basisData.Add(basis);
            }
        }
        
        return basisData;
    }

    private static decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (!values.Any()) return 0;
        
        var mean = values.Average();
        var squaredDifferences = values.Select(x => (x - mean) * (x - mean));
        var variance = squaredDifferences.Average();
        return (decimal)Math.Sqrt((double)variance);
    }

    private static string ExtractBenchmarkFromFuturesContract(string futuresContract)
    {
        // Extract benchmark name from futures contract
        // e.g., "BRENT-2024-03" -> "BRENT"
        var parts = futuresContract.Split('-');
        return parts.Length > 0 ? parts[0] : futuresContract;
    }

    private static DateTime ExtractContractMonth(string futuresContract)
    {
        // Extract contract month from futures contract
        // e.g., "BRENT-2024-03" -> March 2024
        var parts = futuresContract.Split('-');
        if (parts.Length >= 3 && 
            int.TryParse(parts[1], out var year) && 
            int.TryParse(parts[2], out var month))
        {
            return new DateTime(year, month, 1);
        }
        
        // Default to next month if parsing fails
        return DateTime.Now.AddMonths(1);
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

    private class BasisConfig
    {
        public (decimal Min, decimal Max) ExpectedBasisRange { get; set; }
        public decimal WarningThreshold { get; set; }
        public decimal CriticalThreshold { get; set; }
        public int HistoricalDays { get; set; }
    }
}