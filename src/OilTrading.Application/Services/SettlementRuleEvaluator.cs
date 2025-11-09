using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Services;

/// <summary>
/// Settlement Rule Evaluator Service
/// Evaluates whether settlement automation rules apply to given settlements
/// </summary>
public interface ISettlementRuleEvaluator
{
    Task<bool> EvaluateRuleAsync(SettlementAutomationRule rule, ContractSettlement settlement, CancellationToken ct = default);
    Task<RuleTestResult> TestRuleAsync(SettlementAutomationRule rule, ContractSettlement settlement, CancellationToken ct = default);
}

public class SettlementRuleEvaluator : ISettlementRuleEvaluator
{
    private readonly ILogger<SettlementRuleEvaluator> _logger;

    public SettlementRuleEvaluator(ILogger<SettlementRuleEvaluator> logger)
    {
        _logger = logger;
    }

    public async Task<bool> EvaluateRuleAsync(SettlementAutomationRule rule, ContractSettlement settlement, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Evaluating rule {RuleId} against settlement {SettlementId}", rule.Id, settlement.Id);

            // Validate rule configuration
            if (!rule.IsEnabled || !rule.IsValid())
            {
                return false;
            }

            // Check scope matching
            if (!MatchesScope(rule, settlement))
            {
                return false;
            }

            // Evaluate conditions
            return EvaluateConditions(rule, settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating rule {RuleId}", rule.Id);
            return false;
        }
    }

    public async Task<RuleTestResult> TestRuleAsync(SettlementAutomationRule rule, ContractSettlement settlement, CancellationToken ct = default)
    {
        var result = new RuleTestResult { RuleId = rule.Id, TestedAt = DateTime.UtcNow };

        try
        {
            if (!rule.IsValid())
            {
                result.TestPassed = false;
                result.ErrorMessage = "Rule is not properly configured";
                return result;
            }

            var applicable = await EvaluateRuleAsync(rule, settlement, ct);
            result.TestPassed = applicable;
            result.ErrorMessage = applicable ? "Rule would be applied" : "Rule would not apply";

            return result;
        }
        catch (Exception ex)
        {
            result.TestPassed = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    private bool MatchesScope(SettlementAutomationRule rule, ContractSettlement settlement)
    {
        return rule.Scope switch
        {
            SettlementRuleScope.All => true,
            SettlementRuleScope.PurchaseOnly => settlement.ContractId != Guid.Empty,
            SettlementRuleScope.SalesOnly => settlement.ContractId != Guid.Empty,
            _ => true
        };
    }

    private bool EvaluateConditions(SettlementAutomationRule rule, ContractSettlement settlement)
    {
        if (!rule.Conditions.Any())
            return true;

        // Simple evaluation: all conditions must pass with AND logic
        foreach (var condition in rule.Conditions.OrderBy(c => c.SequenceNumber))
        {
            if (!EvaluateCondition(condition, settlement))
                return false;
        }

        return true;
    }

    private bool EvaluateCondition(SettlementRuleCondition condition, ContractSettlement settlement)
    {
        try
        {
            return condition.OperatorType.ToUpper() switch
            {
                "EQUALS" => settlement.TotalSettlementAmount.ToString() == condition.Value,
                "GREATERTHAN" => decimal.Parse(settlement.TotalSettlementAmount.ToString() ?? "0") > decimal.Parse(condition.Value),
                "LESSTHAN" => decimal.Parse(settlement.TotalSettlementAmount.ToString() ?? "0") < decimal.Parse(condition.Value),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }
}

public class RuleTestResult
{
    public Guid RuleId { get; set; }
    public bool TestPassed { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime TestedAt { get; set; }
}
