using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.Services;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

/// <summary>
/// Implementation of IPricingService for unified market price query management.
///
/// Responsibilities:
/// 1. Query market prices from repository layer (with (ProductCode, ContractMonth, PriceDate, PriceType) index)
/// 2. Calculate price statistics (min, max, avg, std dev) on-demand
/// 3. Support different query patterns:
///    - Settlement: Single price on specific date
///    - Risk: Historical range for volatility
///    - Position: Latest price for valuation
///    - UI: Available months for dropdowns
/// 4. Provide audit trail via logging for compliance
/// 5. Cache statistics where appropriate for performance
///
/// Design Notes:
/// - All methods are async-first for scalability
/// - Null handling: Returns null for not-found (vs. throwing), allowing caller to decide error handling
/// - No caching in this service - let consumer cache if needed (separation of concerns)
/// - ProductId is used (not ProductCode string) for type safety and permission checking
/// </summary>
public class PricingService : IPricingService
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly ILogger<PricingService> _logger;

    public PricingService(
        IMarketDataRepository marketDataRepository,
        ILogger<PricingService> logger)
    {
        _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<MarketPrice?> GetSettlementPriceAsync(
        Guid productId,
        string contractMonth,
        DateTime priceDate,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (string.IsNullOrWhiteSpace(contractMonth))
            throw new ArgumentException("Contract month cannot be empty", nameof(contractMonth));

        _logger.LogInformation(
            "Querying settlement price: ProductId={ProductId}, ContractMonth={ContractMonth}, Date={Date}, Type={Type}",
            productId, contractMonth, priceDate.Date, priceType);

        try
        {
            // Query by ProductCode and contract month
            // NOTE: ProductId is passed but not used - MarketPrice uses ProductCode (string) as natural key
            // The index on (ProductCode, ContractMonth, PriceDate, PriceType) ensures fast lookup
            var price = await _marketDataRepository.GetByProductCodeAndContractMonthAsync(
                productId.ToString(), // Convert GUID to string for ProductCode lookup
                contractMonth,
                priceDate.Date,
                priceType,
                cancellationToken);

            if (price != null)
            {
                _logger.LogInformation(
                    "Settlement price found: {PriceValue} {Currency}",
                    price.Price, price.Currency);
            }
            else
            {
                _logger.LogWarning(
                    "Settlement price not found for ProductId={ProductId}, ContractMonth={ContractMonth}, Date={Date}",
                    productId, contractMonth, priceDate.Date);
            }

            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving settlement price for ProductId={ProductId}, ContractMonth={ContractMonth}, Date={Date}",
                productId, contractMonth, priceDate.Date);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<MarketPrice>> GetHistoricalPricesAsync(
        Guid productId,
        string contractMonth,
        DateTime startDate,
        DateTime endDate,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (string.IsNullOrWhiteSpace(contractMonth))
            throw new ArgumentException("Contract month cannot be empty", nameof(contractMonth));

        if (startDate > endDate)
            throw new ArgumentException("Start date must be before or equal to end date");

        _logger.LogInformation(
            "Querying historical prices: ProductId={ProductId}, ContractMonth={ContractMonth}, Range={StartDate}-{EndDate}",
            productId, contractMonth, startDate.Date, endDate.Date);

        try
        {
            var prices = await _marketDataRepository.GetByProductCodeAndContractMonthRangeAsync(
                productId.ToString(), // Convert GUID to string for ProductCode lookup
                contractMonth,
                startDate.Date,
                endDate.Date,
                priceType,
                cancellationToken);

            var priceList = prices.OrderBy(p => p.PriceDate).ToList();

            _logger.LogInformation(
                "Retrieved {Count} historical price points for ProductId={ProductId}, ContractMonth={ContractMonth}",
                priceList.Count, productId, contractMonth);

            return priceList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving historical prices for ProductId={ProductId}, ContractMonth={ContractMonth}",
                productId, contractMonth);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<MarketPrice?> GetLatestPriceAsync(
        Guid productId,
        string contractMonth,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (string.IsNullOrWhiteSpace(contractMonth))
            throw new ArgumentException("Contract month cannot be empty", nameof(contractMonth));

        _logger.LogInformation(
            "Querying latest price: ProductId={ProductId}, ContractMonth={ContractMonth}, Type={Type}",
            productId, contractMonth, priceType);

        try
        {
            var price = await _marketDataRepository.GetLatestByProductCodeAndContractMonthAsync(
                productId.ToString(), // Convert GUID to string for ProductCode lookup
                contractMonth,
                priceType,
                cancellationToken);

            if (price != null)
            {
                _logger.LogInformation(
                    "Latest price found: Date={Date}, Price={Price}",
                    price.PriceDate, price.Price);
            }
            else
            {
                _logger.LogWarning(
                    "No price found for ProductId={ProductId}, ContractMonth={ContractMonth}",
                    productId, contractMonth);
            }

            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving latest price for ProductId={ProductId}, ContractMonth={ContractMonth}",
                productId, contractMonth);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetAvailableContractMonthsAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        _logger.LogInformation("Querying available contract months for ProductId={ProductId}", productId);

        try
        {
            var months = await _marketDataRepository.GetDistinctContractMonthsByProductCodeAsync(
                productId.ToString(), // Convert GUID to string for ProductCode lookup
                cancellationToken);

            var monthList = months.OrderBy(m => m).ToList();

            _logger.LogInformation(
                "Found {Count} contract months for ProductId={ProductId}",
                monthList.Count, productId);

            return monthList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving contract months for ProductId={ProductId}", productId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<OilTrading.Core.ValueObjects.PriceStatistics> GetPriceStatisticsAsync(
        Guid productId,
        string contractMonth,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (string.IsNullOrWhiteSpace(contractMonth))
            throw new ArgumentException("Contract month cannot be empty", nameof(contractMonth));

        _logger.LogInformation(
            "Calculating price statistics: ProductId={ProductId}, ContractMonth={ContractMonth}",
            productId, contractMonth);

        try
        {
            // Get all prices for the contract month
            var prices = await _marketDataRepository.GetAllByProductCodeAndContractMonthAsync(
                productId.ToString(), // Convert GUID to string for ProductCode lookup
                contractMonth,
                priceType,
                cancellationToken);

            var priceList = prices.ToList();

            if (priceList.Count == 0)
            {
                _logger.LogWarning(
                    "No prices found for statistics calculation: ProductId={ProductId}, ContractMonth={ContractMonth}",
                    productId, contractMonth);

                return new OilTrading.Core.ValueObjects.PriceStatistics
                {
                    DataPointCount = 0,
                    MinPrice = 0,
                    MaxPrice = 0,
                    AveragePrice = 0,
                    StandardDeviation = 0,
                    StartDate = null,
                    EndDate = null
                };
            }

            // Calculate statistics
            var priceValues = priceList.Select(p => p.Price).ToList();
            var minPrice = priceValues.Min();
            var maxPrice = priceValues.Max();
            var avgPrice = priceValues.Average();
            var variance = priceValues.Count > 1
                ? priceValues.Sum(p => Math.Pow((double)(p - avgPrice), 2)) / (priceValues.Count - 1)
                : 0;
            var stdDev = (decimal)Math.Sqrt(variance);

            var statistics = new OilTrading.Core.ValueObjects.PriceStatistics
            {
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                AveragePrice = avgPrice,
                StandardDeviation = stdDev,
                DataPointCount = priceList.Count,
                StartDate = priceList.Min(p => p.PriceDate),
                EndDate = priceList.Max(p => p.PriceDate)
            };

            _logger.LogInformation(
                "Price statistics calculated: Min={Min}, Max={Max}, Avg={Avg}, StdDev={StdDev}, Points={Points}",
                statistics.MinPrice, statistics.MaxPrice, statistics.AveragePrice,
                statistics.StandardDeviation, statistics.DataPointCount);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error calculating price statistics for ProductId={ProductId}, ContractMonth={ContractMonth}",
                productId, contractMonth);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> PriceExistsAsync(
        Guid productId,
        string contractMonth,
        DateTime priceDate,
        MarketPriceType priceType,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (string.IsNullOrWhiteSpace(contractMonth))
            throw new ArgumentException("Contract month cannot be empty", nameof(contractMonth));

        try
        {
            var price = await _marketDataRepository.GetByProductCodeAndContractMonthAsync(
                productId.ToString(), // Convert GUID to string for ProductCode lookup
                contractMonth,
                priceDate.Date,
                priceType,
                cancellationToken);

            return price != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking price existence for ProductId={ProductId}, ContractMonth={ContractMonth}, Date={Date}",
                productId, contractMonth, priceDate.Date);
            throw;
        }
    }
}
