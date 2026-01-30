using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Queries.MarketData;

/// <summary>
/// Handler for GetPriceStatisticsQuery
/// Calculates price statistics using the PricingService
/// </summary>
public class GetPriceStatisticsQueryHandler : IRequestHandler<GetPriceStatisticsQuery, PriceStatistics?>
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly ILogger<GetPriceStatisticsQueryHandler> _logger;

    public GetPriceStatisticsQueryHandler(
        IMarketDataRepository marketDataRepository,
        ILogger<GetPriceStatisticsQueryHandler> logger)
    {
        _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PriceStatistics?> Handle(GetPriceStatisticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Calculating price statistics: ProductCode={ProductCode}, ContractMonth={ContractMonth}, PriceType={PriceType}",
            request.ProductCode, request.ContractMonth, request.PriceType);

        try
        {
            // Get all prices for the contract month
            var prices = await _marketDataRepository.GetAllByProductCodeAndContractMonthAsync(
                request.ProductCode,
                request.ContractMonth,
                request.PriceType,
                cancellationToken);

            var priceList = prices.ToList();

            if (priceList.Count == 0)
            {
                _logger.LogWarning(
                    "No prices found for statistics calculation: ProductCode={ProductCode}, ContractMonth={ContractMonth}",
                    request.ProductCode, request.ContractMonth);

                return new PriceStatistics
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

            var statistics = new PriceStatistics
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
                "Error calculating price statistics for ProductCode={ProductCode}, ContractMonth={ContractMonth}",
                request.ProductCode, request.ContractMonth);
            throw;
        }
    }
}
