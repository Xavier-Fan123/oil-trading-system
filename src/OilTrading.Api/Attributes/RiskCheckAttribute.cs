using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Services;
using System.Linq;
using ApplicationSystemRiskStatus = OilTrading.Application.Services.SystemRiskStatus;

namespace OilTrading.Api.Attributes;

/// <summary>
/// Risk check result class
/// </summary>
public class RiskCheckResult
{
    public bool IsSuccess { get; set; }
    public bool IsApproved { get; set; }
    public double RiskScore { get; set; }
    public string? ErrorMessage { get; set; }
    public RiskCheckLevel RiskLevel { get; set; }
    public Dictionary<string, object> RiskMetrics { get; set; } = new();
    public List<string> Violations { get; set; } = new();
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;
    public decimal CurrentVaR { get; set; }
}

/// <summary>
/// Attribute for method-level risk checking
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RiskCheckAttribute : ActionFilterAttribute
{
    private readonly RiskCheckLevel _riskLevel;
    private readonly bool _allowOverride;
    private readonly string[] _exemptRoles;

    public RiskCheckAttribute(
        RiskCheckLevel riskLevel = RiskCheckLevel.Standard,
        bool allowOverride = false,
        params string[] exemptRoles)
    {
        _riskLevel = riskLevel;
        _allowOverride = allowOverride;
        _exemptRoles = exemptRoles ?? Array.Empty<string>();
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RiskCheckAttribute>>();
        var riskMonitoringService = context.HttpContext.RequestServices.GetRequiredService<IRealTimeRiskMonitoringService>();

        try
        {
            // Check if user is exempt from risk checks
            if (IsUserExempt(context))
            {
                logger.LogDebug("User {User} is exempt from risk checks", GetUserId(context));
                await next();
                return;
            }

            // Check if risk override is requested and allowed
            var overrideRequested = context.HttpContext.Request.Headers.ContainsKey("X-Risk-Override");
            if (overrideRequested && !_allowOverride)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    success = false,
                    error = "Risk override not allowed for this operation",
                    message = "This operation does not support risk limit overrides"
                });
                return;
            }

            // Perform risk check based on level
            var riskCheckResult = await PerformRiskCheckAsync(context, riskMonitoringService, logger);

            if (!riskCheckResult.IsApproved && !overrideRequested)
            {
                // Block the operation
                context.Result = new BadRequestObjectResult(new
                {
                    success = false,
                    error = "Risk limit violation",
                    message = "Operation blocked due to risk violations",
                    riskDetails = new
                    {
                        level = _riskLevel.ToString(),
                        violations = riskCheckResult.Violations,
                        riskScore = riskCheckResult.RiskScore,
                        allowOverride = _allowOverride
                    }
                });
                return;
            }

            if (!riskCheckResult.IsApproved && overrideRequested)
            {
                // Log the override
                logger.LogWarning("Risk override used for operation {Action} by user {User}. Violations: {Violations}",
                    GetActionName(context), GetUserId(context), string.Join(", ", riskCheckResult.Violations));
                
                // Add override information to context
                context.HttpContext.Items["RiskOverrideUsed"] = true;
                context.HttpContext.Items["RiskViolations"] = riskCheckResult.Violations;
            }

            // Store risk check result for post-action processing
            context.HttpContext.Items["RiskCheckResult"] = riskCheckResult;

            // Execute the action
            var executedContext = await next();

            // Post-action risk monitoring
            await PerformPostActionRiskCheckAsync(executedContext, riskMonitoringService, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during risk check for action {Action}", GetActionName(context));
            
            // In case of error, allow operation but log the issue
            await next();
        }
    }

    private bool IsUserExempt(ActionExecutingContext context)
    {
        if (_exemptRoles.Length == 0)
            return false;

        var user = context.HttpContext.User;
        return _exemptRoles.Any(role => user.IsInRole(role));
    }

    private async Task<RiskCheckResult> PerformRiskCheckAsync(
        ActionExecutingContext context, 
        IRealTimeRiskMonitoringService riskService,
        ILogger logger)
    {
        logger.LogDebug("Performing {Level} risk check for action {Action}", _riskLevel, GetActionName(context));

        try
        {
            return _riskLevel switch
            {
                RiskCheckLevel.Basic => await PerformBasicRiskCheckAsync(context, riskService),
                RiskCheckLevel.Standard => await PerformStandardRiskCheckAsync(context, riskService),
                RiskCheckLevel.Enhanced => await PerformEnhancedRiskCheckAsync(context, riskService),
                RiskCheckLevel.Critical => await PerformCriticalRiskCheckAsync(context, riskService),
                _ => new RiskCheckResult { IsApproved = true }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error performing {Level} risk check", _riskLevel);
            
            // Return approval for basic/standard checks, denial for enhanced/critical
            return new RiskCheckResult 
            { 
                IsApproved = _riskLevel <= RiskCheckLevel.Standard,
                Violations = new List<string> { $"Risk check error: {ex.Message}" }
            };
        }
    }

    private async Task<RiskCheckResult> PerformBasicRiskCheckAsync(
        ActionExecutingContext context,
        IRealTimeRiskMonitoringService riskService)
    {
        // Basic check: just verify system is not in emergency mode
        var systemStatus = await riskService.GetSystemRiskStatusAsync();
        
        return new RiskCheckResult
        {
            IsApproved = systemStatus != ApplicationSystemRiskStatus.Emergency,
            RiskScore = systemStatus == ApplicationSystemRiskStatus.Emergency ? 100 : 0,
            Violations = systemStatus == ApplicationSystemRiskStatus.Emergency 
                ? new List<string> { "System is in emergency risk mode" }
                : new List<string>()
        };
    }

    private async Task<RiskCheckResult> PerformStandardRiskCheckAsync(
        ActionExecutingContext context,
        IRealTimeRiskMonitoringService riskService)
    {
        // Standard check: VaR limits and basic concentration
        var currentRisk = await riskService.GetRealTimeRiskAsync();
        var limitCheck = await riskService.CheckRiskLimitsAsync();

        var violations = new List<string>();
        
        // Check VaR limits
        if (currentRisk.VaR95 > 100000) // $100K limit
        {
            violations.Add($"Portfolio VaR ${currentRisk.VaR95:N0} exceeds limit $100,000");
        }

        // Add any other limit violations
        violations.AddRange(limitCheck.Breaches.Select(b => b.Description));

        return new RiskCheckResult
        {
            IsApproved = violations.Count == 0,
            RiskScore = violations.Count * 20,
            Violations = violations,
            CurrentVaR = currentRisk.VaR95
        };
    }

    private async Task<RiskCheckResult> PerformEnhancedRiskCheckAsync(
        ActionExecutingContext context,
        IRealTimeRiskMonitoringService riskService)
    {
        // Enhanced check: includes stress testing and scenario analysis
        var standardResult = await PerformStandardRiskCheckAsync(context, riskService);
        
        // Add stress test checks
        var stressResults = await riskService.RunRealTimeStressTestAsync();
        
        foreach (var stressResult in stressResults)
        {
            if (stressResult.WorstCaseLoss > 500000) // $500K stress loss limit
            {
                standardResult.Violations.Add($"Stress test {stressResult.ScenarioName} shows potential loss of ${stressResult.WorstCaseLoss:N0}");
            }
        }

        standardResult.IsApproved = standardResult.Violations.Count == 0;
        standardResult.RiskScore += stressResults.Count(r => r.WorstCaseLoss > 500000) * 30;

        return standardResult;
    }

    private async Task<RiskCheckResult> PerformCriticalRiskCheckAsync(
        ActionExecutingContext context,
        IRealTimeRiskMonitoringService riskService)
    {
        // Critical check: full comprehensive risk analysis
        var enhancedResult = await PerformEnhancedRiskCheckAsync(context, riskService);
        
        // Add Monte Carlo simulation
        var monteCarloResult = await riskService.RunMonteCarloSimulationAsync(10000);
        
        if (monteCarloResult.VaR99 > 1000000) // $1M VaR99 limit for critical operations
        {
            enhancedResult.Violations.Add($"Monte Carlo VaR99 ${monteCarloResult.VaR99:N0} exceeds critical limit $1,000,000");
        }

        // Check correlation risk
        var correlationRisk = await riskService.CalculateCorrelationRiskAsync();
        if (correlationRisk > 0.8m) // High correlation risk
        {
            enhancedResult.Violations.Add($"High portfolio correlation risk: {correlationRisk:P1}");
        }

        enhancedResult.IsApproved = enhancedResult.Violations.Count == 0;
        enhancedResult.RiskScore += (correlationRisk > 0.8m ? 50 : 0);

        return enhancedResult;
    }

    private async Task PerformPostActionRiskCheckAsync(
        ActionExecutedContext context,
        IRealTimeRiskMonitoringService riskService,
        ILogger logger)
    {
        // Only perform post-check if action was successful
        if (context.Exception != null || context.HttpContext.Response.StatusCode >= 400)
            return;

        try
        {
            logger.LogDebug("Performing post-action risk check for {Action}", GetActionName(context));

            // Recalculate portfolio risk
            var updatedRisk = await riskService.GetRealTimeRiskAsync();
            var limitCheck = await riskService.CheckRiskLimitsAsync();

            if (limitCheck.HasBreaches)
            {
                logger.LogWarning("Post-action risk limits breached for {Action}: {Violations}",
                    GetActionName(context), string.Join(", ", limitCheck.Breaches.Select(b => b.Description)));

                // Trigger alerts but don't block the response
                await riskService.TriggerRiskAlertAsync(new RiskAlert
                {
                    Type = RiskAlertType.LimitExceeded,
                    Severity = RiskAlertSeverity.High,
                    Title = "Risk Limit Breach",
                    Description = $"Risk limits breached after {GetActionName(context)}",
                    Data = new Dictionary<string, object>
                    {
                        ["violations"] = limitCheck.Breaches.Select(b => b.Description).ToList(),
                        ["source"] = GetActionName(context)
                    }
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during post-action risk check for {Action}", GetActionName(context));
        }
    }

    private string GetActionName(ActionContext context)
    {
        return $"{context.ActionDescriptor.RouteValues["controller"]}.{context.ActionDescriptor.RouteValues["action"]}";
    }

    private string GetUserId(ActionContext context)
    {
        return context.HttpContext.User?.Identity?.Name ?? "Anonymous";
    }
}

/// <summary>
/// Risk check levels with increasing stringency
/// </summary>
public enum RiskCheckLevel
{
    /// <summary>
    /// Basic system status check only
    /// </summary>
    Basic = 1,
    
    /// <summary>
    /// Standard VaR and limit checks
    /// </summary>
    Standard = 2,
    
    /// <summary>
    /// Enhanced checks including stress testing
    /// </summary>
    Enhanced = 3,
    
    /// <summary>
    /// Critical operations requiring full risk analysis
    /// </summary>
    Critical = 4
}

