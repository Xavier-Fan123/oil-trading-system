namespace OilTrading.Application.DTOs;

/// <summary>
/// Main risk calculation result with multiple VaR methods and stress tests
/// </summary>
public class RiskCalculationResultDto
{
    public DateTime CalculationDate { get; set; }
    public decimal TotalPortfolioValue { get; set; }
    public decimal PortfolioValue => TotalPortfolioValue; // Alias for backward compatibility
    public int PositionCount { get; set; }
    
    // Historical VaR
    public decimal HistoricalVaR95 { get; set; }
    public decimal HistoricalVaR99 { get; set; }
    public decimal VaR95 => HistoricalVaR95; // Default to historical VaR for backward compatibility
    public decimal VaR99 => HistoricalVaR99; // Default to historical VaR for backward compatibility
    
    // GARCH VaR
    public decimal GarchVaR95 { get; set; }
    public decimal GarchVaR99 { get; set; }
    
    // Monte Carlo VaR
    public decimal McVaR95 { get; set; }
    public decimal McVaR99 { get; set; }
    
    // Stress Tests
    public List<StressTestResultDto> StressTests { get; set; } = new();
    
    // Additional Metrics
    public decimal ExpectedShortfall95 { get; set; }
    public decimal ExpectedShortfall99 { get; set; }
    public decimal PortfolioVolatility { get; set; }
    public decimal MaxDrawdown { get; set; }
    
    // Product breakdown
    public List<ProductExposureDto> ProductExposures { get; set; } = new();
}

/// <summary>
/// Individual stress test scenario result
/// </summary>
public class StressTestResultDto
{
    public string Scenario { get; set; } = string.Empty;
    public decimal PnlImpact { get; set; }
    public decimal PercentageChange { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Product-level exposure and risk metrics
/// </summary>
public class ProductExposureDto
{
    public string ProductType { get; set; } = string.Empty;
    public decimal NetExposure { get; set; }
    public decimal GrossExposure { get; set; }
    public int LongPositions { get; set; }
    public int ShortPositions { get; set; }
    public decimal VaR95 { get; set; }
    public decimal VaR99 { get; set; }
    public decimal Volatility { get; set; }
}

/// <summary>
/// Portfolio-level risk summary
/// </summary>
public class PortfolioRiskSummaryDto
{
    public DateTime AsOfDate { get; set; }
    public decimal TotalExposure { get; set; }
    public decimal NetExposure { get; set; }
    public decimal GrossExposure { get; set; }
    public int TotalPositions { get; set; }
    public decimal PortfolioVaR95 { get; set; }
    public decimal PortfolioVaR99 { get; set; }
    public decimal ConcentrationRisk { get; set; }
    public List<RiskLimitDto> RiskLimits { get; set; } = new();
    public Dictionary<string, decimal> CorrelationMatrix { get; set; } = new();
}

/// <summary>
/// Risk limits and utilization
/// </summary>
public class RiskLimitDto
{
    public string LimitType { get; set; } = string.Empty;
    public decimal LimitValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Utilization { get; set; }
    public string Status { get; set; } = string.Empty; // OK, Warning, Breach
}

/// <summary>
/// Product-specific risk metrics
/// </summary>
public class ProductRiskDto
{
    public string ProductType { get; set; } = string.Empty;
    public DateTime CalculationDate { get; set; }
    public decimal NetPosition { get; set; }
    public decimal MarketValue { get; set; }
    public decimal VaR95 { get; set; }
    public decimal VaR99 { get; set; }
    public decimal DailyVolatility { get; set; }
    public decimal AnnualizedVolatility { get; set; }
    public decimal Beta { get; set; }
    public decimal Sharpe { get; set; }
    public List<decimal> HistoricalReturns { get; set; } = new();
    public Dictionary<string, decimal> Greeks { get; set; } = new(); // Delta, Gamma, etc.
}

/// <summary>
/// VaR backtesting result
/// </summary>
public class BacktestResultDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    
    // Historical VaR performance
    public int HistoricalVaR95Breaches { get; set; }
    public decimal HistoricalVaR95BreachRate { get; set; }
    public int HistoricalVaR99Breaches { get; set; }
    public decimal HistoricalVaR99BreachRate { get; set; }
    
    // GARCH VaR performance
    public int GarchVaR95Breaches { get; set; }
    public decimal GarchVaR95BreachRate { get; set; }
    public int GarchVaR99Breaches { get; set; }
    public decimal GarchVaR99BreachRate { get; set; }
    
    // Monte Carlo VaR performance
    public int McVaR95Breaches { get; set; }
    public decimal McVaR95BreachRate { get; set; }
    public int McVaR99Breaches { get; set; }
    public decimal McVaR99BreachRate { get; set; }
    
    // Kupiec test results
    public Dictionary<string, bool> KupiecTestResults { get; set; } = new();
    public List<DailyBacktestDto> DailyResults { get; set; } = new();
}

/// <summary>
/// Daily backtesting detail
/// </summary>
public class DailyBacktestDto
{
    public DateTime Date { get; set; }
    public decimal ActualPnL { get; set; }
    public decimal HistoricalVaR95 { get; set; }
    public decimal GarchVaR95 { get; set; }
    public decimal McVaR95 { get; set; }
    public bool HistoricalBreach { get; set; }
    public bool GarchBreach { get; set; }
    public bool McBreach { get; set; }
}