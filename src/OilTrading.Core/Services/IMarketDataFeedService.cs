using OilTrading.Core.Entities;
using OilTrading.Core.Entities.TimeSeries;

namespace OilTrading.Core.Services;

public interface IMarketDataFeedService
{
    Task<bool> StartRealTimeDataFeed();
    Task<bool> StopRealTimeDataFeed();
    bool IsConnected { get; }
    
    Task<IEnumerable<MarketPrice>> GetLatestPricesAsync(IEnumerable<string> symbols);
    Task<IEnumerable<MarketData>> GetHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate);
    Task<MarketPrice?> GetLatestPriceAsync(string symbol);
    
    event EventHandler<MarketPriceUpdatedEventArgs> MarketPriceUpdated;
}

public class MarketPriceUpdatedEventArgs : EventArgs
{
    public MarketPrice MarketPrice { get; }
    public DateTime UpdatedAt { get; }
    
    public MarketPriceUpdatedEventArgs(MarketPrice marketPrice, DateTime updatedAt)
    {
        MarketPrice = marketPrice;
        UpdatedAt = updatedAt;
    }
}