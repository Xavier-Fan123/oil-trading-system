using FluentAssertions;
using OilTrading.Core.Entities;
using Xunit;

namespace OilTrading.UnitTests.Application.SettlementAutomationRules;

/// <summary>
/// Unit tests for SettlementAutomationRule domain entity
/// Tests: Creation, state transitions, update operations, execution recording
/// Fixed for Data Lineage Enhancement v2.18.0 - method signatures aligned with entity
/// </summary>
public class SettlementAutomationRuleTests
{
    private const string TestUser = "test-user";

    #region Creation and Initialization Tests

    [Fact]
    public void Create_ValidRule_ShouldInitializeWithDefaults()
    {
        // Arrange
        var ruleName = "Test Rule";
        var description = "Test rule description";
        var ruleType = SettlementRuleType.AutoSettlement;

        // Act
        var rule = new SettlementAutomationRule(ruleName, description, ruleType, "High", TestUser);

        // Assert
        rule.Id.Should().NotBeEmpty();
        rule.Name.Should().Be(ruleName);
        rule.Description.Should().Be(description);
        rule.RuleType.Should().Be(ruleType);
        rule.Status.Should().Be(RuleStatus.Active);
        rule.IsEnabled.Should().BeTrue();
        rule.RuleVersion.Should().Be(1);
        rule.ExecutionCount.Should().Be(0);
        rule.Priority.Should().Be("High");
        rule.CreatedBy.Should().Be(TestUser);
    }

    [Fact]
    public void Create_WithDefaultPriority_ShouldUseNormal()
    {
        // Act
        var rule = new SettlementAutomationRule("Test", "Description", SettlementRuleType.AutoSettlement);

        // Assert
        rule.Priority.Should().Be("Normal");
    }

    [Fact]
    public void Create_WithConditions_ShouldAddConditionsToRule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Rule with conditions", "Description", SettlementRuleType.AutoSettlement);
        var condition = new SettlementRuleCondition(
            rule.Id,
            "SettlementAmount",
            "GREATERTHAN",
            "10000",
            1);

        // Act
        rule.Conditions.Add(condition);

        // Assert
        rule.Conditions.Should().HaveCount(1);
        rule.Conditions.First().Field.Should().Be("SettlementAmount");
        rule.Conditions.First().OperatorType.Should().Be("GREATERTHAN");
        rule.Conditions.First().Value.Should().Be("10000");
    }

    [Fact]
    public void Create_WithActions_ShouldAddActionsToRule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Rule with actions", "Description", SettlementRuleType.AutoSettlement);
        var action = new SettlementRuleAction(rule.Id, "CreateSettlement", 1);

        // Act
        rule.Actions.Add(action);

        // Assert
        rule.Actions.Should().HaveCount(1);
        rule.Actions.First().ActionType.Should().Be("CreateSettlement");
        rule.Actions.First().SequenceNumber.Should().Be(1);
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new SettlementAutomationRule(null!, "Description", SettlementRuleType.AutoSettlement);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void Create_WithNullDescription_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new SettlementAutomationRule("Name", null!, SettlementRuleType.AutoSettlement);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("description");
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public void Enable_DisabledRule_ShouldEnableRule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        rule.Disable("Testing");
        rule.IsEnabled.Should().BeFalse();

        // Act
        rule.Enable();

        // Assert
        rule.IsEnabled.Should().BeTrue();
        rule.DisabledDate.Should().BeNull();
        rule.DisabledReason.Should().BeNull();
    }

    [Fact]
    public void Disable_EnabledRule_ShouldDisableRule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        rule.IsEnabled.Should().BeTrue();

        // Act
        rule.Disable("Testing disable functionality");

        // Assert
        rule.IsEnabled.Should().BeFalse();
        rule.DisabledReason.Should().Be("Testing disable functionality");
        rule.DisabledDate.Should().NotBeNull();
        rule.DisabledDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Disable_WithNoReason_ShouldDisableWithNullReason()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);

        // Act
        rule.Disable();

        // Assert
        rule.IsEnabled.Should().BeFalse();
        rule.DisabledReason.Should().BeNull();
        rule.DisabledDate.Should().NotBeNull();
    }

    [Fact]
    public void Enable_ThenDisable_ShouldUpdateStateCorrectly()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);

        // Act & Assert - Cycle through states
        rule.Disable("First disable");
        rule.IsEnabled.Should().BeFalse();

        rule.Enable();
        rule.IsEnabled.Should().BeTrue();
        rule.DisabledReason.Should().BeNull();

        rule.Disable("Second disable");
        rule.IsEnabled.Should().BeFalse();

        rule.Enable();
        rule.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Enable_AlreadyEnabled_ShouldNotChangeState()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        var originalModifiedDate = rule.LastModifiedDate;

        // Act
        rule.Enable(); // Already enabled

        // Assert
        rule.IsEnabled.Should().BeTrue();
        rule.LastModifiedDate.Should().Be(originalModifiedDate);
    }

    [Fact]
    public void Disable_AlreadyDisabled_ShouldNotChangeState()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        rule.Disable("First disable");
        var originalDisabledDate = rule.DisabledDate;

        // Act
        rule.Disable("Second disable"); // Already disabled

        // Assert
        rule.IsEnabled.Should().BeFalse();
        rule.DisabledDate.Should().Be(originalDisabledDate);
        rule.DisabledReason.Should().Be("First disable"); // Original reason preserved
    }

    #endregion

    #region Update Operations Tests

    [Fact]
    public void UpdateBasicInfo_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Old Name", "Old Description", SettlementRuleType.AutoSettlement);
        var newName = "New Name";
        var newDescription = "New Description";

        // Act
        rule.UpdateBasicInfo(newName, newDescription, "High", "Some notes", TestUser);

        // Assert
        rule.Name.Should().Be(newName);
        rule.Description.Should().Be(newDescription);
        rule.Priority.Should().Be("High");
        rule.Notes.Should().Be("Some notes");
        rule.LastModifiedBy.Should().Be(TestUser);
        rule.LastModifiedDate.Should().NotBeNull();
        rule.LastModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateBasicInfo_WithNullNotes_ShouldClearNotes()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Name", "Description", SettlementRuleType.AutoSettlement);
        rule.UpdateBasicInfo("Name", "Description", "Normal", "Initial notes", TestUser);
        rule.Notes.Should().Be("Initial notes");

        // Act
        rule.UpdateBasicInfo("Name", "Description", "Normal", null, TestUser);

        // Assert
        rule.Notes.Should().BeNull();
    }

    [Fact]
    public void UpdateTrigger_ShouldUpdateTriggerAndSchedule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        var newTrigger = SettlementRuleTrigger.OnSchedule;
        var newSchedule = "0 0 * * *";

        // Act
        rule.UpdateTrigger(newTrigger, newSchedule, TestUser);

        // Assert
        rule.Trigger.Should().Be(newTrigger);
        rule.ScheduleExpression.Should().Be(newSchedule);
        rule.LastModifiedBy.Should().Be(TestUser);
        rule.LastModifiedDate.Should().NotBeNull();
    }

    [Fact]
    public void UpdateTrigger_ToManual_ShouldClearSchedule()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        rule.UpdateTrigger(SettlementRuleTrigger.OnSchedule, "0 0 * * *", TestUser);

        // Act
        rule.UpdateTrigger(SettlementRuleTrigger.OnManualTrigger, null, TestUser);

        // Assert
        rule.Trigger.Should().Be(SettlementRuleTrigger.OnManualTrigger);
        rule.ScheduleExpression.Should().BeNull();
    }

    [Fact]
    public void UpdateScope_ShouldUpdateScopeAndFilter()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        var newScope = SettlementRuleScope.PurchaseOnly;
        var newFilter = "USD";

        // Act
        rule.UpdateScope(newScope, newFilter, TestUser);

        // Assert
        rule.Scope.Should().Be(newScope);
        rule.ScopeFilter.Should().Be(newFilter);
        rule.LastModifiedBy.Should().Be(TestUser);
    }

    [Fact]
    public void UpdateScope_ToAll_ShouldClearFilter()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        rule.UpdateScope(SettlementRuleScope.ByPartner, "Partner-001", TestUser);

        // Act
        rule.UpdateScope(SettlementRuleScope.All, null, TestUser);

        // Assert
        rule.Scope.Should().Be(SettlementRuleScope.All);
        rule.ScopeFilter.Should().BeNull();
    }

    [Fact]
    public void UpdateOrchestration_ShouldUpdateStrategyAndSettings()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        var newStrategy = SettlementOrchestrationStrategy.Parallel;
        var maxSettlements = 100;
        var grouping = "bypartner";

        // Act
        rule.UpdateOrchestration(newStrategy, maxSettlements, grouping, TestUser);

        // Assert
        rule.OrchestrationStrategy.Should().Be(newStrategy);
        rule.MaxSettlementsPerExecution.Should().Be(maxSettlements);
        rule.GroupingDimension.Should().Be(grouping);
        rule.LastModifiedBy.Should().Be(TestUser);
    }

    [Fact]
    public void UpdateOrchestration_ToSequential_ShouldClearGrouping()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        rule.UpdateOrchestration(SettlementOrchestrationStrategy.Grouped, 50, "byproduct", TestUser);

        // Act
        rule.UpdateOrchestration(SettlementOrchestrationStrategy.Sequential, null, null, TestUser);

        // Assert
        rule.OrchestrationStrategy.Should().Be(SettlementOrchestrationStrategy.Sequential);
        rule.MaxSettlementsPerExecution.Should().BeNull();
        rule.GroupingDimension.Should().BeNull();
    }

    [Fact]
    public void IncrementVersion_ShouldIncrementRuleVersion()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        var initialVersion = rule.RuleVersion;

        // Act
        rule.IncrementVersion(TestUser);

        // Assert
        rule.RuleVersion.Should().Be(initialVersion + 1);
        rule.LastModifiedBy.Should().Be(TestUser);
        rule.LastModifiedDate.Should().NotBeNull();
    }

    [Fact]
    public void MultipleVersionIncrements_ShouldAccumulate()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);

        // Act
        rule.IncrementVersion(TestUser);
        rule.IncrementVersion(TestUser);
        rule.IncrementVersion(TestUser);

        // Assert
        rule.RuleVersion.Should().Be(4); // Started at 1, incremented 3 times
    }

    #endregion

    #region Execution Recording Tests

    [Fact]
    public void RecordSuccessfulExecution_ShouldUpdateExecutionCounters()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        var settlementCount = 5;

        // Act
        rule.RecordSuccessfulExecution(settlementCount);

        // Assert
        rule.ExecutionCount.Should().Be(1);
        rule.SuccessCount.Should().Be(1);
        rule.FailureCount.Should().Be(0);
        rule.LastExecutionSettlementCount.Should().Be(settlementCount);
        rule.LastExecutedDate.Should().NotBeNull();
        rule.LastExecutedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        rule.LastExecutionError.Should().BeNull();
    }

    [Fact]
    public void RecordFailedExecution_ShouldUpdateFailureCounters()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        var errorMessage = "Settlement not found";

        // Act
        rule.RecordFailedExecution(errorMessage);

        // Assert
        rule.ExecutionCount.Should().Be(1);
        rule.SuccessCount.Should().Be(0);
        rule.FailureCount.Should().Be(1);
        rule.LastExecutionError.Should().Be(errorMessage);
        rule.LastExecutedDate.Should().NotBeNull();
        rule.LastExecutionSettlementCount.Should().BeNull();
    }

    [Fact]
    public void MultipleExecutions_ShouldAccumulateCounters()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);

        // Act
        rule.RecordSuccessfulExecution(5);
        rule.RecordSuccessfulExecution(3);
        rule.RecordFailedExecution("Error");
        rule.RecordSuccessfulExecution(7);

        // Assert
        rule.ExecutionCount.Should().Be(4);  // 4 total executions
        rule.SuccessCount.Should().Be(3);    // 3 successful
        rule.FailureCount.Should().Be(1);    // 1 failed
        rule.LastExecutionSettlementCount.Should().Be(7);  // Last was 7 settlements
        rule.LastExecutionError.Should().BeNull(); // Cleared by last success
    }

    [Fact]
    public void RecordSuccessfulExecution_ShouldClearPreviousError()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);
        rule.RecordFailedExecution("Previous error");
        rule.LastExecutionError.Should().Be("Previous error");

        // Act
        rule.RecordSuccessfulExecution(10);

        // Assert
        rule.LastExecutionError.Should().BeNull();
    }

    #endregion

    #region Business Rule Validation Tests

    [Fact]
    public void IsValid_WithNoConditions_ShouldReturnFalse()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Rule without conditions", "Description", SettlementRuleType.AutoSettlement);
        rule.Actions.Add(new SettlementRuleAction(rule.Id, "CreateSettlement", 1));

        // Act & Assert
        rule.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithNoActions_ShouldReturnFalse()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Rule without actions", "Description", SettlementRuleType.AutoSettlement);
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "Amount", "GreaterThan", "1000", 1));

        // Act & Assert
        rule.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithConditionsAndActions_ShouldReturnTrue()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Valid Rule", "Description", SettlementRuleType.AutoSettlement);
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "Amount", "GreaterThan", "1000", 1));
        rule.Actions.Add(new SettlementRuleAction(rule.Id, "CreateSettlement", 1));

        // Act & Assert
        rule.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_ScheduledTriggerWithoutExpression_ShouldReturnFalse()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Scheduled Rule", "Description", SettlementRuleType.AutoSettlement);
        rule.UpdateTrigger(SettlementRuleTrigger.OnSchedule, null, TestUser);
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "Amount", "GreaterThan", "1000", 1));
        rule.Actions.Add(new SettlementRuleAction(rule.Id, "CreateSettlement", 1));

        // Act & Assert
        rule.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ScheduledTriggerWithExpression_ShouldReturnTrue()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Scheduled Rule", "Description", SettlementRuleType.AutoSettlement);
        rule.UpdateTrigger(SettlementRuleTrigger.OnSchedule, "0 0 * * *", TestUser);
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "Amount", "GreaterThan", "1000", 1));
        rule.Actions.Add(new SettlementRuleAction(rule.Id, "CreateSettlement", 1));

        // Act & Assert
        rule.IsValid().Should().BeTrue();
    }

    #endregion

    #region Scope and Trigger Enum Coverage Tests

    [Theory]
    [InlineData(SettlementRuleScope.All)]
    [InlineData(SettlementRuleScope.PurchaseOnly)]
    [InlineData(SettlementRuleScope.SalesOnly)]
    [InlineData(SettlementRuleScope.ByPartner)]
    [InlineData(SettlementRuleScope.ByProduct)]
    [InlineData(SettlementRuleScope.ByQuantityRange)]
    public void Rule_CanHaveDifferentScopes(SettlementRuleScope scope)
    {
        // Arrange
        var rule = new SettlementAutomationRule($"Rule with scope {scope}", "Description", SettlementRuleType.AutoSettlement);

        // Act
        rule.UpdateScope(scope, scope == SettlementRuleScope.All ? null : "filter-value", TestUser);

        // Assert
        rule.Scope.Should().Be(scope);
    }

    [Theory]
    [InlineData(SettlementRuleTrigger.OnContractCompletion)]
    [InlineData(SettlementRuleTrigger.OnSettlementCreation)]
    [InlineData(SettlementRuleTrigger.OnSchedule)]
    [InlineData(SettlementRuleTrigger.OnManualTrigger)]
    public void Rule_CanHaveDifferentTriggers(SettlementRuleTrigger trigger)
    {
        // Arrange
        var rule = new SettlementAutomationRule($"Rule with trigger {trigger}", "Description", SettlementRuleType.AutoSettlement);
        var schedule = trigger == SettlementRuleTrigger.OnSchedule ? "0 0 * * *" : null;

        // Act
        rule.UpdateTrigger(trigger, schedule, TestUser);

        // Assert
        rule.Trigger.Should().Be(trigger);
    }

    [Theory]
    [InlineData(SettlementOrchestrationStrategy.Sequential)]
    [InlineData(SettlementOrchestrationStrategy.Parallel)]
    [InlineData(SettlementOrchestrationStrategy.Grouped)]
    [InlineData(SettlementOrchestrationStrategy.Consolidated)]
    public void Rule_CanHaveDifferentOrchestrationStrategies(SettlementOrchestrationStrategy strategy)
    {
        // Arrange
        var rule = new SettlementAutomationRule($"Rule with strategy {strategy}", "Description", SettlementRuleType.AutoSettlement);
        var grouping = strategy == SettlementOrchestrationStrategy.Grouped ? "bypartner" : null;

        // Act
        rule.UpdateOrchestration(strategy, 100, grouping, TestUser);

        // Assert
        rule.OrchestrationStrategy.Should().Be(strategy);
    }

    [Theory]
    [InlineData(SettlementRuleType.AutoSettlement)]
    [InlineData(SettlementRuleType.AutoApproval)]
    [InlineData(SettlementRuleType.AutoFinalization)]
    [InlineData(SettlementRuleType.ChargeCalculation)]
    [InlineData(SettlementRuleType.PaymentMatching)]
    [InlineData(SettlementRuleType.Consolidation)]
    public void Rule_CanHaveDifferentRuleTypes(SettlementRuleType ruleType)
    {
        // Act
        var rule = new SettlementAutomationRule($"Rule with type {ruleType}", "Description", ruleType);

        // Assert
        rule.RuleType.Should().Be(ruleType);
    }

    #endregion

    #region Execution History Tests

    [Fact]
    public void ExecutionHistory_ShouldTrackIndividualExecutions()
    {
        // Arrange
        var rule = new SettlementAutomationRule("Test Rule", "Description", SettlementRuleType.AutoSettlement);

        var record1 = new RuleExecutionRecord(rule.Id, "ContractCompletion");
        record1.Complete(5, 2, 1);

        var record2 = new RuleExecutionRecord(rule.Id, "Manual");
        record2.Complete(3, 1, 1);

        // Act
        rule.ExecutionHistory.Add(record1);
        rule.ExecutionHistory.Add(record2);

        // Assert
        rule.ExecutionHistory.Should().HaveCount(2);
        rule.ExecutionHistory.Should().Contain(record1);
        rule.ExecutionHistory.Should().Contain(record2);
    }

    [Fact]
    public void RuleExecutionRecord_Complete_ShouldSetAllFields()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var record = new RuleExecutionRecord(ruleId, "API");

        // Act
        record.Complete(10, 5, 3);

        // Assert
        record.Status.Should().Be(ExecutionStatus.Completed);
        record.SettlementCount.Should().Be(10);
        record.ConditionsEvaluated.Should().Be(5);
        record.ActionsExecuted.Should().Be(3);
        record.ExecutionEndTime.Should().NotBeNull();
        record.ExecutionDurationMs.Should().NotBeNull();
        record.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RuleExecutionRecord_Failed_ShouldSetErrorMessage()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var record = new RuleExecutionRecord(ruleId, "ScheduledJob");
        var errorMessage = "Database connection failed";

        // Act
        record.Failed(errorMessage);

        // Assert
        record.Status.Should().Be(ExecutionStatus.Failed);
        record.ErrorMessage.Should().Be(errorMessage);
        record.ExecutionEndTime.Should().NotBeNull();
        record.ExecutionDurationMs.Should().NotBeNull();
    }

    [Fact]
    public void RuleExecutionRecord_AddLog_ShouldAppendToDetailedLog()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var record = new RuleExecutionRecord(ruleId, "Manual");

        // Act
        record.AddLog("Starting execution");
        record.AddLog("Processing 5 settlements");
        record.AddLog("Execution completed");

        // Assert
        record.DetailedLog.Should().Contain("Starting execution");
        record.DetailedLog.Should().Contain("Processing 5 settlements");
        record.DetailedLog.Should().Contain("Execution completed");
    }

    [Fact]
    public void RuleExecutionRecord_Initial_ShouldHaveRunningStatus()
    {
        // Arrange & Act
        var ruleId = Guid.NewGuid();
        var record = new RuleExecutionRecord(ruleId, "ContractCompletion");

        // Assert
        record.Status.Should().Be(ExecutionStatus.Running);
        record.ExecutionStartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        record.TriggerSource.Should().Be("ContractCompletion");
        record.RuleId.Should().Be(ruleId);
    }

    #endregion

    #region Condition Tests

    [Fact]
    public void SettlementRuleCondition_Create_ShouldSetAllFields()
    {
        // Arrange
        var ruleId = Guid.NewGuid();

        // Act
        var condition = new SettlementRuleCondition(
            ruleId,
            "SettlementAmount",
            "GreaterThan",
            "50000",
            1);

        // Assert
        condition.RuleId.Should().Be(ruleId);
        condition.Field.Should().Be("SettlementAmount");
        condition.OperatorType.Should().Be("GreaterThan");
        condition.Value.Should().Be("50000");
        condition.SequenceNumber.Should().Be(1);
        condition.LogicalOperator.Should().Be("AND"); // Default
    }

    [Fact]
    public void SettlementRuleCondition_WithNullField_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new SettlementRuleCondition(Guid.NewGuid(), null!, "Equals", "value", 1);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("field");
    }

    #endregion

    #region Action Tests

    [Fact]
    public void SettlementRuleAction_Create_ShouldSetAllFields()
    {
        // Arrange
        var ruleId = Guid.NewGuid();

        // Act
        var action = new SettlementRuleAction(ruleId, "ApproveSettlement", 2);

        // Assert
        action.RuleId.Should().Be(ruleId);
        action.ActionType.Should().Be("ApproveSettlement");
        action.SequenceNumber.Should().Be(2);
        action.StopOnFailure.Should().BeTrue(); // Default
    }

    [Fact]
    public void SettlementRuleAction_WithNullActionType_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new SettlementRuleAction(Guid.NewGuid(), null!, 1);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("actionType");
    }

    #endregion

    #region RuleStatus Enum Tests

    [Theory]
    [InlineData(RuleStatus.Draft)]
    [InlineData(RuleStatus.Testing)]
    [InlineData(RuleStatus.Active)]
    [InlineData(RuleStatus.Deprecated)]
    [InlineData(RuleStatus.Archived)]
    public void RuleStatus_AllValuesValid(RuleStatus status)
    {
        // Act & Assert
        Enum.IsDefined(typeof(RuleStatus), status).Should().BeTrue();
    }

    #endregion

    #region ExecutionStatus Enum Tests

    [Theory]
    [InlineData(ExecutionStatus.Running)]
    [InlineData(ExecutionStatus.Completed)]
    [InlineData(ExecutionStatus.Failed)]
    [InlineData(ExecutionStatus.PartiallyCompleted)]
    [InlineData(ExecutionStatus.Cancelled)]
    public void ExecutionStatus_AllValuesValid(ExecutionStatus status)
    {
        // Act & Assert
        Enum.IsDefined(typeof(ExecutionStatus), status).Should().BeTrue();
    }

    #endregion
}
