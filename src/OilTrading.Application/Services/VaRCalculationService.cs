using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Services;

/// <summary>
/// Service for calculating Value at Risk (VaR) using EWMA methodology.
/// Ported from Python var_calculator.py to C#.
/// </summary>
public interface IVaRCalculationService
{
    /// <summary>
    /// Calculate VaR metrics for a product.
    /// </summary>
    Task<VaRMetricsDto> CalculateVaRAsync(
        string productCode,
        int lookbackDays = 252,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate VaR for a portfolio of positions.
    /// </summary>
    Task<PortfolioVaRResultDto> CalculatePortfolioVaRAsync(
        IEnumerable<PositionDto> positions,
        int lookbackDays = 252,
        CancellationToken cancellationToken = default);
}

public class VaRCalculationService : IVaRCalculationService
{
    private readonly IMarketDataRepository _marketDataRepository;

    // EWMA decay factor (industry standard)
    private const double EWMA_LAMBDA = 0.94;

    // Confidence levels and their Z-scores
    private static readonly Dictionary<double, double> ConfidenceZScores = new()
    {
        { 0.95, 1.6449 },
        { 0.99, 2.3263 }
    };

    // Trading days per year for annualization
    private const int TRADING_DAYS_PER_YEAR = 252;

    public VaRCalculationService(IMarketDataRepository marketDataRepository)
    {
        _marketDataRepository = marketDataRepository;
    }

    public async Task<VaRMetricsDto> CalculateVaRAsync(
        string productCode,
        int lookbackDays = 252,
        CancellationToken cancellationToken = default)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-lookbackDays - 10); // Extra buffer for return calculation

        var prices = await _marketDataRepository.GetByProductAsync(
            productCode, startDate, endDate, cancellationToken);

        var priceList = prices
            .OrderBy(p => p.PriceDate)
            .ToList();

        if (priceList.Count < 30)
        {
            throw new InvalidOperationException($"Insufficient price data for VaR calculation. Found {priceList.Count} records, need at least 30.");
        }

        // Calculate log returns
        var returns = CalculateLogReturns(priceList);

        if (returns.Count < 20)
        {
            throw new InvalidOperationException($"Insufficient return data for VaR calculation. Found {returns.Count} returns, need at least 20.");
        }

        // Calculate EWMA volatility
        var ewmaVariance = CalculateEWMAVariance(returns);
        var dailyVolatility = Math.Sqrt(ewmaVariance);
        var annualizedVolatility = dailyVolatility * Math.Sqrt(TRADING_DAYS_PER_YEAR);

        // Get latest price for VaR dollar calculation
        var latestPrice = priceList.Last().Price;

        // Calculate VaR at different confidence levels
        var var1Day95 = latestPrice * (decimal)dailyVolatility * (decimal)ConfidenceZScores[0.95];
        var var1Day99 = latestPrice * (decimal)dailyVolatility * (decimal)ConfidenceZScores[0.99];

        // 10-day VaR (square root of time scaling)
        var var10Day95 = var1Day95 * (decimal)Math.Sqrt(10);
        var var10Day99 = var1Day99 * (decimal)Math.Sqrt(10);

        return new VaRMetricsDto
        {
            ProductCode = productCode,
            LatestPrice = latestPrice,
            Currency = priceList.Last().Currency,
            DailyVolatility = (decimal)dailyVolatility,
            AnnualizedVolatility = (decimal)annualizedVolatility,
            Var1Day95 = var1Day95,
            Var1Day99 = var1Day99,
            Var10Day95 = var10Day95,
            Var10Day99 = var10Day99,
            DataPoints = priceList.Count,
            ReturnDataPoints = returns.Count,
            CalculationDate = DateTime.UtcNow,
            StartDate = priceList.First().PriceDate,
            EndDate = priceList.Last().PriceDate
        };
    }

    public async Task<PortfolioVaRResultDto> CalculatePortfolioVaRAsync(
        IEnumerable<PositionDto> positions,
        int lookbackDays = 252,
        CancellationToken cancellationToken = default)
    {
        var positionList = positions.ToList();
        if (!positionList.Any())
        {
            return new PortfolioVaRResultDto
            {
                CalculationDate = DateTime.UtcNow,
                TotalPositionValue = 0,
                PortfolioVar1Day95 = 0,
                PortfolioVar1Day99 = 0,
                PositionVaRs = new List<PositionVaRDto>()
            };
        }

        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-lookbackDays - 10);

        // Collect returns for all products
        var productReturns = new Dictionary<string, List<(DateTime Date, double Return)>>();
        var productPrices = new Dictionary<string, decimal>();

        foreach (var position in positionList.DistinctBy(p => p.ProductCode))
        {
            var prices = await _marketDataRepository.GetByProductAsync(
                position.ProductCode, startDate, endDate, cancellationToken);

            var priceList = prices.OrderBy(p => p.PriceDate).ToList();

            if (priceList.Count >= 30)
            {
                var returns = CalculateLogReturnsWithDates(priceList);
                productReturns[position.ProductCode] = returns;
                productPrices[position.ProductCode] = priceList.Last().Price;
            }
        }

        // Calculate individual VaRs
        var positionVaRs = new List<PositionVaRDto>();
        var totalPositionValue = 0m;

        foreach (var position in positionList)
        {
            if (!productReturns.ContainsKey(position.ProductCode))
                continue;

            var returns = productReturns[position.ProductCode].Select(r => r.Return).ToList();
            var ewmaVariance = CalculateEWMAVariance(returns);
            var dailyVol = Math.Sqrt(ewmaVariance);

            var positionValue = Math.Abs(position.Quantity) * productPrices[position.ProductCode];
            var var95 = positionValue * (decimal)dailyVol * (decimal)ConfidenceZScores[0.95];

            positionVaRs.Add(new PositionVaRDto
            {
                ProductCode = position.ProductCode,
                Quantity = position.Quantity,
                PositionValue = positionValue,
                DailyVolatility = (decimal)dailyVol,
                Var1Day95 = var95
            });

            totalPositionValue += positionValue;
        }

        // Calculate portfolio VaR with correlation
        // For simplicity, using sum of individual VaRs (conservative approach)
        // Full correlation matrix would require more computation
        var portfolioVar95 = positionVaRs.Sum(p => p.Var1Day95);
        var portfolioVar99 = portfolioVar95 * (decimal)(ConfidenceZScores[0.99] / ConfidenceZScores[0.95]);

        return new PortfolioVaRResultDto
        {
            CalculationDate = DateTime.UtcNow,
            TotalPositionValue = totalPositionValue,
            PortfolioVar1Day95 = portfolioVar95,
            PortfolioVar1Day99 = portfolioVar99,
            PortfolioVar10Day95 = portfolioVar95 * (decimal)Math.Sqrt(10),
            PortfolioVar10Day99 = portfolioVar99 * (decimal)Math.Sqrt(10),
            PositionVaRs = positionVaRs
        };
    }

    /// <summary>
    /// Calculate log returns from price list.
    /// </summary>
    private static List<double> CalculateLogReturns(List<MarketPrice> prices)
    {
        var returns = new List<double>();

        for (int i = 1; i < prices.Count; i++)
        {
            var prevPrice = (double)prices[i - 1].Price;
            var currPrice = (double)prices[i].Price;

            if (prevPrice > 0 && currPrice > 0)
            {
                var logReturn = Math.Log(currPrice / prevPrice);
                returns.Add(logReturn);
            }
        }

        return returns;
    }

    /// <summary>
    /// Calculate log returns with dates.
    /// </summary>
    private static List<(DateTime Date, double Return)> CalculateLogReturnsWithDates(List<MarketPrice> prices)
    {
        var returns = new List<(DateTime, double)>();

        for (int i = 1; i < prices.Count; i++)
        {
            var prevPrice = (double)prices[i - 1].Price;
            var currPrice = (double)prices[i].Price;

            if (prevPrice > 0 && currPrice > 0)
            {
                var logReturn = Math.Log(currPrice / prevPrice);
                returns.Add((prices[i].PriceDate, logReturn));
            }
        }

        return returns;
    }

    /// <summary>
    /// Calculate EWMA variance using decay factor lambda.
    /// σ²(t) = λ * σ²(t-1) + (1-λ) * r²(t)
    /// </summary>
    private static double CalculateEWMAVariance(List<double> returns)
    {
        if (!returns.Any())
            return 0;

        // Initialize with simple variance of first few returns
        var initialReturns = returns.Take(Math.Min(10, returns.Count)).ToList();
        var variance = initialReturns.Sum(r => r * r) / initialReturns.Count;

        // Apply EWMA
        foreach (var ret in returns)
        {
            variance = EWMA_LAMBDA * variance + (1 - EWMA_LAMBDA) * ret * ret;
        }

        return variance;
    }
}

#region DTOs

public class VaRMetricsDto
{
    public string ProductCode { get; set; } = string.Empty;
    public decimal LatestPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal DailyVolatility { get; set; }
    public decimal AnnualizedVolatility { get; set; }
    public decimal Var1Day95 { get; set; }
    public decimal Var1Day99 { get; set; }
    public decimal Var10Day95 { get; set; }
    public decimal Var10Day99 { get; set; }
    public int DataPoints { get; set; }
    public int ReturnDataPoints { get; set; }
    public DateTime CalculationDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class PortfolioVaRResultDto
{
    public DateTime CalculationDate { get; set; }
    public decimal TotalPositionValue { get; set; }
    public decimal PortfolioVar1Day95 { get; set; }
    public decimal PortfolioVar1Day99 { get; set; }
    public decimal PortfolioVar10Day95 { get; set; }
    public decimal PortfolioVar10Day99 { get; set; }
    public List<PositionVaRDto> PositionVaRs { get; set; } = new();
}

public class PositionVaRDto
{
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal PositionValue { get; set; }
    public decimal DailyVolatility { get; set; }
    public decimal Var1Day95 { get; set; }
}

public class PositionDto
{
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

#endregion
