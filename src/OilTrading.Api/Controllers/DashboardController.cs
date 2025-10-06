using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Queries.Dashboard;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/dashboard")]
[ApiVersion("2.0")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IMediator mediator, IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _mediator = mediator;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive dashboard overview with all key metrics
    /// </summary>
    /// <returns>Dashboard overview including positions, PnL, risk metrics, and contract counts</returns>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(DashboardOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOverview()
    {
        try
        {
            var query = new GetDashboardOverviewQuery();
            var result = await _mediator.Send(query);
            
            _logger.LogInformation(
                "Dashboard overview retrieved. Total Positions: {TotalPositions}, Total Exposure: {TotalExposure}, VaR95: {VaR95}",
                result.TotalPositions, result.TotalExposure, result.VaR95);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard overview");
            return BadRequest(new { error = "Failed to retrieve dashboard overview", details = ex.Message });
        }
    }

    /// <summary>
    /// Get detailed trading metrics and statistics
    /// </summary>
    /// <param name="startDate">Start date for the analysis period</param>
    /// <param name="endDate">End date for the analysis period</param>
    /// <returns>Trading metrics including volume, trade counts, and product breakdowns</returns>
    [HttpGet("trading-metrics")]
    [ProducesResponseType(typeof(TradingMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTradingMetrics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var query = new GetTradingMetricsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };
            
            var result = await _mediator.Send(query);
            
            _logger.LogInformation(
                "Trading metrics retrieved for period {Period}. Total Trades: {TotalTrades}, Total Volume: {TotalVolume}",
                result.Period, result.TotalTrades, result.TotalVolume);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trading metrics. StartDate: {StartDate}, EndDate: {EndDate}",
                startDate, endDate);
            return BadRequest(new { error = "Failed to retrieve trading metrics", details = ex.Message });
        }
    }

    /// <summary>
    /// Get performance analytics and portfolio statistics
    /// </summary>
    /// <param name="startDate">Start date for the analysis period</param>
    /// <param name="endDate">End date for the analysis period</param>
    /// <returns>Performance metrics including PnL, Sharpe ratio, and drawdown statistics</returns>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(PerformanceAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPerformanceAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var query = new GetPerformanceAnalyticsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };
            
            var result = await _mediator.Send(query);
            
            _logger.LogInformation(
                "Performance analytics retrieved for period {Period}. Total PnL: {TotalPnL}, Sharpe Ratio: {SharpeRatio}",
                result.Period, result.TotalPnL, result.SharpeRatio);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance analytics. StartDate: {StartDate}, EndDate: {EndDate}",
                startDate, endDate);
            return BadRequest(new { error = "Failed to retrieve performance analytics", details = ex.Message });
        }
    }

    /// <summary>
    /// Get market insights and analysis
    /// </summary>
    /// <returns>Market insights including price data, volatility, correlations, and technical indicators</returns>
    [HttpGet("market-insights")]
    [ProducesResponseType(typeof(MarketInsightsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMarketInsights()
    {
        try
        {
            var query = new GetMarketInsightsQuery();
            var result = await _mediator.Send(query);
            
            _logger.LogInformation(
                "Market insights retrieved. Market Data Count: {MarketDataCount}, Last Update: {LastUpdate}",
                result.MarketDataCount, result.LastUpdate);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving market insights");
            return BadRequest(new { error = "Failed to retrieve market insights", details = ex.Message });
        }
    }

    /// <summary>
    /// Get operational status and system health
    /// </summary>
    /// <returns>Operational metrics including shipments, deliveries, contract execution status, and system health</returns>
    [HttpGet("operational-status")]
    [ProducesResponseType(typeof(OperationalStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOperationalStatus()
    {
        try
        {
            var query = new GetOperationalStatusQuery();
            var result = await _mediator.Send(query);
            
            _logger.LogInformation(
                "Operational status retrieved. Active Shipments: {ActiveShipments}, Pending Deliveries: {PendingDeliveries}",
                result.ActiveShipments, result.PendingDeliveries);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving operational status");
            return BadRequest(new { error = "Failed to retrieve operational status", details = ex.Message });
        }
    }

    /// <summary>
    /// Get active alerts and notifications
    /// </summary>
    /// <returns>List of active alerts including position limits, risk warnings, and data quality issues</returns>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(IEnumerable<AlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetActiveAlerts()
    {
        try
        {
            var result = await _dashboardService.GetActiveAlertsAsync();
            
            _logger.LogInformation("Active alerts retrieved. Alert Count: {AlertCount}", result.Count());
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active alerts");
            return BadRequest(new { error = "Failed to retrieve active alerts", details = ex.Message });
        }
    }

    /// <summary>
    /// Get key performance indicators summary
    /// </summary>
    /// <returns>KPI summary with key metrics and utilization percentages</returns>
    [HttpGet("kpis")]
    [ProducesResponseType(typeof(KpiSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetKpiSummary()
    {
        try
        {
            var result = await _dashboardService.GetKpiSummaryAsync();
            
            _logger.LogInformation(
                "KPI summary retrieved. Total Exposure: {TotalExposure}, Daily PnL: {DailyPnL}, VaR95: {VaR95}",
                result.TotalExposure, result.DailyPnL, result.VaR95);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI summary");
            return BadRequest(new { error = "Failed to retrieve KPI summary", details = ex.Message });
        }
    }
}