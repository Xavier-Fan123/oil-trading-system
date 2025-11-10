using Xunit;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using System;
using System.Collections.Generic;

namespace OilTrading.UnitTests.Application.SettlementAutomationRules;

/// <summary>
/// Unit tests for Settlement Automation Rule command operations
/// Tests: Domain command logic (Enable, Disable, Update, Execution Recording)
/// Note: Handler tests are excluded due to entity design changes requiring proper constructors
/// </summary>
public class SettlementAutomationRuleCommandHandlerTests
{
    #region Enable Command Tests

    [Fact]
    public void EnableCommand_DisabledRule_ShouldEnableRule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test description", SettlementRuleType.Automatic);
        Assert.False(rule.IsEnabled);

        // Act
        rule.Enable();

        // Assert
        Assert.True(rule.IsEnabled);
    }

    [Fact]
    public void EnableCommand_AlreadyEnabled_ShouldRemainEnabled()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test description", SettlementRuleType.Automatic);
        rule.Enable();

        // Act
        rule.Enable();

        // Assert
        Assert.True(rule.IsEnabled);
    }

    #endregion

    #region Disable Command Tests

    [Fact]
    public void DisableCommand_EnabledRule_ShouldDisableRule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test description", SettlementRuleType.Automatic);
        rule.Enable();
        Assert.True(rule.IsEnabled);

        // Act
        rule.Disable("Manual disable for testing");

        // Assert
        Assert.False(rule.IsEnabled);
        Assert.Equal("Manual disable for testing", rule.DisabledReason);
    }

    [Fact]
    public void DisableCommand_ShouldSetDisabledDate()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test description", SettlementRuleType.Automatic);
        rule.Enable();

        // Act
        var beforeDisable = DateTime.UtcNow;
        rule.Disable("Testing");
        var afterDisable = DateTime.UtcNow;

        // Assert
        Assert.NotNull(rule.DisabledDate);
        Assert.True(rule.DisabledDate >= beforeDisable && rule.DisabledDate <= afterDisable);
    }

    #endregion

    #region Update Basic Info Command Tests

    [Fact]
    public void UpdateBasicInfoCommand_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Original Name", "Original Description", SettlementRuleType.Automatic);
        var originalVersion = rule.RuleVersion;

        // Act
        rule.UpdateBasicInfo("Updated Name", "Updated Description");

        // Assert
        Assert.Equal("Updated Name", rule.Name);
        Assert.Equal("Updated Description", rule.Description);
        Assert.Equal(originalVersion + 1, rule.RuleVersion);
    }

    [Fact]
    public void UpdateBasicInfoCommand_ShouldUpdateLastModifiedDate()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test", "Test", SettlementRuleType.Automatic);
        var beforeUpdate = DateTime.UtcNow;

        // Act
        rule.UpdateBasicInfo("New", "New");
        var afterUpdate = DateTime.UtcNow;

        // Assert
        Assert.NotNull(rule.LastModifiedDate);
        Assert.True(rule.LastModifiedDate >= beforeUpdate && rule.LastModifiedDate <= afterUpdate);
    }

    #endregion

    #region Update Trigger Command Tests

    [Fact]
    public void UpdateTriggerCommand_ShouldUpdateTriggerAndSchedule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.Automatic);

        // Act
        rule.UpdateTrigger(SettlementRuleTrigger.Scheduled, "0 9 * * *");

        // Assert
        Assert.Equal(SettlementRuleTrigger.Scheduled, rule.Trigger);
        Assert.Equal("0 9 * * *", rule.ScheduleExpression);
    }

    #endregion

    #region Update Scope Command Tests

    [Fact]
    public void UpdateScopeCommand_ShouldUpdateScopeAndFilter()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.Automatic);

        // Act
        rule.UpdateScope(SettlementRuleScope.ByCurrency, "USD");

        // Assert
        Assert.Equal(SettlementRuleScope.ByCurrency, rule.Scope);
        Assert.Equal("USD", rule.ScopeFilter);
    }

    #endregion

    #region Update Orchestration Command Tests

    [Fact]
    public void UpdateOrchestrationCommand_ShouldUpdateStrategyAndSettings()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.Automatic);

        // Act
        rule.UpdateOrchestration(SettlementOrchestrationStrategy.Grouped, 50, "bypartner");

        // Assert
        Assert.Equal(SettlementOrchestrationStrategy.Grouped, rule.OrchestrationStrategy);
        Assert.Equal(50, rule.MaxSettlementsPerExecution);
        Assert.Equal("bypartner", rule.GroupingDimension);
    }

    #endregion

    #region Execution Recording Command Tests

    [Fact]
    public void ExecuteCommand_RecordSuccessfulExecution_ShouldUpdateCounters()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.Automatic);
        var beforeExecute = DateTime.UtcNow;

        // Act
        rule.RecordSuccessfulExecution(5);
        var afterExecute = DateTime.UtcNow;

        // Assert
        Assert.Equal(1, rule.ExecutionCount);
        Assert.Equal(1, rule.SuccessCount);
        Assert.Equal(0, rule.FailureCount);
        Assert.Equal(5, rule.LastExecutionSettlementCount);
        Assert.NotNull(rule.LastExecutedDate);
        Assert.True(rule.LastExecutedDate >= beforeExecute && rule.LastExecutedDate <= afterExecute);
    }

    [Fact]
    public void ExecuteCommand_RecordFailedExecution_ShouldUpdateFailureCounters()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.Automatic);

        // Act
        rule.RecordFailedExecution("Settlement not found");

        // Assert
        Assert.Equal(1, rule.ExecutionCount);
        Assert.Equal(0, rule.SuccessCount);
        Assert.Equal(1, rule.FailureCount);
        Assert.Equal("Settlement not found", rule.LastExecutionError);
    }

    [Fact]
    public void ExecuteCommand_MultipleExecutions_ShouldAccumulateMetrics()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.Automatic);

        // Act
        rule.RecordSuccessfulExecution(5);
        rule.RecordSuccessfulExecution(3);
        rule.RecordFailedExecution("Error");
        rule.RecordSuccessfulExecution(7);

        // Assert
        Assert.Equal(4, rule.ExecutionCount);
        Assert.Equal(3, rule.SuccessCount);
        Assert.Equal(1, rule.FailureCount);
    }

    #endregion

    #region Version Management Tests

    [Fact]
    public void UpdateOperation_ShouldIncrementVersion()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test", "Test", SettlementRuleType.Automatic);
        var initialVersion = rule.RuleVersion;

        // Act
        rule.UpdateBasicInfo("New Name", "New Description");

        // Assert
        Assert.Equal(initialVersion + 1, rule.RuleVersion);
    }

    [Fact]
    public void MultipleUpdates_ShouldIncrementVersionSequentially()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test", "Test", SettlementRuleType.Automatic);
        var v0 = rule.RuleVersion;

        // Act
        rule.UpdateBasicInfo("Update 1", "Desc 1");
        var v1 = rule.RuleVersion;

        rule.UpdateTrigger(SettlementRuleTrigger.Scheduled, "0 0 * * *");
        var v2 = rule.RuleVersion;

        rule.UpdateScope(SettlementRuleScope.PurchaseOnly, null);
        var v3 = rule.RuleVersion;

        // Assert
        Assert.True(v1 > v0);
        Assert.True(v2 > v1);
        Assert.True(v3 > v2);
    }

    #endregion

    #region Audit Trail Tests

    [Fact]
    public void Commands_ShouldMaintainAuditTrail()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test description", SettlementRuleType.Automatic, "High", "original-user");
        var createdDate = rule.CreatedDate;
        var createdBy = rule.CreatedBy;

        // Act
        rule.UpdateBasicInfo("Modified", "Modified");
        var modifiedDate = rule.LastModifiedDate;

        // Assert
        Assert.Equal("original-user", createdBy);
        Assert.Equal(createdDate, rule.CreatedDate);
        Assert.NotNull(modifiedDate);
        Assert.True(modifiedDate > createdDate);
    }

    #endregion

    #region Execution History Tests

    [Fact]
    public void ExecuteCommand_ShouldCreateExecutionRecord()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.Automatic);
        var record = new RuleExecutionRecord
        {
            Id = Guid.NewGuid(),
            RuleId = rule.Id,
            ExecutionStartTime = DateTime.UtcNow,
            Status = ExecutionStatus.Completed,
            SettlementCount = 5
        };

        // Act
        rule.ExecutionHistory.Add(record);

        // Assert
        Assert.Single(rule.ExecutionHistory);
        Assert.Equal(rule.Id, rule.ExecutionHistory[0].RuleId);
    }

    #endregion
}
