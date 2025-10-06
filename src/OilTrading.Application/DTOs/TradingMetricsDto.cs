namespace OilTrading.Application.DTOs;

public class TradingMetricsDto
{
    public string Period { get; set; } = string.Empty;
    public int TotalTrades { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal AverageTradeSize { get; set; }
    
    public decimal PurchaseVolume { get; set; }
    public decimal SalesVolume { get; set; }
    public decimal PaperVolume { get; set; }
    
    public decimal LongPaperVolume { get; set; }
    public decimal ShortPaperVolume { get; set; }
    
    public Dictionary<string, decimal> ProductBreakdown { get; set; } = new();
    public Dictionary<string, decimal> CounterpartyBreakdown { get; set; } = new();
    
    public decimal TradeFrequency { get; set; }
    public Dictionary<string, decimal> VolumeByProduct { get; set; } = new();
    
    public DateTime CalculatedAt { get; set; }
}