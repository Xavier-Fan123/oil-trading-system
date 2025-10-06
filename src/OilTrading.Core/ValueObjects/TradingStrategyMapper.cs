using OilTrading.Core.Entities;

namespace OilTrading.Core.ValueObjects;

/// <summary>
/// 交易策略映射器 - Maps between Tag strategy names and TradeGroup StrategyType
/// Purpose: 提供Tag系统中的策略标签与TradeGroup策略类型之间的双向映射
/// </summary>
public static class TradingStrategyMapper
{
    /// <summary>
    /// Tag策略名称到TradeGroup策略类型的映射
    /// </summary>
    private static readonly Dictionary<string, StrategyType> TagToStrategyMapping = new()
    {
        { "Directional", StrategyType.Directional },
        { "Calendar Spread", StrategyType.CalendarSpread },
        { "Intercommodity Spread", StrategyType.IntercommoditySpread },
        { "Location Spread", StrategyType.LocationSpread },
        { "Basis Hedge", StrategyType.BasisHedge },
        { "Cross Hedge", StrategyType.CrossHedge },
        { "Average Price Contract", StrategyType.AveragePriceContract },
        { "Arbitrage", StrategyType.Arbitrage },
        { "Crack Spread", StrategyType.CrackSpread }
    };

    /// <summary>
    /// TradeGroup策略类型到Tag策略名称的反向映射
    /// </summary>
    private static readonly Dictionary<StrategyType, string> StrategyToTagMapping = 
        TagToStrategyMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    /// <summary>
    /// 根据Tag策略名称获取对应的TradeGroup策略类型
    /// </summary>
    /// <param name="tagStrategyName">Tag中的策略名称</param>
    /// <returns>对应的StrategyType，如果不匹配则返回null</returns>
    public static StrategyType? GetStrategyTypeFromTag(string tagStrategyName)
    {
        if (string.IsNullOrWhiteSpace(tagStrategyName))
            return null;

        return TagToStrategyMapping.TryGetValue(tagStrategyName.Trim(), out var strategyType) 
            ? strategyType 
            : null;
    }

    /// <summary>
    /// 根据TradeGroup策略类型获取对应的Tag策略名称
    /// </summary>
    /// <param name="strategyType">TradeGroup中的策略类型</param>
    /// <returns>对应的Tag策略名称</returns>
    public static string GetTagNameFromStrategyType(StrategyType strategyType)
    {
        return StrategyToTagMapping.TryGetValue(strategyType, out var tagName) 
            ? tagName 
            : strategyType.ToString();
    }

    /// <summary>
    /// 检查指定的Tag名称是否为有效的交易策略标签
    /// </summary>
    /// <param name="tagName">待检查的Tag名称</param>
    /// <returns>是否为有效的交易策略标签</returns>
    public static bool IsValidTradingStrategyTag(string tagName)
    {
        return !string.IsNullOrWhiteSpace(tagName) && 
               TagToStrategyMapping.ContainsKey(tagName.Trim());
    }

    /// <summary>
    /// 获取所有支持的交易策略标签名称
    /// </summary>
    /// <returns>所有交易策略标签名称列表</returns>
    public static IEnumerable<string> GetAllTradingStrategyTagNames()
    {
        return TagToStrategyMapping.Keys;
    }

    /// <summary>
    /// 获取所有支持的TradeGroup策略类型
    /// </summary>
    /// <returns>所有策略类型列表</returns>
    public static IEnumerable<StrategyType> GetAllSupportedStrategyTypes()
    {
        return TagToStrategyMapping.Values;
    }

    /// <summary>
    /// 根据策略类型获取相关的风险级别建议
    /// </summary>
    /// <param name="strategyType">策略类型</param>
    /// <returns>建议的风险级别</returns>
    public static RiskLevel GetSuggestedRiskLevel(StrategyType strategyType)
    {
        return strategyType switch
        {
            StrategyType.Directional => RiskLevel.High,
            StrategyType.CalendarSpread => RiskLevel.Medium,
            StrategyType.IntercommoditySpread => RiskLevel.Medium,
            StrategyType.LocationSpread => RiskLevel.Medium,
            StrategyType.BasisHedge => RiskLevel.Low,
            StrategyType.CrossHedge => RiskLevel.Medium,
            StrategyType.AveragePriceContract => RiskLevel.Low,
            StrategyType.Arbitrage => RiskLevel.Low,
            StrategyType.CrackSpread => RiskLevel.Medium,
            StrategyType.Custom => RiskLevel.Medium,
            _ => RiskLevel.Medium
        };
    }

    /// <summary>
    /// 根据策略类型获取建议的标签组合
    /// </summary>
    /// <param name="strategyType">策略类型</param>
    /// <returns>建议的标签名称列表</returns>
    public static string[] GetSuggestedTagsForStrategy(StrategyType strategyType)
    {
        var strategyTag = GetTagNameFromStrategyType(strategyType);
        var riskLevel = GetSuggestedRiskLevel(strategyType);
        
        var baseTags = new List<string> { strategyTag };
        
        // 添加风险级别标签
        baseTags.Add($"{riskLevel} Risk");
        
        // 根据策略类型添加特定的标签
        switch (strategyType)
        {
            case StrategyType.Directional:
                baseTags.AddRange(new[] { "VaR Monitor", "Position Limit" });
                break;
            case StrategyType.BasisHedge:
            case StrategyType.CrossHedge:
                baseTags.AddRange(new[] { "Hedged", "Risk Control" });
                break;
            case StrategyType.CalendarSpread:
            case StrategyType.IntercommoditySpread:
            case StrategyType.LocationSpread:
                baseTags.AddRange(new[] { "Spread Strategy", "Pairs Trading" });
                break;
            case StrategyType.Arbitrage:
                baseTags.AddRange(new[] { "Low Risk", "Market Neutral" });
                break;
        }
        
        return baseTags.ToArray();
    }
}