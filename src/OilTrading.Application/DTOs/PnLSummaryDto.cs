namespace OilTrading.Application.DTOs;

public class PnLSummaryDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalUnrealizedPnL { get; set; }
    public decimal TotalRealizedPnL { get; set; }
    public decimal NetPnL { get; set; }
    public int OpenPositions { get; set; }
    public int ClosedPositions { get; set; }
    public List<ProductPnLDto> ProductBreakdown { get; set; } = new();
    public List<DailyPnLDto> DailyPnL { get; set; } = new();
}

public class ProductPnLDto
{
    public string ProductType { get; set; } = string.Empty;
    public decimal UnrealizedPnL { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal NetPnL { get; set; }
    public int PositionCount { get; set; }
}

public class DailyPnLDto
{
    public DateTime Date { get; set; }
    public decimal DailyPnL { get; set; }
    public decimal CumulativePnL { get; set; }
}