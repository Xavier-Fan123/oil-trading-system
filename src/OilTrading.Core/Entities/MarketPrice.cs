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
    Spot = 1,              // 现货价格 (MOPS)
    FuturesSettlement = 2, // 期货结算价
    FuturesClose = 3,      // 期货收盘价
    Index = 4,             // 指数价格
    Spread = 5             // 价差
}