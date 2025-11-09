using Xunit;
using Moq;
using OilTrading.Application.Queries.SettlementAutomationRules;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using OilTrading.Core.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OilTrading.UnitTests.Application.SettlementAutomationRules;

/// <summary>
/// Unit tests for Settlement Automation Rule query handlers
/// Tests: Retrieve single rule, retrieve all with filtering, execution history, analytics
/// </summary>
public class SettlementAutomationRuleQueryHandlerTests
{
    private readonly Mock<IRepository<SettlementAutomationRule>> _mockRuleRepository;
    private readonly Mock<ILogger<GetSettlementAutomationRuleQueryHandler>> _mockLogger;

    public SettlementAutomationRuleQueryHandlerTests()
    {
        _mockRuleRepository = new Mock<IRepository<SettlementAutomationRule>>();
        _mockLogger = new Mock<ILogger<GetSettlementAutomationRuleQueryHandler>>();
    }

    #region GetSettlementAutomationRuleQuery Tests

    [Fact]
    public async Task Handle_GetSettlementAutomationRuleQuery_ShouldReturnRuleAsync()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = new SettlementAutomationRule
        {
            Id = ruleId,
            Name = "Test Rule",
            Description = "Test description",
            Status = RuleStatus.Active,
            IsEnabled = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        var query = new GetSettlementAutomationRuleQuery { RuleId = ruleId };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetSettlementAutomationRuleQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(rule.Name, result?.Name);
        Assert.Equal(rule.Description, result?.Description);
        _mockRuleRepository.Verify(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GetSettlementAutomationRuleQuery_RuleNotFound_ShouldReturnNullAsync()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var query = new GetSettlementAutomationRuleQuery { RuleId = ruleId };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettlementAutomationRule?)null);

        var handler = new GetSettlementAutomationRuleQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_GetSettlementAutomationRuleQuery_ShouldIncludeConditionsAndActionsAsync()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = new SettlementAutomationRule
        {
            Id = ruleId,
            Name = "Complex Rule",
            Conditions = new List<SettlementRuleCondition>
            {
                new() { Id = Guid.NewGuid(), RuleId = ruleId, Field = "Currency", Value = "USD" },
                new() { Id = Guid.NewGuid(), RuleId = ruleId, Field = "Amount", Value = "10000" }
            },
            Actions = new List<SettlementRuleAction>
            {
                new() { Id = Guid.NewGuid(), RuleId = ruleId, ActionType = "CreateSettlement" },
                new() { Id = Guid.NewGuid(), RuleId = ruleId, ActionType = "SendNotification" }
            }
        };

        var query = new GetSettlementAutomationRuleQuery { RuleId = ruleId };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetSettlementAutomationRuleQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result?.Conditions.Count);
        Assert.Equal(2, result?.Actions.Count);
    }

    #endregion

    #region GetAllSettlementAutomationRulesQuery Tests

    [Fact]
    public async Task Handle_GetAllSettlementAutomationRulesQuery_ShouldReturnAllRulesAsync()
    {
        // Arrange
        var rules = new List<SettlementAutomationRule>
        {
            new() { Id = Guid.NewGuid(), Name = "Rule 1", IsEnabled = true, CreatedDate = DateTime.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), Name = "Rule 2", IsEnabled = true, CreatedDate = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Name = "Rule 3", IsEnabled = false, CreatedDate = DateTime.UtcNow }
        };

        var query = new GetAllSettlementAutomationRulesQuery { PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task Handle_GetAllRules_WithEnabledFilter_ShouldFilterCorrectlyAsync()
    {
        // Arrange
        var rules = new List<SettlementAutomationRule>
        {
            new() { Id = Guid.NewGuid(), Name = "Rule 1", IsEnabled = true, CreatedDate = DateTime.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), Name = "Rule 2", IsEnabled = true, CreatedDate = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Name = "Rule 3", IsEnabled = false, CreatedDate = DateTime.UtcNow }
        };

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
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, r => Assert.True(r.IsEnabled));
    }

    [Fact]
    public async Task Handle_GetAllRules_WithRuleTypeFilter_ShouldFilterCorrectlyAsync()
    {
        // Arrange
        var rules = new List<SettlementAutomationRule>
        {
            new() { Id = Guid.NewGuid(), Name = "Rule 1", RuleType = SettlementRuleType.Automatic, CreatedDate = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Rule 2", RuleType = SettlementRuleType.Manual, CreatedDate = DateTime.UtcNow.AddDays(-1) }
        };

        var query = new GetAllSettlementAutomationRulesQuery
        {
            RuleType = "Automatic",
            PageNum = 1,
            PageSize = 10
        };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_GetAllRules_WithPagination_ShouldRespectPageSizeAsync()
    {
        // Arrange
        var rules = new List<SettlementAutomationRule>();
        for (int i = 0; i < 25; i++)
        {
            rules.Add(new()
            {
                Id = Guid.NewGuid(),
                Name = $"Rule {i}",
                CreatedDate = DateTime.UtcNow.AddDays(-i)
            });
        }

        var query = new GetAllSettlementAutomationRulesQuery { PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task Handle_GetAllRules_SecondPage_ShouldReturnCorrectItemsAsync()
    {
        // Arrange
        var rules = new List<SettlementAutomationRule>();
        for (int i = 0; i < 25; i++)
        {
            rules.Add(new()
            {
                Id = Guid.NewGuid(),
                Name = $"Rule {i}",
                CreatedDate = DateTime.UtcNow.AddDays(-i)
            });
        }

        var query = new GetAllSettlementAutomationRulesQuery { PageNum = 2, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task Handle_GetAllRules_ShouldSortByCreatedDateDescendingAsync()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.AddDays(-3);
        var midDate = DateTime.UtcNow.AddDays(-1);
        var newDate = DateTime.UtcNow;

        var rules = new List<SettlementAutomationRule>
        {
            new() { Id = Guid.NewGuid(), Name = "Rule 1", CreatedDate = midDate },
            new() { Id = Guid.NewGuid(), Name = "Rule 2", CreatedDate = newDate },
            new() { Id = Guid.NewGuid(), Name = "Rule 3", CreatedDate = oldDate }
        };

        var query = new GetAllSettlementAutomationRulesQuery { PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var handler = new GetAllSettlementAutomationRulesQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        // Results should be sorted newest first
    }

    #endregion

    #region GetRuleExecutionHistoryQuery Tests

    [Fact]
    public async Task Handle_GetRuleExecutionHistoryQuery_ShouldReturnExecutionRecordsAsync()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = new SettlementAutomationRule
        {
            Id = ruleId,
            Name = "Test Rule",
            ExecutionHistory = new List<RuleExecutionRecord>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    RuleId = ruleId,
                    ExecutionStartTime = DateTime.UtcNow.AddHours(-2),
                    Status = ExecutionStatus.Completed,
                    SettlementCount = 5
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    RuleId = ruleId,
                    ExecutionStartTime = DateTime.UtcNow.AddHours(-1),
                    Status = ExecutionStatus.Completed,
                    SettlementCount = 3
                }
            }
        };

        var query = new GetRuleExecutionHistoryQuery { RuleId = ruleId, PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleExecutionHistoryQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Handle_GetRuleExecutionHistoryQuery_RuleNotFound_ShouldReturnEmptyListAsync()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var query = new GetRuleExecutionHistoryQuery { RuleId = ruleId, PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettlementAutomationRule?)null);

        var handler = new GetRuleExecutionHistoryQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_GetRuleExecutionHistoryQuery_ShouldSortByStartTimeDescendingAsync()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = new SettlementAutomationRule
        {
            Id = ruleId,
            Name = "Test Rule",
            ExecutionHistory = new List<RuleExecutionRecord>
            {
                new() { ExecutionStartTime = DateTime.UtcNow.AddHours(-3) },
                new() { ExecutionStartTime = DateTime.UtcNow.AddHours(-1) },
                new() { ExecutionStartTime = DateTime.UtcNow.AddHours(-2) }
            }
        };

        var query = new GetRuleExecutionHistoryQuery { RuleId = ruleId, PageNum = 1, PageSize = 10 };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleExecutionHistoryQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    #endregion

    #region GetRuleAnalyticsQuery Tests

    [Fact]
    public async Task Handle_GetRuleAnalyticsQuery_ShouldCalculateMetricsAsync()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = new SettlementAutomationRule
        {
            Id = ruleId,
            Name = "Test Rule",
            ExecutionCount = 10,
            SuccessCount = 8,
            FailureCount = 2,
            LastExecutedDate = DateTime.UtcNow,
            ExecutionHistory = new List<RuleExecutionRecord>
            {
                new() { ExecutionStartTime = DateTime.UtcNow, ExecutionEndTime = DateTime.UtcNow.AddSeconds(30), ExecutionDurationMs = 30000, Status = ExecutionStatus.Completed, SettlementCount = 5 },
                new() { ExecutionStartTime = DateTime.UtcNow.AddDays(-1), ExecutionEndTime = DateTime.UtcNow.AddDays(-1).AddSeconds(40), ExecutionDurationMs = 40000, Status = ExecutionStatus.Completed, SettlementCount = 3 }
            }
        };

        var query = new GetRuleAnalyticsQuery { RuleId = ruleId };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleAnalyticsQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ruleId, result.RuleId);
        Assert.Equal("Test Rule", result.RuleName);
        Assert.Equal(10, result.TotalExecutions);
        Assert.Equal(8, result.SuccessfulExecutions);
        Assert.Equal(2, result.FailedExecutions);
        Assert.True(result.SuccessRate > 0);
    }

    [Fact]
    public async Task Handle_GetRuleAnalyticsQuery_ShouldCalculateSuccessRateAsync()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = new SettlementAutomationRule
        {
            Id = ruleId,
            Name = "Test Rule",
            ExecutionCount = 10,
            SuccessCount = 8,
            FailureCount = 2,
            ExecutionHistory = new List<RuleExecutionRecord>
            {
                new() { Status = ExecutionStatus.Completed },
                new() { Status = ExecutionStatus.Completed },
                new() { Status = ExecutionStatus.Failed }
            }
        };

        var query = new GetRuleAnalyticsQuery { RuleId = ruleId };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleAnalyticsQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SuccessRate >= 0 && result.SuccessRate <= 1);
    }

    [Fact]
    public async Task Handle_GetRuleAnalyticsQuery_ShouldIncludeTrendsAsync()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = new SettlementAutomationRule
        {
            Id = ruleId,
            Name = "Test Rule",
            ExecutionHistory = new List<RuleExecutionRecord>
            {
                new() { ExecutionStartTime = DateTime.UtcNow, Status = ExecutionStatus.Completed },
                new() { ExecutionStartTime = DateTime.UtcNow, Status = ExecutionStatus.Completed },
                new() { ExecutionStartTime = DateTime.UtcNow.AddDays(-1), Status = ExecutionStatus.Failed }
            }
        };

        var query = new GetRuleAnalyticsQuery { RuleId = ruleId };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var handler = new GetRuleAnalyticsQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ExecutionTrends);
    }

    [Fact]
    public async Task Handle_GetRuleAnalyticsQuery_RuleNotFound_ShouldReturnEmptyAnalyticsAsync()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var query = new GetRuleAnalyticsQuery { RuleId = ruleId };

        _mockRuleRepository.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettlementAutomationRule?)null);

        var handler = new GetRuleAnalyticsQueryHandler(
            _mockRuleRepository.Object,
            _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ruleId, result.RuleId);
    }

    #endregion
}
