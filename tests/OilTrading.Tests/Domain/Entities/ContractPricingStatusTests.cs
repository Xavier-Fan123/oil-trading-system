using FluentAssertions;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Enums;
using Xunit;

namespace OilTrading.Tests.Domain.Entities;

/// <summary>
/// Tests for Contract Pricing Status functionality (Data Lineage Enhancement v2.18.0)
/// Covers: UpdatePricingStatus, AddFixedQuantity, ResetPricingStatus for both Purchase and Sales contracts
/// </summary>
public class ContractPricingStatusTests
{
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _traderId = Guid.NewGuid();

    #region PurchaseContract - Initial State Tests

    [Fact]
    public void PurchaseContract_NewContract_HasUnpricedStatus()
    {
        // Arrange & Act
        var contract = CreateValidPurchaseContract();

        // Assert
        contract.PricingStatus.Should().Be(ContractPricingStatus.Unpriced);
        contract.FixedQuantity.Should().Be(0);
        contract.UnfixedQuantity.Should().Be(0); // Will be set on first pricing update
        contract.FixedPercentage.Should().Be(0);
        contract.LastPricingDate.Should().BeNull();
        contract.PriceSource.Should().BeNull();
    }

    #endregion

    #region PurchaseContract - UpdatePricingStatus Tests

    [Fact]
    public void PurchaseContract_UpdatePricingStatus_ToFullyPriced_UpdatesFieldsCorrectly()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();
        var totalQuantity = contract.ContractQuantity!.Value; // 1000 MT

        // Act
        contract.UpdatePricingStatus(totalQuantity, PriceSourceType.Manual, "TestUser");

        // Assert
        contract.PricingStatus.Should().Be(ContractPricingStatus.FullyPriced);
        contract.FixedQuantity.Should().Be(1000);
        contract.UnfixedQuantity.Should().Be(0);
        contract.FixedPercentage.Should().Be(100);
        contract.PriceSource.Should().Be(PriceSourceType.Manual);
        contract.LastPricingDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void PurchaseContract_UpdatePricingStatus_ToPartiallyPriced_UpdatesFieldsCorrectly()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();

        // Act - Fix 500 out of 1000 MT
        contract.UpdatePricingStatus(500, PriceSourceType.MarketData, "TestUser");

        // Assert
        contract.PricingStatus.Should().Be(ContractPricingStatus.PartiallyPriced);
        contract.FixedQuantity.Should().Be(500);
        contract.UnfixedQuantity.Should().Be(500);
        contract.FixedPercentage.Should().Be(50);
        contract.PriceSource.Should().Be(PriceSourceType.MarketData);
    }

    [Fact]
    public void PurchaseContract_UpdatePricingStatus_ToZero_ResultsInUnpriced()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();
        contract.UpdatePricingStatus(500, PriceSourceType.Manual, "TestUser"); // First set to partial

        // Act
        contract.UpdatePricingStatus(0, PriceSourceType.Manual, "TestUser");

        // Assert
        contract.PricingStatus.Should().Be(ContractPricingStatus.Unpriced);
        contract.FixedQuantity.Should().Be(0);
        contract.UnfixedQuantity.Should().Be(1000);
        contract.FixedPercentage.Should().Be(0);
    }

    [Fact]
    public void PurchaseContract_UpdatePricingStatus_WithNegativeQuantity_ThrowsDomainException()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();

        // Act & Assert
        var action = () => contract.UpdatePricingStatus(-100, PriceSourceType.Manual, "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Fixed quantity cannot be negative*");
    }

    [Fact]
    public void PurchaseContract_UpdatePricingStatus_ExceedingTotalQuantity_ThrowsDomainException()
    {
        // Arrange
        var contract = CreateValidPurchaseContract(); // 1000 MT

        // Act & Assert
        var action = () => contract.UpdatePricingStatus(1500, PriceSourceType.Manual, "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Fixed quantity cannot exceed contract quantity*");
    }

    [Fact]
    public void PurchaseContract_UpdatePricingStatus_WithFormulaPriceSource_SetsCorrectSource()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();

        // Act
        contract.UpdatePricingStatus(750, PriceSourceType.Formula, "TestUser");

        // Assert
        contract.PriceSource.Should().Be(PriceSourceType.Formula);
        contract.FixedPercentage.Should().Be(75);
        contract.PricingStatus.Should().Be(ContractPricingStatus.PartiallyPriced);
    }

    #endregion

    #region PurchaseContract - AddFixedQuantity Tests

    [Fact]
    public void PurchaseContract_AddFixedQuantity_IncreasesFixedQuantityAndPercentage()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();
        contract.UpdatePricingStatus(300, PriceSourceType.Manual, "TestUser"); // Initial: 30%

        // Act
        contract.AddFixedQuantity(200, PriceSourceType.MarketData, "TestUser"); // Add 200, now 50%

        // Assert
        contract.FixedQuantity.Should().Be(500);
        contract.UnfixedQuantity.Should().Be(500);
        contract.FixedPercentage.Should().Be(50);
        contract.PricingStatus.Should().Be(ContractPricingStatus.PartiallyPriced);
    }

    [Fact]
    public void PurchaseContract_AddFixedQuantity_ToReachFullyPriced()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();
        contract.UpdatePricingStatus(700, PriceSourceType.Manual, "TestUser"); // 70%

        // Act
        contract.AddFixedQuantity(300, PriceSourceType.Manual, "TestUser"); // Add remaining 30%

        // Assert
        contract.FixedQuantity.Should().Be(1000);
        contract.FixedPercentage.Should().Be(100);
        contract.PricingStatus.Should().Be(ContractPricingStatus.FullyPriced);
    }

    [Fact]
    public void PurchaseContract_AddFixedQuantity_WithZero_ThrowsDomainException()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();

        // Act & Assert
        var action = () => contract.AddFixedQuantity(0, PriceSourceType.Manual, "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Additional fixed quantity must be greater than zero*");
    }

    [Fact]
    public void PurchaseContract_AddFixedQuantity_WithNegative_ThrowsDomainException()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();

        // Act & Assert
        var action = () => contract.AddFixedQuantity(-50, PriceSourceType.Manual, "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Additional fixed quantity must be greater than zero*");
    }

    [Fact]
    public void PurchaseContract_AddFixedQuantity_ExceedingTotal_ThrowsDomainException()
    {
        // Arrange
        var contract = CreateValidPurchaseContract(); // 1000 MT
        contract.UpdatePricingStatus(800, PriceSourceType.Manual, "TestUser"); // 80%

        // Act & Assert - Try to add 300 more (would be 1100 total)
        var action = () => contract.AddFixedQuantity(300, PriceSourceType.Manual, "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Fixed quantity cannot exceed contract quantity*");
    }

    #endregion

    #region PurchaseContract - ResetPricingStatus Tests

    [Fact]
    public void PurchaseContract_ResetPricingStatus_ClearsAllPricingFields()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();
        contract.UpdatePricingStatus(750, PriceSourceType.MarketData, "TestUser");
        contract.PricingStatus.Should().Be(ContractPricingStatus.PartiallyPriced); // Verify setup

        // Act
        contract.ResetPricingStatus("TestUser");

        // Assert
        contract.PricingStatus.Should().Be(ContractPricingStatus.Unpriced);
        contract.FixedQuantity.Should().Be(0);
        contract.UnfixedQuantity.Should().Be(1000); // Reset to full contract quantity
        contract.FixedPercentage.Should().Be(0);
        contract.PriceSource.Should().BeNull();
        contract.LastPricingDate.Should().BeNull();
    }

    [Fact]
    public void PurchaseContract_ResetPricingStatus_FromFullyPriced_ResetsToUnpriced()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();
        contract.UpdatePricingStatus(1000, PriceSourceType.Manual, "TestUser");
        contract.PricingStatus.Should().Be(ContractPricingStatus.FullyPriced);

        // Act
        contract.ResetPricingStatus("TestUser");

        // Assert
        contract.PricingStatus.Should().Be(ContractPricingStatus.Unpriced);
    }

    #endregion

    #region SalesContract - Pricing Status Tests

    [Fact]
    public void SalesContract_NewContract_HasUnpricedStatus()
    {
        // Arrange & Act
        var contract = CreateValidSalesContract();

        // Assert
        contract.PricingStatus.Should().Be(ContractPricingStatus.Unpriced);
        contract.FixedQuantity.Should().Be(0);
        contract.FixedPercentage.Should().Be(0);
    }

    [Fact]
    public void SalesContract_UpdatePricingStatus_ToPartiallyPriced_UpdatesFieldsCorrectly()
    {
        // Arrange
        var contract = CreateValidSalesContract();

        // Act - Fix 400 out of 1000 MT
        contract.UpdatePricingStatus(400, PriceSourceType.MarketData, "TestUser");

        // Assert
        contract.PricingStatus.Should().Be(ContractPricingStatus.PartiallyPriced);
        contract.FixedQuantity.Should().Be(400);
        contract.UnfixedQuantity.Should().Be(600);
        contract.FixedPercentage.Should().Be(40);
    }

    [Fact]
    public void SalesContract_UpdatePricingStatus_ToFullyPriced_UpdatesFieldsCorrectly()
    {
        // Arrange
        var contract = CreateValidSalesContract();

        // Act
        contract.UpdatePricingStatus(1000, PriceSourceType.Formula, "TestUser");

        // Assert
        contract.PricingStatus.Should().Be(ContractPricingStatus.FullyPriced);
        contract.FixedQuantity.Should().Be(1000);
        contract.UnfixedQuantity.Should().Be(0);
        contract.FixedPercentage.Should().Be(100);
    }

    [Fact]
    public void SalesContract_AddFixedQuantity_IncreasesFixedQuantity()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        contract.UpdatePricingStatus(250, PriceSourceType.Manual, "TestUser"); // 25%

        // Act
        contract.AddFixedQuantity(250, PriceSourceType.Manual, "TestUser"); // Add 25%

        // Assert
        contract.FixedQuantity.Should().Be(500);
        contract.FixedPercentage.Should().Be(50);
        contract.PricingStatus.Should().Be(ContractPricingStatus.PartiallyPriced);
    }

    [Fact]
    public void SalesContract_ResetPricingStatus_ClearsAllFields()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        contract.UpdatePricingStatus(600, PriceSourceType.MarketData, "TestUser");

        // Act
        contract.ResetPricingStatus("TestUser");

        // Assert
        contract.PricingStatus.Should().Be(ContractPricingStatus.Unpriced);
        contract.FixedQuantity.Should().Be(0);
        contract.FixedPercentage.Should().Be(0);
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public void PurchaseContract_UpdatePricingStatus_WithVerySmallPercentage()
    {
        // Arrange
        var contract = CreateValidPurchaseContract(); // 1000 MT

        // Act - Fix only 1 MT out of 1000 (0.1%)
        contract.UpdatePricingStatus(1, PriceSourceType.Manual, "TestUser");

        // Assert
        contract.PricingStatus.Should().Be(ContractPricingStatus.PartiallyPriced);
        contract.FixedQuantity.Should().Be(1);
        contract.FixedPercentage.Should().Be(0.1m);
    }

    [Fact]
    public void PurchaseContract_UpdatePricingStatus_SequentialUpdates()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();

        // Act & Assert - Sequential pricing updates
        contract.UpdatePricingStatus(200, PriceSourceType.Manual, "TestUser");
        contract.PricingStatus.Should().Be(ContractPricingStatus.PartiallyPriced);
        contract.FixedPercentage.Should().Be(20);

        contract.UpdatePricingStatus(500, PriceSourceType.MarketData, "TestUser");
        contract.PricingStatus.Should().Be(ContractPricingStatus.PartiallyPriced);
        contract.FixedPercentage.Should().Be(50);

        contract.UpdatePricingStatus(1000, PriceSourceType.Formula, "TestUser");
        contract.PricingStatus.Should().Be(ContractPricingStatus.FullyPriced);
        contract.FixedPercentage.Should().Be(100);
    }

    [Fact]
    public void PurchaseContract_PricingStatusTransitions_CanGoBackwards()
    {
        // Arrange
        var contract = CreateValidPurchaseContract();
        contract.UpdatePricingStatus(1000, PriceSourceType.Manual, "TestUser");
        contract.PricingStatus.Should().Be(ContractPricingStatus.FullyPriced);

        // Act - Reduce fixed quantity (status can regress)
        contract.UpdatePricingStatus(500, PriceSourceType.Manual, "TestUser");

        // Assert - Status goes from FullyPriced back to PartiallyPriced
        contract.PricingStatus.Should().Be(ContractPricingStatus.PartiallyPriced);
        contract.FixedQuantity.Should().Be(500);
    }

    [Fact]
    public void PurchaseContract_FixedPercentage_RoundsToTwoDecimals()
    {
        // Arrange
        var contract = CreateValidPurchaseContract(); // 1000 MT

        // Act - Fix 333 MT (33.3%)
        contract.UpdatePricingStatus(333, PriceSourceType.Manual, "TestUser");

        // Assert
        contract.FixedPercentage.Should().Be(33.3m);
    }

    #endregion

    #region Helper Methods

    private PurchaseContract CreateValidPurchaseContract()
    {
        var contractNumber = ContractNumber.Parse("PC-2024-001");
        var contractType = OilTrading.Core.ValueObjects.ContractType.CARGO;
        var quantity = new Quantity(1000, QuantityUnit.MT);
        var tonBarrelRatio = 7.6m;

        return new PurchaseContract(
            contractNumber: contractNumber,
            contractType: contractType,
            tradingPartnerId: _supplierId,
            productId: _productId,
            traderId: _traderId,
            contractQuantity: quantity,
            tonBarrelRatio: tonBarrelRatio);
    }

    private SalesContract CreateValidSalesContract()
    {
        var contractNumber = ContractNumber.Parse("SC-2024-001");
        var contractType = OilTrading.Core.ValueObjects.ContractType.CARGO;
        var quantity = new Quantity(1000, QuantityUnit.MT);
        var tonBarrelRatio = 7.6m;

        return new SalesContract(
            contractNumber: contractNumber,
            contractType: contractType,
            tradingPartnerId: _customerId,
            productId: _productId,
            traderId: _traderId,
            contractQuantity: quantity,
            tonBarrelRatio: tonBarrelRatio);
    }

    #endregion
}
