namespace OilTrading.Application.DTOs;

public class PaperContractDto
{
    public Guid Id { get; set; }
    public string ContractMonth { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal LotSize { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal? CurrentPrice { get; set; }
    public DateTime TradeDate { get; set; }
    public DateTime? SettlementDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? RealizedPnL { get; set; }
    public decimal? UnrealizedPnL { get; set; }
    public decimal? DailyPnL { get; set; }
    public DateTime? LastMTMDate { get; set; }
    
    // Spread information
    public bool IsSpread { get; set; }
    public string? Leg1Product { get; set; }
    public string? Leg2Product { get; set; }
    public decimal? SpreadValue { get; set; }
    
    // Risk metrics
    public decimal? VaRValue { get; set; }
    public decimal? Volatility { get; set; }
    
    // Additional info
    public string? TradeReference { get; set; }
    public string? CounterpartyName { get; set; }
    public string? Notes { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class PaperContractListDto
{
    public Guid Id { get; set; }
    public string ContractMonth { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? UnrealizedPnL { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime TradeDate { get; set; }
}

public class CreatePaperContractDto
{
    public string ContractMonth { get; set; } = string.Empty; // "AUG25"
    public string ProductType { get; set; } = string.Empty;   // "380cst"
    public string Position { get; set; } = string.Empty;      // "Long" or "Short"
    public decimal Quantity { get; set; }
    public decimal LotSize { get; set; } = 1000;
    public decimal EntryPrice { get; set; }
    public DateTime TradeDate { get; set; }
    public string? TradeReference { get; set; }
    public string? CounterpartyName { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class MTMUpdateDto
{
    public Guid ContractId { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime MTMDate { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal? DailyPnL { get; set; }
}