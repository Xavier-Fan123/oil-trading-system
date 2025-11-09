using MediatR;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Commands.SettlementAutomationRules;

namespace OilTrading.Application.Queries.SettlementAutomationRules;

/// <summary>
/// Handler for getting a settlement automation rule by ID
/// </summary>
public class GetSettlementAutomationRuleQueryHandler : IRequestHandler<GetSettlementAutomationRuleQuery, SettlementAutomationRuleDto?>
{
    private readonly IRepository<SettlementAutomationRule> _ruleRepository;
    private readonly ILogger<GetSettlementAutomationRuleQueryHandler> _logger;

    public GetSettlementAutomationRuleQueryHandler(
        IRepository<SettlementAutomationRule> ruleRepository,
        ILogger<GetSettlementAutomationRuleQueryHandler> logger)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    public async Task<SettlementAutomationRuleDto?> Handle(GetSettlementAutomationRuleQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting settlement automation rule: {RuleId}", request.RuleId);

        var rule = await _ruleRepository.GetByIdAsync(request.RuleId, cancellationToken);

        return rule == null ? null : MapToDto(rule);
    }

    private static SettlementAutomationRuleDto MapToDto(SettlementAutomationRule rule)
    {
        return new SettlementAutomationRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            RuleType = rule.RuleType.ToString(),
            Priority = rule.Priority,
            Status = rule.Status.ToString(),
            IsEnabled = rule.IsEnabled,
            RuleVersion = rule.RuleVersion,
            Scope = rule.Scope.ToString(),
            ScopeFilter = rule.ScopeFilter,
            Trigger = rule.Trigger.ToString(),
            ScheduleExpression = rule.ScheduleExpression,
            OrchestrationStrategy = rule.OrchestrationStrategy.ToString(),
            MaxSettlementsPerExecution = rule.MaxSettlementsPerExecution,
            GroupingDimension = rule.GroupingDimension,
            ExecutionCount = rule.ExecutionCount,
            SuccessCount = rule.SuccessCount,
            FailureCount = rule.FailureCount,
            LastExecutedDate = rule.LastExecutedDate,
            LastExecutionSettlementCount = rule.LastExecutionSettlementCount,
            LastExecutionError = rule.LastExecutionError,
            Conditions = rule.Conditions.Select(c => new SettlementRuleConditionDto
            {
                Id = c.Id,
                RuleId = c.RuleId,
                Field = c.Field,
                OperatorType = c.OperatorType,
                Value = c.Value,
                SequenceNumber = c.SequenceNumber,
                LogicalOperator = c.LogicalOperator,
                GroupReference = c.GroupReference
            }).ToList(),
            Actions = rule.Actions.Select(a => new SettlementRuleActionDto
            {
                Id = a.Id,
                RuleId = a.RuleId,
                ActionType = a.ActionType,
                SequenceNumber = a.SequenceNumber,
                Parameters = a.Parameters,
                StopOnFailure = a.StopOnFailure,
                NotificationTemplateId = a.NotificationTemplateId
            }).ToList(),
            CreatedDate = rule.CreatedDate,
            CreatedBy = rule.CreatedBy,
            LastModifiedDate = rule.LastModifiedDate,
            LastModifiedBy = rule.LastModifiedBy,
            Notes = rule.Notes,
            DisabledDate = rule.DisabledDate,
            DisabledReason = rule.DisabledReason
        };
    }
}

/// <summary>
/// Handler for getting all settlement automation rules
/// </summary>
public class GetAllSettlementAutomationRulesQueryHandler : IRequestHandler<GetAllSettlementAutomationRulesQuery, List<SettlementAutomationRuleDto>>
{
    private readonly IRepository<SettlementAutomationRule> _ruleRepository;
    private readonly ILogger<GetAllSettlementAutomationRulesQueryHandler> _logger;

    public GetAllSettlementAutomationRulesQueryHandler(
        IRepository<SettlementAutomationRule> ruleRepository,
        ILogger<GetAllSettlementAutomationRulesQueryHandler> logger)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    public async Task<List<SettlementAutomationRuleDto>> Handle(GetAllSettlementAutomationRulesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all settlement automation rules");

        var rules = await _ruleRepository.GetAllAsync(cancellationToken);

        // Apply filters
        var filtered = rules.AsEnumerable();

        if (request.IsEnabled.HasValue)
            filtered = filtered.Where(r => r.IsEnabled == request.IsEnabled.Value);

        if (!string.IsNullOrEmpty(request.RuleType))
            filtered = filtered.Where(r => r.RuleType.ToString() == request.RuleType);

        if (!string.IsNullOrEmpty(request.Status))
            filtered = filtered.Where(r => r.Status.ToString() == request.Status);

        var sorted = filtered.OrderByDescending(r => r.CreatedDate).ToList();

        // Apply pagination
        var skip = (request.PageNum - 1) * request.PageSize;
        var paged = sorted.Skip(skip).Take(request.PageSize).ToList();

        return paged.Select(MapToDto).ToList();
    }

    private static SettlementAutomationRuleDto MapToDto(SettlementAutomationRule rule)
    {
        return new SettlementAutomationRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            RuleType = rule.RuleType.ToString(),
            Priority = rule.Priority,
            Status = rule.Status.ToString(),
            IsEnabled = rule.IsEnabled,
            RuleVersion = rule.RuleVersion,
            Scope = rule.Scope.ToString(),
            ScopeFilter = rule.ScopeFilter,
            Trigger = rule.Trigger.ToString(),
            ScheduleExpression = rule.ScheduleExpression,
            OrchestrationStrategy = rule.OrchestrationStrategy.ToString(),
            MaxSettlementsPerExecution = rule.MaxSettlementsPerExecution,
            GroupingDimension = rule.GroupingDimension,
            ExecutionCount = rule.ExecutionCount,
            SuccessCount = rule.SuccessCount,
            FailureCount = rule.FailureCount,
            LastExecutedDate = rule.LastExecutedDate,
            LastExecutionSettlementCount = rule.LastExecutionSettlementCount,
            LastExecutionError = rule.LastExecutionError,
            Conditions = rule.Conditions.Select(c => new SettlementRuleConditionDto
            {
                Id = c.Id,
                RuleId = c.RuleId,
                Field = c.Field,
                OperatorType = c.OperatorType,
                Value = c.Value,
                SequenceNumber = c.SequenceNumber,
                LogicalOperator = c.LogicalOperator,
                GroupReference = c.GroupReference
            }).ToList(),
            Actions = rule.Actions.Select(a => new SettlementRuleActionDto
            {
                Id = a.Id,
                RuleId = a.RuleId,
                ActionType = a.ActionType,
                SequenceNumber = a.SequenceNumber,
                Parameters = a.Parameters,
                StopOnFailure = a.StopOnFailure,
                NotificationTemplateId = a.NotificationTemplateId
            }).ToList(),
            CreatedDate = rule.CreatedDate,
            CreatedBy = rule.CreatedBy,
            LastModifiedDate = rule.LastModifiedDate,
            LastModifiedBy = rule.LastModifiedBy,
            Notes = rule.Notes,
            DisabledDate = rule.DisabledDate,
            DisabledReason = rule.DisabledReason
        };
    }
}

/// <summary>
/// Handler for getting settlement automation rule execution history
/// </summary>
public class GetRuleExecutionHistoryQueryHandler : IRequestHandler<GetRuleExecutionHistoryQuery, List<RuleExecutionRecordDto>>
{
    private readonly IRepository<SettlementAutomationRule> _ruleRepository;
    private readonly ILogger<GetRuleExecutionHistoryQueryHandler> _logger;

    public GetRuleExecutionHistoryQueryHandler(
        IRepository<SettlementAutomationRule> ruleRepository,
        ILogger<GetRuleExecutionHistoryQueryHandler> logger)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    public async Task<List<RuleExecutionRecordDto>> Handle(GetRuleExecutionHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting rule execution history: {RuleId}", request.RuleId);

        var rule = await _ruleRepository.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule == null)
            return new List<RuleExecutionRecordDto>();

        var history = rule.ExecutionHistory
            .OrderByDescending(e => e.ExecutionStartTime)
            .ToList();

        var skip = (request.PageNum - 1) * request.PageSize;
        var paged = history.Skip(skip).Take(request.PageSize).ToList();

        return paged.Select(MapToDto).ToList();
    }

    private static RuleExecutionRecordDto MapToDto(RuleExecutionRecord record)
    {
        return new RuleExecutionRecordDto
        {
            Id = record.Id,
            RuleId = record.RuleId,
            TriggerSource = record.TriggerSource,
            ExecutionStartTime = record.ExecutionStartTime,
            ExecutionEndTime = record.ExecutionEndTime,
            ExecutionDurationMs = record.ExecutionDurationMs,
            Status = record.Status.ToString(),
            SettlementCount = record.SettlementCount,
            ConditionsEvaluated = record.ConditionsEvaluated,
            ActionsExecuted = record.ActionsExecuted,
            ErrorMessage = record.ErrorMessage,
            AffectedSettlementIds = record.AffectedSettlementIds ?? new List<Guid>()
        };
    }
}

/// <summary>
/// Handler for getting settlement automation rule analytics
/// </summary>
public class GetRuleAnalyticsQueryHandler : IRequestHandler<GetRuleAnalyticsQuery, RuleAnalyticsDto>
{
    private readonly IRepository<SettlementAutomationRule> _ruleRepository;
    private readonly ILogger<GetRuleAnalyticsQueryHandler> _logger;

    public GetRuleAnalyticsQueryHandler(
        IRepository<SettlementAutomationRule> ruleRepository,
        ILogger<GetRuleAnalyticsQueryHandler> logger)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    public async Task<RuleAnalyticsDto> Handle(GetRuleAnalyticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting rule analytics: {RuleId}", request.RuleId);

        var rule = await _ruleRepository.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule == null)
            return new RuleAnalyticsDto { RuleId = request.RuleId };

        var executionHistory = rule.ExecutionHistory.ToList();
        var successfulExecutions = executionHistory.Count(e => e.Status == ExecutionStatus.Completed);
        var failedExecutions = executionHistory.Count(e => e.Status == ExecutionStatus.Failed);
        var totalSettlementsProcessed = executionHistory.Sum(e => e.SettlementCount);
        var successRate = executionHistory.Count > 0 ? (double)successfulExecutions / executionHistory.Count : 0;

        var durations = executionHistory
            .Where(e => e.ExecutionDurationMs.HasValue)
            .Select(e => e.ExecutionDurationMs.Value)
            .ToList();

        var analytics = new RuleAnalyticsDto
        {
            RuleId = rule.Id,
            RuleName = rule.Name,
            TotalExecutions = rule.ExecutionCount,
            SuccessfulExecutions = rule.SuccessCount,
            FailedExecutions = rule.FailureCount,
            TotalSettlementsProcessed = totalSettlementsProcessed,
            SuccessRate = successRate,
            FirstExecutionDate = executionHistory.OrderBy(e => e.ExecutionStartTime).FirstOrDefault()?.ExecutionStartTime,
            LastExecutionDate = rule.LastExecutedDate,
            AverageExecutionDurationMs = durations.Count > 0 ? (int)durations.Average() : null,
            MinExecutionDurationMs = durations.Count > 0 ? durations.Min() : null,
            MaxExecutionDurationMs = durations.Count > 0 ? durations.Max() : null
        };

        // Build execution trends
        var trends = executionHistory
            .GroupBy(e => e.ExecutionStartTime.Date)
            .OrderByDescending(g => g.Key)
            .Take(30) // Last 30 days
            .Select(g => new ExecutionTrendDto
            {
                Date = g.Key,
                ExecutionCount = g.Count(),
                SettlementCount = g.Sum(e => e.SettlementCount),
                SuccessfulExecutions = g.Count(e => e.Status == ExecutionStatus.Completed),
                FailedExecutions = g.Count(e => e.Status == ExecutionStatus.Failed)
            })
            .OrderBy(t => t.Date)
            .ToList();

        analytics.ExecutionTrends = trends;

        return analytics;
    }
}
