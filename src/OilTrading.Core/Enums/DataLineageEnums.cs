namespace OilTrading.Core.Enums;

/// <summary>
/// Contract Pricing Status - Explicit state for pricing lifecycle
/// Replaces implicit determination from data presence
/// </summary>
public enum ContractPricingStatus
{
    /// <summary>
    /// Contract has no pricing fixed yet
    /// </summary>
    Unpriced = 1,

    /// <summary>
    /// Contract has partial quantity priced (0% < FixedPercentage < 100%)
    /// </summary>
    PartiallyPriced = 2,

    /// <summary>
    /// Contract is fully priced (FixedPercentage = 100%)
    /// </summary>
    FullyPriced = 3
}

/// <summary>
/// Settlement Amendment Type - Tracks why a settlement was created/modified
/// Enables proper audit trail for invoice lifecycle
/// </summary>
public enum SettlementAmendmentType
{
    /// <summary>
    /// Initial settlement - first invoice for this contract/shipment
    /// </summary>
    Initial = 1,

    /// <summary>
    /// Amendment - modification to original settlement terms
    /// </summary>
    Amendment = 2,

    /// <summary>
    /// Correction - error correction to prior settlement
    /// </summary>
    Correction = 3,

    /// <summary>
    /// Secondary Pricing - additional pricing event (e.g., second cargo lift)
    /// </summary>
    SecondaryPricing = 4,

    /// <summary>
    /// Final Settlement - closing settlement superseding provisional
    /// </summary>
    FinalSettlement = 5
}

/// <summary>
/// Price Source Type - How the price was determined
/// Enables auditing of price origin
/// </summary>
public enum PriceSourceType
{
    /// <summary>
    /// Manual entry by user
    /// </summary>
    Manual = 1,

    /// <summary>
    /// Automatically fetched from market data feed
    /// </summary>
    MarketData = 2,

    /// <summary>
    /// Calculated from price formula
    /// </summary>
    Formula = 3,

    /// <summary>
    /// System-generated estimate (fallback)
    /// </summary>
    Estimate = 4,

    /// <summary>
    /// Imported from external system
    /// </summary>
    Import = 5
}

/// <summary>
/// Hedged Contract Type - Type of physical contract being hedged
/// Used in PaperContract to specify what the hedge covers
/// </summary>
public enum HedgedContractType
{
    /// <summary>
    /// Hedge covers a purchase contract (long physical)
    /// </summary>
    Purchase = 1,

    /// <summary>
    /// Hedge covers a sales contract (short physical)
    /// </summary>
    Sales = 2,

    /// <summary>
    /// Hedge covers both purchase and sales (spread)
    /// </summary>
    Both = 3
}

/// <summary>
/// Split Reason - Business reason for logistics split
/// Provides context for shipping operation splits
/// </summary>
public enum SplitReason
{
    /// <summary>
    /// Vessel capacity constraint
    /// </summary>
    VesselCapacity = 1,

    /// <summary>
    /// Port draft/berth limitation
    /// </summary>
    PortLimitation = 2,

    /// <summary>
    /// Customer request for partial delivery
    /// </summary>
    CustomerRequest = 3,

    /// <summary>
    /// Cargo quality segregation
    /// </summary>
    QualitySegregation = 4,

    /// <summary>
    /// Regulatory or customs requirement
    /// </summary>
    RegulatoryRequirement = 5,

    /// <summary>
    /// Operational efficiency optimization
    /// </summary>
    OperationalOptimization = 6,

    /// <summary>
    /// Other/unspecified reason
    /// </summary>
    Other = 99
}
