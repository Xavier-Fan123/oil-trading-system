namespace OilTrading.Core.Entities.TimeSeries;

public class MarketData : BaseEntity
{
    public DateTime Timestamp { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Open { get; set; }
    public decimal Close { get; set; }
    public string Currency { get; set; } = "USD";
    public string DataSource { get; set; } = string.Empty;
}