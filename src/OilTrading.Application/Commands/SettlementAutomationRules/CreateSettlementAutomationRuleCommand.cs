using MediatR;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Commands.SettlementAutomationRules;

/// <summary>
/// Command to create a new settlement automation rule
/// </summary>
public class CreateSettlementAutomationRuleCommand : IRequest<SettlementAutomationRuleDto>
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required SettlementRuleType RuleType { get; set; }
    public string Priority { get; set; } = "Normal";
    public SettlementRuleScope Scope { get; set; } = SettlementRuleScope.All;
    public string? ScopeFilter { get; set; }
    public SettlementRuleTrigger Trigger { get; set; } = SettlementRuleTrigger.OnContractCompletion;
    public string? ScheduleExpression { get; set; }
    public SettlementOrchestrationStrategy OrchestrationStrategy { get; set; } = SettlementOrchestrationStrategy.Sequential;
    public int? MaxSettlementsPerExecution { get; set; }
    public string? GroupingDimension { get; set; }
    public List<CreateSettlementRuleConditionDto> Conditions { get; set; } = new();
    public List<CreateSettlementRuleActionDto> Actions { get; set; } = new();
    public string? Notes { get; set; }
    public required string CreatedBy { get; set; }
}

/// <summary>
/// Command to update an existing settlement automation rule
/// </summary>
public class UpdateSettlementAutomationRuleCommand : IRequest<SettlementAutomationRuleDto>
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string Priority { get; set; } = "Normal";
    public SettlementRuleScope Scope { get; set; }
    public string? ScopeFilter { get; set; }
    public SettlementRuleTrigger Trigger { get; set; }
    public string? ScheduleExpression { get; set; }
    public SettlementOrchestrationStrategy OrchestrationStrategy { get; set; }
    public int? MaxSettlementsPerExecution { get; set; }
    public string? GroupingDimension { get; set; }
    public string? Notes { get; set; }
    public required string ModifiedBy { get; set; }
}

/// <summary>
/// Command to enable a settlement automation rule
/// </summary>
public class EnableSettlementAutomationRuleCommand : IRequest<SettlementAutomationRuleDto>
{
    public required Guid RuleId { get; set; }
    public required string ModifiedBy { get; set; }
}

/// <summary>
/// Command to disable a settlement automation rule
/// </summary>
public class DisableSettlementAutomationRuleCommand : IRequest<SettlementAutomationRuleDto>
{
    public required Guid RuleId { get; set; }
    public string? Reason { get; set; }
    public required string ModifiedBy { get; set; }
}

/// <summary>
/// Command to test a settlement automation rule configuration
/// </summary>
public class TestSettlementAutomationRuleCommand : IRequest<RuleTestResultDto>
{
    public required Guid RuleId { get; set; }
    public required Guid SettlementId { get; set; }
}

/// <summary>
/// Command to execute a settlement automation rule
/// </summary>
public class ExecuteSettlementAutomationRuleCommand : IRequest<OrchestrationResultDto>
{
    public required Guid RuleId { get; set; }
    public List<Guid>? SettlementIds { get; set; }
    public required string ExecutedBy { get; set; }
}

/// <summary>
/// DTO for creating settlement rule conditions
/// </summary>
public class CreateSettlementRuleConditionDto
{
    public required string Field { get; set; }
    public required string OperatorType { get; set; }
    public required string Value { get; set; }
    public int SequenceNumber { get; set; }
    public string LogicalOperator { get; set; } = "AND";
    public string? GroupReference { get; set; }
}

/// <summary>
/// DTO for creating settlement rule actions
/// </summary>
public class CreateSettlementRuleActionDto
{
    public required string ActionType { get; set; }
    public int SequenceNumber { get; set; }
    public string? Parameters { get; set; }
    public bool StopOnFailure { get; set; } = true;
    public string? NotificationTemplateId { get; set; }
}

/// <summary>
/// DTO for settlement automation rule (response)
/// </summary>
public class SettlementAutomationRuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int RuleVersion { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string? ScopeFilter { get; set; }
    public string Trigger { get; set; } = string.Empty;
    public string? ScheduleExpression { get; set; }
    public string OrchestrationStrategy { get; set; } = string.Empty;
    public int? MaxSettlementsPerExecution { get; set; }
    public string? GroupingDimension { get; set; }
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime? LastExecutedDate { get; set; }
    public int? LastExecutionSettlementCount { get; set; }
    public string? LastExecutionError { get; set; }
    public List<SettlementRuleConditionDto> Conditions { get; set; } = new();
    public List<SettlementRuleActionDto> Actions { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? LastModifiedDate { get; set; }
    public string? LastModifiedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime? DisabledDate { get; set; }
    public string? DisabledReason { get; set; }
}

/// <summary>
/// DTO for settlement rule conditions (response)
/// </summary>
public class SettlementRuleConditionDto
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public string Field { get; set; } = string.Empty;
    public string OperatorType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string LogicalOperator { get; set; } = "AND";
    public string? GroupReference { get; set; }
}

/// <summary>
/// DTO for settlement rule actions (response)
/// </summary>
public class SettlementRuleActionDto
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string? Parameters { get; set; }
    public bool StopOnFailure { get; set; }
    public string? NotificationTemplateId { get; set; }
}

/// <summary>
/// DTO for rule test result
/// </summary>
public class RuleTestResultDto
{
    public Guid RuleId { get; set; }
    public bool TestPassed { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime TestedAt { get; set; }
}

/// <summary>
/// DTO for orchestration result
/// </summary>
public class OrchestrationResultDto
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
