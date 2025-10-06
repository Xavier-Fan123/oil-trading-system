namespace OilTrading.Application.Services;

/// <summary>
/// Service for handling missing price data through interpolation and other methods
/// </summary>
public interface IPriceInterpolationService
{
    /// <summary>
    /// Get price for a specific date, using interpolation if necessary
    /// </summary>
    Task<decimal> GetPriceForDateAsync(string productCode, DateTime date);

    /// <summary>
    /// Get interpolated prices for a date range
    /// </summary>
    Task<decimal[]> GetInterpolatedPricesAsync(string productCode, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Fill missing prices in a price series using linear interpolation
    /// </summary>
    Task<Dictionary<DateTime, decimal>> FillMissingPricesAsync(
        string productCode, 
        Dictionary<DateTime, decimal> existingPrices, 
        DateTime startDate, 
        DateTime endDate);

    /// <summary>
    /// Get the nearest available price (forward or backward looking)
    /// </summary>
    Task<decimal?> GetNearestPriceAsync(string productCode, DateTime targetDate, int maxDaysLookback = 7, int maxDaysLookforward = 3);

    /// <summary>
    /// Check if a price date is a business day (excludes weekends and holidays)
    /// </summary>
    bool IsBusinessDay(DateTime date);

    /// <summary>
    /// Get next business day from a given date
    /// </summary>
    DateTime GetNextBusinessDay(DateTime date);

    /// <summary>
    /// Get previous business day from a given date
    /// </summary>
    DateTime GetPreviousBusinessDay(DateTime date);
}