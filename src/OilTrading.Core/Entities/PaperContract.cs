using OilTrading.Core.Common;
using OilTrading.Core.Events;
using OilTrading.Core.Enums;

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

    // ═══════════════════════════════════════════════════════════════════════════
    // DATA LINEAGE ENHANCEMENT - Hedge Linking
    // Purpose: Establish direct FK relationship between paper contracts and physical contracts
    // Solves: String-based matching leading to wrong hedges applied to physical positions
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Hedged Contract ID - Direct FK to the physical contract being hedged
    /// Can reference either PurchaseContract or SalesContract based on HedgedContractType
    /// </summary>
    public Guid? HedgedContractId { get; private set; }

    /// <summary>
    /// Hedged Contract Type - Whether this hedges a Purchase or Sales contract
    /// Required when HedgedContractId is set
    /// </summary>
    public HedgedContractType? HedgedContractType { get; private set; }

    /// <summary>
    /// Hedge Ratio - The ratio of hedge quantity to physical quantity (e.g., 1.0 for 1:1, 0.5 for 50%)
    /// Default: 1.0 (full hedge)
    /// </summary>
    public decimal HedgeRatio { get; private set; } = 1.0m;

    /// <summary>
    /// Hedge Effectiveness - Accounting metric for hedge effectiveness (0-100%)
    /// Used for IFRS 9 / ASC 815 compliance reporting if needed
    /// </summary>
    public decimal? HedgeEffectiveness { get; private set; }

    /// <summary>
    /// Hedge Designation Date - When this paper contract was formally designated as a hedge
    /// Required for proper hedge accounting treatment
    /// </summary>
    public DateTime? HedgeDesignationDate { get; private set; }

    /// <summary>
    /// Is Designated Hedge - Quick filter flag indicating formal hedge designation
    /// True if HedgedContractId is set and HedgeDesignationDate is not null
    /// </summary>
    public bool IsDesignatedHedge { get; private set; } = false;

    // Navigation properties for hedge relationship (loaded manually, not EF relationships due to polymorphism)
    // Use repository methods to load the actual hedged contract
    
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

    // ═══════════════════════════════════════════════════════════════════════════
    // DATA LINEAGE METHODS - Hedge Linking Management
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Designate this paper contract as a hedge for a physical contract
    /// </summary>
    public void DesignateAsHedge(
        Guid hedgedContractId,
        HedgedContractType hedgedContractType,
        decimal hedgeRatio = 1.0m,
        string updatedBy = "System")
    {
        if (Status == PaperContractStatus.Closed || Status == PaperContractStatus.Settled)
            throw new DomainException("Cannot designate closed or settled contract as hedge");

        if (hedgeRatio <= 0 || hedgeRatio > 10)
            throw new DomainException("Hedge ratio must be between 0 and 10");

        HedgedContractId = hedgedContractId;
        HedgedContractType = hedgedContractType;
        HedgeRatio = hedgeRatio;
        HedgeDesignationDate = DateTime.UtcNow;
        IsDesignatedHedge = true;
        SetUpdatedBy(updatedBy);

        AddDomainEvent(new PaperContractHedgeDesignatedEvent(
            Id, hedgedContractId, hedgedContractType.ToString(), hedgeRatio));
    }

    /// <summary>
    /// Remove the hedge designation from this paper contract
    /// </summary>
    public void RemoveHedgeDesignation(string reason, string updatedBy = "System")
    {
        if (!IsDesignatedHedge)
            throw new DomainException("This contract is not designated as a hedge");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Reason for removing hedge designation is required");

        var previousHedgedContractId = HedgedContractId;

        HedgedContractId = null;
        HedgedContractType = null;
        HedgeRatio = 1.0m;
        HedgeEffectiveness = null;
        HedgeDesignationDate = null;
        IsDesignatedHedge = false;
        Notes = string.IsNullOrEmpty(Notes)
            ? $"Hedge designation removed: {reason}"
            : $"{Notes}\nHedge designation removed: {reason}";
        SetUpdatedBy(updatedBy);

        if (previousHedgedContractId.HasValue)
        {
            AddDomainEvent(new PaperContractHedgeRemovedEvent(Id, previousHedgedContractId.Value, reason));
        }
    }

    /// <summary>
    /// Update the hedge effectiveness measure
    /// </summary>
    public void UpdateHedgeEffectiveness(decimal effectiveness, string updatedBy = "System")
    {
        if (!IsDesignatedHedge)
            throw new DomainException("Cannot update hedge effectiveness for non-designated hedge");

        if (effectiveness < 0 || effectiveness > 100)
            throw new DomainException("Hedge effectiveness must be between 0 and 100");

        HedgeEffectiveness = effectiveness;
        SetUpdatedBy(updatedBy);
    }

    /// <summary>
    /// Update the hedge ratio
    /// </summary>
    public void UpdateHedgeRatio(decimal newHedgeRatio, string updatedBy = "System")
    {
        if (!IsDesignatedHedge)
            throw new DomainException("Cannot update hedge ratio for non-designated hedge");

        if (newHedgeRatio <= 0 || newHedgeRatio > 10)
            throw new DomainException("Hedge ratio must be between 0 and 10");

        HedgeRatio = newHedgeRatio;
        SetUpdatedBy(updatedBy);
    }

    /// <summary>
    /// Check if this paper contract can be used as a hedge
    /// </summary>
    public bool CanBeDesignatedAsHedge() =>
        Status == PaperContractStatus.Open && !IsDesignatedHedge;

    /// <summary>
    /// Get the hedged quantity (paper quantity * hedge ratio)
    /// </summary>
    public decimal GetHedgedQuantity()
    {
        if (!IsDesignatedHedge)
            return 0;

        return Quantity * LotSize * HedgeRatio;
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