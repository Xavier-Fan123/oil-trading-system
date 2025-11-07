using Microsoft.AspNetCore.Mvc;
using OilTrading.Api.Services;
using OilTrading.Application.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OilTrading.Api.Controllers;

/// <summary>
/// Payment Risk Alert Management API
/// Handles creation, retrieval, filtering, and resolution of payment risk alerts
/// </summary>
[ApiController]
[Route("api/payment-risk-alerts")]
public class PaymentRiskAlertController : ControllerBase
{
    private readonly PaymentRiskAlertService _paymentRiskAlertService;
    private readonly ILogger<PaymentRiskAlertController> _logger;

    public PaymentRiskAlertController(
        PaymentRiskAlertService paymentRiskAlertService,
        ILogger<PaymentRiskAlertController> logger)
    {
        _paymentRiskAlertService = paymentRiskAlertService ?? throw new ArgumentNullException(nameof(paymentRiskAlertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all payment risk alerts with optional filtering and pagination
    /// </summary>
    /// <param name="tradingPartnerId">Filter by trading partner (optional)</param>
    /// <param name="alertType">Filter by alert type (optional)</param>
    /// <param name="severity">Filter by severity level (optional)</param>
    /// <param name="onlyUnresolved">Get only unresolved alerts (default: true)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <returns>Paginated list of payment risk alerts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PaymentRiskAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<PaymentRiskAlertDto>>> GetAlerts(
        [FromQuery] Guid? tradingPartnerId = null,
        [FromQuery] int? alertType = null,
        [FromQuery] int? severity = null,
        [FromQuery] bool onlyUnresolved = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("Fetching payment risk alerts. TradingPartnerId: {TradingPartnerId}, AlertType: {AlertType}, Severity: {Severity}, OnlyUnresolved: {OnlyUnresolved}, Page: {PageNumber}/{PageSize}",
                tradingPartnerId, alertType, severity, onlyUnresolved, pageNumber, pageSize);

            var filterRequest = new PaymentRiskAlertFilterRequest
            {
                TradingPartnerId = tradingPartnerId,
                AlertType = alertType.HasValue ? (AlertType)alertType.Value : null,
                Severity = severity.HasValue ? (AlertSeverity)severity.Value : null,
                OnlyUnresolved = onlyUnresolved,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _paymentRiskAlertService.GetAlertsAsync(filterRequest);
            _logger.LogInformation("Successfully fetched {Count} payment risk alerts", result.Items.Count());
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid filter parameters provided");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment risk alerts");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while fetching alerts" });
        }
    }

    /// <summary>
    /// Get alerts summary with aggregated statistics
    /// </summary>
    /// <returns>Summary of payment risk alerts including counts and totals</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(PaymentRiskAlertSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentRiskAlertSummaryDto>> GetAlertSummary()
    {
        try
        {
            _logger.LogInformation("Fetching payment risk alert summary");
            var summary = await _paymentRiskAlertService.GetAlertSummaryAsync();
            _logger.LogInformation("Successfully fetched alert summary. Total: {Total}, Critical: {Critical}, Warning: {Warning}",
                summary.TotalAlerts, summary.CriticalAlerts, summary.WarningAlerts);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment risk alert summary");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while fetching summary" });
        }
    }

    /// <summary>
    /// Get alerts for a specific trading partner
    /// </summary>
    /// <param name="tradingPartnerId">Trading partner ID</param>
    /// <returns>List of alerts for the specified partner</returns>
    [HttpGet("partner/{tradingPartnerId}")]
    [ProducesResponseType(typeof(List<PaymentRiskAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<PaymentRiskAlertDto>>> GetPartnerAlerts(Guid tradingPartnerId)
    {
        try
        {
            if (tradingPartnerId == Guid.Empty)
            {
                _logger.LogWarning("Invalid trading partner ID provided: {TradingPartnerId}", tradingPartnerId);
                return BadRequest(new { error = "Invalid trading partner ID" });
            }

            _logger.LogInformation("Fetching alerts for trading partner: {TradingPartnerId}", tradingPartnerId);
            var alerts = await _paymentRiskAlertService.GetPartnerAlertsAsync(tradingPartnerId);

            if (alerts == null || alerts.Count == 0)
            {
                _logger.LogInformation("No alerts found for trading partner: {TradingPartnerId}", tradingPartnerId);
                return Ok(new List<PaymentRiskAlertDto>());
            }

            _logger.LogInformation("Successfully fetched {Count} alerts for trading partner: {TradingPartnerId}",
                alerts.Count, tradingPartnerId);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching alerts for trading partner: {TradingPartnerId}", tradingPartnerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while fetching partner alerts" });
        }
    }

    /// <summary>
    /// Get a specific alert by ID
    /// </summary>
    /// <param name="alertId">Alert ID</param>
    /// <returns>Alert details</returns>
    [HttpGet("{alertId}")]
    [ProducesResponseType(typeof(PaymentRiskAlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentRiskAlertDto>> GetAlert(Guid alertId)
    {
        try
        {
            if (alertId == Guid.Empty)
            {
                _logger.LogWarning("Invalid alert ID provided: {AlertId}", alertId);
                return BadRequest(new { error = "Invalid alert ID" });
            }

            _logger.LogInformation("Fetching alert: {AlertId}", alertId);
            var alert = await _paymentRiskAlertService.GetAlertByIdAsync(alertId);

            if (alert == null)
            {
                _logger.LogWarning("Alert not found: {AlertId}", alertId);
                return NotFound(new { error = "Alert not found" });
            }

            return Ok(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching alert: {AlertId}", alertId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while fetching the alert" });
        }
    }

    /// <summary>
    /// Create a new payment risk alert
    /// </summary>
    /// <param name="createRequest">Create alert request</param>
    /// <returns>Created alert</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentRiskAlertDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentRiskAlertDto>> CreateAlert([FromBody] CreatePaymentRiskAlertRequest createRequest)
    {
        try
        {
            if (createRequest == null)
            {
                _logger.LogWarning("Create alert request is null");
                return BadRequest(new { error = "Request body is required" });
            }

            if (createRequest.TradingPartnerId == Guid.Empty)
            {
                _logger.LogWarning("Invalid trading partner ID in create request");
                return BadRequest(new { error = "Trading partner ID is required" });
            }

            _logger.LogInformation("Creating new alert for trading partner: {TradingPartnerId}, Type: {AlertType}",
                createRequest.TradingPartnerId, createRequest.AlertType);

            var alert = await _paymentRiskAlertService.CreateAlertAsync(createRequest);
            _logger.LogInformation("Successfully created alert: {AlertId}", alert.AlertId);
            return CreatedAtAction(nameof(GetAlert), new { alertId = alert.AlertId }, alert);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid arguments in create alert request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment risk alert");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while creating the alert" });
        }
    }

    /// <summary>
    /// Resolve (close) a payment risk alert
    /// </summary>
    /// <param name="alertId">Alert ID</param>
    /// <returns>Resolved alert</returns>
    [HttpPut("{alertId}/resolve")]
    [ProducesResponseType(typeof(PaymentRiskAlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentRiskAlertDto>> ResolveAlert(Guid alertId)
    {
        try
        {
            if (alertId == Guid.Empty)
            {
                _logger.LogWarning("Invalid alert ID provided: {AlertId}", alertId);
                return BadRequest(new { error = "Invalid alert ID" });
            }

            _logger.LogInformation("Resolving alert: {AlertId}", alertId);
            var alert = await _paymentRiskAlertService.ResolveAlertAsync(alertId);

            if (alert == null)
            {
                _logger.LogWarning("Alert not found for resolution: {AlertId}", alertId);
                return NotFound(new { error = "Alert not found" });
            }

            _logger.LogInformation("Successfully resolved alert: {AlertId}", alertId);
            return Ok(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert: {AlertId}", alertId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while resolving the alert" });
        }
    }

    /// <summary>
    /// Generate automatic alerts based on current trading partner exposures
    /// This operation scans all trading partners and creates alerts for those exceeding risk thresholds
    /// </summary>
    /// <returns>Summary of generated alerts</returns>
    [HttpPost("generate-automatic")]
    [ProducesResponseType(typeof(PaymentRiskAlertSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentRiskAlertSummaryDto>> GenerateAutomaticAlerts()
    {
        try
        {
            _logger.LogInformation("Starting automatic alert generation");
            await _paymentRiskAlertService.GenerateAutomaticAlertsAsync();
            var summary = await _paymentRiskAlertService.GetAlertSummaryAsync();

            _logger.LogInformation("Successfully generated automatic alerts. Total: {Total}, Critical: {Critical}, Warning: {Warning}",
                summary.TotalAlerts, summary.CriticalAlerts, summary.WarningAlerts);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating automatic payment risk alerts");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while generating alerts" });
        }
    }
}
