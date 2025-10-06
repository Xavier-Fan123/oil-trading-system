using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

/// <summary>
/// Tag Entity
/// Purpose: Provides tag-based classification management for contracts, trades, and other business objects
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
        
        // Set business rules
        SetBusinessRules();
    }

    /// <summary>
    /// Tag name (unique)
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Tag description
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Tag color (hex code)
    /// </summary>
    public string Color { get; private set; } = "#6B7280";

    /// <summary>
    /// Tag category for grouping
    /// </summary>
    public TagCategory Category { get; private set; }

    /// <summary>
    /// Tag priority for sorting and display
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Is tag active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Usage count for analytics
    /// </summary>
    public int UsageCount { get; private set; }

    /// <summary>
    /// Last used date
    /// </summary>
    public DateTime? LastUsedAt { get; private set; }

    /// <summary>
    /// Tags that cannot coexist with this tag
    /// </summary>
    public string? MutuallyExclusiveTags { get; private set; }

    /// <summary>
    /// Maximum usage limit per entity
    /// </summary>
    public int? MaxUsagePerEntity { get; private set; }

    /// <summary>
    /// Contract statuses where this tag can be applied
    /// </summary>
    public string? AllowedContractStatuses { get; private set; }

    // Navigation Properties
    public ICollection<ContractTag> ContractTags { get; private set; } = new List<ContractTag>();
    
    /// <summary>
    /// Associated trade group tags (for strategy and risk management)
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
    /// Check if tag can be applied to specified contract status
    /// </summary>
    public bool CanBeAppliedToContractStatus(ContractStatus status)
    {
        if (string.IsNullOrEmpty(AllowedContractStatuses))
            return true;

        var allowedStatuses = AllowedContractStatuses.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return allowedStatuses.Contains(status.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if this tag conflicts with other tags
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
    /// Check if maximum usage limit has been reached
    /// </summary>
    public bool HasReachedMaxUsage(int currentUsageCount)
    {
        return MaxUsagePerEntity.HasValue && currentUsageCount >= MaxUsagePerEntity.Value;
    }

    /// <summary>
    /// Set business rules
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
                // Some strategies may be mutually exclusive, e.g., directional vs spread strategies
                if (Name == "Directional")
                {
                    MutuallyExclusiveTags = "Calendar Spread,Intercommodity Spread,Location Spread,Crack Spread";
                }
                MaxUsagePerEntity = 2; // Allow combined strategies, e.g., Calendar Spread + Basis Hedge
                break;
            
            case TagCategory.PositionManagement:
                MutuallyExclusiveTags = "Long Position,Short Position,Flat";
                MaxUsagePerEntity = 1;
                break;
            
            case TagCategory.RiskControl:
                // Risk control tags can be used simultaneously
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
                MaxUsagePerEntity = 3; // Can have multiple compliance identifiers simultaneously
                break;
            
            case TagCategory.MarketCondition:
                MutuallyExclusiveTags = "Backwardation,Contango";
                MaxUsagePerEntity = 2; // Can mark both market structure and volatility simultaneously
                break;
            
            case TagCategory.ProductClass:
                MutuallyExclusiveTags = "Crude Oil,Refined Products,Natural Gas";
                MaxUsagePerEntity = 1;
                break;
        }
    }

    /// <summary>
    /// Get tag display text
    /// </summary>
    public string GetDisplayText()
    {
        return $"{Category.GetDisplayName()}: {Name}";
    }

    /// <summary>
    /// Validate tag integrity
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && 
               !string.IsNullOrWhiteSpace(Color) &&
               Category != 0;
    }
}