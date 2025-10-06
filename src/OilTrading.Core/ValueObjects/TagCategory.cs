namespace OilTrading.Core.ValueObjects;

/// <summary>
/// Tag Category Enumeration
/// Purpose: Defines business-related tag categories in the oil trading system, focusing on trading strategies and risk management
/// </summary>
public enum TagCategory
{
    /// <summary>
    /// Risk Level (Low, Medium, High, VeryHigh)
    /// </summary>
    RiskLevel = 1,

    /// <summary>
    /// Trading Strategy (Corresponds to 9 strategy types in TradeGroup)
    /// </summary>
    TradingStrategy = 2,

    /// <summary>
    /// Position Management (Long, Short, Hedge, Flat)
    /// </summary>
    PositionManagement = 3,

    /// <summary>
    /// Risk Control (VaR monitoring, stop loss, limit management)
    /// </summary>
    RiskControl = 4,

    /// <summary>
    /// Compliance Tags (KYC, sanctions check, regulatory requirements)
    /// </summary>
    Compliance = 5,

    /// <summary>
    /// Market Conditions (Backwardation, Contango, Volatile)
    /// </summary>
    MarketCondition = 6,

    /// <summary>
    /// Product Classification (Crude, Refined, Gas, Quality Grade)
    /// </summary>
    ProductClass = 7,

    /// <summary>
    /// Geographic Region (Asia Pacific, Europe, Americas, etc.)
    /// </summary>
    Region = 8,

    /// <summary>
    /// Business Priority (Urgent, High, Normal, Low)
    /// </summary>
    Priority = 9,

    /// <summary>
    /// Customer Classification (VIP, Regular, New, Problem)
    /// </summary>
    Customer = 10,

    /// <summary>
    /// Custom Category
    /// </summary>
    Custom = 99
}

/// <summary>
/// Tag Category Extension Methods
/// </summary>
public static class TagCategoryExtensions
{
    /// <summary>
    /// Get display name of the category
    /// </summary>
    public static string GetDisplayName(this TagCategory category)
    {
        return category switch
        {
            TagCategory.RiskLevel => "Risk Level",
            TagCategory.TradingStrategy => "Trading Strategy",
            TagCategory.PositionManagement => "Position Management",
            TagCategory.RiskControl => "Risk Control",
            TagCategory.Compliance => "Compliance",
            TagCategory.MarketCondition => "Market Condition",
            TagCategory.ProductClass => "Product Classification",
            TagCategory.Region => "Geographic Region",
            TagCategory.Priority => "Business Priority",
            TagCategory.Customer => "Customer Classification",
            TagCategory.Custom => "Custom",
            _ => category.ToString()
        };
    }

    /// <summary>
    /// Get description of the category
    /// </summary>
    public static string GetDescription(this TagCategory category)
    {
        return category switch
        {
            TagCategory.RiskLevel => "Trading position risk level classification",
            TagCategory.TradingStrategy => "Trading strategy type identifier, corresponds to TradeGroup strategies",
            TagCategory.PositionManagement => "Position management status identifier",
            TagCategory.RiskControl => "Risk control measures and limit management",
            TagCategory.Compliance => "Compliance checks and regulatory requirement identifiers",
            TagCategory.MarketCondition => "Market conditions and price structure identifiers",
            TagCategory.ProductClass => "Petroleum product classification and quality grade",
            TagCategory.Region => "Geographic region and trading market classification",
            TagCategory.Priority => "Business processing priority level",
            TagCategory.Customer => "Customer classification and credit rating",
            TagCategory.Custom => "User-defined classification",
            _ => "Unknown category"
        };
    }

    /// <summary>
    /// Get default color of the category
    /// </summary>
    public static string GetDefaultColor(this TagCategory category)
    {
        return category switch
        {
            TagCategory.RiskLevel => "#EF4444", // Red - Risk warning
            TagCategory.TradingStrategy => "#8B5CF6", // Purple - Strategy identification
            TagCategory.PositionManagement => "#10B981", // Green - Position status
            TagCategory.RiskControl => "#DC2626", // Dark Red - Strict risk control
            TagCategory.Compliance => "#059669", // Dark Green - Compliance safety
            TagCategory.MarketCondition => "#0891B2", // Dark Cyan - Market status
            TagCategory.ProductClass => "#EA580C", // Dark Orange - Product classification
            TagCategory.Region => "#2563EB", // Blue - Geographic region
            TagCategory.Priority => "#D97706", // Amber - Priority
            TagCategory.Customer => "#DB2777", // Pink - Customer relationship
            TagCategory.Custom => "#6B7280", // Gray - Custom
            _ => "#6B7280"
        };
    }

    /// <summary>
    /// Get predefined tag names for the category
    /// </summary>
    public static string[] GetPredefinedTags(this TagCategory category)
    {
        return category switch
        {
            TagCategory.RiskLevel => new[] { "Low Risk", "Medium Risk", "High Risk", "Very High Risk" },
            TagCategory.TradingStrategy => new[] { 
                "Directional", "Calendar Spread", "Intercommodity Spread", "Location Spread", 
                "Basis Hedge", "Cross Hedge", "Average Price Contract", "Arbitrage", "Crack Spread" 
            },
            TagCategory.PositionManagement => new[] { "Long Position", "Short Position", "Hedged", "Flat", "Covered", "Naked" },
            TagCategory.RiskControl => new[] { "VaR Monitor", "Stop Loss", "Position Limit", "Credit Limit", "Concentration Risk", "Stress Test" },
            TagCategory.Compliance => new[] { "KYC Verified", "Sanctions Cleared", "Under Review", "Restricted", "OFAC Check", "AML Verified" },
            TagCategory.MarketCondition => new[] { "Backwardation", "Contango", "Volatile", "Stable", "Trending", "Range-bound" },
            TagCategory.ProductClass => new[] { "Crude Oil", "Refined Products", "Natural Gas", "Premium Grade", "Standard Grade", "Off-Spec" },
            TagCategory.Region => new[] { "Asia Pacific", "Europe", "Americas", "Middle East", "Africa", "North Sea", "Gulf Coast" },
            TagCategory.Priority => new[] { "Urgent", "High", "Normal", "Low" },
            TagCategory.Customer => new[] { "VIP Client", "Regular Client", "New Customer", "Problem Account", "Credit Watch", "Preferred" },
            TagCategory.Custom => Array.Empty<string>(),
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// 检查是否为交易策略相关的分类
    /// </summary>
    public static bool IsTradingRelated(this TagCategory category)
    {
        return category == TagCategory.TradingStrategy ||
               category == TagCategory.PositionManagement ||
               category == TagCategory.RiskControl ||
               category == TagCategory.MarketCondition;
    }

    /// <summary>
    /// 检查是否为风险管理相关的分类
    /// </summary>
    public static bool IsRiskRelated(this TagCategory category)
    {
        return category == TagCategory.RiskLevel ||
               category == TagCategory.RiskControl ||
               category == TagCategory.Compliance;
    }
}