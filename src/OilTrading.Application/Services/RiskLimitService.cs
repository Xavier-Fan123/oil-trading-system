using OilTrading.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace OilTrading.Application.Services;

public class RiskLimitService : IRiskLimitService
{
    private readonly ILogger<RiskLimitService> _logger;
    
    // Default risk limits
    private readonly Dictionary<string, decimal> _defaultLimits = new()
    {
        { "SingleContractLimit", 10000000 }, // $10M per contract
        { "DailyVaRLimit", 5000000 },        // $5M daily VaR
        { "TotalExposureLimit", 100000000 }, // $100M total exposure
        { "ProductConcentrationLimit", 0.4m }, // 40% concentration per product
        { "CounterpartyLimit", 20000000 }    // $20M per counterparty
    };

    public RiskLimitService(ILogger<RiskLimitService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> CheckRiskLimitsAsync(Guid contractId, decimal contractValue)
    {
        var violations = await GetRiskLimitViolationsAsync(contractId, contractValue);
        return !violations.Any();
    }

    public async Task<List<RiskLimitViolation>> GetRiskLimitViolationsAsync(Guid contractId, decimal contractValue)
    {
        var violations = new List<RiskLimitViolation>();
        
        // Check single contract limit
        if (contractValue > _defaultLimits["SingleContractLimit"])
        {
            violations.Add(new RiskLimitViolation
            {
                LimitName = "Single Contract Limit",
                LimitType = "Absolute",
                CurrentValue = contractValue,
                LimitValue = _defaultLimits["SingleContractLimit"],
                ExcessAmount = contractValue - _defaultLimits["SingleContractLimit"],
                Severity = contractValue > _defaultLimits["SingleContractLimit"] * 1.5m ? "Critical" : "High",
                Description = $"Contract value ${contractValue:N0} exceeds single contract limit of ${_defaultLimits["SingleContractLimit"]:N0}"
            });
        }
        
        // In a real implementation, would check:
        // - Total portfolio exposure
        // - Product concentration
        // - Counterparty exposure
        // - VaR limits
        // - Regulatory limits
        
        _logger.LogInformation("Risk limit check for contract {ContractId}: {ViolationCount} violations found",
            contractId, violations.Count);
        
        return violations;
    }

    public async Task<RiskLimitCheckResult> ValidateRiskLimitsAsync(RiskLimitRequest request)
    {
        var violations = new List<RiskLimitViolation>();
        var totalExposure = request.ExposureAmount; // In real implementation, would calculate total
        
        // Check various risk limits
        if (request.ExposureAmount > _defaultLimits["SingleContractLimit"])
        {
            violations.Add(new RiskLimitViolation
            {
                LimitName = "Exposure Limit",
                LimitType = "Absolute",
                CurrentValue = request.ExposureAmount,
                LimitValue = _defaultLimits["SingleContractLimit"],
                ExcessAmount = request.ExposureAmount - _defaultLimits["SingleContractLimit"],
                Severity = "High",
                Description = $"Exposure exceeds limit by ${(request.ExposureAmount - _defaultLimits["SingleContractLimit"]):N0}"
            });
        }
        
        var result = new RiskLimitCheckResult
        {
            IsWithinLimits = !violations.Any(),
            Violations = violations,
            TotalExposure = totalExposure,
            RemainingLimit = Math.Max(0, _defaultLimits["TotalExposureLimit"] - totalExposure),
            ApprovalRequired = violations.Any(v => v.Severity == "High" || v.Severity == "Critical") ? "Risk Management Approval Required" : null
        };
        
        _logger.LogInformation("Risk limit validation for {EntityType} {EntityId}: {Result}",
            request.EntityType, request.EntityId, result.IsWithinLimits ? "PASSED" : "FAILED");
        
        return result;
    }
}