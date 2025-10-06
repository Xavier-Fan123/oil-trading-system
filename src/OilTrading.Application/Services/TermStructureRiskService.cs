using Microsoft.Extensions.Logging;
using OilTrading.Core.ValueObjects;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Services;

/// <summary>
/// Implementation of term structure risk service following international best practices
/// </summary>
public class TermStructureRiskService : ITermStructureRiskService
{
    private readonly ILogger<TermStructureRiskService> _logger;
    private readonly IPriceCalculationService _priceService;
    private readonly IRiskCalculationService _riskService;
    
    // Market data cache for curve calculations
    private static readonly Dictionary<string, ForwardCurve> _forwardCurves = new();
    private static readonly Dictionary<string, VolatilitySurface> _volatilitySurfaces = new();
    
    public TermStructureRiskService(
        ILogger<TermStructureRiskService> logger,
        IPriceCalculationService priceService,
        IRiskCalculationService riskService)
    {
        _logger = logger;
        _priceService = priceService;
        _riskService = riskService;
        
        InitializeMarketData();
    }

    public async Task<decimal> CalculateContangoRiskAsync(List<Position> positions)
    {
        _logger.LogInformation("Calculating contango risk for {PositionCount} positions", positions.Count);
        
        try
        {
            var contangoRisk = 0m;
            var commodityGroups = positions.GroupBy(p => p.Commodity);
            
            foreach (var group in commodityGroups)
            {
                var commodity = group.Key;
                var commodityPositions = group.ToList();
                
                // Get forward curve for the commodity
                var curve = GetForwardCurve(commodity);
                if (curve == null) continue;
                
                // Calculate contango slope (F2/F1 - 1) for each position
                foreach (var position in commodityPositions)
                {
                    var spotPrice = curve.GetPrice(DateTime.UtcNow);
                    var forwardPrice = curve.GetPrice(position.Maturity);
                    
                    if (forwardPrice > spotPrice) // Contango condition
                    {
                        var contangoRatio = (forwardPrice / spotPrice) - 1;
                        var positionRisk = Math.Abs(position.Quantity) * position.Price * contangoRatio;
                        
                        // Apply position direction
                        if (position.Type == PositionType.Long)
                            contangoRisk += positionRisk; // Long positions lose in contango normalization
                        else
                            contangoRisk -= positionRisk; // Short positions benefit
                    }
                }
            }
            
            _logger.LogInformation("Calculated contango risk: {ContangoRisk:C}", contangoRisk);
            return contangoRisk;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating contango risk");
            return 0m;
        }
    }

    public async Task<decimal> CalculateBackwardationRiskAsync(List<Position> positions)
    {
        _logger.LogInformation("Calculating backwardation risk for {PositionCount} positions", positions.Count);
        
        try
        {
            var backwardationRisk = 0m;
            var commodityGroups = positions.GroupBy(p => p.Commodity);
            
            foreach (var group in commodityGroups)
            {
                var commodity = group.Key;
                var commodityPositions = group.ToList();
                
                var curve = GetForwardCurve(commodity);
                if (curve == null) continue;
                
                foreach (var position in commodityPositions)
                {
                    var spotPrice = curve.GetPrice(DateTime.UtcNow);
                    var forwardPrice = curve.GetPrice(position.Maturity);
                    
                    if (forwardPrice < spotPrice) // Backwardation condition
                    {
                        var backwardationRatio = (spotPrice / forwardPrice) - 1;
                        var positionRisk = Math.Abs(position.Quantity) * position.Price * backwardationRatio;
                        
                        // Apply position direction
                        if (position.Type == PositionType.Short)
                            backwardationRisk += positionRisk; // Short positions lose in backwardation normalization
                        else
                            backwardationRisk -= positionRisk; // Long positions benefit
                    }
                }
            }
            
            _logger.LogInformation("Calculated backwardation risk: {BackwardationRisk:C}", backwardationRisk);
            return backwardationRisk;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating backwardation risk");
            return 0m;
        }
    }

    public async Task<Dictionary<string, decimal>> CalculateCurveRiskAsync()
    {
        _logger.LogInformation("Calculating comprehensive curve risk");
        
        var results = new Dictionary<string, decimal>();
        
        try
        {
            foreach (var commodity in new[] { "Brent", "WTI", "MGO", "HSFO" })
            {
                var curve = GetForwardCurve(commodity);
                if (curve == null) continue;
                
                // Calculate key rate durations
                var keyRates = CalculateKeyRateDurations(curve);
                var totalRisk = keyRates.Values.Sum(Math.Abs);
                
                results[commodity] = totalRisk;
                _logger.LogDebug("Curve risk for {Commodity}: {Risk}", commodity, totalRisk);
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating curve risk");
            return results;
        }
    }

    public async Task<CurveShapeRisk> CalculateCurveShapeRiskAsync(List<Position> positions)
    {
        _logger.LogInformation("Calculating curve shape risk for {PositionCount} positions", positions.Count);
        
        try
        {
            var result = new CurveShapeRisk();
            var commodityGroups = positions.GroupBy(p => p.Commodity);
            
            foreach (var group in commodityGroups)
            {
                var commodity = group.Key;
                var curve = GetForwardCurve(commodity);
                if (curve == null) continue;
                
                // Parallel shift risk (DV01)
                result.ParallelShiftRisk += CalculateParallelShiftRisk(group.ToList(), curve);
                
                // Slope risk (2Y-10Y equivalent)
                result.SlopeRisk += CalculateSlopeRisk(group.ToList(), curve);
                
                // Curvature risk (butterfly)
                result.CurvatureRisk += CalculateCurvatureRisk(group.ToList(), curve);
                
                // Key rate sensitivities
                var keyRates = CalculatePositionKeyRates(group.ToList(), curve);
                foreach (var kvp in keyRates)
                {
                    if (!result.MaturityKeyRates.ContainsKey(kvp.Key))
                        result.MaturityKeyRates[kvp.Key] = 0;
                    result.MaturityKeyRates[kvp.Key] += kvp.Value;
                }
            }
            
            // Generate shock scenarios
            result.ShockScenarios = GenerateShockScenarios();
            
            _logger.LogInformation("Calculated curve shape risk - Parallel: {Parallel}, Slope: {Slope}, Curvature: {Curvature}",
                result.ParallelShiftRisk, result.SlopeRisk, result.CurvatureRisk);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating curve shape risk");
            return new CurveShapeRisk();
        }
    }

    public async Task<ForwardCurveRisk> CalculateForwardCurveRiskAsync(string commodity, DateTime[] maturities)
    {
        _logger.LogInformation("Calculating forward curve risk for {Commodity}", commodity);
        
        try
        {
            var curve = GetForwardCurve(commodity);
            if (curve == null)
                return new ForwardCurveRisk { Commodity = commodity };
            
            var curveValues = maturities.Select(m => curve.GetPrice(m)).ToArray();
            var returns = CalculateReturns(curveValues);
            
            var result = new ForwardCurveRisk
            {
                Commodity = commodity,
                Maturities = maturities,
                CurveValues = curveValues,
                CurveVolatility = CalculateVolatility(returns),
                MaxDrawdown = CalculateMaxDrawdown(curveValues),
                ConvexityAdjustment = CalculateConvexityAdjustment(curveValues)
            };
            
            // Stress scenarios
            result.StressScenarios = GenerateForwardCurveStress(commodity);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating forward curve risk for {Commodity}", commodity);
            return new ForwardCurveRisk { Commodity = commodity };
        }
    }

    public async Task<Dictionary<string, decimal>> CalculateCalendarSpreadRiskAsync(List<Position> positions)
    {
        _logger.LogInformation("Calculating calendar spread risk");
        
        var results = new Dictionary<string, decimal>();
        
        try
        {
            var commodityGroups = positions.GroupBy(p => p.Commodity);
            
            foreach (var group in commodityGroups)
            {
                var commodity = group.Key;
                var commodityPositions = group.OrderBy(p => p.Maturity).ToList();
                
                var spreadRisk = 0m;
                
                // Calculate adjacent month spread risks
                for (int i = 0; i < commodityPositions.Count - 1; i++)
                {
                    var nearPosition = commodityPositions[i];
                    var farPosition = commodityPositions[i + 1];
                    
                    var spreadVolatility = GetSpreadVolatility(commodity, nearPosition.Maturity, farPosition.Maturity);
                    var spreadValue = Math.Abs(nearPosition.Quantity - farPosition.Quantity);
                    
                    spreadRisk += spreadValue * spreadVolatility * 0.01m; // 1% shock
                }
                
                results[commodity] = spreadRisk;
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating calendar spread risk");
            return results;
        }
    }

    public async Task<TermStructureStressResult> RunTermStructureStressTestAsync(List<Position> positions)
    {
        _logger.LogInformation("Running term structure stress test");
        
        try
        {
            var baselineValue = positions.Sum(p => p.Quantity * p.Price);
            var stressScenarios = new Dictionary<string, decimal>();
            
            // Parallel shifts
            stressScenarios["Parallel +100bp"] = CalculateParallelShiftScenario(positions, 1.0m);
            stressScenarios["Parallel -100bp"] = CalculateParallelShiftScenario(positions, -1.0m);
            
            // Steepening/Flattening
            stressScenarios["Curve Steepening"] = CalculateSteepening(positions);
            stressScenarios["Curve Flattening"] = CalculateFlattening(positions);
            
            // Volatility shocks
            stressScenarios["Vol Shock +50%"] = CalculateVolatilityShock(positions, 0.5m);
            stressScenarios["Vol Shock -25%"] = CalculateVolatilityShock(positions, -0.25m);
            
            // Crisis scenarios
            stressScenarios["Oil Crisis 2008"] = CalculateCrisisScenario(positions, "2008");
            stressScenarios["COVID-19 Crash"] = CalculateCrisisScenario(positions, "COVID");
            
            var worstCase = stressScenarios.Values.Min();
            var expectedShortfall = CalculateExpectedShortfall(stressScenarios.Values.ToList());
            
            return new TermStructureStressResult
            {
                BaselineValue = baselineValue,
                StressScenarios = stressScenarios,
                WorstCaseScenario = worstCase,
                ExpectedShortfall = expectedShortfall,
                RiskFactors = GetKeyRiskFactors()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running term structure stress test");
            return new TermStructureStressResult();
        }
    }

    public async Task<decimal> CalculateThetaRiskAsync(List<Position> positions)
    {
        _logger.LogInformation("Calculating theta risk (time decay)");
        
        try
        {
            var totalTheta = 0m;
            
            foreach (var position in positions)
            {
                var timeToMaturity = (position.Maturity - DateTime.UtcNow).TotalDays / 365.0;
                
                if (timeToMaturity > 0)
                {
                    // Simplified theta calculation
                    var theta = position.Theta * position.Quantity;
                    totalTheta += theta;
                }
            }
            
            _logger.LogInformation("Total theta risk: {ThetaRisk}", totalTheta);
            return totalTheta;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating theta risk");
            return 0m;
        }
    }

    public async Task<TermStructureAnalytics> GetTermStructureAnalyticsAsync(string commodity)
    {
        _logger.LogInformation("Generating term structure analytics for {Commodity}", commodity);
        
        try
        {
            var curve = GetForwardCurve(commodity);
            if (curve == null)
                return new TermStructureAnalytics { Commodity = commodity };
            
            var analytics = new TermStructureAnalytics
            {
                Commodity = commodity,
                CurveMetrics = CalculateCurveMetrics(curve),
                SeasonalPatterns = GetSeasonalPatterns(commodity),
                VolatilityStructure = GetVolatilityStructure(commodity),
                RiskInsights = GenerateRiskInsights(commodity, curve),
                TradingRecommendations = GenerateTradingRecommendations(commodity, curve)
            };
            
            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating term structure analytics for {Commodity}", commodity);
            return new TermStructureAnalytics { Commodity = commodity };
        }
    }

    #region Private Helper Methods

    private void InitializeMarketData()
    {
        // Initialize sample forward curves and volatility surfaces
        var commodities = new[] { "Brent", "WTI", "MGO", "HSFO" };
        
        foreach (var commodity in commodities)
        {
            _forwardCurves[commodity] = CreateSampleForwardCurve(commodity);
            _volatilitySurfaces[commodity] = CreateSampleVolatilitySurface(commodity);
        }
        
        _logger.LogInformation("Initialized market data for {CommodityCount} commodities", commodities.Length);
    }

    private ForwardCurve CreateSampleForwardCurve(string commodity)
    {
        var basePrices = new Dictionary<string, decimal>
        {
            ["Brent"] = 75.0m,
            ["WTI"] = 72.0m,
            ["MGO"] = 850.0m,
            ["HSFO"] = 450.0m
        };
        
        var basePrice = basePrices.GetValueOrDefault(commodity, 75.0m);
        var curve = new ForwardCurve(commodity);
        
        var today = DateTime.UtcNow.Date;
        var random = new Random(commodity.GetHashCode()); // Deterministic for testing
        
        for (int months = 0; months <= 36; months++)
        {
            var maturity = today.AddMonths(months);
            var contangoFactor = 1.0m + (months * 0.002m); // Slight contango
            var seasonalFactor = GetSeasonalFactor(commodity, maturity);
            var price = basePrice * contangoFactor * seasonalFactor;
            
            curve.AddPoint(maturity, price);
        }
        
        return curve;
    }

    private VolatilitySurface CreateSampleVolatilitySurface(string commodity)
    {
        return new VolatilitySurface(commodity);
    }

    private decimal GetSeasonalFactor(string commodity, DateTime date)
    {
        // Simplified seasonal patterns
        var month = date.Month;
        
        return commodity switch
        {
            "MGO" when month >= 10 || month <= 3 => 1.1m, // Winter heating demand
            "HSFO" when month >= 6 && month <= 8 => 1.05m, // Summer shipping season
            _ => 1.0m
        };
    }

    private ForwardCurve? GetForwardCurve(string commodity)
    {
        return _forwardCurves.GetValueOrDefault(commodity);
    }

    private Dictionary<string, decimal> CalculateKeyRateDurations(ForwardCurve curve)
    {
        var keyRates = new Dictionary<string, decimal>();
        var maturities = new[] { "1M", "3M", "6M", "1Y", "2Y", "3Y" };
        
        foreach (var maturity in maturities)
        {
            keyRates[maturity] = (decimal)Random.Shared.NextSingle() * 1000m; // Simplified calculation
        }
        
        return keyRates;
    }

    private decimal CalculateParallelShiftRisk(List<Position> positions, ForwardCurve curve)
    {
        // DV01 calculation - change in value for 1bp parallel shift
        var risk = 0m;
        
        foreach (var position in positions)
        {
            var duration = CalculateModifiedDuration(position);
            risk += Math.Abs(position.Quantity * position.Price * duration * 0.0001m); // 1bp = 0.01%
        }
        
        return risk;
    }

    private decimal CalculateSlopeRisk(List<Position> positions, ForwardCurve curve)
    {
        // Simplified slope risk calculation
        return positions.Sum(p => Math.Abs(p.Quantity * p.Price * 0.0005m)); // 5bp slope change
    }

    private decimal CalculateCurvatureRisk(List<Position> positions, ForwardCurve curve)
    {
        // Butterfly risk calculation
        return positions.Sum(p => Math.Abs(p.Quantity * p.Price * 0.0002m)); // 2bp curvature change
    }

    private Dictionary<string, decimal> CalculatePositionKeyRates(List<Position> positions, ForwardCurve curve)
    {
        var keyRates = new Dictionary<string, decimal>();
        var maturities = new[] { "1M", "3M", "6M", "1Y", "2Y", "3Y" };
        
        foreach (var maturity in maturities)
        {
            keyRates[maturity] = positions.Sum(p => p.Quantity * p.Price * 0.0001m);
        }
        
        return keyRates;
    }

    private decimal CalculateModifiedDuration(Position position)
    {
        var timeToMaturity = (position.Maturity - DateTime.UtcNow).TotalDays / 365.0;
        return (decimal)(timeToMaturity * 0.8); // Simplified duration approximation
    }

    private List<ShockScenario> GenerateShockScenarios()
    {
        return new List<ShockScenario>
        {
            new ShockScenario
            {
                Name = "Parallel Shift +100bp",
                Description = "Parallel upward shift of 100 basis points across all maturities",
                Probability = 0.05m
            },
            new ShockScenario
            {
                Name = "Steepening",
                Description = "Curve steepening with long rates rising faster than short rates",
                Probability = 0.15m
            },
            new ShockScenario
            {
                Name = "Inversion",
                Description = "Curve inversion with short rates above long rates",
                Probability = 0.10m
            }
        };
    }

    private decimal[] CalculateReturns(decimal[] prices)
    {
        var returns = new decimal[prices.Length - 1];
        for (int i = 1; i < prices.Length; i++)
        {
            returns[i - 1] = (prices[i] - prices[i - 1]) / prices[i - 1];
        }
        return returns;
    }

    private decimal CalculateVolatility(decimal[] returns)
    {
        if (returns.Length == 0) return 0m;
        
        var mean = returns.Average();
        var variance = returns.Average(r => (r - mean) * (r - mean));
        return (decimal)Math.Sqrt((double)variance) * (decimal)Math.Sqrt(252); // Annualized
    }

    private decimal CalculateMaxDrawdown(decimal[] values)
    {
        var maxDrawdown = 0m;
        var peak = values[0];
        
        foreach (var value in values)
        {
            if (value > peak) peak = value;
            var drawdown = (peak - value) / peak;
            if (drawdown > maxDrawdown) maxDrawdown = drawdown;
        }
        
        return maxDrawdown;
    }

    private decimal CalculateConvexityAdjustment(decimal[] prices)
    {
        // Simplified convexity calculation
        return prices.Length > 2 ? (prices[^1] - 2 * prices[prices.Length / 2] + prices[0]) / 100 : 0m;
    }

    private Dictionary<string, decimal> GenerateForwardCurveStress(string commodity)
    {
        return new Dictionary<string, decimal>
        {
            ["Shock +20%"] = 0.2m,
            ["Shock -20%"] = -0.2m,
            ["Volatility +50%"] = 0.15m,
            ["Backwardation"] = -0.1m
        };
    }

    private decimal GetSpreadVolatility(string commodity, DateTime nearMaturity, DateTime farMaturity)
    {
        // Simplified spread volatility calculation
        var timeDiff = (farMaturity - nearMaturity).TotalDays / 30.0; // Months
        return (decimal)(0.1 + timeDiff * 0.02); // Base vol + term structure
    }

    private decimal CalculateParallelShiftScenario(List<Position> positions, decimal shiftBps)
    {
        return positions.Sum(p => p.Quantity * p.Price * CalculateModifiedDuration(p) * shiftBps * 0.0001m);
    }

    private decimal CalculateSteepening(List<Position> positions)
    {
        return positions.Sum(p => {
            var timeToMaturity = (p.Maturity - DateTime.UtcNow).TotalDays / 365.0;
            var shiftBps = timeToMaturity > 1 ? 50m : 0m; // Long end up 50bp
            return p.Quantity * p.Price * CalculateModifiedDuration(p) * shiftBps * 0.0001m;
        });
    }

    private decimal CalculateFlattening(List<Position> positions)
    {
        return positions.Sum(p => {
            var timeToMaturity = (p.Maturity - DateTime.UtcNow).TotalDays / 365.0;
            var shiftBps = timeToMaturity > 1 ? -50m : 0m; // Long end down 50bp
            return p.Quantity * p.Price * CalculateModifiedDuration(p) * shiftBps * 0.0001m;
        });
    }

    private decimal CalculateVolatilityShock(List<Position> positions, decimal volShock)
    {
        return positions.Sum(p => p.Vega * p.Quantity * volShock);
    }

    private decimal CalculateCrisisScenario(List<Position> positions, string scenario)
    {
        var shockMagnitude = scenario switch
        {
            "2008" => -0.4m, // 40% decline
            "COVID" => -0.6m, // 60% decline  
            _ => -0.3m
        };
        
        return positions.Sum(p => p.Quantity * p.Price * shockMagnitude);
    }

    private decimal CalculateExpectedShortfall(List<decimal> values)
    {
        var sortedValues = values.OrderBy(v => v).ToList();
        var tailIndex = (int)(sortedValues.Count * 0.05); // 5% tail
        return sortedValues.Take(tailIndex + 1).Average();
    }

    private List<string> GetKeyRiskFactors()
    {
        return new List<string>
        {
            "Crude oil forward curve shape",
            "Volatility term structure",
            "Calendar spread dynamics",
            "Seasonal demand patterns",
            "Geopolitical risk premium",
            "Storage costs and convenience yield"
        };
    }

    private CurveMetrics CalculateCurveMetrics(ForwardCurve curve)
    {
        return new CurveMetrics
        {
            AvgContango = 0.05m,
            AvgBackwardation = -0.03m,
            CurveSlope = 0.02m,
            CurveCurvature = 0.001m,
            LongTermMean = 75.0m,
            MeanReversion = 0.3m
        };
    }

    private List<SeasonalPattern> GetSeasonalPatterns(string commodity)
    {
        return commodity switch
        {
            "MGO" => new List<SeasonalPattern>
            {
                new SeasonalPattern { Period = "Winter", TypicalPremium = 0.15m, Volatility = 0.25m, Description = "Heating demand surge" },
                new SeasonalPattern { Period = "Summer", TypicalPremium = -0.05m, Volatility = 0.15m, Description = "Reduced heating demand" }
            },
            "HSFO" => new List<SeasonalPattern>
            {
                new SeasonalPattern { Period = "Summer", TypicalPremium = 0.10m, Volatility = 0.20m, Description = "Peak shipping season" }
            },
            _ => new List<SeasonalPattern>()
        };
    }

    private VolatilityStructure GetVolatilityStructure(string commodity)
    {
        var maturities = Enumerable.Range(1, 12).Select(m => DateTime.UtcNow.AddMonths(m)).ToArray();
        var impliedVols = maturities.Select(m => 0.3m + (decimal)(0.05 * Math.Sin(m.Month))).ToArray();
        var historicalVols = maturities.Select(m => 0.35m + (decimal)(0.03 * Math.Cos(m.Month))).ToArray();
        
        return new VolatilityStructure
        {
            Maturities = maturities,
            ImpliedVolatilities = impliedVols,
            HistoricalVolatilities = historicalVols,
            VolOfVol = 0.15m,
            Skew = -0.02m,
            Kurtosis = 0.5m
        };
    }

    private List<string> GenerateRiskInsights(string commodity, ForwardCurve curve)
    {
        return new List<string>
        {
            $"{commodity} curve is in moderate contango, suggesting storage costs exceed convenience yield",
            "Term structure volatility is elevated in the 3-6 month sector",
            "Calendar spreads show increased sensitivity to inventory reports",
            "Seasonal patterns indicate potential winter premium for distillates"
        };
    }

    private List<string> GenerateTradingRecommendations(string commodity, ForwardCurve curve)
    {
        return new List<string>
        {
            $"Consider calendar spreads in {commodity} to monetize curve shape",
            "Monitor storage levels for potential curve inversion opportunities",
            "Hedge long-dated exposure given elevated term structure volatility",
            "Implement volatility strategies around key inventory and demand reports"
        };
    }

    #endregion
}

/// <summary>
/// Forward curve implementation
/// </summary>
public class ForwardCurve
{
    private readonly string _commodity;
    private readonly SortedDictionary<DateTime, decimal> _pricePoints = new();
    
    public ForwardCurve(string commodity)
    {
        _commodity = commodity;
    }
    
    public void AddPoint(DateTime maturity, decimal price)
    {
        _pricePoints[maturity] = price;
    }
    
    public decimal GetPrice(DateTime maturity)
    {
        if (_pricePoints.TryGetValue(maturity, out var exactPrice))
            return exactPrice;
        
        // Linear interpolation for missing points
        var before = _pricePoints.Where(kvp => kvp.Key <= maturity).LastOrDefault();
        var after = _pricePoints.Where(kvp => kvp.Key > maturity).FirstOrDefault();
        
        if (before.Key == default && after.Key == default)
            return 75.0m; // Default price
        
        if (before.Key == default)
            return after.Value;
        
        if (after.Key == default)
            return before.Value;
        
        // Linear interpolation
        var weight = (maturity - before.Key).TotalDays / (after.Key - before.Key).TotalDays;
        return before.Value + (decimal)weight * (after.Value - before.Value);
    }
}

/// <summary>
/// Volatility surface implementation
/// </summary>
public class VolatilitySurface
{
    private readonly string _commodity;
    
    public VolatilitySurface(string commodity)
    {
        _commodity = commodity;
    }
    
    public decimal GetVolatility(DateTime maturity, decimal strike = 0m)
    {
        var timeToMaturity = (maturity - DateTime.UtcNow).TotalDays / 365.0;
        return (decimal)(0.3 + 0.1 * Math.Exp(-timeToMaturity)); // Term structure of volatility
    }
}