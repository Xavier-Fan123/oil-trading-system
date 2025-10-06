using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

/// <summary>
/// Represents a futures trading deal from daily Deal Report
/// </summary>
public class FuturesDeal : BaseEntity
{
    public string DealNumber { get; set; } = string.Empty;
    public DateTime TradeDate { get; set; }
    public DateTime ValueDate { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ContractMonth { get; set; } = string.Empty; // e.g., "AUG25"
    public DealDirection Direction { get; set; } // Buy or Sell
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; } = QuantityUnit.MT; // MT, BBL, LOTS
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string PriceUnit { get; set; } = string.Empty; // USD/MT, USD/BBL
    public decimal TotalValue { get; set; } // Calculated: Quantity * Price
    
    // Trading details
    public string Trader { get; set; } = string.Empty;
    public string Broker { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty; // ICE, SGX, etc.
    public string ClearingHouse { get; set; } = string.Empty;
    
    // Status and settlement
    public DealStatus Status { get; set; } = DealStatus.Executed;
    public DateTime? SettlementDate { get; set; }
    public bool IsCleared { get; set; }
    public string? ClearingReference { get; set; }
    
    // P&L tracking
    public decimal? MarketPrice { get; set; } // Current market price for MTM
    public decimal? UnrealizedPnL { get; set; } // Mark-to-market P&L
    public decimal? RealizedPnL { get; set; } // For closed positions
    
    // Import tracking
    public string DataSource { get; set; } = string.Empty; // "Deal Report", "Manual"
    public DateTime ImportedAt { get; set; }
    public string ImportedBy { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    
    // Validation
    public bool Validate()
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(DealNumber))
            errors.Add("Deal number is required");
        
        if (string.IsNullOrEmpty(ProductCode))
            errors.Add("Product code is required");
        
        if (Quantity <= 0)
            errors.Add("Quantity must be positive");
        
        if (Price < 0)
            errors.Add("Price cannot be negative");
        
        if (TradeDate > DateTime.UtcNow.AddDays(1))
            errors.Add("Trade date cannot be in the future");
        
        return errors.Count == 0;
    }
    
    // Calculate unrealized P&L
    public void CalculateUnrealizedPnL(decimal currentMarketPrice)
    {
        MarketPrice = currentMarketPrice;
        
        if (Direction == DealDirection.Buy)
        {
            UnrealizedPnL = (currentMarketPrice - Price) * Quantity;
        }
        else // Sell
        {
            UnrealizedPnL = (Price - currentMarketPrice) * Quantity;
        }
    }
    
    // Link to related entities
    public Guid? PaperContractId { get; set; }
    public PaperContract? PaperContract { get; set; }
}

public enum DealDirection
{
    Buy = 1,
    Sell = 2
}

public enum DealStatus
{
    Executed = 1,
    Pending = 2,
    Cancelled = 3,
    Settled = 4,
    Failed = 5
}