using OilTrading.Core.Common;

namespace OilTrading.Core.Events;

/// <summary>
/// Paper contract designated as hedge event
/// Raised when a paper contract is formally designated as a hedge for a physical contract
/// </summary>
public class PaperContractHedgeDesignatedEvent : IDomainEvent
{
    public PaperContractHedgeDesignatedEvent(
        Guid paperContractId,
        Guid hedgedContractId,
        string hedgedContractType,
        decimal hedgeRatio)
    {
        PaperContractId = paperContractId;
        HedgedContractId = hedgedContractId;
        HedgedContractType = hedgedContractType;
        HedgeRatio = hedgeRatio;
        OccurredOn = DateTime.UtcNow;
    }

    public Guid PaperContractId { get; }
    public Guid HedgedContractId { get; }
    public string HedgedContractType { get; }
    public decimal HedgeRatio { get; }
    public DateTime OccurredOn { get; }
}

/// <summary>
/// Paper contract hedge designation removed event
/// Raised when a paper contract's hedge designation is removed
/// </summary>
public class PaperContractHedgeRemovedEvent : IDomainEvent
{
    public PaperContractHedgeRemovedEvent(
        Guid paperContractId,
        Guid previousHedgedContractId,
        string reason)
    {
        PaperContractId = paperContractId;
        PreviousHedgedContractId = previousHedgedContractId;
        Reason = reason;
        OccurredOn = DateTime.UtcNow;
    }

    public Guid PaperContractId { get; }
    public Guid PreviousHedgedContractId { get; }
    public string Reason { get; }
    public DateTime OccurredOn { get; }
}

/// <summary>
/// Paper contract hedge effectiveness updated event
/// Raised when the hedge effectiveness metric is recalculated
/// </summary>
public class PaperContractHedgeEffectivenessUpdatedEvent : IDomainEvent
{
    public PaperContractHedgeEffectivenessUpdatedEvent(
        Guid paperContractId,
        Guid hedgedContractId,
        decimal newEffectiveness,
        decimal? previousEffectiveness)
    {
        PaperContractId = paperContractId;
        HedgedContractId = hedgedContractId;
        NewEffectiveness = newEffectiveness;
        PreviousEffectiveness = previousEffectiveness;
        OccurredOn = DateTime.UtcNow;
    }

    public Guid PaperContractId { get; }
    public Guid HedgedContractId { get; }
    public decimal NewEffectiveness { get; }
    public decimal? PreviousEffectiveness { get; }
    public DateTime OccurredOn { get; }
}

/// <summary>
/// Paper contract hedge ratio updated event
/// Raised when the hedge ratio is modified
/// </summary>
public class PaperContractHedgeRatioUpdatedEvent : IDomainEvent
{
    public PaperContractHedgeRatioUpdatedEvent(
        Guid paperContractId,
        Guid hedgedContractId,
        decimal newHedgeRatio,
        decimal previousHedgeRatio)
    {
        PaperContractId = paperContractId;
        HedgedContractId = hedgedContractId;
        NewHedgeRatio = newHedgeRatio;
        PreviousHedgeRatio = previousHedgeRatio;
        OccurredOn = DateTime.UtcNow;
    }

    public Guid PaperContractId { get; }
    public Guid HedgedContractId { get; }
    public decimal NewHedgeRatio { get; }
    public decimal PreviousHedgeRatio { get; }
    public DateTime OccurredOn { get; }
}
