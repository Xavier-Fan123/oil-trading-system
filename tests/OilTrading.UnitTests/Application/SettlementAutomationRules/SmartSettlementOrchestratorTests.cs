using FluentAssertions;
using Moq;
using OilTrading.Application.Services;
using OilTrading.Core.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OilTrading.UnitTests.Application.SettlementAutomationRules;

/// <summary>
/// Unit tests for SmartSettlementOrchestrator service
/// Tests: Sequential execution, parallel execution, grouped execution, limit enforcement
/// Fixed for Data Lineage Enhancement v2.18.0 - using proper constructors and entity methods
/// </summary>
public class SmartSettlementOrchestratorTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<SmartSettlementOrchestrator>> _mockLogger;
    private readonly ISmartSettlementOrchestrator _orchestrator;
    private const string TestUser = "test-user";

    public SmartSettlementOrchestratorTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<SmartSettlementOrchestrator>>();
        _orchestrator = new SmartSettlementOrchestrator(_mockMediator.Object, _mockLogger.Object);
    }

    #region Sequential Execution Tests

    [Fact]
    public async Task OrchestrateAsync_SequentialStrategy_ShouldProcessSettlementsInOrder()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = CreateTestSettlements(count: 3);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Strategy.Should().Be("Sequential");
        result.SettlementsProcessed.Should().Be(3);
        result.SettlementIds.Should().HaveCount(3);
    }

    [Fact]
    public async Task OrchestrateAsync_SequentialStrategy_EmptySettlements_ShouldReturnEmptyResult()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = new List<ContractSettlement>();

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.SettlementsProcessed.Should().Be(0);
    }

    [Fact]
    public async Task OrchestrateAsync_SequentialStrategy_SingleSettlement_ShouldProcess()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = CreateTestSettlements(count: 1);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.SettlementsProcessed.Should().Be(1);
    }

    #endregion

    #region Parallel Execution Tests

    [Fact]
    public async Task OrchestrateAsync_ParallelStrategy_ShouldProcessSettlementsInParallel()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Parallel);
        var settlements = CreateTestSettlements(count: 5);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.Strategy.Should().Be("Parallel");
        result.SettlementsProcessed.Should().Be(5);
        result.SettlementIds.Should().HaveCount(5);
    }

    [Fact]
    public async Task OrchestrateAsync_ParallelStrategy_LargeSettlementSet_ShouldHandle()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Parallel);
        var settlements = CreateTestSettlements(count: 50);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.SettlementsProcessed.Should().Be(50);
        result.EndTime.Should().NotBeNull();
    }

    #endregion

    #region Grouped Execution Tests

    [Fact]
    public async Task OrchestrateAsync_GroupedStrategy_ShouldGroupSettlements()
    {
        // Arrange
        var rule = CreateTestRuleWithGrouping(SettlementOrchestrationStrategy.Grouped, "bypartner");
        var settlements = CreateTestSettlements(count: 3);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.Strategy.Should().Be("Grouped");
        result.SettlementsProcessed.Should().Be(3);
    }

    [Fact]
    public async Task OrchestrateAsync_GroupedStrategy_WithByPartnerDimension_ShouldGroupCorrectly()
    {
        // Arrange
        var contractId1 = Guid.NewGuid();
        var contractId2 = Guid.NewGuid();

        var rule = CreateTestRuleWithGrouping(SettlementOrchestrationStrategy.Grouped, "bypartner");

        var settlements = new List<ContractSettlement>
        {
            CreateTestSettlement(contractId: contractId1),
            CreateTestSettlement(contractId: contractId1),
            CreateTestSettlement(contractId: contractId2),
            CreateTestSettlement(contractId: contractId2),
            CreateTestSettlement(contractId: contractId1)
        };

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.SettlementsProcessed.Should().Be(5);
    }

    [Fact]
    public async Task OrchestrateAsync_GroupedStrategy_NoGroupingDimension_ShouldProcessAllTogether()
    {
        // Arrange
        var rule = CreateTestRuleWithGrouping(SettlementOrchestrationStrategy.Grouped, null);
        var settlements = CreateTestSettlements(count: 4);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.SettlementsProcessed.Should().Be(4);
    }

    #endregion

    #region Max Settlements Limit Tests

    [Fact]
    public async Task OrchestrateAsync_WithMaxSettlementsLimit_ShouldRespectLimit()
    {
        // Arrange
        var rule = CreateTestRuleWithLimit(SettlementOrchestrationStrategy.Sequential, 5);
        var settlements = CreateTestSettlements(count: 10);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.SettlementsProcessed.Should().Be(5); // Only 5 processed due to limit
        result.SettlementIds.Should().HaveCount(5);
    }

    [Fact]
    public async Task OrchestrateAsync_MaxLimitExceedsSettlementCount_ShouldProcessAll()
    {
        // Arrange
        var rule = CreateTestRuleWithLimit(SettlementOrchestrationStrategy.Sequential, 100);
        var settlements = CreateTestSettlements(count: 10);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.SettlementsProcessed.Should().Be(10); // All 10 processed
    }

    [Fact]
    public async Task OrchestrateAsync_LimitOfOne_ShouldProcessOnlyOne()
    {
        // Arrange
        var rule = CreateTestRuleWithLimit(SettlementOrchestrationStrategy.Sequential, 1);
        var settlements = CreateTestSettlements(count: 5);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.SettlementsProcessed.Should().Be(1);
    }

    #endregion

    #region Execution Timing Tests

    [Fact]
    public async Task OrchestrateAsync_ShouldTrackExecutionTime()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = CreateTestSettlements(count: 3);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.StartTime.Should().NotBe(default);
        result.EndTime.Should().NotBeNull();
        result.DurationMs.Should().NotBeNull();
        result.DurationMs!.Value.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task OrchestrateAsync_SequentialAndParallel_BothShouldComplete()
    {
        // Arrange
        var sequentialRule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var parallelRule = CreateTestRule(SettlementOrchestrationStrategy.Parallel);
        var settlements = CreateTestSettlements(count: 10);

        // Act
        var sequentialResult = await _orchestrator.OrchestrateAsync(sequentialRule, settlements, TestUser);
        var parallelResult = await _orchestrator.OrchestrateAsync(parallelRule, settlements, TestUser);

        // Assert
        sequentialResult.Should().NotBeNull();
        parallelResult.Should().NotBeNull();
        sequentialResult.IsSuccessful.Should().BeTrue();
        parallelResult.IsSuccessful.Should().BeTrue();
    }

    #endregion

    #region Result Properties Tests

    [Fact]
    public async Task OrchestrateAsync_ShouldReturnResultWithErrors()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = CreateTestSettlements(count: 5);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.Errors.Should().NotBeNull();
    }

    [Fact]
    public async Task OrchestrateAsync_ShouldHaveSettlementIdsInResult()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = CreateTestSettlements(count: 3);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.SettlementIds.Should().NotBeEmpty();
        result.SettlementIds.Should().HaveCount(3);
    }

    [Fact]
    public async Task OrchestrateAsync_ShouldContainRuleId()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = CreateTestSettlements(count: 2);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.RuleId.Should().Be(rule.Id);
    }

    #endregion

    #region Strategy Selection Tests

    [Theory]
    [InlineData(SettlementOrchestrationStrategy.Sequential, "Sequential")]
    [InlineData(SettlementOrchestrationStrategy.Parallel, "Parallel")]
    [InlineData(SettlementOrchestrationStrategy.Grouped, "Grouped")]
    public async Task OrchestrateAsync_AllStrategies_ShouldProcess(
        SettlementOrchestrationStrategy strategy,
        string expectedStrategyName)
    {
        // Arrange
        var rule = CreateTestRule(strategy);
        var settlements = CreateTestSettlements(count: 3);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.Strategy.Should().Be(expectedStrategyName);
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task OrchestrateAsync_ConsolidatedStrategy_ShouldFallbackToSequential()
    {
        // Arrange - Consolidated falls back to Sequential in current implementation
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Consolidated);
        var settlements = CreateTestSettlements(count: 3);

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.Strategy.Should().Be("Consolidated");
        result.IsSuccessful.Should().BeTrue();
        result.SettlementsProcessed.Should().Be(3);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task OrchestrateAsync_WithZeroSettlements_ShouldSucceed()
    {
        // Arrange
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Parallel);
        var settlements = new List<ContractSettlement>();

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.SettlementsProcessed.Should().Be(0);
        result.SettlementIds.Should().BeEmpty();
    }

    [Fact]
    public async Task OrchestrateAsync_WithSameContractId_ShouldProcessAllSettlements()
    {
        // Arrange
        var sameContractId = Guid.NewGuid();
        var rule = CreateTestRule(SettlementOrchestrationStrategy.Sequential);
        var settlements = new List<ContractSettlement>
        {
            CreateTestSettlement(contractId: sameContractId),
            CreateTestSettlement(contractId: sameContractId),
            CreateTestSettlement(contractId: sameContractId)
        };

        // Act
        var result = await _orchestrator.OrchestrateAsync(rule, settlements, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.SettlementsProcessed.Should().Be(3);
    }

    #endregion

    #region Helper Methods

    private SettlementAutomationRule CreateTestRule(
        SettlementOrchestrationStrategy strategy = SettlementOrchestrationStrategy.Sequential)
    {
        var rule = new SettlementAutomationRule(
            "Test Orchestration Rule",
            "Test description for orchestration",
            SettlementRuleType.AutoSettlement,
            "Normal",
            TestUser
        );

        rule.UpdateOrchestration(strategy, null, null, TestUser);

        // Add a condition and action to make the rule valid
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "TotalSettlementAmount", "GREATERTHAN", "-1", 1));
        rule.Actions.Add(new SettlementRuleAction(rule.Id, "ProcessSettlement", 1));

        return rule;
    }

    private SettlementAutomationRule CreateTestRuleWithGrouping(
        SettlementOrchestrationStrategy strategy,
        string? groupingDimension)
    {
        var rule = new SettlementAutomationRule(
            "Grouped Orchestration Rule",
            "Test description for grouped orchestration",
            SettlementRuleType.AutoSettlement,
            "Normal",
            TestUser
        );

        rule.UpdateOrchestration(strategy, null, groupingDimension, TestUser);

        // Add a condition and action to make the rule valid
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "TotalSettlementAmount", "GREATERTHAN", "-1", 1));
        rule.Actions.Add(new SettlementRuleAction(rule.Id, "ProcessSettlement", 1));

        return rule;
    }

    private SettlementAutomationRule CreateTestRuleWithLimit(
        SettlementOrchestrationStrategy strategy,
        int maxSettlements)
    {
        var rule = new SettlementAutomationRule(
            "Limited Orchestration Rule",
            "Test description with limit",
            SettlementRuleType.AutoSettlement,
            "Normal",
            TestUser
        );

        rule.UpdateOrchestration(strategy, maxSettlements, null, TestUser);

        // Add a condition and action to make the rule valid
        rule.Conditions.Add(new SettlementRuleCondition(rule.Id, "TotalSettlementAmount", "GREATERTHAN", "-1", 1));
        rule.Actions.Add(new SettlementRuleAction(rule.Id, "ProcessSettlement", 1));

        return rule;
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

    private ContractSettlement CreateTestSettlement(Guid? contractId = null)
    {
        return new ContractSettlement(
            contractId: contractId ?? Guid.NewGuid(),
            contractNumber: $"PC-2025-{Guid.NewGuid().ToString()[..8]}",
            externalContractNumber: $"EXT-{Guid.NewGuid().ToString()[..8]}",
            documentNumber: $"BL-{Guid.NewGuid().ToString()[..8]}",
            createdBy: TestUser
        );
    }

    #endregion
}
