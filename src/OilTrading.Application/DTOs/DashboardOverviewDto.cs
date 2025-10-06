namespace OilTrading.Application.DTOs;

public class DashboardOverviewDto
{
    public int TotalPositions { get; set; }
    public decimal TotalExposure { get; set; }
    public decimal NetExposure { get; set; }
    public int LongPositions { get; set; }
    public int ShortPositions { get; set; }
    public int FlatPositions { get; set; }
    
    public decimal DailyPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }
    
    public decimal VaR95 { get; set; }
    public decimal VaR99 { get; set; }
    public decimal PortfolioVolatility { get; set; }
    
    public int ActivePurchaseContracts { get; set; }
    public int ActiveSalesContracts { get; set; }
    public int PendingContracts { get; set; }
    
    public int MarketDataPoints { get; set; }
    public DateTime LastMarketUpdate { get; set; }
    
    public int AlertCount { get; set; }
    public DateTime CalculatedAt { get; set; }
}