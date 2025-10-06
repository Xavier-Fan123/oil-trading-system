namespace OilTrading.Application.DTOs;

public class MarketInsightsDto
{
    public int MarketDataCount { get; set; }
    public DateTime LastUpdate { get; set; }
    
    public List<KeyPriceDto> KeyPrices { get; set; } = new();
    public Dictionary<string, decimal> VolatilityIndicators { get; set; } = new();
    public Dictionary<string, Dictionary<string, decimal>> CorrelationMatrix { get; set; } = new();
    
    public Dictionary<string, decimal> TechnicalIndicators { get; set; } = new();
    public List<MarketTrendDto> MarketTrends { get; set; } = new();
    public Dictionary<string, decimal> SentimentIndicators { get; set; } = new();
    
    public DateTime CalculatedAt { get; set; }
}

public class KeyPriceDto
{
    public string Product { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public DateTime LastUpdate { get; set; }
}

public class MarketTrendDto
{
    public string Product { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public decimal Strength { get; set; }
}