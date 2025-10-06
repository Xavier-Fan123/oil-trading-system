using Microsoft.Extensions.Logging;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

/// <summary>
/// Emergency Risk Circuit Breaker - Implements immediate risk protection
/// Created by Risk Management Expert to prevent catastrophic losses
/// </summary>
public class EmergencyRiskBreaker
{
    private readonly IRiskCalculationService _riskService;
    private readonly ILogger<EmergencyRiskBreaker> _logger;
    
    // Emergency limits set by risk management expert
    private readonly decimal _emergencyVaR95Limit = 1_000_000m; // $1M emergency limit
    private readonly decimal _emergencyVaR99Limit = 2_000_000m; // $2M emergency limit
    private readonly decimal _portfolioConcentrationLimit = 0.25m; // 25% max concentration
    
    public EmergencyRiskBreaker(
        IRiskCalculationService riskService,
        ILogger<EmergencyRiskBreaker> logger)
    {
        _riskService = riskService;
        _logger = logger;
    }
    
    /// <summary>
    /// Checks emergency risk limits before allowing new trades
    /// CRITICAL: This method must be called before any trade execution
    /// </summary>
    public async Task<RiskBreakResult> CheckEmergencyLimitsAsync()
    {
        try
        {
            _logger.LogInformation("Emergency risk breaker check initiated");
            
            // Calculate current VaR
            var riskMetrics = await _riskService.CalculatePortfolioRiskAsync(DateTime.UtcNow, 252, true);
            
            var result = new RiskBreakResult { IsTradeAllowed = true };
            
            // Check VaR 95% limit
            if (riskMetrics.VaR95 > _emergencyVaR95Limit)
            {
                result.IsTradeAllowed = false;
                result.BreachType = "VaR95_EMERGENCY_BREACH";
                result.CurrentValue = riskMetrics.VaR95;
                result.LimitValue = _emergencyVaR95Limit;
                result.Message = $"EMERGENCY: VaR 95% ({riskMetrics.VaR95:C}) exceeds emergency limit ({_emergencyVaR95Limit:C})";
                
                _logger.LogCritical("🚨 EMERGENCY VaR BREACH: {CurrentVaR:C} > {Limit:C}", 
                    riskMetrics.VaR95, _emergencyVaR95Limit);
                
                await TriggerEmergencyProtocolAsync(result);
                return result;
            }
            
            // Check VaR 99% limit
            if (riskMetrics.VaR99 > _emergencyVaR99Limit)
            {
                result.IsTradeAllowed = false;
                result.BreachType = "VaR99_EMERGENCY_BREACH";
                result.CurrentValue = riskMetrics.VaR99;
                result.LimitValue = _emergencyVaR99Limit;
                result.Message = $"EMERGENCY: VaR 99% ({riskMetrics.VaR99:C}) exceeds emergency limit ({_emergencyVaR99Limit:C})";
                
                _logger.LogCritical("🚨 EMERGENCY VaR99 BREACH: {CurrentVaR:C} > {Limit:C}", 
                    riskMetrics.VaR99, _emergencyVaR99Limit);
                
                await TriggerEmergencyProtocolAsync(result);
                return result;
            }
            
            // Check concentration risk
            var maxConcentration = riskMetrics.ProductExposures.Count > 0 
                ? riskMetrics.ProductExposures.Max(x => Math.Abs(x.NetExposure))
                : 0m;
            if (maxConcentration > _portfolioConcentrationLimit)
            {
                result.IsTradeAllowed = false;
                result.BreachType = "CONCENTRATION_EMERGENCY_BREACH";
                result.CurrentValue = maxConcentration;
                result.LimitValue = _portfolioConcentrationLimit;
                result.Message = $"EMERGENCY: Portfolio concentration ({maxConcentration:P}) exceeds limit ({_portfolioConcentrationLimit:P})";
                
                _logger.LogCritical("🚨 EMERGENCY CONCENTRATION BREACH: {Concentration:P} > {Limit:P}", 
                    maxConcentration, _portfolioConcentrationLimit);
                
                await TriggerEmergencyProtocolAsync(result);
                return result;
            }
            
            _logger.LogInformation("✅ Emergency risk check passed - Trade allowed");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Emergency risk breaker check failed");
            
            // In case of system failure, default to SAFE mode (no trades)
            return new RiskBreakResult
            {
                IsTradeAllowed = false,
                BreachType = "SYSTEM_ERROR",
                Message = "Emergency risk system failure - trades blocked for safety"
            };
        }
    }
    
    /// <summary>
    /// Triggers emergency protocol when limits are breached
    /// </summary>
    private async Task TriggerEmergencyProtocolAsync(RiskBreakResult breach)
    {
        _logger.LogCritical("EMERGENCY PROTOCOL TRIGGERED: {BreachType}", breach.BreachType);

        // IMPLEMENTATION NOTE: Emergency Notification System
        // This method currently logs emergency events. For production deployment, implement:
        //
        // 1. Email Notifications:
        //    - Use IEmailService to send alerts to risk management team
        //    - Recipients: risk@company.com, cro@company.com, trading-desk-heads@company.com
        //    - Subject: "URGENT - Risk Limit Breach: {BreachType}"
        //
        // 2. SMS/Push Notifications:
        //    - Use ISmsService or IPushNotificationService for immediate alerts
        //    - Target: Risk managers, CRO, trading desk heads (on-call personnel)
        //
        // 3. Dashboard Integration:
        //    - Use ISignalRHub to push real-time alerts to risk dashboard
        //    - Display red banner alert with breach details
        //    - Auto-disable new trade entry until breach is resolved
        //
        // 4. Regulatory Reporting:
        //    - Log to IRegulatoryReportingService
        //    - Store breach event in compliance database
        //    - Generate timestamped audit trail for regulatory review
        //
        // 5. Automated Actions:
        //    - Auto-halt new trade entry via ITradeAuthorizationService
        //    - Trigger portfolio rebalancing recommendations
        //    - Initiate emergency risk committee meeting workflow
        //
        // Example implementation structure:
        // await _emailService.SendEmergencyAlertAsync(breach);
        // await _smsService.SendAlertAsync(GetEmergencyContacts(), breach.Message);
        // await _dashboardHub.BroadcastEmergencyAlert(breach);
        // await _regulatoryReportingService.LogRiskBreach(breach);
        // await _tradeAuthorizationService.HaltNewTrades("Emergency risk limit breach");

        // Current implementation: Comprehensive logging for audit trail
        _logger.LogCritical(
            "EMERGENCY BREACH DETAILS - Type: {BreachType}, Current: {CurrentValue}, Limit: {LimitValue}, Message: {Message}",
            breach.BreachType,
            breach.CurrentValue,
            breach.LimitValue,
            breach.Message);

        // Log to structured logging for monitoring systems (Prometheus/Grafana/ELK)
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "RiskLimitBreach",
            ["BreachType"] = breach.BreachType ?? "Unknown",
            ["Severity"] = "Critical",
            ["CurrentValue"] = breach.CurrentValue ?? 0m,
            ["LimitValue"] = breach.LimitValue ?? 0m,
            ["Timestamp"] = breach.CheckTime
        }))
        {
            _logger.LogCritical("Emergency risk protocol activated - all new trades blocked");
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Result of emergency risk check
/// </summary>
public class RiskBreakResult
{
    public bool IsTradeAllowed { get; set; }
    public string? BreachType { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? LimitValue { get; set; }
    public string? Message { get; set; }
    public DateTime CheckTime { get; init; } = DateTime.UtcNow;
}