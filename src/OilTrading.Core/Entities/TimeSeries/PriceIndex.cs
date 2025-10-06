namespace OilTrading.Core.Entities.TimeSeries;

public class PriceIndex : BaseEntity
{
    public DateTime Timestamp { get; set; }
    public string IndexName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string Region { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
}