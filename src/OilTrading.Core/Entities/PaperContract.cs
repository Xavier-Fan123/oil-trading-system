using OilTrading.Core.Common;
using OilTrading.Core.Events;

namespace OilTrading.Core.Entities;

public class PaperContract : BaseEntity
{
    public string ContractNumber { get; set; } = string.Empty; // Auto-generated
    public string ContractMonth { get; set; } = string.Empty; // "AUG25"
    public string ProductType { get; set; } = string.Empty;   // "380cst", "0.5%", "Brent"
    public PositionType Position { get; set; }
    public decimal Quantity { get; set; }  // Number of lots
    public decimal LotSize { get; set; }   // MT per lot (usually 1000)
    public decimal EntryPrice { get; set; }
    public decimal? CurrentPrice { get; set; }
    public DateTime TradeDate { get; set; }
    public DateTime? SettlementDate { get; set; }
    public PaperContractStatus Status { get; set; }
    public decimal? RealizedPnL { get; set; }
    public decimal? UnrealizedPnL { get; set; }
    public decimal? DailyPnL { get; set; }
    public DateTime? LastMTMDate { get; set; }

    // Trade Group Association
    /// <summary>
    /// Trade Group ID for multi-leg strategies
    /// </summary>
    public Guid? TradeGroupId { get; private set; }

    /// <summary>
    /// Trade Group navigation property
    /// </summary>
    public TradeGroup? TradeGroup { get; private set; }
    
    // Spread contracts
    public bool IsSpread { get; set; }
    public string? Leg1Product { get; set; }  // e.g., "380cst AUG25"
    public string? Leg2Product { get; set; }  // e.g., "380cst SEP25"
    public decimal? SpreadValue { get; set; }
    
    // Risk metrics
    public decimal? VaRValue { get; set; }
    public decimal? Volatility { get; set; }
    
    // Reference
    public string? TradeReference { get; set; }
    public string? CounterpartyName { get; set; }
    public string? Notes { get; set; }
    
    public void UpdateMTM(decimal currentPrice, DateTime mtmDate)
    {
        CurrentPrice = currentPrice;
        LastMTMDate = mtmDate;
        
        var priceDiff = currentPrice - EntryPrice;
        var multiplier = Position == PositionType.Long ? 1 : -1;
        UnrealizedPnL = priceDiff * Quantity * LotSize * multiplier;
        
        // Calculate daily P&L if we have previous MTM
        if (LastMTMDate.HasValue && LastMTMDate.Value.Date < mtmDate.Date)
        {
            // This would need previous price from database
            DailyPnL = 0; // Placeholder - will be calculated in handler
        }
    }
    
    public void ClosePosition(decimal closingPrice, DateTime closeDate)
    {
        CurrentPrice = closingPrice;
        SettlementDate = closeDate;
        Status = PaperContractStatus.Closed;
        
        var priceDiff = closingPrice - EntryPrice;
        var multiplier = Position == PositionType.Long ? 1 : -1;
        RealizedPnL = priceDiff * Quantity * LotSize * multiplier;
        UnrealizedPnL = 0;
    }

    /// <summary>
    /// Assign to trade group
    /// </summary>
    public void AssignToTradeGroup(Guid tradeGroupId, string updatedBy = "System")
    {
        if (Status == PaperContractStatus.Closed || Status == PaperContractStatus.Settled)
            throw new DomainException("Cannot assign closed or settled contract to trade group");

        var previousGroupId = TradeGroupId;
        TradeGroupId = tradeGroupId;
        SetUpdatedBy(updatedBy);

        if (previousGroupId.HasValue)
        {
            AddDomainEvent(new ContractRemovedFromTradeGroupEvent(
                previousGroupId.Value, Id, "PaperContract", "Previous Group"));
        }

        AddDomainEvent(new ContractAddedToTradeGroupEvent(
            tradeGroupId, Id, "PaperContract", "New Group"));
    }

    /// <summary>
    /// Remove from trade group
    /// </summary>
    public void RemoveFromTradeGroup(string updatedBy = "System")
    {
        if (TradeGroupId.HasValue)
        {
            var removedGroupId = TradeGroupId.Value;
            TradeGroupId = null;
            SetUpdatedBy(updatedBy);

            AddDomainEvent(new ContractRemovedFromTradeGroupEvent(
                removedGroupId, Id, "PaperContract", "Removed Group"));
        }
    }

    /// <summary>
    /// Get unrealized P&L
    /// </summary>
    public decimal GetUnrealizedPnL()
    {
        if (Status != PaperContractStatus.Open || !CurrentPrice.HasValue)
            return 0;

        var priceDiff = CurrentPrice.Value - EntryPrice;
        var multiplier = Position == PositionType.Long ? 1 : -1;
        return priceDiff * Quantity * LotSize * multiplier;
    }

    /// <summary>
    /// Get total contract value
    /// </summary>
    public decimal GetTotalValue()
    {
        var price = CurrentPrice ?? EntryPrice;
        return Math.Abs(Quantity * LotSize * price);
    }
}

public enum PositionType
{
    Long = 1,
    Short = 2
}

public enum PaperContractStatus
{
    Open = 1,
    Closed = 2,
    Settled = 3,
    Cancelled = 4
}