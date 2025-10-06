namespace OilTrading.Application.DTOs;

public class PerformanceAnalyticsDto
{
    public string Period { get; set; } = string.Empty;
    
    public decimal TotalPnL { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }
    
    public string BestPerformingProduct { get; set; } = string.Empty;
    public string WorstPerformingProduct { get; set; } = string.Empty;
    
    public decimal TotalReturn { get; set; }
    public decimal AnnualizedReturn { get; set; }
    
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    
    public decimal VaRUtilization { get; set; }
    public decimal VolatilityAdjustedReturn { get; set; }
    
    public List<DailyPnLDto> DailyPnLHistory { get; set; } = new();
    public List<ProductPerformanceDto> ProductPerformance { get; set; } = new();
    
    public DateTime CalculatedAt { get; set; }
}

public class ProductPerformanceDto
{
    public string Product { get; set; } = string.Empty;
    public decimal Exposure { get; set; }
    public decimal PnL { get; set; }
    public decimal Return { get; set; }
}