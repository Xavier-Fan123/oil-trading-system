namespace OilTrading.Core.ValueObjects;

/// <summary>
/// 标签分类枚举 - Tag Category Enumeration
/// Purpose: 定义油品交易系统中业务相关的标签分类，专注于交易策略和风险管理
/// </summary>
public enum TagCategory
{
    /// <summary>
    /// 风险等级 - Risk Level (Low, Medium, High, VeryHigh)
    /// </summary>
    RiskLevel = 1,

    /// <summary>
    /// 交易策略 - Trading Strategy (对应TradeGroup中的9种策略类型)
    /// </summary>
    TradingStrategy = 2,

    /// <summary>
    /// 头寸管理 - Position Management (Long, Short, Hedge, Flat)
    /// </summary>
    PositionManagement = 3,

    /// <summary>
    /// 风险控制 - Risk Control (VaR监控, 止损, 限额管理)
    /// </summary>
    RiskControl = 4,

    /// <summary>
    /// 合规标识 - Compliance Tags (KYC, 制裁检查, 监管要求)
    /// </summary>
    Compliance = 5,

    /// <summary>
    /// 市场条件 - Market Conditions (Backwardation, Contango, Volatile)
    /// </summary>
    MarketCondition = 6,

    /// <summary>
    /// 产品分级 - Product Classification (Crude, Refined, Gas, Quality Grade)
    /// </summary>
    ProductClass = 7,

    /// <summary>
    /// 地理区域 - Geographic Region (Asia Pacific, Europe, Americas, etc.)
    /// </summary>
    Region = 8,

    /// <summary>
    /// 业务优先级 - Business Priority (Urgent, High, Normal, Low)
    /// </summary>
    Priority = 9,

    /// <summary>
    /// 客户分类 - Customer Classification (VIP, Regular, New, Problem)
    /// </summary>
    Customer = 10,

    /// <summary>
    /// 自定义分类 - Custom Category
    /// </summary>
    Custom = 99
}

/// <summary>
/// 标签分类扩展方法
/// </summary>
public static class TagCategoryExtensions
{
    /// <summary>
    /// 获取分类的显示名称
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
    /// 获取分类的描述
    /// </summary>
    public static string GetDescription(this TagCategory category)
    {
        return category switch
        {
            TagCategory.RiskLevel => "交易头寸风险等级分类",
            TagCategory.TradingStrategy => "交易策略类型标识，对应TradeGroup策略",
            TagCategory.PositionManagement => "头寸管理状态标识",
            TagCategory.RiskControl => "风险控制措施和限额管理",
            TagCategory.Compliance => "合规性检查和监管要求标识",
            TagCategory.MarketCondition => "市场状况和价格结构标识",
            TagCategory.ProductClass => "石油产品分类和质量等级",
            TagCategory.Region => "地理区域和交易市场分类",
            TagCategory.Priority => "业务处理优先级别",
            TagCategory.Customer => "客户分类和信用等级",
            TagCategory.Custom => "用户自定义分类",
            _ => "未知分类"
        };
    }

    /// <summary>
    /// 获取分类的默认颜色
    /// </summary>
    public static string GetDefaultColor(this TagCategory category)
    {
        return category switch
        {
            TagCategory.RiskLevel => "#EF4444", // Red - 风险警示
            TagCategory.TradingStrategy => "#8B5CF6", // Purple - 策略识别
            TagCategory.PositionManagement => "#10B981", // Green - 头寸状态
            TagCategory.RiskControl => "#DC2626", // Dark Red - 风控严格
            TagCategory.Compliance => "#059669", // Dark Green - 合规安全
            TagCategory.MarketCondition => "#0891B2", // Dark Cyan - 市场状态
            TagCategory.ProductClass => "#EA580C", // Dark Orange - 产品分类
            TagCategory.Region => "#2563EB", // Blue - 地理区域
            TagCategory.Priority => "#D97706", // Amber - 优先级
            TagCategory.Customer => "#DB2777", // Pink - 客户关系
            TagCategory.Custom => "#6B7280", // Gray - 自定义
            _ => "#6B7280"
        };
    }

    /// <summary>
    /// 获取预定义的标签名称
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