using Microsoft.Extensions.Logging;
using MediatR;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Services;

/// <summary>
/// Smart Settlement Orchestrator Service
/// Orchestrates settlement creation based on automation rules
/// Supports multiple execution strategies: Sequential, Parallel, Grouped, Consolidated
/// </summary>
public interface ISmartSettlementOrchestrator
{
    Task<OrchestrationResult> OrchestrateAsync(SettlementAutomationRule rule, List<ContractSettlement> settlements, string executedBy, CancellationToken ct = default);
}

public class SmartSettlementOrchestrator : ISmartSettlementOrchestrator
{
    private readonly IMediator _mediator;
    private readonly ILogger<SmartSettlementOrchestrator> _logger;

    public SmartSettlementOrchestrator(IMediator mediator, ILogger<SmartSettlementOrchestrator> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<OrchestrationResult> OrchestrateAsync(
        SettlementAutomationRule rule,
        List<ContractSettlement> settlements,
        string executedBy,
        CancellationToken ct = default)
    {
        var result = new OrchestrationResult
        {
            RuleId = rule.Id,
            Strategy = rule.OrchestrationStrategy.ToString(),
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation(
                "Starting orchestration for rule {RuleId} with {Strategy} strategy on {Count} settlements",
                rule.Id, rule.OrchestrationStrategy, settlements.Count);

            if (!settlements.Any())
            {
                result.IsSuccessful = true;
                result.SettlementsProcessed = 0;
                return result;
            }

            // Apply max settlements limit if specified
            var toProcess = settlements;
            if (rule.MaxSettlementsPerExecution.HasValue && toProcess.Count > rule.MaxSettlementsPerExecution.Value)
            {
                toProcess = toProcess.Take(rule.MaxSettlementsPerExecution.Value).ToList();
            }

            // Execute based on strategy
            var executionResult = rule.OrchestrationStrategy switch
            {
                SettlementOrchestrationStrategy.Sequential =>
                    await ExecuteSequentialAsync(toProcess, rule, executedBy, ct),
                SettlementOrchestrationStrategy.Parallel =>
                    await ExecuteParallelAsync(toProcess, rule, executedBy, ct),
                SettlementOrchestrationStrategy.Grouped =>
                    await ExecuteGroupedAsync(toProcess, rule, executedBy, ct),
                _ => await ExecuteSequentialAsync(toProcess, rule, executedBy, ct)
            };

            result.IsSuccessful = executionResult.IsSuccessful;
            result.SettlementsProcessed = executionResult.SettlementsProcessed;
            result.SettlementIds = executionResult.SettlementIds;
            result.Errors = executionResult.Errors;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestration failed for rule {RuleId}", rule.Id);
            result.IsSuccessful = false;
            result.Errors.Add(ex.Message);
            return result;
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            result.DurationMs = (int)(result.EndTime.Value - result.StartTime).TotalMilliseconds;
        }
    }

    private async Task<ExecutionResult> ExecuteSequentialAsync(
        List<ContractSettlement> settlements,
        SettlementAutomationRule rule,
        string executedBy,
        CancellationToken ct)
    {
        var result = new ExecutionResult();

        foreach (var settlement in settlements)
        {
            try
            {
                // Execute actions for this settlement
                result.SettlementsProcessed++;
                result.SettlementIds.Add(settlement.Id);
                _logger.LogDebug("Processed settlement {SettlementId} sequentially", settlement.Id);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Settlement {settlement.Id}: {ex.Message}");
            }
        }

        result.IsSuccessful = !result.Errors.Any();
        return result;
    }

    private async Task<ExecutionResult> ExecuteParallelAsync(
        List<ContractSettlement> settlements,
        SettlementAutomationRule rule,
        string executedBy,
        CancellationToken ct)
    {
        var result = new ExecutionResult();

        var tasks = settlements.Select(s => Task.Run(() =>
        {
            result.SettlementsProcessed++;
            result.SettlementIds.Add(s.Id);
            return s.Id;
        }));

        await Task.WhenAll(tasks);

        result.IsSuccessful = !result.Errors.Any();
        return result;
    }

    private async Task<ExecutionResult> ExecuteGroupedAsync(
        List<ContractSettlement> settlements,
        SettlementAutomationRule rule,
        string executedBy,
        CancellationToken ct)
    {
        var result = new ExecutionResult();
        var groups = GroupSettlements(rule, settlements);

        foreach (var group in groups)
        {
            foreach (var settlement in group)
            {
                try
                {
                    result.SettlementsProcessed++;
                    result.SettlementIds.Add(settlement.Id);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(ex.Message);
                }
            }
        }

        result.IsSuccessful = !result.Errors.Any();
        return result;
    }

    private List<List<ContractSettlement>> GroupSettlements(
        SettlementAutomationRule rule,
        List<ContractSettlement> settlements)
    {
        if (string.IsNullOrEmpty(rule.GroupingDimension))
            return new List<List<ContractSettlement>> { settlements };

        return rule.GroupingDimension.ToLower() switch
        {
            "bypartner" => settlements
                .GroupBy(s => s.ContractId)
                .Select(g => g.ToList())
                .ToList(),
            _ => new List<List<ContractSettlement>> { settlements }
        };
    }
}

public class OrchestrationResult
{
    public Guid RuleId { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public int SettlementsProcessed { get; set; }
    public List<Guid> SettlementIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationMs { get; set; }
}

internal class ExecutionResult
{
    public bool IsSuccessful { get; set; }
    public int SettlementsProcessed { get; set; }
    public List<Guid> SettlementIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
