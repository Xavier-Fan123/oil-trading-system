using FluentAssertions;
using Moq;
using OilTrading.Application.Commands.SettlementAutomationRules;
using OilTrading.Application.Queries.SettlementAutomationRules;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OilTrading.UnitTests.Application.SettlementAutomationRules;

/// <summary>
/// Unit tests for Settlement Automation Rule query handlers
/// Tests: Retrieve single rule, retrieve all with filtering, execution history, analytics
/// Fixed for Data Lineage Enhancement v2.18.0 - using proper constructors and entity methods
/// </summary>
public class SettlementAutomationRuleQueryHandlerTests
{
    private readonly Mock<IRepository<SettlementAutomationRule>> _mockRuleRepository;
    private readonly Mock<ILogger<GetSettlementAutomationRuleQueryHandler>> _mockGetRuleLogger;
    private readonly Mock<ILogger<GetAllSettlementAutomationRulesQueryHandler>> _mockGetAllLogger;
    private readonly Mock<ILogger<GetRuleExecutionHistoryQueryHandler>> _mockHistoryLogger;
    private readonly Mock<ILogger<GetRuleAnalyticsQueryHandler>> _mockAnalyticsLogger;
    private const string TestUser = "test-user";

    public SettlementAutomationRuleQueryHandlerTests()
    {
        _mockRuleRepository = new Mock<IRepository<SettlementAutomationRule>>();
        _mockGetRuleLogger = new Mock<ILogger<GetSettlementAutomationRuleQueryHandler>>();
        _mockGetAllLogger = new Mock<ILogger<GetAllSettlementAutomationRulesQueryHandler>>();
        _mockHistoryLogger = new Mock<ILogger<GetRuleExecutionHistoryQueryHandler>>();
        _mockAnalyticsLogger = new Mock<ILogger<GetRuleAnalyticsQueryHandler>>();
    }

    #region GetSettlementAutomationRuleQuery Tests

    [Fact]
    public async Task Handle_GetSettlementAutomationRuleQuery_ShouldReturnRule()
    {
        // Arrange
        var rule = CreateTestRule("Test Rule", "Test description");

        var query = new GetSettlementAutomationRuleQuery { RuleId = rule.Id };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetSettlementAutomationRuleQueryHandler(
            _mockRuleRepository.Object,
            _mockGetRuleLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Rule");
        result.Description.Should().Be("Test description");
        _mockRuleRepository.Verify(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GetSettlementAutomationRuleQuery_RuleNotFound_ShouldReturnNull()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var query = new GetSettlementAutomationRuleQuery { RuleId = ruleId };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettlementAutomationRule?)null);

        var handler = new GetSettlementAutomationRuleQueryHandler(
            _mockRuleRepository.Object,
            _mockGetRuleLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_GetSettlementAutomationRuleQuery_ShouldIncludeConditionsAndActions()
    {
        // Arrange
        var rule = CreateTestRule("Complex Rule", "With conditions and actions");
        // Rule already has conditions and actions from CreateTestRule
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "Currency", "EQUALS", "USD", 2));
        rule.Actions.Add(new SettlementRuleAction(rule.Id, "SendNotification", 2));

        var query = new GetSettlementAutomationRuleQuery { RuleId = rule.Id };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetSettlementAutomationRuleQueryHandler(
            _mockRuleRepository.Object,
            _mockGetRuleLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Conditions.Should().HaveCount(2);
        result.Actions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_GetSettlementAutomationRuleQuery_ShouldMapAllProperties()
    {
        // Arrange
        var rule = CreateTestRule("Full Property Rule", "Testing all properties");
        rule.UpdateScope(SettlementRuleScope.ByPartner, "Partner-001", TestUser);
        rule.UpdateTrigger(SettlementRuleTrigger.OnSchedule, "0 9 * * *", TestUser);
        rule.UpdateOrchestration(SettlementOrchestrationStrategy.Parallel, 50, "bypartner", TestUser);

        var query = new GetSettlementAutomationRuleQuery { RuleId = rule.Id };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetSettlementAutomationRuleQueryHandler(
            _mockRuleRepository.Object,
            _mockGetRuleLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Scope.Should().Be("ByPartner");
        result.ScopeFilter.Should().Be("Partner-001");
        result.Trigger.Should().Be("OnSchedule");
        result.ScheduleExpression.Should().Be("0 9 * * *");
        result.OrchestrationStrategy.Should().Be("Parallel");
        result.MaxSettlementsPerExecution.Should().Be(50);
        result.GroupingDimension.Should().Be("bypartner");
    }

    #endregion

    #region GetAllSettlementAutomationRulesQuery Tests

    [Fact]
    public async Task Handle_GetAllSettlementAutomationRulesQuery_ShouldReturnAllRules()
    {
        // Arrange
        var rules = new List<SettlementAutomationRule>
        {
            CreateTestRule("Rule 1", "Description 1"),
            CreateTestRule("Rule 2", "Description 2"),
            CreateTestRule("Rule 3", "Description 3")
        };

        var query = new GetAllSettlementAutomationRulesQuery { PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockGetAllLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_GetAllRules_WithEnabledFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var enabledRule1 = CreateTestRule("Enabled Rule 1", "Description 1");
        var enabledRule2 = CreateTestRule("Enabled Rule 2", "Description 2");
        var disabledRule = CreateTestRule("Disabled Rule", "Description 3");
        disabledRule.Disable("Test disable");

        var rules = new List<SettlementAutomationRule> { enabledRule1, enabledRule2, disabledRule };

        var query = new GetAllSettlementAutomationRulesQuery
        {
            IsEnabled = true,
            PageNum = 1,
            PageSize = 10
        };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockGetAllLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.IsEnabled);
    }

    [Fact]
    public async Task Handle_GetAllRules_WithDisabledFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var enabledRule = CreateTestRule("Enabled Rule", "Description 1");
        var disabledRule = CreateTestRule("Disabled Rule", "Description 2");
        disabledRule.Disable("Test disable");

        var rules = new List<SettlementAutomationRule> { enabledRule, disabledRule };

        var query = new GetAllSettlementAutomationRulesQuery
        {
            IsEnabled = false,
            PageNum = 1,
            PageSize = 10
        };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockGetAllLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().OnlyContain(r => !r.IsEnabled);
    }

    [Fact]
    public async Task Handle_GetAllRules_WithRuleTypeFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var autoSettlementRule = new SettlementAutomationRule(
            "Auto Settlement Rule", "Description", SettlementRuleType.AutoSettlement, "Normal", TestUser);
        autoSettlementRule.Conditions.Add(new SettlementRuleCondition(autoSettlementRule.Id, "Amount", "GREATERTHAN", "0", 1));
        autoSettlementRule.Actions.Add(new SettlementRuleAction(autoSettlementRule.Id, "Process", 1));

        var autoApprovalRule = new SettlementAutomationRule(
            "Auto Approval Rule", "Description", SettlementRuleType.AutoApproval, "Normal", TestUser);
        autoApprovalRule.Conditions.Add(new SettlementRuleCondition(autoApprovalRule.Id, "Amount", "GREATERTHAN", "0", 1));
        autoApprovalRule.Actions.Add(new SettlementRuleAction(autoApprovalRule.Id, "Approve", 1));

        var rules = new List<SettlementAutomationRule> { autoSettlementRule, autoApprovalRule };

        var query = new GetAllSettlementAutomationRulesQuery
        {
            RuleType = "AutoSettlement",
            PageNum = 1,
            PageSize = 10
        };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockGetAllLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().RuleType.Should().Be("AutoSettlement");
    }

    [Fact]
    public async Task Handle_GetAllRules_WithPagination_ShouldRespectPageSize()
    {
        // Arrange
        var rules = new List<SettlementAutomationRule>();
        for (int i = 0; i < 25; i++)
        {
            rules.Add(CreateTestRule($"Rule {i}", $"Description {i}"));
        }

        var query = new GetAllSettlementAutomationRulesQuery { PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockGetAllLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task Handle_GetAllRules_SecondPage_ShouldReturnCorrectItems()
    {
        // Arrange
        var rules = new List<SettlementAutomationRule>();
        for (int i = 0; i < 25; i++)
        {
            rules.Add(CreateTestRule($"Rule {i}", $"Description {i}"));
        }

        var query = new GetAllSettlementAutomationRulesQuery { PageNum = 2, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockGetAllLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task Handle_GetAllRules_LastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        var rules = new List<SettlementAutomationRule>();
        for (int i = 0; i < 25; i++)
        {
            rules.Add(CreateTestRule($"Rule {i}", $"Description {i}"));
        }

        var query = new GetAllSettlementAutomationRulesQuery { PageNum = 3, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockGetAllLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5); // Last 5 of 25 items
    }

    [Fact]
    public async Task Handle_GetAllRules_EmptyRepository_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetAllSettlementAutomationRulesQuery { PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementAutomationRule>());

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockGetAllLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetRuleExecutionHistoryQuery Tests

    [Fact]
    public async Task Handle_GetRuleExecutionHistoryQuery_ShouldReturnExecutionRecords()
    {
        // Arrange
        var rule = CreateTestRule("Test Rule", "Description");

        // Add execution records
        var record1 = new RuleExecutionRecord(rule.Id, "ContractCompletion");
        record1.Complete(5, 2, 1);
        var record2 = new RuleExecutionRecord(rule.Id, "ScheduledJob");
        record2.Complete(3, 1, 1);

        rule.ExecutionHistory.Add(record1);
        rule.ExecutionHistory.Add(record2);

        var query = new GetRuleExecutionHistoryQuery { RuleId = rule.Id, PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleExecutionHistoryQueryHandler(
            _mockRuleRepository.Object,
            _mockHistoryLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_GetRuleExecutionHistoryQuery_RuleNotFound_ShouldReturnEmptyList()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var query = new GetRuleExecutionHistoryQuery { RuleId = ruleId, PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettlementAutomationRule?)null);

        var handler = new GetRuleExecutionHistoryQueryHandler(
            _mockRuleRepository.Object,
            _mockHistoryLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetRuleExecutionHistoryQuery_NoHistory_ShouldReturnEmptyList()
    {
        // Arrange
        var rule = CreateTestRule("Test Rule", "Description");
        // No execution history added

        var query = new GetRuleExecutionHistoryQuery { RuleId = rule.Id, PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleExecutionHistoryQueryHandler(
            _mockRuleRepository.Object,
            _mockHistoryLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetRuleExecutionHistoryQuery_ShouldMapRecordProperties()
    {
        // Arrange
        var rule = CreateTestRule("Test Rule", "Description");
        var record = new RuleExecutionRecord(rule.Id, "ManualTrigger");
        record.Complete(10, 3, 2);
        rule.ExecutionHistory.Add(record);

        var query = new GetRuleExecutionHistoryQuery { RuleId = rule.Id, PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleExecutionHistoryQueryHandler(
            _mockRuleRepository.Object,
            _mockHistoryLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        var dto = result.First();
        dto.RuleId.Should().Be(rule.Id);
        dto.TriggerSource.Should().Be("ManualTrigger");
        dto.SettlementCount.Should().Be(10);
        dto.Status.Should().Be("Completed");
    }

    #endregion

    #region GetRuleAnalyticsQuery Tests

    [Fact]
    public async Task Handle_GetRuleAnalyticsQuery_ShouldCalculateMetrics()
    {
        // Arrange
        var rule = CreateTestRule("Analytics Rule", "Description");
        rule.RecordSuccessfulExecution(5);
        rule.RecordSuccessfulExecution(3);
        rule.RecordFailedExecution("Error occurred");

        // Add execution history records for analytics
        var record1 = new RuleExecutionRecord(rule.Id, "Test");
        record1.Complete(5, 1, 1);
        var record2 = new RuleExecutionRecord(rule.Id, "Test");
        record2.Complete(3, 1, 1);
        var record3 = new RuleExecutionRecord(rule.Id, "Test");
        record3.Failed("Error");

        rule.ExecutionHistory.Add(record1);
        rule.ExecutionHistory.Add(record2);
        rule.ExecutionHistory.Add(record3);

        var query = new GetRuleAnalyticsQuery { RuleId = rule.Id };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleAnalyticsQueryHandler(
            _mockRuleRepository.Object,
            _mockAnalyticsLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RuleId.Should().Be(rule.Id);
        result.RuleName.Should().Be("Analytics Rule");
        result.TotalExecutions.Should().Be(3); // 2 success + 1 failure
        result.SuccessfulExecutions.Should().Be(2);
        result.FailedExecutions.Should().Be(1);
    }

    [Fact]
    public async Task Handle_GetRuleAnalyticsQuery_ShouldCalculateSuccessRate()
    {
        // Arrange
        var rule = CreateTestRule("Success Rate Rule", "Description");

        // 8 successful, 2 failed = 80% success rate
        for (int i = 0; i < 8; i++)
        {
            rule.RecordSuccessfulExecution(1);
            var successRecord = new RuleExecutionRecord(rule.Id, "Test");
            successRecord.Complete(1, 1, 1);
            rule.ExecutionHistory.Add(successRecord);
        }
        for (int i = 0; i < 2; i++)
        {
            rule.RecordFailedExecution("Error");
            var failedRecord = new RuleExecutionRecord(rule.Id, "Test");
            failedRecord.Failed("Error");
            rule.ExecutionHistory.Add(failedRecord);
        }

        var query = new GetRuleAnalyticsQuery { RuleId = rule.Id };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleAnalyticsQueryHandler(
            _mockRuleRepository.Object,
            _mockAnalyticsLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SuccessRate.Should().BeApproximately(0.8, 0.01); // 80% success rate
    }

    [Fact]
    public async Task Handle_GetRuleAnalyticsQuery_RuleNotFound_ShouldReturnEmptyAnalytics()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var query = new GetRuleAnalyticsQuery { RuleId = ruleId };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettlementAutomationRule?)null);

        var handler = new GetRuleAnalyticsQueryHandler(
            _mockRuleRepository.Object,
            _mockAnalyticsLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RuleId.Should().Be(ruleId);
        result.TotalExecutions.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GetRuleAnalyticsQuery_NoExecutions_ShouldReturnZeroMetrics()
    {
        // Arrange
        var rule = CreateTestRule("New Rule", "Description");
        // No executions recorded

        var query = new GetRuleAnalyticsQuery { RuleId = rule.Id };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleAnalyticsQueryHandler(
            _mockRuleRepository.Object,
            _mockAnalyticsLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalExecutions.Should().Be(0);
        result.SuccessfulExecutions.Should().Be(0);
        result.FailedExecutions.Should().Be(0);
        result.SuccessRate.Should().Be(0);
    }

    [Fact]
    public async Task Handle_GetRuleAnalyticsQuery_ShouldIncludeTrends()
    {
        // Arrange
        var rule = CreateTestRule("Trends Rule", "Description");

        // Add execution records for different days
        var today = DateTime.UtcNow;
        var yesterday = DateTime.UtcNow.AddDays(-1);

        var record1 = new RuleExecutionRecord(rule.Id, "Test");
        record1.Complete(5, 1, 1);
        rule.ExecutionHistory.Add(record1);

        var record2 = new RuleExecutionRecord(rule.Id, "Test");
        record2.Complete(3, 1, 1);
        rule.ExecutionHistory.Add(record2);

        var query = new GetRuleAnalyticsQuery { RuleId = rule.Id };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleAnalyticsQueryHandler(
            _mockRuleRepository.Object,
            _mockAnalyticsLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ExecutionTrends.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_GetRuleAnalyticsQuery_ShouldCalculateTotalSettlementsProcessed()
    {
        // Arrange
        var rule = CreateTestRule("Settlement Count Rule", "Description");

        var record1 = new RuleExecutionRecord(rule.Id, "Test");
        record1.Complete(10, 1, 1);
        var record2 = new RuleExecutionRecord(rule.Id, "Test");
        record2.Complete(15, 1, 1);
        var record3 = new RuleExecutionRecord(rule.Id, "Test");
        record3.Complete(5, 1, 1);

        rule.ExecutionHistory.Add(record1);
        rule.ExecutionHistory.Add(record2);
        rule.ExecutionHistory.Add(record3);

        var query = new GetRuleAnalyticsQuery { RuleId = rule.Id };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleAnalyticsQueryHandler(
            _mockRuleRepository.Object,
            _mockAnalyticsLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalSettlementsProcessed.Should().Be(30); // 10 + 15 + 5
    }

    #endregion

    #region Helper Methods

    private SettlementAutomationRule CreateTestRule(string name, string description)
    {
        var rule = new SettlementAutomationRule(
            name,
            description,
            SettlementRuleType.AutoSettlement,
            "Normal",
            TestUser
        );

        // Add a condition and action to make the rule valid
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "TotalSettlementAmount", "GREATERTHAN", "-1", 1));
        rule.Actions.Add(new SettlementRuleAction(rule.Id, "ProcessSettlement", 1));

        return rule;
    }

    #endregion
}
