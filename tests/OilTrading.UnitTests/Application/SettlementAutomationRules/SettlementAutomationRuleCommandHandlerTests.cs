using FluentAssertions;
using OilTrading.Core.Entities;
using Xunit;

namespace OilTrading.UnitTests.Application.SettlementAutomationRules;

/// <summary>
/// Unit tests for Settlement Automation Rule command operations
/// Tests: Domain command logic (Enable, Disable, Update, Execution Recording)
/// Fixed for Data Lineage Enhancement v2.18.0 - method signatures aligned with entity
/// </summary>
public class SettlementAutomationRuleCommandHandlerTests
{
    private const string TestUser = "test-user";

    #region Enable Command Tests

    [Fact]
    public void EnableCommand_DisabledRule_ShouldEnableRule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test description", SettlementRuleType.AutoSettlement);
        rule.Disable("Initial disable");
        rule.IsEnabled.Should().BeFalse();

        // Act
        rule.Enable();

        // Assert
        rule.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void EnableCommand_AlreadyEnabled_ShouldRemainEnabled()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test description", SettlementRuleType.AutoSettlement);
        rule.IsEnabled.Should().BeTrue(); // Enabled by default

        // Act
        rule.Enable();

        // Assert
        rule.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region Disable Command Tests

    [Fact]
    public void DisableCommand_EnabledRule_ShouldDisableRule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test description", SettlementRuleType.AutoSettlement);
        rule.IsEnabled.Should().BeTrue();

        // Act
        rule.Disable("Manual disable for testing");

        // Assert
        rule.IsEnabled.Should().BeFalse();
        rule.DisabledReason.Should().Be("Manual disable for testing");
    }

    [Fact]
    public void DisableCommand_ShouldSetDisabledDate()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test description", SettlementRuleType.AutoSettlement);

        // Act
        var beforeDisable = DateTime.UtcNow;
        rule.Disable("Testing");
        var afterDisable = DateTime.UtcNow;

        // Assert
        rule.DisabledDate.Should().NotBeNull();
        rule.DisabledDate.Should().BeOnOrAfter(beforeDisable);
        rule.DisabledDate.Should().BeOnOrBefore(afterDisable);
    }

    #endregion

    #region Update Basic Info Command Tests

    [Fact]
    public void UpdateBasicInfoCommand_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Original Name", "Original Description", SettlementRuleType.AutoSettlement);

        // Act
        rule.UpdateBasicInfo("Updated Name", "Updated Description", "High", "Notes", TestUser);

        // Assert
        rule.Name.Should().Be("Updated Name");
        rule.Description.Should().Be("Updated Description");
        rule.Priority.Should().Be("High");
        rule.Notes.Should().Be("Notes");
        rule.LastModifiedBy.Should().Be(TestUser);
    }

    [Fact]
    public void UpdateBasicInfoCommand_ShouldUpdateLastModifiedDate()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test", "Test", SettlementRuleType.AutoSettlement);
        var beforeUpdate = DateTime.UtcNow;

        // Act
        rule.UpdateBasicInfo("New", "New", "Normal", null, TestUser);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        rule.LastModifiedDate.Should().NotBeNull();
        rule.LastModifiedDate.Should().BeOnOrAfter(beforeUpdate);
        rule.LastModifiedDate.Should().BeOnOrBefore(afterUpdate);
    }

    #endregion

    #region Update Trigger Command Tests

    [Fact]
    public void UpdateTriggerCommand_ShouldUpdateTriggerAndSchedule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);

        // Act
        rule.UpdateTrigger(SettlementRuleTrigger.OnSchedule, "0 9 * * *", TestUser);

        // Assert
        rule.Trigger.Should().Be(SettlementRuleTrigger.OnSchedule);
        rule.ScheduleExpression.Should().Be("0 9 * * *");
    }

    [Fact]
    public void UpdateTriggerCommand_ToContractCompletion_ShouldClearSchedule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);
        rule.UpdateTrigger(SettlementRuleTrigger.OnSchedule, "0 9 * * *", TestUser);

        // Act
        rule.UpdateTrigger(SettlementRuleTrigger.OnContractCompletion, null, TestUser);

        // Assert
        rule.Trigger.Should().Be(SettlementRuleTrigger.OnContractCompletion);
        rule.ScheduleExpression.Should().BeNull();
    }

    #endregion

    #region Update Scope Command Tests

    [Fact]
    public void UpdateScopeCommand_ShouldUpdateScopeAndFilter()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);

        // Act
        rule.UpdateScope(SettlementRuleScope.ByPartner, "Partner-001", TestUser);

        // Assert
        rule.Scope.Should().Be(SettlementRuleScope.ByPartner);
        rule.ScopeFilter.Should().Be("Partner-001");
    }

    [Fact]
    public void UpdateScopeCommand_ToAll_ShouldClearFilter()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);
        rule.UpdateScope(SettlementRuleScope.ByProduct, "Brent", TestUser);

        // Act
        rule.UpdateScope(SettlementRuleScope.All, null, TestUser);

        // Assert
        rule.Scope.Should().Be(SettlementRuleScope.All);
        rule.ScopeFilter.Should().BeNull();
    }

    #endregion

    #region Update Orchestration Command Tests

    [Fact]
    public void UpdateOrchestrationCommand_ShouldUpdateStrategyAndSettings()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);

        // Act
        rule.UpdateOrchestration(SettlementOrchestrationStrategy.Grouped, 50, "bypartner", TestUser);

        // Assert
        rule.OrchestrationStrategy.Should().Be(SettlementOrchestrationStrategy.Grouped);
        rule.MaxSettlementsPerExecution.Should().Be(50);
        rule.GroupingDimension.Should().Be("bypartner");
    }

    [Fact]
    public void UpdateOrchestrationCommand_ToParallel_ShouldSetStrategy()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);

        // Act
        rule.UpdateOrchestration(SettlementOrchestrationStrategy.Parallel, 100, null, TestUser);

        // Assert
        rule.OrchestrationStrategy.Should().Be(SettlementOrchestrationStrategy.Parallel);
        rule.MaxSettlementsPerExecution.Should().Be(100);
    }

    #endregion

    #region Execution Recording Command Tests

    [Fact]
    public void ExecuteCommand_RecordSuccessfulExecution_ShouldUpdateCounters()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);
        var beforeExecute = DateTime.UtcNow;

        // Act
        rule.RecordSuccessfulExecution(5);
        var afterExecute = DateTime.UtcNow;

        // Assert
        rule.ExecutionCount.Should().Be(1);
        rule.SuccessCount.Should().Be(1);
        rule.FailureCount.Should().Be(0);
        rule.LastExecutionSettlementCount.Should().Be(5);
        rule.LastExecutedDate.Should().NotBeNull();
        rule.LastExecutedDate.Should().BeOnOrAfter(beforeExecute);
        rule.LastExecutedDate.Should().BeOnOrBefore(afterExecute);
    }

    [Fact]
    public void ExecuteCommand_RecordFailedExecution_ShouldUpdateFailureCounters()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);

        // Act
        rule.RecordFailedExecution("Settlement not found");

        // Assert
        rule.ExecutionCount.Should().Be(1);
        rule.SuccessCount.Should().Be(0);
        rule.FailureCount.Should().Be(1);
        rule.LastExecutionError.Should().Be("Settlement not found");
    }

    [Fact]
    public void ExecuteCommand_MultipleExecutions_ShouldAccumulateMetrics()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);

        // Act
        rule.RecordSuccessfulExecution(5);
        rule.RecordSuccessfulExecution(3);
        rule.RecordFailedExecution("Error");
        rule.RecordSuccessfulExecution(7);

        // Assert
        rule.ExecutionCount.Should().Be(4);
        rule.SuccessCount.Should().Be(3);
        rule.FailureCount.Should().Be(1);
    }

    #endregion

    #region Version Management Tests

    [Fact]
    public void IncrementVersion_ShouldIncrementRuleVersion()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test", "Test", SettlementRuleType.AutoSettlement);
        var initialVersion = rule.RuleVersion;

        // Act
        rule.IncrementVersion(TestUser);

        // Assert
        rule.RuleVersion.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void MultipleVersionIncrements_ShouldIncrementSequentially()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test", "Test", SettlementRuleType.AutoSettlement);
        var v0 = rule.RuleVersion;

        // Act
        rule.IncrementVersion(TestUser);
        var v1 = rule.RuleVersion;

        rule.IncrementVersion(TestUser);
        var v2 = rule.RuleVersion;

        rule.IncrementVersion(TestUser);
        var v3 = rule.RuleVersion;

        // Assert
        v1.Should().BeGreaterThan(v0);
        v2.Should().BeGreaterThan(v1);
        v3.Should().BeGreaterThan(v2);
        v3.Should().Be(4);
    }

    #endregion

    #region Audit Trail Tests

    [Fact]
    public void Commands_ShouldMaintainAuditTrail()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test description", SettlementRuleType.AutoSettlement, "High", "original-user");
        var createdDate = rule.CreatedDate;
        var createdBy = rule.CreatedBy;

        // Act
        rule.UpdateBasicInfo("Modified", "Modified", "Low", null, "modifier-user");
        var modifiedDate = rule.LastModifiedDate;

        // Assert
        createdBy.Should().Be("original-user");
        rule.CreatedDate.Should().Be(createdDate);
        modifiedDate.Should().NotBeNull();
        modifiedDate.Should().BeAfter(createdDate);
        rule.LastModifiedBy.Should().Be("modifier-user");
    }

    #endregion

    #region Execution History Tests

    [Fact]
    public void ExecuteCommand_ShouldCreateExecutionRecord()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);
        var record = new RuleExecutionRecord(rule.Id, "ContractCompletion");
        record.Complete(5, 2, 1);

        // Act
        rule.ExecutionHistory.Add(record);

        // Assert
        rule.ExecutionHistory.Should().HaveCount(1);
        rule.ExecutionHistory.First().RuleId.Should().Be(rule.Id);
        rule.ExecutionHistory.First().Status.Should().Be(ExecutionStatus.Completed);
        rule.ExecutionHistory.First().SettlementCount.Should().Be(5);
    }

    [Fact]
    public void ExecuteCommand_FailedExecution_ShouldCreateFailedRecord()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);
        var record = new RuleExecutionRecord(rule.Id, "ScheduledJob");
        record.Failed("Database connection timeout");

        // Act
        rule.ExecutionHistory.Add(record);

        // Assert
        rule.ExecutionHistory.Should().HaveCount(1);
        rule.ExecutionHistory.First().Status.Should().Be(ExecutionStatus.Failed);
        rule.ExecutionHistory.First().ErrorMessage.Should().Be("Database connection timeout");
    }

    #endregion

    #region Condition and Action Management Tests

    [Fact]
    public void AddCondition_ShouldAddToConditionsCollection()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);
        var condition = new SettlementRuleCondition(rule.Id, "Amount", "GreaterThan", "10000", 1);

        // Act
        rule.Conditions.Add(condition);

        // Assert
        rule.Conditions.Should().HaveCount(1);
        rule.Conditions.First().Field.Should().Be("Amount");
    }

    [Fact]
    public void AddAction_ShouldAddToActionsCollection()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);
        var action = new SettlementRuleAction(rule.Id, "ApproveSettlement", 1);

        // Act
        rule.Actions.Add(action);

        // Assert
        rule.Actions.Should().HaveCount(1);
        rule.Actions.First().ActionType.Should().Be("ApproveSettlement");
    }

    [Fact]
    public void AddMultipleConditions_ShouldMaintainSequence()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Test", SettlementRuleType.AutoSettlement);

        // Act
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "Amount", "GreaterThan", "5000", 1));
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "Partner", "Equals", "Acme Corp", 2));
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "Currency", "In", "USD,EUR", 3));

        // Assert
        rule.Conditions.Should().HaveCount(3);
        rule.Conditions.OrderBy(c => c.SequenceNumber).First().SequenceNumber.Should().Be(1);
        rule.Conditions.OrderBy(c => c.SequenceNumber).Last().SequenceNumber.Should().Be(3);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void IsValid_WithConditionsAndActions_ShouldReturnTrue()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Valid Rule", "Test", SettlementRuleType.AutoSettlement);
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "Amount", "GreaterThan", "1000", 1));
        rule.Actions.Add(new SettlementRuleAction(rule.Id, "CreateSettlement", 1));

        // Act & Assert
        rule.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithoutConditions_ShouldReturnFalse()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Invalid Rule", "Test", SettlementRuleType.AutoSettlement);
        rule.Actions.Add(new SettlementRuleAction(rule.Id, "CreateSettlement", 1));

        // Act & Assert
        rule.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithoutActions_ShouldReturnFalse()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Invalid Rule", "Test", SettlementRuleType.AutoSettlement);
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "Amount", "GreaterThan", "1000", 1));

        // Act & Assert
        rule.IsValid().Should().BeFalse();
    }

    #endregion
}
