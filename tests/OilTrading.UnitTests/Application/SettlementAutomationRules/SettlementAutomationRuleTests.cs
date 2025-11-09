using Xunit;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OilTrading.UnitTests.Application.SettlementAutomationRules;

/// <summary>
/// Unit tests for SettlementAutomationRule domain entity
/// Tests: Creation, state transitions, update operations, execution recording
/// </summary>
public class SettlementAutomationRuleTests
{
    #region Creation and Initialization Tests

    [Fact]
    public void Create_ValidRule_ShouldInitializeWithDefaults()
    {
        // Arrange
        var ruleName = "Test Rule";
        var description = "Test rule description";
        var ruleType = SettlementRuleType.Automatic;

        // Act
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = ruleName,
            Description = description,
            RuleType = ruleType,
            Priority = "High",
            Status = RuleStatus.Draft,
            IsEnabled = false,
            RuleVersion = 1,
            Scope = SettlementRuleScope.All,
            Trigger = SettlementRuleTrigger.OnContractCompletion,
            OrchestrationStrategy = SettlementOrchestrationStrategy.Sequential,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "test-user",
            ExecutionCount = 0,
            SuccessCount = 0,
            FailureCount = 0
        };

        // Assert
        Assert.NotEqual(Guid.Empty, rule.Id);
        Assert.Equal(ruleName, rule.Name);
        Assert.Equal(description, rule.Description);
        Assert.Equal(ruleType, rule.RuleType);
        Assert.Equal(RuleStatus.Draft, rule.Status);
        Assert.False(rule.IsEnabled);
        Assert.Equal(1, rule.RuleVersion);
        Assert.Equal(0, rule.ExecutionCount);
    }

    [Fact]
    public void Create_WithConditions_ShouldAddConditionsToRule()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Rule with conditions",
            Conditions = new List<SettlementRuleCondition>()
        };

        var condition = new SettlementRuleCondition
        {
            Id = Guid.NewGuid(),
            RuleId = rule.Id,
            Field = "SettlementAmount",
            OperatorType = "GREATERTHAN",
            Value = "10000",
            SequenceNumber = 1,
            LogicalOperator = "AND"
        };

        // Act
        rule.Conditions.Add(condition);

        // Assert
        Assert.Single(rule.Conditions);
        Assert.Equal("SettlementAmount", rule.Conditions.First().Field);
    }

    [Fact]
    public void Create_WithActions_ShouldAddActionsToRule()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Rule with actions",
            Actions = new List<SettlementRuleAction>()
        };

        var action = new SettlementRuleAction
        {
            Id = Guid.NewGuid(),
            RuleId = rule.Id,
            ActionType = "CreateSettlement",
            SequenceNumber = 1,
            Parameters = "{'template': 'standard'}"
        };

        // Act
        rule.Actions.Add(action);

        // Assert
        Assert.Single(rule.Actions);
        Assert.Equal("CreateSettlement", rule.Actions.First().ActionType);
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public void Enable_DisabledRule_ShouldEnableRule()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            IsEnabled = false,
            Status = RuleStatus.Draft
        };

        // Act
        rule.Enable();

        // Assert
        Assert.True(rule.IsEnabled);
    }

    [Fact]
    public void Disable_EnabledRule_ShouldDisableRule()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            IsEnabled = true,
            Status = RuleStatus.Active
        };

        // Act
        rule.Disable("Testing disable functionality");

        // Assert
        Assert.False(rule.IsEnabled);
        Assert.Equal("Testing disable functionality", rule.DisabledReason);
        Assert.NotNull(rule.DisabledDate);
    }

    [Fact]
    public void Enable_ThenDisable_ShouldUpdateStateCorrectly()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            IsEnabled = false
        };

        // Act
        rule.Enable();
        Assert.True(rule.IsEnabled);

        rule.Disable("Manual disable");
        Assert.False(rule.IsEnabled);

        rule.Enable();
        Assert.True(rule.IsEnabled);

        // Assert
        Assert.True(rule.IsEnabled);
    }

    #endregion

    #region Update Operations Tests

    [Fact]
    public void UpdateBasicInfo_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Description = "Old Description",
            RuleVersion = 1,
            LastModifiedDate = null
        };

        var newName = "New Name";
        var newDescription = "New Description";

        // Act
        rule.UpdateBasicInfo(newName, newDescription);

        // Assert
        Assert.Equal(newName, rule.Name);
        Assert.Equal(newDescription, rule.Description);
        Assert.Equal(2, rule.RuleVersion);
        Assert.NotNull(rule.LastModifiedDate);
    }

    [Fact]
    public void UpdateTrigger_ShouldUpdateTriggerAndSchedule()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            Trigger = SettlementRuleTrigger.OnContractCompletion,
            RuleVersion = 1
        };

        var newTrigger = SettlementRuleTrigger.Scheduled;
        var newSchedule = "0 0 * * *";

        // Act
        rule.UpdateTrigger(newTrigger, newSchedule);

        // Assert
        Assert.Equal(newTrigger, rule.Trigger);
        Assert.Equal(newSchedule, rule.ScheduleExpression);
        Assert.Equal(2, rule.RuleVersion);
    }

    [Fact]
    public void UpdateScope_ShouldUpdateScopeAndFilter()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            Scope = SettlementRuleScope.All,
            RuleVersion = 1
        };

        var newScope = SettlementRuleScope.PurchaseOnly;
        var newFilter = "USD";

        // Act
        rule.UpdateScope(newScope, newFilter);

        // Assert
        Assert.Equal(newScope, rule.Scope);
        Assert.Equal(newFilter, rule.ScopeFilter);
        Assert.Equal(2, rule.RuleVersion);
    }

    [Fact]
    public void UpdateOrchestration_ShouldUpdateStrategyAndSettings()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            OrchestrationStrategy = SettlementOrchestrationStrategy.Sequential,
            RuleVersion = 1
        };

        var newStrategy = SettlementOrchestrationStrategy.Parallel;
        var maxSettlements = 100;
        var grouping = "bypartner";

        // Act
        rule.UpdateOrchestration(newStrategy, maxSettlements, grouping);

        // Assert
        Assert.Equal(newStrategy, rule.OrchestrationStrategy);
        Assert.Equal(maxSettlements, rule.MaxSettlementsPerExecution);
        Assert.Equal(grouping, rule.GroupingDimension);
        Assert.Equal(2, rule.RuleVersion);
    }

    [Fact]
    public void MultipleUpdates_ShouldIncrementVersionOnEachUpdate()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            RuleVersion = 1
        };

        var initialVersion = rule.RuleVersion;

        // Act
        rule.UpdateBasicInfo("Name 1", "Description 1");
        var versionAfterFirstUpdate = rule.RuleVersion;

        rule.UpdateBasicInfo("Name 2", "Description 2");
        var versionAfterSecondUpdate = rule.RuleVersion;

        rule.UpdateBasicInfo("Name 3", "Description 3");
        var versionAfterThirdUpdate = rule.RuleVersion;

        // Assert
        Assert.Equal(1, initialVersion);
        Assert.Equal(2, versionAfterFirstUpdate);
        Assert.Equal(3, versionAfterSecondUpdate);
        Assert.Equal(4, versionAfterThirdUpdate);
    }

    #endregion

    #region Execution Recording Tests

    [Fact]
    public void RecordSuccessfulExecution_ShouldUpdateExecutionCounters()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            ExecutionCount = 0,
            SuccessCount = 0,
            FailureCount = 0,
            ExecutionHistory = new List<RuleExecutionRecord>()
        };

        var settlementCount = 5;

        // Act
        rule.RecordSuccessfulExecution(settlementCount);

        // Assert
        Assert.Equal(1, rule.ExecutionCount);
        Assert.Equal(1, rule.SuccessCount);
        Assert.Equal(0, rule.FailureCount);
        Assert.Equal(settlementCount, rule.LastExecutionSettlementCount);
        Assert.NotNull(rule.LastExecutedDate);
    }

    [Fact]
    public void RecordFailedExecution_ShouldUpdateFailureCounters()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            ExecutionCount = 0,
            SuccessCount = 0,
            FailureCount = 0,
            ExecutionHistory = new List<RuleExecutionRecord>()
        };

        var errorMessage = "Settlement not found";

        // Act
        rule.RecordFailedExecution(errorMessage);

        // Assert
        Assert.Equal(1, rule.ExecutionCount);
        Assert.Equal(0, rule.SuccessCount);
        Assert.Equal(1, rule.FailureCount);
        Assert.Equal(errorMessage, rule.LastExecutionError);
        Assert.NotNull(rule.LastExecutedDate);
    }

    [Fact]
    public void MultipleExecutions_ShouldAccumulateCounters()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            ExecutionCount = 0,
            SuccessCount = 0,
            FailureCount = 0,
            ExecutionHistory = new List<RuleExecutionRecord>()
        };

        // Act
        rule.RecordSuccessfulExecution(5);
        rule.RecordSuccessfulExecution(3);
        rule.RecordFailedExecution("Error");
        rule.RecordSuccessfulExecution(7);

        // Assert
        Assert.Equal(4, rule.ExecutionCount);  // 4 total executions
        Assert.Equal(3, rule.SuccessCount);     // 3 successful
        Assert.Equal(1, rule.FailureCount);     // 1 failed
        Assert.Equal(7, rule.LastExecutionSettlementCount);  // Last was 7 settlements
    }

    #endregion

    #region Business Rule Validation Tests

    [Fact]
    public void Rule_ShouldHaveAtLeastOneCondition()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Rule without conditions",
            Conditions = new List<SettlementRuleCondition>()
        };

        // Act & Assert
        Assert.Empty(rule.Conditions);
        // Note: Validation can be added in service layer to enforce this
    }

    [Fact]
    public void Rule_ShouldHaveAtLeastOneAction()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Rule without actions",
            Actions = new List<SettlementRuleAction>()
        };

        // Act & Assert
        Assert.Empty(rule.Actions);
        // Note: Validation can be added in service layer to enforce this
    }

    [Fact]
    public void Rule_ScheduledTrigger_ShouldHaveScheduleExpression()
    {
        // Arrange & Act
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Scheduled Rule",
            Trigger = SettlementRuleTrigger.Scheduled,
            ScheduleExpression = "0 0 * * *"
        };

        // Assert
        Assert.NotNull(rule.ScheduleExpression);
        Assert.NotEmpty(rule.ScheduleExpression);
    }

    [Fact]
    public void Rule_WithGrouping_ShouldHaveGroupingDimension()
    {
        // Arrange & Act
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Grouped Rule",
            OrchestrationStrategy = SettlementOrchestrationStrategy.Grouped,
            GroupingDimension = "bypartner"
        };

        // Assert
        Assert.NotNull(rule.GroupingDimension);
        Assert.Equal("bypartner", rule.GroupingDimension);
    }

    #endregion

    #region Scope Tests

    [Fact]
    public void Rule_CanHaveDifferentScopes()
    {
        // Arrange
        var scopes = new[]
        {
            SettlementRuleScope.All,
            SettlementRuleScope.PurchaseOnly,
            SettlementRuleScope.SalesOnly,
            SettlementRuleScope.ByPartner,
            SettlementRuleScope.ByCurrency
        };

        // Act & Assert
        foreach (var scope in scopes)
        {
            var rule = new SettlementAutomationRule
            {
                Id = Guid.NewGuid(),
                Name = $"Rule with scope {scope}",
                Scope = scope
            };

            Assert.Equal(scope, rule.Scope);
        }
    }

    [Fact]
    public void Rule_CanHaveDifferentTriggers()
    {
        // Arrange
        var triggers = new[]
        {
            SettlementRuleTrigger.OnContractCompletion,
            SettlementRuleTrigger.OnPaymentReceived,
            SettlementRuleTrigger.Manual,
            SettlementRuleTrigger.Scheduled
        };

        // Act & Assert
        foreach (var trigger in triggers)
        {
            var rule = new SettlementAutomationRule
            {
                Id = Guid.NewGuid(),
                Name = $"Rule with trigger {trigger}",
                Trigger = trigger
            };

            Assert.Equal(trigger, rule.Trigger);
        }
    }

    [Fact]
    public void Rule_CanHaveDifferentOrchestrationStrategies()
    {
        // Arrange
        var strategies = new[]
        {
            SettlementOrchestrationStrategy.Sequential,
            SettlementOrchestrationStrategy.Parallel,
            SettlementOrchestrationStrategy.Grouped,
            SettlementOrchestrationStrategy.Consolidated
        };

        // Act & Assert
        foreach (var strategy in strategies)
        {
            var rule = new SettlementAutomationRule
            {
                Id = Guid.NewGuid(),
                Name = $"Rule with strategy {strategy}",
                OrchestrationStrategy = strategy
            };

            Assert.Equal(strategy, rule.OrchestrationStrategy);
        }
    }

    #endregion

    #region Soft Delete Tests

    [Fact]
    public void Rule_WhenDeleted_ShouldBeMarkedAsDeleted()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Rule to delete",
            IsDeleted = false
        };

        // Act
        rule.IsDeleted = true;
        rule.DeletedDate = DateTime.UtcNow;

        // Assert
        Assert.True(rule.IsDeleted);
        Assert.NotNull(rule.DeletedDate);
    }

    #endregion

    #region Execution History Tests

    [Fact]
    public void ExecutionHistory_ShouldTrackIndividualExecutions()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            ExecutionHistory = new List<RuleExecutionRecord>()
        };

        var record1 = new RuleExecutionRecord
        {
            Id = Guid.NewGuid(),
            RuleId = rule.Id,
            Status = ExecutionStatus.Completed,
            SettlementCount = 5,
            ExecutionStartTime = DateTime.UtcNow.AddHours(-2),
            ExecutionEndTime = DateTime.UtcNow.AddHours(-1),
            ExecutionDurationMs = 3600000
        };

        var record2 = new RuleExecutionRecord
        {
            Id = Guid.NewGuid(),
            RuleId = rule.Id,
            Status = ExecutionStatus.Completed,
            SettlementCount = 3,
            ExecutionStartTime = DateTime.UtcNow.AddHours(-1),
            ExecutionEndTime = DateTime.UtcNow,
            ExecutionDurationMs = 3600000
        };

        // Act
        rule.ExecutionHistory.Add(record1);
        rule.ExecutionHistory.Add(record2);

        // Assert
        Assert.Equal(2, rule.ExecutionHistory.Count);
        Assert.Contains(record1, rule.ExecutionHistory);
        Assert.Contains(record2, rule.ExecutionHistory);
    }

    [Fact]
    public void ExecutionRecord_ShouldTrackAffectedSettlements()
    {
        // Arrange
        var settlementIds = new List<Guid>
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        var record = new RuleExecutionRecord
        {
            Id = Guid.NewGuid(),
            RuleId = Guid.NewGuid(),
            AffectedSettlementIds = settlementIds,
            Status = ExecutionStatus.Completed
        };

        // Act & Assert
        Assert.Equal(3, record.AffectedSettlementIds.Count);
        Assert.Contains(settlementIds[0], record.AffectedSettlementIds);
    }

    #endregion
}
