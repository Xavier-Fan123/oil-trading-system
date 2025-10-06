using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Services;

public class PriceInterpolationService : IPriceInterpolationService
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly ILogger<PriceInterpolationService> _logger;
    
    // Simple list of common holidays - in production, you might want a more sophisticated holiday calendar
    private static readonly HashSet<DateTime> Holidays = new()
    {
        new DateTime(2024, 1, 1),   // New Year's Day
        new DateTime(2024, 7, 4),   // Independence Day
        new DateTime(2024, 12, 25), // Christmas
        new DateTime(2025, 1, 1),   // New Year's Day
        new DateTime(2025, 7, 4),   // Independence Day
        new DateTime(2025, 12, 25), // Christmas
    };

    public PriceInterpolationService(
        IMarketDataRepository marketDataRepository,
        ILogger<PriceInterpolationService> logger)
    {
        _marketDataRepository = marketDataRepository;
        _logger = logger;
    }

    public async Task<decimal> GetPriceForDateAsync(string productCode, DateTime date)
    {
        _logger.LogDebug("Getting price for {ProductCode} on {Date}", productCode, date.ToString("yyyy-MM-dd"));

        // First try to get exact price for the date
        var exactPrice = await _marketDataRepository.GetByProductAndDateAsync(productCode, date);
        if (exactPrice != null)
        {
            _logger.LogDebug("Found exact price {Price} for {ProductCode} on {Date}", 
                exactPrice.Price, productCode, date.ToString("yyyy-MM-dd"));
            return exactPrice.Price;
        }

        // If it's a weekend or holiday, look for the previous business day
        if (!IsBusinessDay(date))
        {
            var previousBusinessDay = GetPreviousBusinessDay(date);
            _logger.LogDebug("Date {Date} is not a business day, looking for price on {PreviousBusinessDay}", 
                date.ToString("yyyy-MM-dd"), previousBusinessDay.ToString("yyyy-MM-dd"));
            
            var prevPrice = await _marketDataRepository.GetByProductAndDateAsync(productCode, previousBusinessDay);
            if (prevPrice != null)
            {
                return prevPrice.Price;
            }
        }

        // Try to find nearest available price
        var nearestPrice = await GetNearestPriceAsync(productCode, date);
        if (nearestPrice.HasValue)
        {
            _logger.LogDebug("Found nearest price {Price} for {ProductCode} around {Date}", 
                nearestPrice.Value, productCode, date.ToString("yyyy-MM-dd"));
            return nearestPrice.Value;
        }

        // Try linear interpolation between available prices
        var interpolatedPrice = await TryLinearInterpolationAsync(productCode, date);
        if (interpolatedPrice.HasValue)
        {
            _logger.LogDebug("Calculated interpolated price {Price} for {ProductCode} on {Date}", 
                interpolatedPrice.Value, productCode, date.ToString("yyyy-MM-dd"));
            return interpolatedPrice.Value;
        }

        // Last resort: use the latest available price
        var latestPrice = await _marketDataRepository.GetLatestPriceAsync(productCode, date.AddDays(-30));
        if (latestPrice != null)
        {
            _logger.LogWarning("Using latest available price {Price} from {PriceDate} for {ProductCode} on {Date}", 
                latestPrice.Price, latestPrice.PriceDate.ToString("yyyy-MM-dd"), productCode, date.ToString("yyyy-MM-dd"));
            return latestPrice.Price;
        }

        throw new InvalidOperationException($"No price data available for {productCode} around {date:yyyy-MM-dd}");
    }

    public async Task<decimal[]> GetInterpolatedPricesAsync(string productCode, DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Getting interpolated prices for {ProductCode} from {StartDate} to {EndDate}", 
            productCode, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

        var prices = new List<decimal>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            if (IsBusinessDay(currentDate))
            {
                try
                {
                    var price = await GetPriceForDateAsync(productCode, currentDate);
                    prices.Add(price);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get price for {ProductCode} on {Date}", 
                        productCode, currentDate.ToString("yyyy-MM-dd"));
                    
                    // If we can't get any price, this is a serious issue
                    throw new InvalidOperationException(
                        $"Unable to obtain price for {productCode} on {currentDate:yyyy-MM-dd}: {ex.Message}");
                }
            }
            currentDate = currentDate.AddDays(1);
        }

        _logger.LogInformation("Retrieved {Count} interpolated prices for {ProductCode}", prices.Count, productCode);
        return prices.ToArray();
    }

    public async Task<Dictionary<DateTime, decimal>> FillMissingPricesAsync(
        string productCode,
        Dictionary<DateTime, decimal> existingPrices,
        DateTime startDate,
        DateTime endDate)
    {
        _logger.LogDebug("Filling missing prices for {ProductCode} from {StartDate} to {EndDate}", 
            productCode, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

        var filledPrices = new Dictionary<DateTime, decimal>(existingPrices);
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            if (IsBusinessDay(currentDate) && !filledPrices.ContainsKey(currentDate))
            {
                var interpolatedPrice = await GetPriceForDateAsync(productCode, currentDate);
                filledPrices[currentDate] = interpolatedPrice;
            }
            currentDate = currentDate.AddDays(1);
        }

        return filledPrices;
    }

    public async Task<decimal?> GetNearestPriceAsync(string productCode, DateTime targetDate, int maxDaysLookback = 7, int maxDaysLookforward = 3)
    {
        _logger.LogDebug("Finding nearest price for {ProductCode} around {TargetDate} (lookback: {Lookback}, lookforward: {Lookforward})", 
            productCode, targetDate.ToString("yyyy-MM-dd"), maxDaysLookback, maxDaysLookforward);

        // First try looking backward
        for (int i = 1; i <= maxDaysLookback; i++)
        {
            var checkDate = targetDate.AddDays(-i);
            var price = await _marketDataRepository.GetByProductAndDateAsync(productCode, checkDate);
            if (price != null)
            {
                _logger.LogDebug("Found price {Price} on {Date} (lookback {Days} days)", 
                    price.Price, checkDate.ToString("yyyy-MM-dd"), i);
                return price.Price;
            }
        }

        // Then try looking forward
        for (int i = 1; i <= maxDaysLookforward; i++)
        {
            var checkDate = targetDate.AddDays(i);
            var price = await _marketDataRepository.GetByProductAndDateAsync(productCode, checkDate);
            if (price != null)
            {
                _logger.LogDebug("Found price {Price} on {Date} (lookforward {Days} days)", 
                    price.Price, checkDate.ToString("yyyy-MM-dd"), i);
                return price.Price;
            }
        }

        _logger.LogWarning("No nearest price found for {ProductCode} around {TargetDate}", 
            productCode, targetDate.ToString("yyyy-MM-dd"));
        return null;
    }

    public bool IsBusinessDay(DateTime date)
    {
        // Check if it's a weekend
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        // Check if it's a holiday
        if (Holidays.Contains(date.Date))
        {
            return false;
        }

        return true;
    }

    public DateTime GetNextBusinessDay(DateTime date)
    {
        var nextDay = date.AddDays(1);
        while (!IsBusinessDay(nextDay))
        {
            nextDay = nextDay.AddDays(1);
        }
        return nextDay;
    }

    public DateTime GetPreviousBusinessDay(DateTime date)
    {
        var prevDay = date.AddDays(-1);
        while (!IsBusinessDay(prevDay))
        {
            prevDay = prevDay.AddDays(-1);
        }
        return prevDay;
    }

    private async Task<decimal?> TryLinearInterpolationAsync(string productCode, DateTime targetDate)
    {
        // Find the closest prices before and after the target date
        var pricesBefore = await _marketDataRepository.GetByProductAsync(
            productCode, targetDate.AddDays(-30), targetDate.AddDays(-1));
        var pricesAfter = await _marketDataRepository.GetByProductAsync(
            productCode, targetDate.AddDays(1), targetDate.AddDays(30));

        var closestBefore = pricesBefore.OrderByDescending(p => p.PriceDate).FirstOrDefault();
        var closestAfter = pricesAfter.OrderBy(p => p.PriceDate).FirstOrDefault();

        if (closestBefore != null && closestAfter != null)
        {
            // Perform linear interpolation
            var daysBetween = (closestAfter.PriceDate - closestBefore.PriceDate).TotalDays;
            var daysFromStart = (targetDate - closestBefore.PriceDate).TotalDays;
            
            var priceDifference = closestAfter.Price - closestBefore.Price;
            var interpolatedPrice = closestBefore.Price + (priceDifference * (decimal)(daysFromStart / daysBetween));

            _logger.LogDebug("Linear interpolation: {BeforePrice} on {BeforeDate} -> {AfterPrice} on {AfterDate} = {InterpolatedPrice} on {TargetDate}", 
                closestBefore.Price, closestBefore.PriceDate.ToString("yyyy-MM-dd"),
                closestAfter.Price, closestAfter.PriceDate.ToString("yyyy-MM-dd"),
                interpolatedPrice, targetDate.ToString("yyyy-MM-dd"));

            return interpolatedPrice;
        }

        return null;
    }
}