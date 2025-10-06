using OilTrading.Core.Common;
using OilTrading.Core.Entities;

namespace OilTrading.Core.Events;

/// <summary>
/// 交易组创建事件 - Trade group created event
/// </summary>
public class TradeGroupCreatedEvent : IDomainEvent
{
    public TradeGroupCreatedEvent(Guid tradeGroupId, string groupName, StrategyType strategyType)
    {
        TradeGroupId = tradeGroupId;
        GroupName = groupName;
        StrategyType = strategyType;
        OccurredOn = DateTime.UtcNow;
    }

    public Guid TradeGroupId { get; }
    public string GroupName { get; }
    public StrategyType StrategyType { get; }
    public DateTime OccurredOn { get; }
}

/// <summary>
/// 交易组关闭事件 - Trade group closed event
/// </summary>
public class TradeGroupClosedEvent : IDomainEvent
{
    public TradeGroupClosedEvent(Guid tradeGroupId, string groupName)
    {
        TradeGroupId = tradeGroupId;
        GroupName = groupName;
        OccurredOn = DateTime.UtcNow;
    }

    public Guid TradeGroupId { get; }
    public string GroupName { get; }
    public DateTime OccurredOn { get; }
}

/// <summary>
/// 合约添加到交易组事件 - Contract added to trade group event
/// </summary>
public class ContractAddedToTradeGroupEvent : IDomainEvent
{
    public ContractAddedToTradeGroupEvent(Guid tradeGroupId, Guid contractId, string contractType, string groupName)
    {
        TradeGroupId = tradeGroupId;
        ContractId = contractId;
        ContractType = contractType;
        GroupName = groupName;
        OccurredOn = DateTime.UtcNow;
    }

    public Guid TradeGroupId { get; }
    public Guid ContractId { get; }
    public string ContractType { get; }
    public string GroupName { get; }
    public DateTime OccurredOn { get; }
}

/// <summary>
/// 合约从交易组移除事件 - Contract removed from trade group event
/// </summary>
public class ContractRemovedFromTradeGroupEvent : IDomainEvent
{
    public ContractRemovedFromTradeGroupEvent(Guid tradeGroupId, Guid contractId, string contractType, string groupName)
    {
        TradeGroupId = tradeGroupId;
        ContractId = contractId;
        ContractType = contractType;
        GroupName = groupName;
        OccurredOn = DateTime.UtcNow;
    }

    public Guid TradeGroupId { get; }
    public Guid ContractId { get; }
    public string ContractType { get; }
    public string GroupName { get; }
    public DateTime OccurredOn { get; }
}