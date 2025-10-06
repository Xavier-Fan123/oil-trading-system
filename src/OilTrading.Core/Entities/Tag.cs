using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

/// <summary>
/// 标签实体 - Tag Entity
/// Purpose: 为合同、交易和其他业务对象提供标签化分类管理功能
/// </summary>
public class Tag : BaseEntity
{
    private Tag() { } // For EF Core

    public Tag(string name, TagCategory category, string? description = null, string? color = null, int priority = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty", nameof(name));

        Name = name.Trim();
        Category = category;
        Description = description?.Trim();
        Color = color?.Trim() ?? category.GetDefaultColor();
        Priority = priority;
        IsActive = true;
        UsageCount = 0;
        
        // 设置业务规则
        SetBusinessRules();
    }

    /// <summary>
    /// 标签名称 - Tag name (unique)
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 标签描述 - Tag description
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// 标签颜色 - Tag color (hex code)
    /// </summary>
    public string Color { get; private set; } = "#6B7280";

    /// <summary>
    /// 标签分类 - Tag category for grouping
    /// </summary>
    public TagCategory Category { get; private set; }

    /// <summary>
    /// 标签优先级 - Tag priority for sorting and display
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// 是否激活 - Is tag active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// 使用次数 - Usage count for analytics
    /// </summary>
    public int UsageCount { get; private set; }

    /// <summary>
    /// 最后使用时间 - Last used date
    /// </summary>
    public DateTime? LastUsedAt { get; private set; }

    /// <summary>
    /// 互斥标签 - Tags that cannot coexist with this tag
    /// </summary>
    public string? MutuallyExclusiveTags { get; private set; }

    /// <summary>
    /// 最大使用限制 - Maximum usage limit per entity
    /// </summary>
    public int? MaxUsagePerEntity { get; private set; }

    /// <summary>
    /// 允许的合同状态 - Contract statuses where this tag can be applied
    /// </summary>
    public string? AllowedContractStatuses { get; private set; }

    // Navigation Properties
    public ICollection<ContractTag> ContractTags { get; private set; } = new List<ContractTag>();
    
    /// <summary>
    /// 关联的交易组标签 - Associated trade group tags (for strategy and risk management)
    /// </summary>
    public ICollection<TradeGroupTag> TradeGroupTags { get; private set; } = new List<TradeGroupTag>();

    // Business Methods
    public void UpdateDetails(string? description = null, string? color = null, int? priority = null)
    {
        if (description != null)
            Description = description.Trim();
        
        if (!string.IsNullOrWhiteSpace(color))
            Color = color.Trim();
        
        if (priority.HasValue)
            Priority = priority.Value;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void IncrementUsage()
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
    }

    public void DecrementUsage()
    {
        if (UsageCount > 0)
            UsageCount--;
    }

    public void UpdateUsageCount(int newCount)
    {
        UsageCount = Math.Max(0, newCount);
        if (newCount > 0)
            LastUsedAt = DateTime.UtcNow;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Tag name cannot be empty", nameof(newName));

        Name = newName.Trim();
    }

    /// <summary>
    /// 检查标签是否可以应用于指定的合同状态
    /// </summary>
    public bool CanBeAppliedToContractStatus(ContractStatus status)
    {
        if (string.IsNullOrEmpty(AllowedContractStatuses))
            return true;

        var allowedStatuses = AllowedContractStatuses.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return allowedStatuses.Contains(status.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 检查是否与其他标签互斥
    /// </summary>
    public bool IsConflictWith(IEnumerable<string> existingTagNames)
    {
        if (string.IsNullOrEmpty(MutuallyExclusiveTags))
            return false;

        var exclusiveTags = MutuallyExclusiveTags.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return existingTagNames.Any(tagName => 
            exclusiveTags.Contains(tagName.Trim(), StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 检查是否达到最大使用限制
    /// </summary>
    public bool HasReachedMaxUsage(int currentUsageCount)
    {
        return MaxUsagePerEntity.HasValue && currentUsageCount >= MaxUsagePerEntity.Value;
    }

    /// <summary>
    /// 设置业务规则
    /// </summary>
    private void SetBusinessRules()
    {
        switch (Category)
        {
            case TagCategory.RiskLevel:
                MutuallyExclusiveTags = "Low Risk,Medium Risk,High Risk,Very High Risk";
                MaxUsagePerEntity = 1;
                break;
            
            case TagCategory.TradingStrategy:
                // 某些策略可能互斥，例如单向策略与价差策略
                if (Name == "Directional")
                {
                    MutuallyExclusiveTags = "Calendar Spread,Intercommodity Spread,Location Spread,Crack Spread";
                }
                MaxUsagePerEntity = 2; // 允许组合策略，如 Calendar Spread + Basis Hedge
                break;
            
            case TagCategory.PositionManagement:
                MutuallyExclusiveTags = "Long Position,Short Position,Flat";
                MaxUsagePerEntity = 1;
                break;
            
            case TagCategory.RiskControl:
                // 风控标签可以多个同时使用
                MaxUsagePerEntity = 5;
                break;
            
            case TagCategory.Priority:
                MutuallyExclusiveTags = "Urgent,High,Normal,Low";
                MaxUsagePerEntity = 1;
                break;
            
            case TagCategory.Compliance:
                if (Name == "Restricted" || Name == "Under Review")
                {
                    AllowedContractStatuses = "Draft,PendingApproval";
                }
                MaxUsagePerEntity = 3; // 可以同时有多个合规标识
                break;
            
            case TagCategory.MarketCondition:
                MutuallyExclusiveTags = "Backwardation,Contango";
                MaxUsagePerEntity = 2; // 可以同时标记市场结构和波动性
                break;
            
            case TagCategory.ProductClass:
                MutuallyExclusiveTags = "Crude Oil,Refined Products,Natural Gas";
                MaxUsagePerEntity = 1;
                break;
        }
    }

    /// <summary>
    /// 获取标签的显示文本
    /// </summary>
    public string GetDisplayText()
    {
        return $"{Category.GetDisplayName()}: {Name}";
    }

    /// <summary>
    /// 验证标签完整性
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && 
               !string.IsNullOrWhiteSpace(Color) &&
               Category != 0;
    }
}