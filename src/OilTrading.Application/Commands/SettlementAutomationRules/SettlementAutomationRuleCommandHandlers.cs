using MediatR;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Services;

namespace OilTrading.Application.Commands.SettlementAutomationRules;

/// <summary>
/// Handler for creating a settlement automation rule
/// </summary>
public class CreateSettlementAutomationRuleCommandHandler : IRequestHandler<CreateSettlementAutomationRuleCommand, SettlementAutomationRuleDto>
{
    private readonly IRepository<SettlementAutomationRule> _ruleRepository;
    private readonly ILogger<CreateSettlementAutomationRuleCommandHandler> _logger;

    public CreateSettlementAutomationRuleCommandHandler(
        IRepository<SettlementAutomationRule> ruleRepository,
        ILogger<CreateSettlementAutomationRuleCommandHandler> logger)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    public async Task<SettlementAutomationRuleDto> Handle(CreateSettlementAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating settlement automation rule: {RuleName}", request.Name);

        // Use domain entity constructor
        var rule = new SettlementAutomationRule(
            request.Name,
            request.Description,
            request.RuleType,
            request.Priority,
            request.CreatedBy
        );

        // Set rule properties via domain methods
        rule.UpdateTrigger(request.Trigger, request.ScheduleExpression, request.CreatedBy);
        rule.UpdateScope(request.Scope, request.ScopeFilter, request.CreatedBy);
        rule.UpdateOrchestration(request.OrchestrationStrategy, request.MaxSettlementsPerExecution, request.GroupingDimension, request.CreatedBy);

        // Note: Add conditions and actions via domain entity collections if supported
        // For now, store placeholder data to unblock build

        await _ruleRepository.AddAsync(rule, cancellationToken);

        _logger.LogInformation("Settlement automation rule created: {RuleId}", rule.Id);

        return MapToDto(rule);
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
/// Handler for updating a settlement automation rule
/// </summary>
public class UpdateSettlementAutomationRuleCommandHandler : IRequestHandler<UpdateSettlementAutomationRuleCommand, SettlementAutomationRuleDto>
{
    private readonly IRepository<SettlementAutomationRule> _ruleRepository;
    private readonly ILogger<UpdateSettlementAutomationRuleCommandHandler> _logger;

    public UpdateSettlementAutomationRuleCommandHandler(
        IRepository<SettlementAutomationRule> ruleRepository,
        ILogger<UpdateSettlementAutomationRuleCommandHandler> logger)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    public async Task<SettlementAutomationRuleDto> Handle(UpdateSettlementAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating settlement automation rule: {RuleId}", request.Id);

        var rule = await _ruleRepository.GetByIdAsync(request.Id, cancellationToken);
        if (rule == null)
            throw new InvalidOperationException($"Rule {request.Id} not found");

        rule.UpdateBasicInfo(request.Name, request.Description, request.Priority, request.Notes, request.ModifiedBy);
        rule.UpdateTrigger(request.Trigger, request.ScheduleExpression, request.ModifiedBy);
        rule.UpdateScope(request.Scope, request.ScopeFilter, request.ModifiedBy);
        rule.UpdateOrchestration(request.OrchestrationStrategy, request.MaxSettlementsPerExecution, request.GroupingDimension, request.ModifiedBy);

        await _ruleRepository.UpdateAsync(rule, cancellationToken);

        _logger.LogInformation("Settlement automation rule updated: {RuleId}", rule.Id);

        return MapToDto(rule);
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
/// Handler for enabling a settlement automation rule
/// </summary>
public class EnableSettlementAutomationRuleCommandHandler : IRequestHandler<EnableSettlementAutomationRuleCommand, SettlementAutomationRuleDto>
{
    private readonly IRepository<SettlementAutomationRule> _ruleRepository;
    private readonly ILogger<EnableSettlementAutomationRuleCommandHandler> _logger;

    public EnableSettlementAutomationRuleCommandHandler(
        IRepository<SettlementAutomationRule> ruleRepository,
        ILogger<EnableSettlementAutomationRuleCommandHandler> logger)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    public async Task<SettlementAutomationRuleDto> Handle(EnableSettlementAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Enabling settlement automation rule: {RuleId}", request.RuleId);

        var rule = await _ruleRepository.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule == null)
            throw new InvalidOperationException($"Rule {request.RuleId} not found");

        rule.Enable();

        await _ruleRepository.UpdateAsync(rule, cancellationToken);

        _logger.LogInformation("Settlement automation rule enabled: {RuleId}", rule.Id);

        return MapToDto(rule);
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
/// Handler for disabling a settlement automation rule
/// </summary>
public class DisableSettlementAutomationRuleCommandHandler : IRequestHandler<DisableSettlementAutomationRuleCommand, SettlementAutomationRuleDto>
{
    private readonly IRepository<SettlementAutomationRule> _ruleRepository;
    private readonly ILogger<DisableSettlementAutomationRuleCommandHandler> _logger;

    public DisableSettlementAutomationRuleCommandHandler(
        IRepository<SettlementAutomationRule> ruleRepository,
        ILogger<DisableSettlementAutomationRuleCommandHandler> logger)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    public async Task<SettlementAutomationRuleDto> Handle(DisableSettlementAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Disabling settlement automation rule: {RuleId}", request.RuleId);

        var rule = await _ruleRepository.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule == null)
            throw new InvalidOperationException($"Rule {request.RuleId} not found");

        rule.Disable(request.Reason);

        await _ruleRepository.UpdateAsync(rule, cancellationToken);

        _logger.LogInformation("Settlement automation rule disabled: {RuleId}", rule.Id);

        return MapToDto(rule);
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
/// Handler for testing a settlement automation rule
/// </summary>
public class TestSettlementAutomationRuleCommandHandler : IRequestHandler<TestSettlementAutomationRuleCommand, RuleTestResultDto>
{
    private readonly IRepository<SettlementAutomationRule> _ruleRepository;
    private readonly IRepository<ContractSettlement> _settlementRepository;
    private readonly ISettlementRuleEvaluator _ruleEvaluator;
    private readonly ILogger<TestSettlementAutomationRuleCommandHandler> _logger;

    public TestSettlementAutomationRuleCommandHandler(
        IRepository<SettlementAutomationRule> ruleRepository,
        IRepository<ContractSettlement> settlementRepository,
        ISettlementRuleEvaluator ruleEvaluator,
        ILogger<TestSettlementAutomationRuleCommandHandler> logger)
    {
        _ruleRepository = ruleRepository;
        _settlementRepository = settlementRepository;
        _ruleEvaluator = ruleEvaluator;
        _logger = logger;
    }

    public async Task<RuleTestResultDto> Handle(TestSettlementAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing settlement automation rule: {RuleId}", request.RuleId);

        var rule = await _ruleRepository.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule == null)
            throw new InvalidOperationException($"Rule {request.RuleId} not found");

        var settlement = await _settlementRepository.GetByIdAsync(request.SettlementId, cancellationToken);
        if (settlement == null)
            throw new InvalidOperationException($"Settlement {request.SettlementId} not found");

        var testPassed = await _ruleEvaluator.EvaluateRuleAsync(rule, settlement, cancellationToken);

        _logger.LogInformation("Rule test completed. RuleId: {RuleId}, Passed: {TestPassed}", request.RuleId, testPassed);

        return new RuleTestResultDto
        {
            RuleId = request.RuleId,
            TestPassed = testPassed,
            TestedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Handler for executing a settlement automation rule
/// </summary>
public class ExecuteSettlementAutomationRuleCommandHandler : IRequestHandler<ExecuteSettlementAutomationRuleCommand, OrchestrationResultDto>
{
    private readonly IRepository<SettlementAutomationRule> _ruleRepository;
    private readonly IRepository<ContractSettlement> _settlementRepository;
    private readonly ISmartSettlementOrchestrator _orchestrator;
    private readonly ILogger<ExecuteSettlementAutomationRuleCommandHandler> _logger;

    public ExecuteSettlementAutomationRuleCommandHandler(
        IRepository<SettlementAutomationRule> ruleRepository,
        IRepository<ContractSettlement> settlementRepository,
        ISmartSettlementOrchestrator orchestrator,
        ILogger<ExecuteSettlementAutomationRuleCommandHandler> logger)
    {
        _ruleRepository = ruleRepository;
        _settlementRepository = settlementRepository;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<OrchestrationResultDto> Handle(ExecuteSettlementAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing settlement automation rule: {RuleId}", request.RuleId);

        var rule = await _ruleRepository.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule == null)
            throw new InvalidOperationException($"Rule {request.RuleId} not found");

        if (!rule.IsEnabled)
            throw new InvalidOperationException($"Rule {request.RuleId} is not enabled");

        // Get settlements to process
        List<ContractSettlement> settlements;
        if (request.SettlementIds?.Count > 0)
        {
            settlements = new List<ContractSettlement>();
            foreach (var id in request.SettlementIds)
            {
                var settlement = await _settlementRepository.GetByIdAsync(id, cancellationToken);
                if (settlement != null)
                    settlements.Add(settlement);
            }
        }
        else
        {
            // Get all eligible settlements for this rule
            settlements = (await _settlementRepository.GetAllAsync(cancellationToken)).ToList();
        }

        var orchestrationResult = await _orchestrator.OrchestrateAsync(rule, settlements, request.ExecutedBy, cancellationToken);

        // Record execution using domain entity methods
        if (orchestrationResult.IsSuccessful)
        {
            rule.RecordSuccessfulExecution(orchestrationResult.SettlementsProcessed);
        }
        else
        {
            rule.RecordFailedExecution(string.Join(", ", orchestrationResult.Errors));
        }

        await _ruleRepository.UpdateAsync(rule, cancellationToken);

        _logger.LogInformation("Rule execution completed. RuleId: {RuleId}, Successful: {IsSuccessful}, SettlementsProcessed: {Count}",
            request.RuleId, orchestrationResult.IsSuccessful, orchestrationResult.SettlementsProcessed);

        return new OrchestrationResultDto
        {
            RuleId = orchestrationResult.RuleId,
            Strategy = orchestrationResult.Strategy,
            IsSuccessful = orchestrationResult.IsSuccessful,
            SettlementsProcessed = orchestrationResult.SettlementsProcessed,
            SettlementIds = orchestrationResult.SettlementIds,
            Errors = orchestrationResult.Errors,
            StartTime = orchestrationResult.StartTime,
            EndTime = orchestrationResult.EndTime,
            DurationMs = orchestrationResult.DurationMs
        };
    }
}
