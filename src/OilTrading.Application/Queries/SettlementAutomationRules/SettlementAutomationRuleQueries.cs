using MediatR;
using OilTrading.Application.Commands.SettlementAutomationRules;

namespace OilTrading.Application.Queries.SettlementAutomationRules;

/// <summary>
/// Query to get a settlement automation rule by ID
/// </summary>
public class GetSettlementAutomationRuleQuery : IRequest<SettlementAutomationRuleDto?>
{
    public required Guid RuleId { get; set; }
}

/// <summary>
/// Query to get all settlement automation rules
/// </summary>
public class GetAllSettlementAutomationRulesQuery : IRequest<List<SettlementAutomationRuleDto>>
{
    public bool? IsEnabled { get; set; }
    public string? RuleType { get; set; }
    public string? Status { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Query to get settlement automation rule execution history
/// </summary>
public class GetRuleExecutionHistoryQuery : IRequest<List<RuleExecutionRecordDto>>
{
    public required Guid RuleId { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Query to get settlement automation rule analytics
/// </summary>
public class GetRuleAnalyticsQuery : IRequest<RuleAnalyticsDto>
{
    public required Guid RuleId { get; set; }
}

/// <summary>
/// DTO for rule execution record
/// </summary>
public class RuleExecutionRecordDto
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public string TriggerSource { get; set; } = string.Empty;
    public DateTime ExecutionStartTime { get; set; }
    public DateTime? ExecutionEndTime { get; set; }
    public int? ExecutionDurationMs { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SettlementCount { get; set; }
    public int ConditionsEvaluated { get; set; }
    public int ActionsExecuted { get; set; }
    public string? ErrorMessage { get; set; }
    public List<Guid> AffectedSettlementIds { get; set; } = new();
}

/// <summary>
/// DTO for rule analytics
/// </summary>
public class RuleAnalyticsDto
{
    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public int TotalSettlementsProcessed { get; set; }
    public double SuccessRate { get; set; }
    public DateTime? FirstExecutionDate { get; set; }
    public DateTime? LastExecutionDate { get; set; }
    public int? AverageExecutionDurationMs { get; set; }
    public int? MinExecutionDurationMs { get; set; }
    public int? MaxExecutionDurationMs { get; set; }
    public List<ExecutionTrendDto> ExecutionTrends { get; set; } = new();
}

/// <summary>
/// DTO for execution trend data
/// </summary>
public class ExecutionTrendDto
{
    public DateTime Date { get; set; }
    public int ExecutionCount { get; set; }
    public int SettlementCount { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
}
