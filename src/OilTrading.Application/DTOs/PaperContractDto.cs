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

    // ═══════════════════════════════════════════════════════════════════════════
    // DATA LINEAGE ENHANCEMENT - Hedge Linking (v2.18.0)
    // Purpose: Expose FK relationship between paper contracts and physical contracts
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Hedged Contract ID - Direct FK to the physical contract being hedged
    /// </summary>
    public Guid? HedgedContractId { get; set; }

    /// <summary>
    /// Hedged Contract Type - Whether this hedges a Purchase or Sales contract
    /// </summary>
    public string? HedgedContractType { get; set; }

    /// <summary>
    /// Hedge Ratio - The ratio of hedge quantity to physical quantity (e.g., 1.0 for 1:1)
    /// </summary>
    public decimal HedgeRatio { get; set; }

    /// <summary>
    /// Hedge Effectiveness - Accounting metric for hedge effectiveness (0-100%)
    /// </summary>
    public decimal? HedgeEffectiveness { get; set; }

    /// <summary>
    /// Hedge Designation Date - When this paper contract was formally designated as a hedge
    /// </summary>
    public DateTime? HedgeDesignationDate { get; set; }

    /// <summary>
    /// Is Designated Hedge - Quick filter flag indicating formal hedge designation
    /// </summary>
    public bool IsDesignatedHedge { get; set; }

    /// <summary>
    /// Hedged Quantity - Calculated quantity (paper quantity * hedge ratio)
    /// </summary>
    public decimal HedgedQuantity { get; set; }

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

    // Data Lineage Enhancement - Hedge Linking (v2.18.0)
    public bool IsDesignatedHedge { get; set; }
    public Guid? HedgedContractId { get; set; }
    public string? HedgedContractType { get; set; }
    public decimal HedgeRatio { get; set; }
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

// ═══════════════════════════════════════════════════════════════════════════
// DATA LINEAGE ENHANCEMENT - Hedge Mapping DTOs (v2.18.0)
// Purpose: Request/Response DTOs for hedge designation operations
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Request DTO for designating a paper contract as a hedge for a physical contract
/// </summary>
public class DesignateHedgeRequestDto
{
    /// <summary>
    /// The physical contract ID being hedged (either PurchaseContract or SalesContract)
    /// </summary>
    public Guid HedgedContractId { get; set; }

    /// <summary>
    /// The type of contract being hedged (Purchase or Sales)
    /// </summary>
    public string HedgedContractType { get; set; } = string.Empty;

    /// <summary>
    /// Hedge ratio (default 1.0 for 1:1 hedge)
    /// </summary>
    public decimal HedgeRatio { get; set; } = 1.0m;
}

/// <summary>
/// Request DTO for removing a hedge designation
/// </summary>
public class RemoveHedgeDesignationRequestDto
{
    /// <summary>
    /// Reason for removing the hedge designation (required for audit trail)
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating hedge effectiveness
/// </summary>
public class UpdateHedgeEffectivenessRequestDto
{
    /// <summary>
    /// Hedge effectiveness percentage (0-100)
    /// </summary>
    public decimal Effectiveness { get; set; }
}

/// <summary>
/// Request DTO for updating hedge ratio
/// </summary>
public class UpdateHedgeRatioRequestDto
{
    /// <summary>
    /// New hedge ratio (0.01 to 10.0)
    /// </summary>
    public decimal HedgeRatio { get; set; }
}

/// <summary>
/// Response DTO for hedge designation operations
/// </summary>
public class HedgeDesignationResultDto
{
    public Guid PaperContractId { get; set; }
    public Guid? HedgedContractId { get; set; }
    public string? HedgedContractType { get; set; }
    public decimal HedgeRatio { get; set; }
    public decimal? HedgeEffectiveness { get; set; }
    public DateTime? HedgeDesignationDate { get; set; }
    public bool IsDesignatedHedge { get; set; }
    public decimal HedgedQuantity { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for listing paper contracts that hedge a specific physical contract
/// </summary>
public class HedgingPaperContractDto
{
    public Guid PaperContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractMonth { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal LotSize { get; set; }
    public decimal HedgeRatio { get; set; }
    public decimal HedgedQuantity { get; set; }
    public decimal? HedgeEffectiveness { get; set; }
    public DateTime? HedgeDesignationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal EntryPrice { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? UnrealizedPnL { get; set; }
}