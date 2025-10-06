using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Queries.Risk;
using OilTrading.Application.DTOs;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/risk")]
public class RiskController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RiskController> _logger;

    public RiskController(IMediator mediator, ILogger<RiskController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Calculate comprehensive risk metrics including VaR and stress tests
    /// </summary>
    /// <returns>Risk calculation results with multiple VaR methods and stress scenarios</returns>
    [HttpGet("calculate")]
    [ProducesResponseType(typeof(RiskCalculationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateRisk(
        [FromQuery] DateTime? calculationDate,
        [FromQuery] int? historicalDays = 252,
        [FromQuery] bool includeStressTests = true)
    {
        var query = new CalculateRiskQuery
        {
            CalculationDate = calculationDate ?? DateTime.UtcNow,
            HistoricalDays = historicalDays ?? 252,
            IncludeStressTests = includeStressTests
        };

        try
        {
            var result = await _mediator.Send(query);
            
            _logger.LogInformation(
                "Risk calculation completed. Date: {Date}, Historical VaR 95%: {HistVaR95}, GARCH VaR 95%: {GarchVaR95}",
                query.CalculationDate, result.HistoricalVaR95, result.GarchVaR95);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating risk metrics. Date: {Date}, HistoricalDays: {HistoricalDays}, IncludeStressTests: {IncludeStressTests}", 
                query.CalculationDate, query.HistoricalDays, query.IncludeStressTests);
            return BadRequest(new { error = "Failed to calculate risk metrics", details = ex.Message });
        }
    }

    /// <summary>
    /// Get portfolio risk summary
    /// </summary>
    /// <returns>Portfolio-level risk metrics and exposures</returns>
    [HttpGet("portfolio-summary")]
    [ProducesResponseType(typeof(PortfolioRiskSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPortfolioRiskSummary()
    {
        var query = new GetPortfolioRiskSummaryQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Calculate risk for specific product type
    /// </summary>
    /// <param name="productType">Product type (e.g., Brent, 380cst)</param>
    /// <returns>Product-specific risk metrics</returns>
    [HttpGet("product/{productType}")]
    [ProducesResponseType(typeof(ProductRiskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductRisk(string productType)
    {
        var query = new GetProductRiskQuery { ProductType = productType };
        
        try
        {
            var result = await _mediator.Send(query);
            
            if (result == null)
                return NotFound(new { error = $"No positions found for product {productType}" });
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating product risk for {ProductType}", productType);
            return BadRequest(new { error = "Failed to calculate product risk", details = ex.Message });
        }
    }

    /// <summary>
    /// Run historical backtesting of VaR models
    /// </summary>
    /// <returns>Backtesting results showing model accuracy</returns>
    [HttpGet("backtest")]
    [ProducesResponseType(typeof(BacktestResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunBacktest(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int lookbackDays = 252)
    {
        var query = new RunBacktestQuery
        {
            StartDate = startDate ?? DateTime.UtcNow.AddYears(-1),
            EndDate = endDate ?? DateTime.UtcNow,
            LookbackDays = lookbackDays
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get portfolio risk summary with trade groups analysis
    /// 获取包含交易组分析的投资组合风险摘要
    /// </summary>
    /// <returns>Portfolio risk with trade groups breakdown</returns>
    [HttpGet("portfolio-with-groups")]
    [ProducesResponseType(typeof(PortfolioRiskWithTradeGroupsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPortfolioRiskWithTradeGroups(
        [FromQuery] DateTime? asOfDate,
        [FromQuery] bool includeStressTests = false,
        [FromQuery] int historicalDays = 252)
    {
        try
        {
            var query = new GetPortfolioRiskSummaryWithTradeGroupsQuery
            {
                AsOfDate = asOfDate,
                IncludeStressTests = includeStressTests,
                HistoricalDays = historicalDays
            };

            var result = await _mediator.Send(query);

            _logger.LogInformation(
                "Portfolio risk with trade groups calculated. Total VaR 95%: {TotalVaR95}, Groups: {GroupCount}, Standalone: {StandaloneVaR}",
                result.TotalPortfolioRisk.TotalVaR95, 
                result.TotalPortfolioRisk.TradeGroupCount,
                result.StandaloneRisk.VaR95);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating portfolio risk with trade groups");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Compare traditional vs trade group-based risk calculations
    /// 比较传统与基于交易组的风险计算方法
    /// </summary>
    /// <returns>Risk calculation comparison showing the differences</returns>
    [HttpGet("compare-methods")]
    [ProducesResponseType(typeof(RiskMethodComparisonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompareRiskMethods()
    {
        try
        {
            // Get traditional risk calculation
            var traditionalQuery = new GetPortfolioRiskSummaryQuery();
            var traditionalResult = await _mediator.Send(traditionalQuery);

            // Get trade group-based risk calculation
            var tradeGroupQuery = new GetPortfolioRiskSummaryWithTradeGroupsQuery();
            var tradeGroupResult = await _mediator.Send(tradeGroupQuery);

            var comparison = new RiskMethodComparisonDto
            {
                AsOfDate = DateTime.UtcNow.Date,
                Traditional = new TraditionalRiskDto
                {
                    GrossExposure = traditionalResult.GrossExposure,
                    NetExposure = traditionalResult.NetExposure,
                    VaR95 = traditionalResult.PortfolioVaR95,
                    VaR99 = traditionalResult.PortfolioVaR99,
                    Method = "Sum of absolute values - ignores correlations and hedging"
                },
                TradeGroupBased = new TradeGroupBasedRiskDto
                {
                    TotalNetExposure = tradeGroupResult.TotalPortfolioRisk.TotalNetExposure,
                    TotalGrossExposure = tradeGroupResult.TotalPortfolioRisk.TotalGrossExposure,
                    TotalVaR95 = tradeGroupResult.TotalPortfolioRisk.TotalVaR95,
                    TotalVaR99 = tradeGroupResult.TotalPortfolioRisk.TotalVaR99,
                    TradeGroupCount = tradeGroupResult.TotalPortfolioRisk.TradeGroupCount,
                    CorrelationBenefit = tradeGroupResult.TotalPortfolioRisk.CorrelationBenefit,
                    Method = "Net exposure of spreads/hedges - accounts for correlations"
                },
                RiskOverstatement = traditionalResult.PortfolioVaR95 - tradeGroupResult.TotalPortfolioRisk.TotalVaR95,
                ExposureReduction = traditionalResult.GrossExposure - tradeGroupResult.TotalPortfolioRisk.TotalGrossExposure
            };

            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing risk calculation methods");
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Risk calculation methods comparison DTO
/// </summary>
public class RiskMethodComparisonDto
{
    public DateTime AsOfDate { get; set; }
    public TraditionalRiskDto Traditional { get; set; } = new();
    public TradeGroupBasedRiskDto TradeGroupBased { get; set; } = new();
    public decimal RiskOverstatement { get; set; }
    public decimal ExposureReduction { get; set; }
}

public class TraditionalRiskDto
{
    public decimal GrossExposure { get; set; }
    public decimal NetExposure { get; set; }
    public decimal VaR95 { get; set; }
    public decimal VaR99 { get; set; }
    public string Method { get; set; } = string.Empty;
}

public class TradeGroupBasedRiskDto
{
    public decimal TotalNetExposure { get; set; }
    public decimal TotalGrossExposure { get; set; }
    public decimal TotalVaR95 { get; set; }
    public decimal TotalVaR99 { get; set; }
    public int TradeGroupCount { get; set; }
    public decimal CorrelationBenefit { get; set; }
    public string Method { get; set; } = string.Empty;
}