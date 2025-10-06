using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

/// <summary>
/// 交易组仓库接口 - Trade Group Repository Interface
/// </summary>
public interface ITradeGroupRepository : IRepository<TradeGroup>
{
    /// <summary>
    /// 根据策略类型获取交易组 - Get trade groups by strategy type
    /// </summary>
    Task<IEnumerable<TradeGroup>> GetByStrategyTypeAsync(StrategyType strategyType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取活跃的交易组 - Get active trade groups
    /// </summary>
    Task<IEnumerable<TradeGroup>> GetActiveTradeGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据名称获取交易组 - Get trade group by name
    /// </summary>
    Task<TradeGroup?> GetByNameAsync(string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取包含合约的交易组 - Get trade group with all its contracts
    /// </summary>
    Task<TradeGroup?> GetWithContractsAsync(Guid tradeGroupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取有持仓的交易组 - Get trade groups with open positions
    /// </summary>
    Task<IEnumerable<TradeGroup>> GetTradeGroupsWithOpenPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取交易组的净值和风险数据 - Get trade group with P&L and risk data
    /// </summary>
    Task<IEnumerable<TradeGroupRiskSummary>> GetTradeGroupRiskSummariesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 交易组风险摘要 - Trade Group Risk Summary
/// </summary>
public class TradeGroupRiskSummary
{
    public Guid TradeGroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public StrategyType StrategyType { get; set; }
    public TradeGroupStatus Status { get; set; }
    public decimal NetPnL { get; set; }
    public decimal TotalValue { get; set; }
    public decimal VaR95 { get; set; }
    public decimal VaR99 { get; set; }
    public int ContractCount { get; set; }
    public DateTime LastUpdated { get; set; }
}