using Microsoft.EntityFrameworkCore;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;
using AlertTypeDtos = OilTrading.Application.DTOs.AlertType;
using AlertSeverityDtos = OilTrading.Application.DTOs.AlertSeverity;
using AlertTypeEntity = OilTrading.Core.Entities.AlertType;
using AlertSeverityEntity = OilTrading.Core.Entities.AlertSeverity;

namespace OilTrading.Api.Services;

/// <summary>
/// Service for managing payment risk alerts
/// Handles alert creation, retrieval, filtering, and automatic alert generation based on trading partner exposures
/// </summary>
public class PaymentRiskAlertService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<PaymentRiskAlertService> _logger;
    private readonly TradingPartnerExposureService _exposureService;

    public PaymentRiskAlertService(
        ApplicationDbContext dbContext,
        ILogger<PaymentRiskAlertService> logger,
        TradingPartnerExposureService exposureService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _exposureService = exposureService ?? throw new ArgumentNullException(nameof(exposureService));
    }

    /// <summary>
    /// Get alerts with filtering and pagination
    /// </summary>
    public async Task<PagedResult<PaymentRiskAlertDto>> GetAlertsAsync(PaymentRiskAlertFilterRequest filter)
    {
        var query = _dbContext.PaymentRiskAlerts.AsQueryable();

        // Apply filters
        if (filter.TradingPartnerId.HasValue)
            query = query.Where(a => a.TradingPartnerId == filter.TradingPartnerId);

        if (filter.AlertType.HasValue)
            query = query.Where(a => a.AlertType == (AlertTypeEntity)filter.AlertType);

        if (filter.Severity.HasValue)
            query = query.Where(a => a.Severity == (AlertSeverityEntity)filter.Severity);

        if (filter.OnlyUnresolved.HasValue && filter.OnlyUnresolved.Value)
            query = query.Where(a => !a.IsResolved);

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply sorting and pagination
        var alerts = await query
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedDate)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var dtos = alerts.Select(MapToDto).ToList();

        return new PagedResult<PaymentRiskAlertDto>(dtos, totalCount, filter.PageNumber, filter.PageSize);
    }

    /// <summary>
    /// Get summary statistics for all alerts
    /// </summary>
    public async Task<PaymentRiskAlertSummaryDto> GetAlertSummaryAsync()
    {
        var allAlerts = await _dbContext.PaymentRiskAlerts.ToListAsync();

        var summary = new PaymentRiskAlertSummaryDto
        {
            TotalAlerts = allAlerts.Count,
            CriticalAlerts = allAlerts.Count(a => a.Severity == AlertSeverityEntity.Critical && !a.IsResolved),
            WarningAlerts = allAlerts.Count(a => a.Severity == AlertSeverityEntity.Warning && !a.IsResolved),
            InfoAlerts = allAlerts.Count(a => a.Severity == AlertSeverityEntity.Info && !a.IsResolved),
            UnresolvedAlerts = allAlerts.Count(a => !a.IsResolved),
            ResolvedAlerts = allAlerts.Count(a => a.IsResolved),
            TotalAmountAtRisk = allAlerts.Where(a => !a.IsResolved).Sum(a => a.Amount),
            OverduePaymentCount = allAlerts.Count(a => a.AlertType == AlertTypeEntity.OverduePayment && !a.IsResolved),
            UpcomingDueDateCount = allAlerts.Count(a => a.AlertType == AlertTypeEntity.UpcomingDueDate && !a.IsResolved),
            CreditLimitExceededCount = allAlerts.Count(a => a.AlertType == AlertTypeEntity.CreditLimitExceeded && !a.IsResolved),
            CreditLimitApproachingCount = allAlerts.Count(a => a.AlertType == AlertTypeEntity.CreditLimitApproaching && !a.IsResolved),
            CreditExpiredCount = allAlerts.Count(a => a.AlertType == AlertTypeEntity.CreditExpired && !a.IsResolved),
            LargeOutstandingAmountCount = allAlerts.Count(a => a.AlertType == AlertTypeEntity.LargeOutstandingAmount && !a.IsResolved),
            FrequentLatePaymentCount = allAlerts.Count(a => a.AlertType == AlertTypeEntity.FrequentLatePayment && !a.IsResolved)
        };

        return summary;
    }

    /// <summary>
    /// Get alerts for a specific trading partner
    /// </summary>
    public async Task<List<PaymentRiskAlertDto>> GetPartnerAlertsAsync(Guid tradingPartnerId)
    {
        var alerts = await _dbContext.PaymentRiskAlerts
            .Where(a => a.TradingPartnerId == tradingPartnerId && !a.IsResolved)
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedDate)
            .ToListAsync();

        return alerts.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Get alert by ID
    /// </summary>
    public async Task<PaymentRiskAlertDto?> GetAlertByIdAsync(Guid alertId)
    {
        var alert = await _dbContext.PaymentRiskAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
        return alert == null ? null : MapToDto(alert);
    }

    /// <summary>
    /// Create a new payment risk alert
    /// </summary>
    public async Task<PaymentRiskAlertDto> CreateAlertAsync(CreatePaymentRiskAlertRequest request)
    {
        var partner = await _dbContext.TradingPartners.FirstOrDefaultAsync(p => p.Id == request.TradingPartnerId);
        if (partner == null)
            throw new ArgumentException($"Trading partner {request.TradingPartnerId} not found");

        var alert = new PaymentRiskAlert
        {
            TradingPartnerId = request.TradingPartnerId,
            AlertType = (AlertTypeEntity)request.AlertType,
            Severity = (AlertSeverityEntity)request.Severity,
            Title = request.Title,
            Description = request.Description,
            Amount = request.Amount,
            DueDate = request.DueDate,
            CreatedDate = DateTime.UtcNow,
            IsResolved = false
        };

        _dbContext.PaymentRiskAlerts.Add(alert);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created payment risk alert {AlertId} for partner {PartnerId}",
            alert.Id, request.TradingPartnerId);

        return MapToDto(alert);
    }

    /// <summary>
    /// Resolve (mark as handled) a payment risk alert
    /// </summary>
    public async Task<PaymentRiskAlertDto> ResolveAlertAsync(Guid alertId)
    {
        var alert = await _dbContext.PaymentRiskAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
        if (alert == null)
            throw new ArgumentException($"Alert {alertId} not found");

        alert.IsResolved = true;
        alert.ResolvedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Resolved payment risk alert {AlertId}", alertId);

        return MapToDto(alert);
    }

    /// <summary>
    /// Generate automatic alerts based on current trading partner exposures
    /// </summary>
    public async Task<int> GenerateAutomaticAlertsAsync()
    {
        _logger.LogInformation("Starting automatic alert generation");

        // Get all trading partners
        var partners = await _dbContext.TradingPartners
            .Where(p => p.IsActive)
            .ToListAsync();

        int alertsCreated = 0;

        foreach (var partner in partners)
        {
            try
            {
                // Get partner exposure
                var exposure = await _exposureService.GetPartnerExposureAsync(partner.Id);
                if (exposure == null) continue;

                // Check for credit limit exceeded
                if (exposure.IsOverLimit && exposure.OverdueApAmount > 0)
                {
                    // Check if alert already exists
                    var existingAlert = await _dbContext.PaymentRiskAlerts
                        .FirstOrDefaultAsync(a =>
                            a.TradingPartnerId == partner.Id &&
                            a.AlertType == AlertTypeEntity.CreditLimitExceeded &&
                            !a.IsResolved);

                    if (existingAlert == null)
                    {
                        var alert = new PaymentRiskAlert
                        {
                            TradingPartnerId = partner.Id,
                            AlertType = AlertTypeEntity.CreditLimitExceeded,
                            Severity = AlertSeverityEntity.Critical,
                            Title = $"Credit Limit Exceeded - {partner.CompanyName}",
                            Description = $"Trading partner {partner.CompanyName} has exceeded credit limit with overdue payments",
                            Amount = exposure.OverdueApAmount,
                            CreatedDate = DateTime.UtcNow,
                            IsResolved = false
                        };

                        _dbContext.PaymentRiskAlerts.Add(alert);
                        alertsCreated++;
                    }
                }

                // Check for credit limit approaching
                if (exposure.CreditUtilizationPercentage >= 80 && exposure.CreditUtilizationPercentage < 100)
                {
                    var existingAlert = await _dbContext.PaymentRiskAlerts
                        .FirstOrDefaultAsync(a =>
                            a.TradingPartnerId == partner.Id &&
                            a.AlertType == AlertTypeEntity.CreditLimitApproaching &&
                            !a.IsResolved);

                    if (existingAlert == null)
                    {
                        var alert = new PaymentRiskAlert
                        {
                            TradingPartnerId = partner.Id,
                            AlertType = AlertTypeEntity.CreditLimitApproaching,
                            Severity = AlertSeverityEntity.Warning,
                            Title = $"Credit Limit Approaching - {partner.CompanyName}",
                            Description = $"Trading partner {partner.CompanyName} is approaching credit limit ({exposure.CreditUtilizationPercentage:F1}% utilized)",
                            Amount = exposure.CurrentExposure,
                            CreatedDate = DateTime.UtcNow,
                            IsResolved = false
                        };

                        _dbContext.PaymentRiskAlerts.Add(alert);
                        alertsCreated++;
                    }
                }

                // Check for credit expired
                if (exposure.IsCreditExpired)
                {
                    var existingAlert = await _dbContext.PaymentRiskAlerts
                        .FirstOrDefaultAsync(a =>
                            a.TradingPartnerId == partner.Id &&
                            a.AlertType == AlertTypeEntity.CreditExpired &&
                            !a.IsResolved);

                    if (existingAlert == null)
                    {
                        var alert = new PaymentRiskAlert
                        {
                            TradingPartnerId = partner.Id,
                            AlertType = AlertTypeEntity.CreditExpired,
                            Severity = AlertSeverityEntity.Warning,
                            Title = $"Credit Limit Expired - {partner.CompanyName}",
                            Description = $"Credit limit for {partner.CompanyName} expired on {partner.CreditLimitValidUntil:yyyy-MM-dd}",
                            Amount = exposure.CurrentExposure,
                            DueDate = partner.CreditLimitValidUntil,
                            CreatedDate = DateTime.UtcNow,
                            IsResolved = false
                        };

                        _dbContext.PaymentRiskAlerts.Add(alert);
                        alertsCreated++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating automatic alerts for partner {PartnerId}", partner.Id);
            }
        }

        if (alertsCreated > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        _logger.LogInformation("Generated {AlertCount} automatic payment risk alerts", alertsCreated);
        return alertsCreated;
    }

    /// <summary>
    /// Map PaymentRiskAlert entity to DTO
    /// </summary>
    private PaymentRiskAlertDto MapToDto(PaymentRiskAlert alert)
    {
        return new PaymentRiskAlertDto
        {
            AlertId = alert.Id,
            TradingPartnerId = alert.TradingPartnerId,
            AlertType = (AlertTypeDtos)alert.AlertType,
            Severity = (AlertSeverityDtos)alert.Severity,
            Title = alert.Title,
            Description = alert.Description,
            Amount = alert.Amount,
            DueDate = alert.DueDate,
            CreatedDate = alert.CreatedDate,
            ResolvedDate = alert.ResolvedDate,
            IsResolved = alert.IsResolved,
            DaysOverdue = alert.DaysOverdue,
            DaysUntilDue = alert.DaysUntilDue
        };
    }
}
