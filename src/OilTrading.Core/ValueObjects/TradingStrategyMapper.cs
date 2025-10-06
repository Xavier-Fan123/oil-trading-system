using OilTrading.Core.Entities;

namespace OilTrading.Core.ValueObjects;

/// <summary>
/// Trading Strategy Mapper - Maps between Tag strategy names and TradeGroup StrategyType
/// Purpose: Provides bidirectional mapping between strategy tags in Tag system and strategy types in TradeGroup
/// </summary>
public static class TradingStrategyMapper
{
    /// <summary>
    /// Tag strategy name to TradeGroup strategy type mapping
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
    /// Reverse mapping from TradeGroup strategy type to Tag strategy name
    /// </summary>
    private static readonly Dictionary<StrategyType, string> StrategyToTagMapping =
        TagToStrategyMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    /// <summary>
    /// Get corresponding TradeGroup strategy type based on Tag strategy name
    /// </summary>
    /// <param name="tagStrategyName">Strategy name in Tag</param>
    /// <returns>Corresponding StrategyType, returns null if no match</returns>
    public static StrategyType? GetStrategyTypeFromTag(string tagStrategyName)
    {
        if (string.IsNullOrWhiteSpace(tagStrategyName))
            return null;

        return TagToStrategyMapping.TryGetValue(tagStrategyName.Trim(), out var strategyType)
            ? strategyType
            : null;
    }

    /// <summary>
    /// Get corresponding Tag strategy name based on TradeGroup strategy type
    /// </summary>
    /// <param name="strategyType">Strategy type in TradeGroup</param>
    /// <returns>Corresponding Tag strategy name</returns>
    public static string GetTagNameFromStrategyType(StrategyType strategyType)
    {
        return StrategyToTagMapping.TryGetValue(strategyType, out var tagName)
            ? tagName
            : strategyType.ToString();
    }

    /// <summary>
    /// Check if the specified Tag name is a valid trading strategy tag
    /// </summary>
    /// <param name="tagName">Tag name to check</param>
    /// <returns>Whether it's a valid trading strategy tag</returns>
    public static bool IsValidTradingStrategyTag(string tagName)
    {
        return !string.IsNullOrWhiteSpace(tagName) &&
               TagToStrategyMapping.ContainsKey(tagName.Trim());
    }

    /// <summary>
    /// Get all supported trading strategy tag names
    /// </summary>
    /// <returns>List of all trading strategy tag names</returns>
    public static IEnumerable<string> GetAllTradingStrategyTagNames()
    {
        return TagToStrategyMapping.Keys;
    }

    /// <summary>
    /// Get all supported TradeGroup strategy types
    /// </summary>
    /// <returns>List of all strategy types</returns>
    public static IEnumerable<StrategyType> GetAllSupportedStrategyTypes()
    {
        return TagToStrategyMapping.Values;
    }

    /// <summary>
    /// Get suggested risk level based on strategy type
    /// </summary>
    /// <param name="strategyType">Strategy type</param>
    /// <returns>Suggested risk level</returns>
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
    /// Get suggested tag combination based on strategy type
    /// </summary>
    /// <param name="strategyType">Strategy type</param>
    /// <returns>List of suggested tag names</returns>
    public static string[] GetSuggestedTagsForStrategy(StrategyType strategyType)
    {
        var strategyTag = GetTagNameFromStrategyType(strategyType);
        var riskLevel = GetSuggestedRiskLevel(strategyType);

        var baseTags = new List<string> { strategyTag };

        // Add risk level tag
        baseTags.Add($"{riskLevel} Risk");

        // Add specific tags based on strategy type
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