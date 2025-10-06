using OilTrading.Core.ValueObjects;
using OilTrading.Core.Entities;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Services;

/// <summary>
/// Term structure risk service for managing curve risk and forward pricing risks
/// </summary>
public interface ITermStructureRiskService
{
    /// <summary>
    /// Calculate contango risk - when longer-term prices are higher than spot prices
    /// </summary>
    Task<decimal> CalculateContangoRiskAsync(List<Position> positions);
    
    /// <summary>
    /// Calculate backwardation risk - when longer-term prices are lower than spot prices
    /// </summary>
    Task<decimal> CalculateBackwardationRiskAsync(List<Position> positions);
    
    /// <summary>
    /// Calculate comprehensive curve risk across all maturities
    /// </summary>
    Task<Dictionary<string, decimal>> CalculateCurveRiskAsync();
    
    /// <summary>
    /// Calculate curve shape risk (parallel, slope, curvature shifts)
    /// </summary>
    Task<CurveShapeRisk> CalculateCurveShapeRiskAsync(List<Position> positions);
    
    /// <summary>
    /// Calculate forward curve volatility and stress scenarios
    /// </summary>
    Task<ForwardCurveRisk> CalculateForwardCurveRiskAsync(string commodity, DateTime[] maturities);
    
    /// <summary>
    /// Calculate calendar spread risks between different maturities
    /// </summary>
    Task<Dictionary<string, decimal>> CalculateCalendarSpreadRiskAsync(List<Position> positions);
    
    /// <summary>
    /// Generate term structure stress scenarios
    /// </summary>
    Task<TermStructureStressResult> RunTermStructureStressTestAsync(List<Position> positions);
    
    /// <summary>
    /// Calculate theta risk (time decay of positions)
    /// </summary>
    Task<decimal> CalculateThetaRiskAsync(List<Position> positions);
    
    /// <summary>
    /// Get term structure analytics and insights
    /// </summary>
    Task<TermStructureAnalytics> GetTermStructureAnalyticsAsync(string commodity);
}

/// <summary>
/// Curve shape risk analysis
/// </summary>
public class CurveShapeRisk
{
    public decimal ParallelShiftRisk { get; set; }
    public decimal SlopeRisk { get; set; }
    public decimal CurvatureRisk { get; set; }
    public decimal ButterflyRisk { get; set; }
    public Dictionary<string, decimal> MaturityKeyRates { get; set; } = new();
    public List<ShockScenario> ShockScenarios { get; set; } = new();
}

/// <summary>
/// Forward curve risk metrics
/// </summary>
public class ForwardCurveRisk
{
    public string Commodity { get; set; } = string.Empty;
    public decimal[] CurveValues { get; set; } = Array.Empty<decimal>();
    public DateTime[] Maturities { get; set; } = Array.Empty<DateTime>();
    public decimal CurveVolatility { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal ConvexityAdjustment { get; set; }
    public Dictionary<string, decimal> StressScenarios { get; set; } = new();
}

/// <summary>
/// Term structure stress test results
/// </summary>
public class TermStructureStressResult
{
    public decimal BaselineValue { get; set; }
    public Dictionary<string, decimal> StressScenarios { get; set; } = new();
    public decimal WorstCaseScenario { get; set; }
    public decimal ExpectedShortfall { get; set; }
    public List<string> RiskFactors { get; set; } = new();
    public DateTime CalculationTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Shock scenario for stress testing
/// </summary>
public class ShockScenario
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, decimal> MaturityShocks { get; set; } = new();
    public decimal PnLImpact { get; set; }
    public decimal Probability { get; set; }
}

/// <summary>
/// Term structure analytics
/// </summary>
public class TermStructureAnalytics
{
    public string Commodity { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    public CurveMetrics CurveMetrics { get; set; } = new();
    public List<SeasonalPattern> SeasonalPatterns { get; set; } = new();
    public VolatilityStructure VolatilityStructure { get; set; } = new();
    public List<string> RiskInsights { get; set; } = new();
    public List<string> TradingRecommendations { get; set; } = new();
}

/// <summary>
/// Curve metrics
/// </summary>
public class CurveMetrics
{
    public decimal AvgContango { get; set; }
    public decimal AvgBackwardation { get; set; }
    public decimal CurveSlope { get; set; }
    public decimal CurveCurvature { get; set; }
    public decimal LongTermMean { get; set; }
    public decimal MeanReversion { get; set; }
}

/// <summary>
/// Seasonal pattern analysis
/// </summary>
public class SeasonalPattern
{
    public string Period { get; set; } = string.Empty; // "Q1", "Summer", "Winter", etc.
    public decimal TypicalPremium { get; set; }
    public decimal Volatility { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Volatility structure across maturities
/// </summary>
public class VolatilityStructure
{
    public DateTime[] Maturities { get; set; } = Array.Empty<DateTime>();
    public decimal[] ImpliedVolatilities { get; set; } = Array.Empty<decimal>();
    public decimal[] HistoricalVolatilities { get; set; } = Array.Empty<decimal>();
    public decimal VolOfVol { get; set; }
    public decimal Skew { get; set; }
    public decimal Kurtosis { get; set; }
}

/// <summary>
/// Position definition for term structure analysis
/// </summary>
public class Position
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Commodity { get; set; } = string.Empty;
    public DateTime Maturity { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public PositionType Type { get; set; }
    public DateTime TradeDate { get; set; }
    public string Trader { get; set; } = string.Empty;
    public decimal Delta { get; set; }
    public decimal Gamma { get; set; }
    public decimal Theta { get; set; }
    public decimal Vega { get; set; }
}

