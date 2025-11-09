using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Queries.Settlements;
using System.ComponentModel.DataAnnotations;

namespace OilTrading.Api.Controllers;

/// <summary>
/// Settlement Analytics API Controller
/// Provides endpoints for comprehensive settlement analytics, metrics, and KPI reporting
/// </summary>
[ApiController]
[Route("api/settlement-analytics")]
public class SettlementAnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SettlementAnalyticsController> _logger;

    public SettlementAnalyticsController(
        IMediator mediator,
        ILogger<SettlementAnalyticsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/settlement-analytics/analytics
    /// Retrieve comprehensive settlement analytics and statistics
    /// </summary>
    /// <param name="daysToAnalyze">Number of days to analyze (default: 30)</param>
    /// <param name="isSalesSettlement">Filter by settlement type: true=sales, false=purchase, null=all</param>
    /// <param name="currency">Filter by currency code (e.g., USD, EUR)</param>
    /// <param name="status">Filter by settlement status (e.g., Pending, Finalized)</param>
    /// <returns>Comprehensive settlement analytics with aggregated metrics</returns>
    /// <response code="200">Settlement analytics successfully calculated</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(SettlementAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSettlementAnalytics(
        [Range(1, 365, ErrorMessage = "Days to analyze must be between 1 and 365")]
        int daysToAnalyze = 30,
        bool? isSalesSettlement = null,
        string? currency = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving settlement analytics: Days={Days}, Type={Type}, Currency={Currency}, Status={Status}",
                daysToAnalyze,
                isSalesSettlement?.ToString() ?? "All",
                currency ?? "Any",
                status ?? "Any");

            var query = new GetSettlementAnalyticsQuery
            {
                DaysToAnalyze = daysToAnalyze,
                IsSalesSettlement = isSalesSettlement,
                Currency = currency,
                Status = status
            };

            var analytics = await _mediator.Send(query, cancellationToken);

            _logger.LogInformation(
                "Settlement analytics retrieved: Total={Total}, Amount={Amount}",
                analytics.TotalSettlements,
                analytics.TotalAmount);

            return Ok(analytics);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument for settlement analytics: {Message}", ex.Message);
            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement analytics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to retrieve settlement analytics" });
        }
    }

    /// <summary>
    /// GET /api/settlement-analytics/metrics
    /// Retrieve key settlement performance metrics and KPIs for dashboard
    /// </summary>
    /// <param name="daysToAnalyze">Number of days to analyze (default: 7)</param>
    /// <returns>Settlement metrics/KPIs including success rate, processing time, trends</returns>
    /// <response code="200">Settlement metrics successfully calculated</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(SettlementMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSettlementMetrics(
        [Range(1, 365, ErrorMessage = "Days to analyze must be between 1 and 365")]
        int daysToAnalyze = 7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving settlement metrics for {Days} days", daysToAnalyze);

            var query = new GetSettlementMetricsQuery
            {
                DaysToAnalyze = daysToAnalyze
            };

            var metrics = await _mediator.Send(query, cancellationToken);

            _logger.LogInformation(
                "Settlement metrics retrieved: Total={Total}, Count={Count}, SuccessRate={Rate}%",
                metrics.TotalSettlementValue,
                metrics.TotalSettlementCount,
                metrics.SuccessRate);

            return Ok(metrics);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument for settlement metrics: {Message}", ex.Message);
            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement metrics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to retrieve settlement metrics" });
        }
    }

    /// <summary>
    /// GET /api/settlement-analytics/daily-trends
    /// Retrieve daily settlement trend data for charting
    /// </summary>
    /// <param name="daysToAnalyze">Number of days of historical data (default: 30)</param>
    /// <returns>Daily settlement trends with count and amounts</returns>
    /// <response code="200">Daily trends successfully retrieved</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("daily-trends")]
    [ProducesResponseType(typeof(IEnumerable<DailySettlementTrendDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDailyTrends(
        [Range(1, 365, ErrorMessage = "Days to analyze must be between 1 and 365")]
        int daysToAnalyze = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving daily settlement trends for {Days} days", daysToAnalyze);

            var query = new GetSettlementAnalyticsQuery { DaysToAnalyze = daysToAnalyze };
            var analytics = await _mediator.Send(query, cancellationToken);

            _logger.LogInformation("Daily trends retrieved: {Count} data points", analytics.DailyTrends.Count);

            return Ok(analytics.DailyTrends);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument for daily trends: {Message}", ex.Message);
            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving daily settlement trends");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to retrieve daily settlement trends" });
        }
    }

    /// <summary>
    /// GET /api/settlement-analytics/currency-breakdown
    /// Retrieve settlement distribution by currency
    /// </summary>
    /// <param name="daysToAnalyze">Number of days to analyze (default: 30)</param>
    /// <returns>Currency-wise settlement breakdown with amounts and percentages</returns>
    /// <response code="200">Currency breakdown successfully retrieved</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("currency-breakdown")]
    [ProducesResponseType(typeof(IEnumerable<CurrencyBreakdownDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrencyBreakdown(
        [Range(1, 365, ErrorMessage = "Days to analyze must be between 1 and 365")]
        int daysToAnalyze = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving currency breakdown for {Days} days", daysToAnalyze);

            var query = new GetSettlementAnalyticsQuery { DaysToAnalyze = daysToAnalyze };
            var analytics = await _mediator.Send(query, cancellationToken);

            _logger.LogInformation("Currency breakdown retrieved: {Count} currencies",
                analytics.CurrencyBreakdown.Count);

            return Ok(analytics.CurrencyBreakdown);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument for currency breakdown: {Message}", ex.Message);
            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving currency breakdown");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to retrieve currency breakdown" });
        }
    }

    /// <summary>
    /// GET /api/settlement-analytics/status-distribution
    /// Retrieve settlement distribution by status for visualization
    /// </summary>
    /// <param name="daysToAnalyze">Number of days to analyze (default: 30)</param>
    /// <returns>Settlement count and percentage by status</returns>
    /// <response code="200">Status distribution successfully retrieved</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("status-distribution")]
    [ProducesResponseType(typeof(IEnumerable<StatusDistributionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatusDistribution(
        [Range(1, 365, ErrorMessage = "Days to analyze must be between 1 and 365")]
        int daysToAnalyze = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving status distribution for {Days} days", daysToAnalyze);

            var query = new GetSettlementAnalyticsQuery { DaysToAnalyze = daysToAnalyze };
            var analytics = await _mediator.Send(query, cancellationToken);

            _logger.LogInformation("Status distribution retrieved: {Count} statuses",
                analytics.StatusDistribution.Count);

            return Ok(analytics.StatusDistribution);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument for status distribution: {Message}", ex.Message);
            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status distribution");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to retrieve status distribution" });
        }
    }

    /// <summary>
    /// GET /api/settlement-analytics/top-partners
    /// Retrieve top trading partners by settlement volume
    /// </summary>
    /// <param name="daysToAnalyze">Number of days to analyze (default: 30)</param>
    /// <returns>Top 10 trading partners with settlement metrics</returns>
    /// <response code="200">Top partners successfully retrieved</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("top-partners")]
    [ProducesResponseType(typeof(IEnumerable<PartnerSettlementSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTopPartners(
        [Range(1, 365, ErrorMessage = "Days to analyze must be between 1 and 365")]
        int daysToAnalyze = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving top partners for {Days} days", daysToAnalyze);

            var query = new GetSettlementAnalyticsQuery { DaysToAnalyze = daysToAnalyze };
            var analytics = await _mediator.Send(query, cancellationToken);

            _logger.LogInformation("Top partners retrieved: {Count} partners",
                analytics.TopPartners.Count);

            return Ok(analytics.TopPartners);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument for top partners: {Message}", ex.Message);
            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top partners");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to retrieve top partners" });
        }
    }

    /// <summary>
    /// GET /api/settlement-analytics/summary
    /// Retrieve comprehensive settlement analytics summary for dashboard
    /// Combines analytics, metrics, and key statistics in single response
    /// </summary>
    /// <param name="daysToAnalyze">Number of days to analyze (default: 30)</param>
    /// <returns>Complete dashboard summary with all analytics and metrics</returns>
    /// <response code="200">Dashboard summary successfully retrieved</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(SettlementDashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboardSummary(
        [Range(1, 365, ErrorMessage = "Days to analyze must be between 1 and 365")]
        int daysToAnalyze = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving settlement dashboard summary for {Days} days", daysToAnalyze);

            // Fetch both analytics and metrics concurrently
            var analyticsQuery = new GetSettlementAnalyticsQuery { DaysToAnalyze = daysToAnalyze };
            var metricsQuery = new GetSettlementMetricsQuery { DaysToAnalyze = daysToAnalyze };

            var analyticsTasks = _mediator.Send(analyticsQuery, cancellationToken);
            var metricsTasks = _mediator.Send(metricsQuery, cancellationToken);

            await Task.WhenAll(analyticsTasks, metricsTasks);

            var analytics = await analyticsTasks;
            var metrics = await metricsTasks;

            var summary = new SettlementDashboardSummaryDto
            {
                Analytics = analytics,
                Metrics = metrics,
                GeneratedAt = DateTime.UtcNow,
                AnalysisPeriodDays = daysToAnalyze
            };

            _logger.LogInformation("Dashboard summary retrieved successfully");

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement dashboard summary");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to retrieve settlement dashboard summary" });
        }
    }
}

/// <summary>
/// Comprehensive dashboard summary combining analytics and metrics
/// </summary>
public class SettlementDashboardSummaryDto
{
    /// <summary>
    /// Detailed settlement analytics
    /// </summary>
    public SettlementAnalyticsDto? Analytics { get; set; }

    /// <summary>
    /// Key settlement performance metrics/KPIs
    /// </summary>
    public SettlementMetricsDto? Metrics { get; set; }

    /// <summary>
    /// Timestamp when summary was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Number of days analyzed
    /// </summary>
    public int AnalysisPeriodDays { get; set; }
}

/// <summary>
/// Standard error response format
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error message
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}
