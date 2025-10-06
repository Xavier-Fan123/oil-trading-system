using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

public class MarketPrice : BaseEntity
{
    public DateTime PriceDate { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public MarketPriceType PriceType { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Source { get; set; }
    public string? ContractMonth { get; set; } // For futures: "AUG25", "SEP25"
    public string? DataSource { get; set; } // "Platts", "ICE", "Manual"
    public bool IsSettlement { get; set; } // True for settlement prices
    public DateTime ImportedAt { get; set; }
    public string? ImportedBy { get; set; }
}

public enum MarketPriceType
{
    Spot = 1,              // Spot price (MOPS)
    FuturesSettlement = 2, // Futures settlement price
    FuturesClose = 3,      // Futures closing price
    Index = 4,             // Index price
    Spread = 5             // Spread
}