using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Services;

public interface IRiskCalculationService
{
    /// <summary>
    /// Calculate comprehensive risk metrics for the portfolio
    /// </summary>
    Task<RiskCalculationResultDto> CalculatePortfolioRiskAsync(
        DateTime calculationDate, 
        int historicalDays = 252,
        bool includeStressTests = true);
    
    /// <summary>
    /// Calculate historical VaR using historical simulation method
    /// </summary>
    Task<(decimal var95, decimal var99)> CalculateHistoricalVaRAsync(
        List<PaperContract> positions,
        List<decimal> historicalReturns);

    /// <summary>
    /// Calculate Delta-Normal (Variance-Covariance) VaR using covariance matrix
    /// This is the mathematically correct multi-product VaR implementation
    /// Formula: VaR = Portfolio_Value × z-score × sqrt(w' Σ w)
    /// </summary>
    Task<(decimal var95, decimal var99)> CalculateDeltaNormalVaRAsync(
        List<PaperContract> positions,
        Dictionary<string, List<decimal>> productReturns);

    /// <summary>
    /// Calculate VaR using GARCH(1,1) model with Student's t-distribution
    /// </summary>
    Task<(decimal var95, decimal var99)> CalculateGarchVaRAsync(
        List<PaperContract> positions,
        Dictionary<string, List<decimal>> productReturns);

    /// <summary>
    /// Calculate VaR using Monte Carlo simulation
    /// </summary>
    Task<(decimal var95, decimal var99)> CalculateMonteCarloVaRAsync(
        List<PaperContract> positions,
        Dictionary<string, List<decimal>> productReturns,
        int simulations = 100000);

    /// <summary>
    /// Run stress test scenarios
    /// </summary>
    Task<List<StressTestResultDto>> RunStressTestsAsync(
        List<PaperContract> positions,
        Dictionary<string, decimal> currentPrices);

    /// <summary>
    /// Calculate Expected Shortfall (CVaR / Conditional VaR)
    /// More coherent risk measure than VaR
    /// </summary>
    Task<(decimal es95, decimal es99)> CalculateExpectedShortfallAsync(
        List<decimal> portfolioReturns,
        List<PaperContract> positions);
    
    /// <summary>
    /// Get historical price returns for products
    /// </summary>
    Task<Dictionary<string, List<decimal>>> GetHistoricalReturnsAsync(
        List<string> productTypes,
        DateTime endDate,
        int days);
    
    /// <summary>
    /// Calculate portfolio volatility
    /// </summary>
    decimal CalculatePortfolioVolatility(
        List<PaperContract> positions,
        Dictionary<string, List<decimal>> productReturns);
    
    /// <summary>
    /// Calculate maximum drawdown
    /// </summary>
    decimal CalculateMaxDrawdown(List<decimal> cumulativeReturns);
}