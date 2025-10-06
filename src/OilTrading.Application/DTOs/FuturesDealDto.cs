namespace OilTrading.Application.DTOs;

public class FuturesDealDto
{
    public Guid Id { get; set; }
    public string DealNumber { get; set; } = string.Empty;
    public DateTime TradeDate { get; set; }
    public DateTime ValueDate { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ContractMonth { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // "Buy" or "Sell"
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = "MT";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string PriceUnit { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    
    // Trading details
    public string Trader { get; set; } = string.Empty;
    public string Broker { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    
    // P&L
    public decimal? MarketPrice { get; set; }
    public decimal? UnrealizedPnL { get; set; }
    public decimal? RealizedPnL { get; set; }
    
    // Status
    public string Status { get; set; } = "Executed";
    public bool IsCleared { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateFuturesDealDto
{
    public string DealNumber { get; set; } = string.Empty;
    public DateTime TradeDate { get; set; }
    public DateTime ValueDate { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ContractMonth { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // "Buy" or "Sell"
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = "MT";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Trader { get; set; }
    public string? Broker { get; set; }
    public string? Exchange { get; set; }
}

public class UpdateFuturesDealDto
{
    public Guid Id { get; set; }
    public decimal? Price { get; set; }
    public decimal? Quantity { get; set; }
    public string? Status { get; set; }
    public bool? IsCleared { get; set; }
    public string? ClearingReference { get; set; }
}

public class FuturesPositionDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ContractMonth { get; set; } = string.Empty;
    public decimal LongQuantity { get; set; }
    public decimal ShortQuantity { get; set; }
    public decimal NetPosition { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal? CurrentMarketPrice { get; set; }
    public decimal? UnrealizedPnL { get; set; }
    public int NumberOfDeals { get; set; }
}

public class FuturesDealUploadResultDto
{
    public bool Success { get; set; }
    public int RecordsProcessed { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsSkipped { get; set; }
    public int DuplicatesFound { get; set; }
    public List<string> Messages { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<FuturesDealDto> ImportedDeals { get; set; } = new();
}