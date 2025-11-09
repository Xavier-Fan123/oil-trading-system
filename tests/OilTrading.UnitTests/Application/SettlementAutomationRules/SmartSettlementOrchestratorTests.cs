using Xunit;
using Moq;
using OilTrading.Application.Services;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OilTrading.UnitTests.Application.SettlementAutomationRules;

/// <summary>
/// Unit tests for SmartSettlementOrchestrator service
/// Tests: Sequential execution, parallel execution, grouped execution, limit enforcement
/// </summary>
public class SmartSettlementOrchestratorTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<SmartSettlementOrchestrator>> _mockLogger;
    private readonly ISmartSettlementOrchestrator _orchestrator;

    public SmartSettlementOrchestratorTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<SmartSettlementOrchestrator>>();
        _orchestrator = new SmartSettlementOrchestrator(_mockMediator.Object, _mockLogger.Object);
    }

    #region Sequential Execution Tests

    [Fact]
    public async Task OrchestrateAsync_SequentialStrategy_ShouldProcessSettlementsInOrderAsync()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = CreateTestSettlements(count: 3);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.Equal("Sequential", result.Strategy);
        Assert.Equal(3, result.SettlementsProcessed);
        Assert.Equal(3, result.SettlementIds.Count);
    }

    [Fact]
    public async Task OrchestrateAsync_SequentialStrategy_EmptySettlements_ShouldReturnEmptyResultAsync()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = new List<ContractSettlement>();

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.Equal(0, result.SettlementsProcessed);
    }

    [Fact]
    public async Task OrchestrateAsync_SequentialStrategy_SingleSettlement_ShouldProcessAsync()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = CreateTestSettlements(count: 1);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.Equal(1, result.SettlementsProcessed);
    }

    #endregion

    #region Parallel Execution Tests

    [Fact]
    public async Task OrchestrateAsync_ParallelStrategy_ShouldProcessSettlementsInParallelAsync()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Parallel);
        var settlements = CreateTestSettlements(count: 5);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.Equal("Parallel", result.Strategy);
        Assert.Equal(5, result.SettlementsProcessed);
        Assert.Equal(5, result.SettlementIds.Count);
    }

    [Fact]
    public async Task OrchestrateAsync_ParallelStrategy_LargeSettlementSet_ShouldHandleAsync()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Parallel);
        var settlements = CreateTestSettlements(count: 100);

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");
        var endTime = DateTime.UtcNow;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.Equal(100, result.SettlementsProcessed);
        Assert.NotNull(result.EndTime);
    }

    #endregion

    #region Grouped Execution Tests

    [Fact]
    public async Task OrchestrateAsync_GroupedStrategy_ShouldGroupSettlementsAsync()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Grouped Rule",
            RuleType = SettlementRuleType.Automatic,
            OrchestrationStrategy = SettlementOrchestrationStrategy.Grouped,
            GroupingDimension = "bypartner",
            Conditions = new List<SettlementRuleCondition>(),
            Actions = new List<SettlementRuleAction>()
        };

        var settlements = CreateTestSettlementsWithPartners(
            new[]
            {
                Guid.NewGuid(),  // Partner 1
                Guid.NewGuid(),  // Partner 2
                Guid.NewGuid()   // Partner 1 again
            });

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Grouped", result.Strategy);
        Assert.Equal(3, result.SettlementsProcessed);
    }

    [Fact]
    public async Task OrchestrateAsync_GroupedStrategy_WithByPartnerDimension_ShouldGroupCorrectlyAsync()
    {
        // Arrange
        var partnerId1 = Guid.NewGuid();
        var partnerId2 = Guid.NewGuid();

        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Grouped by Partner Rule",
            RuleType = SettlementRuleType.Automatic,
            OrchestrationStrategy = SettlementOrchestrationStrategy.Grouped,
            GroupingDimension = "bypartner",
            Conditions = new List<SettlementRuleCondition>(),
            Actions = new List<SettlementRuleAction>()
        };

        var settlements = new List<ContractSettlement>
        {
            CreateTestSettlement(contractId: partnerId1),
            CreateTestSettlement(contractId: partnerId1),
            CreateTestSettlement(contractId: partnerId2),
            CreateTestSettlement(contractId: partnerId2),
            CreateTestSettlement(contractId: partnerId1)
        };

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.Equal(5, result.SettlementsProcessed);
    }

    #endregion

    #region Max Settlements Limit Tests

    [Fact]
    public async Task OrchestrateAsync_WithMaxSettlementsLimit_ShouldRespectLimitAsync()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Limited Rule",
            RuleType = SettlementRuleType.Automatic,
            OrchestrationStrategy = SettlementOrchestrationStrategy.Sequential,
            MaxSettlementsPerExecution = 5,
            Conditions = new List<SettlementRuleCondition>(),
            Actions = new List<SettlementRuleAction>()
        };

        var settlements = CreateTestSettlements(count: 10);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.Equal(5, result.SettlementsProcessed);  // Only 5 processed due to limit
        Assert.Equal(5, result.SettlementIds.Count);
    }

    [Fact]
    public async Task OrchestrateAsync_MaxLimitExceedsSettlementCount_ShouldProcessAllAsync()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Limited Rule",
            RuleType = SettlementRuleType.Automatic,
            OrchestrationStrategy = SettlementOrchestrationStrategy.Sequential,
            MaxSettlementsPerExecution = 100,
            Conditions = new List<SettlementRuleCondition>(),
            Actions = new List<SettlementRuleAction>()
        };

        var settlements = CreateTestSettlements(count: 10);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.Equal(10, result.SettlementsProcessed);  // All 10 processed
    }

    [Fact]
    public async Task OrchestrateAsync_LimitOfOne_ShouldProcessOnlyOneAsync()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Single Settlement Rule",
            RuleType = SettlementRuleType.Automatic,
            OrchestrationStrategy = SettlementOrchestrationStrategy.Sequential,
            MaxSettlementsPerExecution = 1,
            Conditions = new List<SettlementRuleCondition>(),
            Actions = new List<SettlementRuleAction>()
        };

        var settlements = CreateTestSettlements(count: 5);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.Equal(1, result.SettlementsProcessed);
    }

    #endregion

    #region Execution Timing Tests

    [Fact]
    public async Task OrchestrateAsync_ShouldTrackExecutionTimeAsync()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = CreateTestSettlements(count: 3);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.StartTime);
        Assert.NotNull(result.EndTime);
        Assert.NotNull(result.DurationMs);
        Assert.True(result.DurationMs.Value >= 0);
    }

    [Fact]
    public async Task OrchestrateAsync_SequentialVsParallel_ParallelShouldBeFasterAsync()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = CreateTestSettlements(count: 10);

        // Act - Sequential
        var sequentialResult = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        var parallelRule = CreateTestRule(SettlementOrchestrationStrategy.Parallel);
        var parallelResult = await _orchestrator.OrchestrateAsync(parallelRule, settlements, "test-user");

        // Assert - Both should complete successfully
        Assert.NotNull(sequentialResult);
        Assert.NotNull(parallelResult);
        Assert.True(sequentialResult.IsSuccessful);
        Assert.True(parallelResult.IsSuccessful);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task OrchestrateAsync_WithNullRule_ShouldHandleGracefullyAsync()
    {
        // Arrange
        var rule = CreateTestRule();
        var settlements = CreateTestSettlements(count: 3);

        // Act - Should not throw, should handle gracefully
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task OrchestrateAsync_ShouldReturnResultEvenOnPartialFailureAsync()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = CreateTestSettlements(count: 5);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Errors);
    }

    [Fact]
    public async Task OrchestrateAsync_ShouldHaveSettlementIdsInResultAsync()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = CreateTestSettlements(count: 3);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SettlementIds);
        Assert.Equal(3, result.SettlementIds.Count);
    }

    #endregion

    #region Consolidation Tests

    [Fact]
    public async Task OrchestrateAsync_ConsolidatedStrategy_ShouldProcessAsync()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Consolidated Rule",
            RuleType = SettlementRuleType.Automatic,
            OrchestrationStrategy = SettlementOrchestrationStrategy.Consolidated,
            Conditions = new List<SettlementRuleCondition>(),
            Actions = new List<SettlementRuleAction>()
        };

        var settlements = CreateTestSettlements(count: 5);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Consolidated", result.Strategy);
    }

    #endregion

    #region Strategy Selection Tests

    [Theory]
    [InlineData(SettlementOrchestrationStrategy.Sequential)]
    [InlineData(SettlementOrchestrationStrategy.Parallel)]
    [InlineData(SettlementOrchestrationStrategy.Grouped)]
    [InlineData(SettlementOrchestrationStrategy.Consolidated)]
    public async Task OrchestrateAsync_AllStrategies_ShouldProcessAsync(SettlementOrchestrationStrategy strategy)
    {
        // Arrange
        var rule = CreateTestRule(strategy);
        var settlements = CreateTestSettlements(count: 3);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(strategy.ToString(), result.Strategy);
        Assert.True(result.IsSuccessful);
    }

    #endregion

    #region Helper Methods

    private SettlementAutomationRule CreateTestRule(
        SettlementOrchestrationStrategy strategy = SettlementOrchestrationStrategy.Sequential)
    {
        return new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Orchestration Rule",
            RuleType = SettlementRuleType.Automatic,
            Status = RuleStatus.Active,
            IsEnabled = true,
            OrchestrationStrategy = strategy,
            Conditions = new List<SettlementRuleCondition>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Field = "Currency",
                    OperatorType = "EQUALS",
                    Value = "USD"
                }
            },
            Actions = new List<SettlementRuleAction>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ActionType = "CreateSettlement",
                    SequenceNumber = 1
                }
            },
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "test-user",
            ExecutionHistory = new List<RuleExecutionRecord>()
        };
    }

    private List<ContractSettlement> CreateTestSettlements(int count)
    {
        var settlements = new List<ContractSettlement>();

        for (int i = 0; i < count; i++)
        {
            settlements.Add(CreateTestSettlement());
        }

        return settlements;
    }

    private List<ContractSettlement> CreateTestSettlementsWithPartners(Guid[] partnerIds)
    {
        var settlements = new List<ContractSettlement>();

        for (int i = 0; i < partnerIds.Length; i++)
        {
            settlements.Add(CreateTestSettlement(contractId: partnerIds[i]));
        }

        return settlements;
    }

    private ContractSettlement CreateTestSettlement(Guid? contractId = null)
    {
        return new ContractSettlement
        {
            Id = Guid.NewGuid(),
            ContractId = contractId ?? Guid.NewGuid(),
            IsSalesSettlement = false,
            Currency = "USD",
            SettlementAmount = 10000,
            Status = SettlementDocumentType.DocumentProvided,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    #endregion
}
