using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Services;
using System.Text.Json;
using System.Linq;

namespace OilTrading.Api.Middleware;

/// <summary>
/// Middleware for automatic risk checking on high-risk operations
/// </summary>
public class RiskCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RiskCheckMiddleware> _logger;
    
    // Define which endpoints require automatic risk checks
    private static readonly HashSet<string> RiskSensitiveEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/purchasecontracts",
        "/api/salescontracts", 
        "/api/contracts/approve",
        "/api/settlements/create",
        "/api/inventory/reserve"
    };

    public RiskCheckMiddleware(
        RequestDelegate next,
        ILogger<RiskCheckMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this endpoint requires risk validation
        if (ShouldPerformRiskCheck(context))
        {
            // Get the scoped service from the request services
            var riskMonitoringService = context.RequestServices.GetRequiredService<IRealTimeRiskMonitoringService>();
            var riskCheckResult = await PerformPreOperationRiskCheckAsync(context, riskMonitoringService);
            
            if (!riskCheckResult.IsApproved)
            {
                await HandleRiskViolationAsync(context, riskCheckResult);
                return; // Don't continue to the next middleware
            }

            // Add risk check result to context for downstream processing
            context.Items["RiskCheckResult"] = riskCheckResult;
        }

        // Continue to next middleware
        await _next(context);

        // Perform post-operation risk check if needed
        if (ShouldPerformPostOperationRiskCheck(context))
        {
            var riskMonitoringService = context.RequestServices.GetRequiredService<IRealTimeRiskMonitoringService>();
            await PerformPostOperationRiskCheckAsync(context, riskMonitoringService);
        }
    }

    private bool ShouldPerformRiskCheck(HttpContext context)
    {
        // Check if the request path requires risk checking
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        // Only check POST/PUT operations on risk-sensitive endpoints
        if (method != "POST" && method != "PUT")
            return false;

        return RiskSensitiveEndpoints.Any(endpoint => 
            path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private bool ShouldPerformPostOperationRiskCheck(HttpContext context)
    {
        // Perform post-operation checks on successful operations
        return context.Response.StatusCode >= 200 && 
               context.Response.StatusCode < 300 &&
               context.Items.ContainsKey("RiskCheckResult");
    }

    private async Task<RiskCheckResult> PerformPreOperationRiskCheckAsync(HttpContext context, IRealTimeRiskMonitoringService riskMonitoringService)
    {
        try
        {
            _logger.LogDebug("Performing pre-operation risk check for {Path}", context.Request.Path);

            // Extract operation details from request
            var operationDetails = await ExtractOperationDetailsAsync(context);
            
            // Get current portfolio risk metrics
            var currentRisk = await riskMonitoringService.GetRealTimeRiskAsync();
            
            // Check if operation would violate risk limits
            var riskCheck = await riskMonitoringService.CheckOperationRiskAsync(operationDetails);

            var result = new RiskCheckResult
            {
                IsApproved = riskCheck.PassesAllChecks,
                RiskScore = riskCheck.OverallRiskScore,
                Violations = riskCheck.Violations.ToList(),
                CurrentVaR = currentRisk.VaR95,
                EstimatedVaRAfterOperation = currentRisk.VaR95 + EstimateVaRImpact(operationDetails),
                CheckedAt = DateTime.UtcNow,
                OperationType = DetermineOperationType(context.Request.Path)
            };

            if (!result.IsApproved)
            {
                _logger.LogWarning("Risk check failed for {Path}. Violations: {Violations}", 
                    context.Request.Path, string.Join(", ", result.Violations));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing risk check for {Path}", context.Request.Path);
            
            // In case of error, allow operation but log the issue
            return new RiskCheckResult
            {
                IsApproved = true,
                RiskScore = 0,
                Violations = new List<string> { $"Risk check error: {ex.Message}" },
                CheckedAt = DateTime.UtcNow,
                OperationType = DetermineOperationType(context.Request.Path)
            };
        }
    }

    private async Task PerformPostOperationRiskCheckAsync(HttpContext context, IRealTimeRiskMonitoringService riskMonitoringService)
    {
        try
        {
            _logger.LogDebug("Performing post-operation risk check for {Path}", context.Request.Path);

            // Recalculate portfolio risk after the operation
            var updatedRisk = await riskMonitoringService.GetRealTimeRiskAsync();
            
            // Check if any risk limits are now breached
            var limitCheck = await riskMonitoringService.CheckRiskLimitsAsync();

            if (limitCheck.HasBreaches)
            {
                _logger.LogWarning("Post-operation risk limits breached: {Violations}", 
                    string.Join(", ", limitCheck.Breaches.Select(b => b.Description)));

                // Trigger risk alerts
                await riskMonitoringService.TriggerRiskAlertAsync(new RiskAlert
                {
                    Type = RiskAlertType.LimitExceeded,
                    Severity = RiskAlertSeverity.High,
                    Title = "Risk Limit Breach",
                    Description = "Risk limits breached after operation",
                    Data = new Dictionary<string, object>
                    {
                        ["violations"] = limitCheck.Breaches.Select(b => b.Description).ToList(),
                        ["source"] = context.Request.Path.Value ?? ""
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing post-operation risk check for {Path}", context.Request.Path);
        }
    }

    private async Task<OperationDetails> ExtractOperationDetailsAsync(HttpContext context)
    {
        // Read request body to extract operation details
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0; // Reset for downstream processing

        var operationType = DetermineOperationType(context.Request.Path);
        
        return new OperationDetails
        {
            OperationType = operationType,
            RequestPath = context.Request.Path.Value ?? "",
            RequestBody = body,
            UserId = ExtractUserId(context),
            Timestamp = DateTime.UtcNow
        };
    }

    private string DetermineOperationType(PathString path)
    {
        var pathValue = path.Value?.ToLower() ?? "";
        
        if (pathValue.Contains("purchasecontracts")) return "PurchaseContract";
        if (pathValue.Contains("salescontracts")) return "SalesContract";
        if (pathValue.Contains("approve")) return "ContractApproval";
        if (pathValue.Contains("settlements")) return "Settlement";
        if (pathValue.Contains("inventory")) return "InventoryOperation";
        
        return "Unknown";
    }

    private string ExtractUserId(HttpContext context)
    {
        // Extract user ID from JWT token or session
        return context.User?.Identity?.Name ?? "System";
    }

    private decimal EstimateVaRImpact(OperationDetails details)
    {
        // Simplified VaR impact estimation
        // In real implementation, this would be more sophisticated
        return details.OperationType switch
        {
            "PurchaseContract" => 5000m,
            "SalesContract" => 4000m,
            "Settlement" => 1000m,
            _ => 2000m
        };
    }

    private async Task HandleRiskViolationAsync(HttpContext context, RiskCheckResult riskResult)
    {
        _logger.LogWarning("Risk violation detected for {Path}. Blocking operation.", context.Request.Path);

        context.Response.StatusCode = 400; // Bad Request
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            error = "Risk limit violation",
            message = "Operation blocked due to risk limit violations",
            riskDetails = new
            {
                riskScore = riskResult.RiskScore,
                violations = riskResult.Violations,
                currentVaR = riskResult.CurrentVaR,
                estimatedVaRAfterOperation = riskResult.EstimatedVaRAfterOperation,
                checkedAt = riskResult.CheckedAt
            }
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Result of a risk check operation (extended version for middleware)
/// </summary>
public class RiskCheckResult
{
    public bool IsApproved { get; set; }
    public decimal RiskScore { get; set; }
    public List<string> Violations { get; set; } = new();
    public decimal CurrentVaR { get; set; }
    public decimal EstimatedVaRAfterOperation { get; set; }
    public DateTime CheckedAt { get; set; }
    public string OperationType { get; set; } = string.Empty;
}