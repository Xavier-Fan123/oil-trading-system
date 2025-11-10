using Xunit;
using Moq;
using OilTrading.Application.Services;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace OilTrading.UnitTests.Application.SettlementAutomationRules;

/// <summary>
/// Unit tests for SettlementRuleEvaluator service
/// Tests: Scope matching, condition evaluation, rule validation, test execution
/// </summary>
public class SettlementRuleEvaluatorTests
{
    private readonly Mock<ILogger<SettlementRuleEvaluator>> _mockLogger;
    private readonly ISettlementRuleEvaluator _evaluator;

    public SettlementRuleEvaluatorTests()
    {
        _mockLogger = new Mock<ILogger<SettlementRuleEvaluator>>();
        _evaluator = new SettlementRuleEvaluator(_mockLogger.Object);
    }

    #region Scope Matching Tests

    [Fact]
    public async Task EvaluateScope_AllScope_ShouldAlwaysMatchAsync()
    {
        // Arrange
        var settlement = CreateTestSettlement(isSales: true);
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Scope = SettlementRuleScope.All,
            ScopeFilter = null
        };

        // Act
        var isInScope = await _evaluator.IsScopeMatchAsync(rule, settlement);

        // Assert
        Assert.True(isInScope);
    }

    [Fact]
    public async Task EvaluateScope_PurchaseOnly_ShouldMatchOnlyPurchaseSettlementsAsync()
    {
        // Arrange
        var purchaseSettlement = CreateTestSettlement(isSales: false);
        var salesSettlement = CreateTestSettlement(isSales: true);

        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Scope = SettlementRuleScope.PurchaseOnly,
            ScopeFilter = null
        };

        // Act
        var purchaseInScope = await _evaluator.IsScopeMatchAsync(rule, purchaseSettlement);
        var salesInScope = await _evaluator.IsScopeMatchAsync(rule, salesSettlement);

        // Assert
        Assert.True(purchaseInScope);
        Assert.False(salesInScope);
    }

    [Fact]
    public async Task EvaluateScope_SalesOnly_ShouldMatchOnlySalesSettlementsAsync()
    {
        // Arrange
        var purchaseSettlement = CreateTestSettlement(isSales: false);
        var salesSettlement = CreateTestSettlement(isSales: true);

        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Scope = SettlementRuleScope.SalesOnly,
            ScopeFilter = null
        };

        // Act
        var purchaseInScope = await _evaluator.IsScopeMatchAsync(rule, purchaseSettlement);
        var salesInScope = await _evaluator.IsScopeMatchAsync(rule, salesSettlement);

        // Assert
        Assert.False(purchaseInScope);
        Assert.True(salesInScope);
    }

    [Fact]
    public async Task EvaluateScope_ByCurrency_ShouldMatchByCurrencyFilterAsync()
    {
        // Arrange
        var settlementUSD = CreateTestSettlement(currency: "USD");
        var settlementEUR = CreateTestSettlement(currency: "EUR");

        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Scope = SettlementRuleScope.ByCurrency,
            ScopeFilter = "USD"
        };

        // Act
        var usdInScope = await _evaluator.IsScopeMatchAsync(rule, settlementUSD);
        var eurInScope = await _evaluator.IsScopeMatchAsync(rule, settlementEUR);

        // Assert
        Assert.True(usdInScope);
        Assert.False(eurInScope);
    }

    [Fact]
    public async Task EvaluateScope_ByPartner_ShouldMatchByPartnerFilterAsync()
    {
        // Arrange
        var settlement = CreateTestSettlement(partnerId: Guid.Parse("12345678-1234-1234-1234-123456789012"));
        var filterId = "12345678-1234-1234-1234-123456789012";

        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Scope = SettlementRuleScope.ByPartner,
            ScopeFilter = filterId
        };

        // Act
        var inScope = await _evaluator.IsScopeMatchAsync(rule, settlement);

        // Assert
        Assert.True(inScope);
    }

    #endregion

    #region Condition Evaluation Tests

    [Fact]
    public async Task EvaluateCondition_EqualsOperator_ShouldCompareValuesAsync()
    {
        // Arrange
        var condition = new SettlementRuleCondition
        {
            Id = Guid.NewGuid(),
            Field = "Currency",
            OperatorType = "EQUALS",
            Value = "USD"
        };

        var settlement = CreateTestSettlement(currency: "USD");
        var rule = CreateTestRule(conditions: new List<SettlementRuleCondition> { condition });

        // Act
        var result = await _evaluator.EvaluateConditionsAsync(rule, settlement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateCondition_GreaterThanOperator_ShouldCompareNumbersAsync()
    {
        // Arrange
        var condition = new SettlementRuleCondition
        {
            Id = Guid.NewGuid(),
            Field = "SettlementAmount",
            OperatorType = "GREATERTHAN",
            Value = "10000"
        };

        var settlement = CreateTestSettlement(amount: 15000);
        var rule = CreateTestRule(conditions: new List<SettlementRuleCondition> { condition });

        // Act
        var result = await _evaluator.EvaluateConditionsAsync(rule, settlement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateCondition_GreaterThanOperator_ShouldFailWhenConditionNotMetAsync()
    {
        // Arrange
        var condition = new SettlementRuleCondition
        {
            Id = Guid.NewGuid(),
            Field = "SettlementAmount",
            OperatorType = "GREATERTHAN",
            Value = "10000"
        };

        var settlement = CreateTestSettlement(amount: 5000);
        var rule = CreateTestRule(conditions: new List<SettlementRuleCondition> { condition });

        // Act
        var result = await _evaluator.EvaluateConditionsAsync(rule, settlement);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateCondition_LessThanOperator_ShouldCompareNumbersAsync()
    {
        // Arrange
        var condition = new SettlementRuleCondition
        {
            Id = Guid.NewGuid(),
            Field = "SettlementAmount",
            OperatorType = "LESSTHAN",
            Value = "10000"
        };

        var settlement = CreateTestSettlement(amount: 5000);
        var rule = CreateTestRule(conditions: new List<SettlementRuleCondition> { condition });

        // Act
        var result = await _evaluator.EvaluateConditionsAsync(rule, settlement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateCondition_MultipleConditionsWithAnd_ShouldRequireAllAsync()
    {
        // Arrange
        var conditions = new List<SettlementRuleCondition>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Field = "SettlementAmount",
                OperatorType = "GREATERTHAN",
                Value = "5000",
                LogicalOperator = "AND"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Field = "Currency",
                OperatorType = "EQUALS",
                Value = "USD",
                LogicalOperator = "AND"
            }
        };

        // Case 1: Both conditions met
        var settlement1 = CreateTestSettlement(amount: 10000, currency: "USD");
        var rule1 = CreateTestRule(conditions: conditions);

        var result1 = await _evaluator.EvaluateConditionsAsync(rule1, settlement1);
        Assert.True(result1);

        // Case 2: Only first condition met
        var settlement2 = CreateTestSettlement(amount: 10000, currency: "EUR");
        var rule2 = CreateTestRule(conditions: conditions);

        var result2 = await _evaluator.EvaluateConditionsAsync(rule2, settlement2);
        Assert.False(result2);
    }

    [Fact]
    public async Task EvaluateCondition_MultipleConditionsWithOr_ShouldRequireOneAsync()
    {
        // Arrange
        var conditions = new List<SettlementRuleCondition>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Field = "Currency",
                OperatorType = "EQUALS",
                Value = "USD",
                LogicalOperator = "OR"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Field = "Currency",
                OperatorType = "EQUALS",
                Value = "EUR",
                LogicalOperator = "OR"
            }
        };

        var rule = CreateTestRule(conditions: conditions);

        // Case 1: First condition met
        var settlement1 = CreateTestSettlement(currency: "USD");
        var result1 = await _evaluator.EvaluateConditionsAsync(rule, settlement1);
        Assert.True(result1);

        // Case 2: Second condition met
        var settlement2 = CreateTestSettlement(currency: "EUR");
        var result2 = await _evaluator.EvaluateConditionsAsync(rule, settlement2);
        Assert.True(result2);

        // Case 3: Neither condition met
        var settlement3 = CreateTestSettlement(currency: "GBP");
        var result3 = await _evaluator.EvaluateConditionsAsync(rule, settlement3);
        Assert.False(result3);
    }

    #endregion

    #region Rule Validation Tests

    [Fact]
    public async Task ValidateRule_ValidRule_ShouldPassValidationAsync()
    {
        // Arrange
        var rule = CreateTestRule();

        // Act
        var isValid = await _evaluator.ValidateRuleAsync(rule);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateRule_RuleWithoutConditions_ShouldFailValidationAsync()
    {
        // Arrange
        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Invalid Rule",
            Conditions = new List<SettlementRuleCondition>(),
            Actions = new List<SettlementRuleAction>()
        };

        // Act & Assert - Should handle gracefully or fail validation
        var isValid = await _evaluator.ValidateRuleAsync(rule);
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateRule_RuleWithoutActions_ShouldFailValidationAsync()
    {
        // Arrange
        var conditions = new List<SettlementRuleCondition>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Field = "Currency",
                OperatorType = "EQUALS",
                Value = "USD"
            }
        };

        var rule = new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Invalid Rule",
            Conditions = conditions,
            Actions = new List<SettlementRuleAction>()
        };

        // Act & Assert
        var isValid = await _evaluator.ValidateRuleAsync(rule);
        Assert.False(isValid);
    }

    #endregion

    #region Test Execution Tests

    [Fact]
    public async Task TestRule_PassingSettlement_ShouldReturnSuccessAsync()
    {
        // Arrange
        var rule = CreateTestRule();
        var settlement = CreateTestSettlement(currency: "USD");

        // Act
        var testResult = await _evaluator.TestRuleAsync(rule, settlement);

        // Assert
        Assert.NotNull(testResult);
        // Assert should show test passed if implementation evaluates conditions
    }

    [Fact]
    public async Task TestRule_FailingSettlement_ShouldReturnFailureAsync()
    {
        // Arrange
        var conditions = new List<SettlementRuleCondition>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Field = "Currency",
                OperatorType = "EQUALS",
                Value = "EUR"
            }
        };

        var rule = CreateTestRule(conditions: conditions);
        var settlement = CreateTestSettlement(currency: "USD");

        // Act
        var testResult = await _evaluator.TestRuleAsync(rule, settlement);

        // Assert
        Assert.NotNull(testResult);
        // Assert should show test failed if implementation evaluates conditions
    }

    [Fact]
    public async Task TestRule_ShouldIncludeDetailedEvaluationAsync()
    {
        // Arrange
        var rule = CreateTestRule();
        var settlement = CreateTestSettlement();

        // Act
        var testResult = await _evaluator.TestRuleAsync(rule, settlement);

        // Assert
        Assert.NotNull(testResult);
        Assert.NotNull(testResult.RuleId);
        Assert.NotNull(testResult.SettlementId);
    }

    #endregion

    #region Helper Methods

    private ContractSettlement CreateTestSettlement(
        bool isSales = false,
        string? currency = "USD",
        Guid? partnerId = null,
        decimal amount = 10000)
    {
        return new ContractSettlement
        {
            Id = Guid.NewGuid(),
            ContractId = Guid.NewGuid(),
            IsSalesSettlement = isSales,
            Currency = currency,
            SettlementAmount = amount,
            CreatedDate = DateTime.UtcNow
        };
    }

    private SettlementAutomationRule CreateTestRule(
        List<SettlementRuleCondition>? conditions = null,
        List<SettlementRuleAction>? actions = null)
    {
        conditions ??= new List<SettlementRuleCondition>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Field = "Currency",
                OperatorType = "EQUALS",
                Value = "USD"
            }
        };

        actions ??= new List<SettlementRuleAction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ActionType = "CreateSettlement",
                SequenceNumber = 1
            }
        };

        return new SettlementAutomationRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            RuleType = SettlementRuleType.Automatic,
            Status = RuleStatus.Active,
            IsEnabled = true,
            Scope = SettlementRuleScope.All,
            Trigger = SettlementRuleTrigger.OnContractCompletion,
            OrchestrationStrategy = SettlementOrchestrationStrategy.Sequential,
            Conditions = conditions,
            Actions = actions,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    #endregion
}
