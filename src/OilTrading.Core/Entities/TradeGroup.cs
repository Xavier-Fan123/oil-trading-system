using OilTrading.Core.Common;
using OilTrading.Core.Events;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

/// <summary>
/// Trade Group - Manages complex multi-leg trading strategies like spreads, hedges, arbitrage
/// </summary>
public class TradeGroup : BaseEntity
{
    private TradeGroup() { } // For EF Core

    public TradeGroup(
        string groupName,
        StrategyType strategyType,
        string? description = null,
        string createdBy = "System")
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new DomainException("Trade group name cannot be empty");

        GroupName = groupName.Trim();
        StrategyType = strategyType;
        Description = description?.Trim();
        Status = TradeGroupStatus.Active;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new TradeGroupCreatedEvent(Id, GroupName, StrategyType));
    }

    /// <summary>
    /// Trade group name
    /// </summary>
    public string GroupName { get; private set; } = string.Empty;

    /// <summary>
    /// Strategy type (Calendar Spread, Intercommodity Spread, etc.)
    /// </summary>
    public StrategyType StrategyType { get; private set; }

    /// <summary>
    /// Optional description of the strategy
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Trade group status
    /// </summary>
    public TradeGroupStatus Status { get; private set; }

    /// <summary>
    /// Expected risk level for this strategy
    /// </summary>
    public RiskLevel? ExpectedRiskLevel { get; private set; }

    /// <summary>
    /// Maximum allowed loss for this group (optional limit)
    /// </summary>
    public decimal? MaxAllowedLoss { get; private set; }

    /// <summary>
    /// Target profit for this group
    /// </summary>
    public decimal? TargetProfit { get; private set; }

    // Navigation Properties
    /// <summary>
    /// Associated paper contracts
    /// </summary>
    public ICollection<PaperContract> PaperContracts { get; private set; } = new List<PaperContract>();

    /// <summary>
    /// Associated purchase contracts
    /// </summary>
    public ICollection<PurchaseContract> PurchaseContracts { get; private set; } = new List<PurchaseContract>();

    /// <summary>
    /// Associated sales contracts
    /// </summary>
    public ICollection<SalesContract> SalesContracts { get; private set; } = new List<SalesContract>();

    /// <summary>
    /// Associated tags for strategy and risk classification
    /// </summary>
    public ICollection<TradeGroupTag> TradeGroupTags { get; private set; } = new List<TradeGroupTag>();

    // Business Methods

    /// <summary>
    /// Update trade group information
    /// </summary>
    public void UpdateInfo(string groupName, string? description = null, string updatedBy = "System")
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new DomainException("Trade group name cannot be empty");

        if (Status == TradeGroupStatus.Closed)
            throw new DomainException("Cannot update closed trade group");

        GroupName = groupName.Trim();
        Description = description?.Trim();
        SetUpdatedBy(updatedBy);
    }

    /// <summary>
    /// Set risk parameters
    /// </summary>
    public void SetRiskParameters(
        RiskLevel expectedRiskLevel,
        decimal? maxAllowedLoss = null,
        decimal? targetProfit = null,
        string updatedBy = "System")
    {
        if (Status == TradeGroupStatus.Closed)
            throw new DomainException("Cannot update closed trade group");

        ExpectedRiskLevel = expectedRiskLevel;
        MaxAllowedLoss = maxAllowedLoss;
        TargetProfit = targetProfit;
        SetUpdatedBy(updatedBy);
    }

    /// <summary>
    /// Close the trade group
    /// </summary>
    public void Close(string closedBy = "System")
    {
        if (Status == TradeGroupStatus.Closed)
            throw new DomainException("Trade group is already closed");

        // Check if all positions are closed
        var hasOpenPositions = PaperContracts.Any(p => p.Status == PaperContractStatus.Open) ||
                              PurchaseContracts.Any(p => p.Status == ContractStatus.Active) ||
                              SalesContracts.Any(p => p.Status == ContractStatus.Active);

        if (hasOpenPositions)
            throw new DomainException("Cannot close trade group with open positions");

        Status = TradeGroupStatus.Closed;
        SetUpdatedBy(closedBy);
        
        AddDomainEvent(new TradeGroupClosedEvent(Id, GroupName));
    }

    /// <summary>
    /// Get the net P&L of all positions in this group
    /// </summary>
    public decimal GetNetPnL()
    {
        decimal totalPnL = 0;

        // Paper contracts P&L
        foreach (var contract in PaperContracts.Where(p => p.Status == PaperContractStatus.Open))
        {
            totalPnL += contract.GetUnrealizedPnL();
        }

        // Add P&L from purchase and sales contracts when they have pricing
        // This would require additional business logic based on your pricing model

        return totalPnL;
    }

    /// <summary>
    /// Get the total value of all positions in this group
    /// </summary>
    public decimal GetTotalValue()
    {
        decimal totalValue = 0;

        foreach (var contract in PaperContracts.Where(p => p.Status == PaperContractStatus.Open))
        {
            totalValue += Math.Abs(contract.Quantity * contract.LotSize * (contract.CurrentPrice ?? contract.EntryPrice));
        }

        return totalValue;
    }

    /// <summary>
    /// 检查是否为价差策略 - Check if this is a spread strategy
    /// </summary>
    public bool IsSpreadStrategy()
    {
        return StrategyType == StrategyType.CalendarSpread ||
               StrategyType == StrategyType.IntercommoditySpread ||
               StrategyType == StrategyType.LocationSpread;
    }

    /// <summary>
    /// 检查是否为对冲策略 - Check if this is a hedge strategy
    /// </summary>
    public bool IsHedgeStrategy()
    {
        return StrategyType == StrategyType.BasisHedge ||
               StrategyType == StrategyType.CrossHedge;
    }

    /// <summary>
    /// 添加标签到交易组 - Add tag to trade group
    /// </summary>
    /// <param name="tagId">标签ID</param>
    /// <param name="assignedBy">分配操作执行人</param>
    /// <param name="notes">备注</param>
    public void AddTag(Guid tagId, string assignedBy = "System", string? notes = null)
    {
        if (Status == TradeGroupStatus.Closed)
            throw new DomainException("Cannot add tags to closed trade group");

        // 检查是否已经存在相同的标签
        var existingTag = TradeGroupTags.FirstOrDefault(t => t.TagId == tagId && t.IsActive);
        if (existingTag != null)
            throw new DomainException("Tag is already assigned to this trade group");

        var tradeGroupTag = new TradeGroupTag(Id, tagId, assignedBy);
        if (!string.IsNullOrEmpty(notes))
        {
            tradeGroupTag.UpdateNotes(notes, assignedBy);
        }

        TradeGroupTags.Add(tradeGroupTag);
        SetUpdatedBy(assignedBy);
    }

    /// <summary>
    /// 从交易组移除标签 - Remove tag from trade group
    /// </summary>
    /// <param name="tagId">标签ID</param>
    /// <param name="removedBy">移除操作执行人</param>
    /// <param name="reason">移除原因</param>
    public void RemoveTag(Guid tagId, string removedBy = "System", string? reason = null)
    {
        if (Status == TradeGroupStatus.Closed)
            throw new DomainException("Cannot remove tags from closed trade group");

        var existingTag = TradeGroupTags.FirstOrDefault(t => t.TagId == tagId && t.IsActive);
        if (existingTag == null)
            throw new DomainException("Tag is not assigned to this trade group");

        existingTag.Deactivate(removedBy, reason);
        SetUpdatedBy(removedBy);
    }

    /// <summary>
    /// 获取活跃的标签列表 - Get list of active tags
    /// </summary>
    /// <returns>活跃的标签ID列表</returns>
    public IEnumerable<Guid> GetActiveTagIds()
    {
        return TradeGroupTags.Where(t => t.IsActive).Select(t => t.TagId);
    }

    /// <summary>
    /// 检查是否包含指定的标签 - Check if contains specific tag
    /// </summary>
    /// <param name="tagId">标签ID</param>
    /// <returns>是否包含该标签</returns>
    public bool HasTag(Guid tagId)
    {
        return TradeGroupTags.Any(t => t.TagId == tagId && t.IsActive);
    }

    /// <summary>
    /// 获取策略相关的标签数量 - Get count of strategy-related tags
    /// </summary>
    /// <returns>策略标签数量</returns>
    public int GetStrategyTagCount()
    {
        return TradeGroupTags.Count(t => t.IsActive && t.Tag?.Category.IsTradingRelated() == true);
    }

    /// <summary>
    /// 获取风险相关的标签数量 - Get count of risk-related tags
    /// </summary>
    /// <returns>风险标签数量</returns>
    public int GetRiskTagCount()
    {
        return TradeGroupTags.Count(t => t.IsActive && t.Tag?.Category.IsRiskRelated() == true);
    }
}

/// <summary>
/// 策略类型枚举 - Strategy type enumeration
/// </summary>
public enum StrategyType
{
    /// <summary>
    /// 单向持仓 - Directional position (simple long/short)
    /// </summary>
    Directional = 1,

    /// <summary>
    /// 日历价差 - Calendar spread (same product, different months)
    /// </summary>
    CalendarSpread = 2,

    /// <summary>
    /// 跨商品价差 - Intercommodity spread (different but related products)
    /// </summary>
    IntercommoditySpread = 3,

    /// <summary>
    /// 地理价差 - Location spread (same product, different locations)
    /// </summary>
    LocationSpread = 4,

    /// <summary>
    /// 基差对冲 - Basis hedge (physical inventory vs futures)
    /// </summary>
    BasisHedge = 5,

    /// <summary>
    /// 交叉对冲 - Cross hedge (related but different products)
    /// </summary>
    CrossHedge = 6,

    /// <summary>
    /// 平均价格合约 - Average price contract
    /// </summary>
    AveragePriceContract = 7,

    /// <summary>
    /// 套利 - Arbitrage (price differences between markets)
    /// </summary>
    Arbitrage = 8,

    /// <summary>
    /// 炼制价差 - Crack spread (crude oil vs refined products)
    /// </summary>
    CrackSpread = 9,

    /// <summary>
    /// 自定义策略 - Custom strategy
    /// </summary>
    Custom = 99
}

/// <summary>
/// 交易组状态 - Trade group status
/// </summary>
public enum TradeGroupStatus
{
    /// <summary>
    /// 活跃 - Active (can add/remove positions)
    /// </summary>
    Active = 1,

    /// <summary>
    /// 已关闭 - Closed (all positions closed)
    /// </summary>
    Closed = 2,

    /// <summary>
    /// 暂停 - Suspended (temporarily inactive)
    /// </summary>
    Suspended = 3
}

/// <summary>
/// 风险水平 - Risk level enumeration
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// 低风险 - Low risk
    /// </summary>
    Low = 1,

    /// <summary>
    /// 中等风险 - Medium risk
    /// </summary>
    Medium = 2,

    /// <summary>
    /// 高风险 - High risk
    /// </summary>
    High = 3,

    /// <summary>
    /// 极高风险 - Very high risk
    /// </summary>
    VeryHigh = 4
}