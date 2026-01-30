using FluentAssertions;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using OilTrading.Tests.TestBuilders;
using Xunit;

namespace OilTrading.Tests.Domain.Entities;

/// <summary>
/// Tests for PaperContract hedge designation functionality (Data Lineage Enhancement v2.18.0)
/// Covers: DesignateAsHedge, RemoveHedgeDesignation, UpdateHedgeRatio, UpdateHedgeEffectiveness
/// </summary>
public class PaperContractHedgeDesignationTests
{
    private readonly Guid _purchaseContractId = Guid.NewGuid();
    private readonly Guid _salesContractId = Guid.NewGuid();

    #region DesignateAsHedge Tests

    [Fact]
    public void DesignateAsHedge_WithValidPurchaseContract_SetsHedgeProperties()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateBasicOpen();

        // Act
        contract.DesignateAsHedge(
            _purchaseContractId,
            HedgedContractType.Purchase,
            1.0m,
            "TestUser");

        // Assert
        contract.IsDesignatedHedge.Should().BeTrue();
        contract.HedgedContractId.Should().Be(_purchaseContractId);
        contract.HedgedContractType.Should().Be(HedgedContractType.Purchase);
        contract.HedgeRatio.Should().Be(1.0m);
        contract.HedgeDesignationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DesignateAsHedge_WithValidSalesContract_SetsHedgeProperties()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateBasicOpen();

        // Act
        contract.DesignateAsHedge(
            _salesContractId,
            HedgedContractType.Sales,
            0.8m,
            "TestUser");

        // Assert
        contract.IsDesignatedHedge.Should().BeTrue();
        contract.HedgedContractId.Should().Be(_salesContractId);
        contract.HedgedContractType.Should().Be(HedgedContractType.Sales);
        contract.HedgeRatio.Should().Be(0.8m);
    }

    [Fact]
    public void DesignateAsHedge_WithBothContractTypes_SetsHedgeProperties()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateBasicOpen();

        // Act
        contract.DesignateAsHedge(
            _purchaseContractId,
            HedgedContractType.Both,
            1.5m,
            "TestUser");

        // Assert
        contract.IsDesignatedHedge.Should().BeTrue();
        contract.HedgedContractType.Should().Be(HedgedContractType.Both);
        contract.HedgeRatio.Should().Be(1.5m);
    }

    [Fact]
    public void DesignateAsHedge_WithZeroHedgeRatio_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateBasicOpen();

        // Act & Assert
        var action = () => contract.DesignateAsHedge(
            _purchaseContractId,
            HedgedContractType.Purchase,
            0m,
            "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Hedge ratio must be between 0 and 10*");
    }

    [Fact]
    public void DesignateAsHedge_WithNegativeHedgeRatio_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateBasicOpen();

        // Act & Assert
        var action = () => contract.DesignateAsHedge(
            _purchaseContractId,
            HedgedContractType.Purchase,
            -0.5m,
            "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Hedge ratio must be between 0 and 10*");
    }

    [Fact]
    public void DesignateAsHedge_WithExcessiveHedgeRatio_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateBasicOpen();

        // Act & Assert
        var action = () => contract.DesignateAsHedge(
            _purchaseContractId,
            HedgedContractType.Purchase,
            15m, // Exceeds maximum of 10
            "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Hedge ratio must be between 0 and 10*");
    }

    [Fact]
    public void DesignateAsHedge_WhenContractIsClosed_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateClosed();

        // Act & Assert
        var action = () => contract.DesignateAsHedge(
            _purchaseContractId,
            HedgedContractType.Purchase,
            1.0m,
            "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Cannot designate closed or settled contract as hedge*");
    }

    [Fact]
    public void DesignateAsHedge_WhenContractIsSettled_ThrowsDomainException()
    {
        // Arrange
        var contract = new PaperContractBuilder()
            .WithStatus(PaperContractStatus.Settled)
            .Build();

        // Act & Assert
        var action = () => contract.DesignateAsHedge(
            _purchaseContractId,
            HedgedContractType.Purchase,
            1.0m,
            "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Cannot designate closed or settled contract as hedge*");
    }

    #endregion

    #region RemoveHedgeDesignation Tests

    [Fact]
    public void RemoveHedgeDesignation_WhenDesignated_ClearsHedgeProperties()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateWithHedge(_purchaseContractId, HedgedContractType.Purchase);
        contract.IsDesignatedHedge.Should().BeTrue(); // Verify precondition

        // Act
        contract.RemoveHedgeDesignation("Testing removal", "TestUser");

        // Assert
        contract.IsDesignatedHedge.Should().BeFalse();
        contract.HedgedContractId.Should().BeNull();
        contract.HedgedContractType.Should().BeNull();
        contract.HedgeRatio.Should().Be(1.0m); // Reset to default
        contract.HedgeEffectiveness.Should().BeNull();
        contract.HedgeDesignationDate.Should().BeNull();
        contract.Notes.Should().Contain("Hedge designation removed: Testing removal");
    }

    [Fact]
    public void RemoveHedgeDesignation_WhenNotDesignated_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateBasicOpen();
        contract.IsDesignatedHedge.Should().BeFalse(); // Verify precondition

        // Act & Assert
        var action = () => contract.RemoveHedgeDesignation("Testing", "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*This contract is not designated as a hedge*");
    }

    [Fact]
    public void RemoveHedgeDesignation_WithEmptyReason_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateWithHedge(_purchaseContractId, HedgedContractType.Purchase);

        // Act & Assert
        var action = () => contract.RemoveHedgeDesignation("", "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Reason for removing hedge designation is required*");
    }

    [Fact]
    public void RemoveHedgeDesignation_WithWhitespaceReason_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateWithHedge(_purchaseContractId, HedgedContractType.Purchase);

        // Act & Assert
        var action = () => contract.RemoveHedgeDesignation("   ", "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Reason for removing hedge designation is required*");
    }

    #endregion

    #region UpdateHedgeRatio Tests

    [Fact]
    public void UpdateHedgeRatio_WithValidRatio_UpdatesRatio()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateWithHedge(_purchaseContractId, HedgedContractType.Purchase);
        var originalRatio = contract.HedgeRatio;

        // Act
        contract.UpdateHedgeRatio(0.75m, "TestUser");

        // Assert
        contract.HedgeRatio.Should().Be(0.75m);
        contract.HedgeRatio.Should().NotBe(originalRatio);
    }

    [Fact]
    public void UpdateHedgeRatio_WhenNotDesignated_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateBasicOpen();

        // Act & Assert
        var action = () => contract.UpdateHedgeRatio(0.5m, "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Cannot update hedge ratio for non-designated hedge*");
    }

    [Fact]
    public void UpdateHedgeRatio_WithZeroRatio_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateWithHedge(_purchaseContractId, HedgedContractType.Purchase);

        // Act & Assert
        var action = () => contract.UpdateHedgeRatio(0m, "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Hedge ratio must be between 0 and 10*");
    }

    [Fact]
    public void UpdateHedgeRatio_WithExcessiveRatio_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateWithHedge(_purchaseContractId, HedgedContractType.Purchase);

        // Act & Assert
        var action = () => contract.UpdateHedgeRatio(11m, "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Hedge ratio must be between 0 and 10*");
    }

    #endregion

    #region UpdateHedgeEffectiveness Tests

    [Fact]
    public void UpdateHedgeEffectiveness_WithValidValue_UpdatesEffectiveness()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateWithHedge(_purchaseContractId, HedgedContractType.Purchase);

        // Act
        contract.UpdateHedgeEffectiveness(92.5m, "TestUser");

        // Assert
        contract.HedgeEffectiveness.Should().Be(92.5m);
    }

    [Fact]
    public void UpdateHedgeEffectiveness_WhenNotDesignated_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateBasicOpen();

        // Act & Assert
        var action = () => contract.UpdateHedgeEffectiveness(85m, "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Cannot update hedge effectiveness for non-designated hedge*");
    }

    [Fact]
    public void UpdateHedgeEffectiveness_WithNegativeValue_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateWithHedge(_purchaseContractId, HedgedContractType.Purchase);

        // Act & Assert
        var action = () => contract.UpdateHedgeEffectiveness(-10m, "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Hedge effectiveness must be between 0 and 100*");
    }

    [Fact]
    public void UpdateHedgeEffectiveness_WithValueOver100_ThrowsDomainException()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateWithHedge(_purchaseContractId, HedgedContractType.Purchase);

        // Act & Assert
        var action = () => contract.UpdateHedgeEffectiveness(150m, "TestUser");

        action.Should().Throw<DomainException>()
            .WithMessage("*Hedge effectiveness must be between 0 and 100*");
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void CanBeDesignatedAsHedge_WhenOpenAndNotDesignated_ReturnsTrue()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateBasicOpen();

        // Act & Assert
        contract.CanBeDesignatedAsHedge().Should().BeTrue();
    }

    [Fact]
    public void CanBeDesignatedAsHedge_WhenClosed_ReturnsFalse()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateClosed();

        // Act & Assert
        contract.CanBeDesignatedAsHedge().Should().BeFalse();
    }

    [Fact]
    public void CanBeDesignatedAsHedge_WhenAlreadyDesignated_ReturnsFalse()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateWithHedge(_purchaseContractId, HedgedContractType.Purchase);

        // Act & Assert
        contract.CanBeDesignatedAsHedge().Should().BeFalse();
    }

    [Fact]
    public void GetHedgedQuantity_WhenDesignated_ReturnsCorrectQuantity()
    {
        // Arrange
        var contract = new PaperContractBuilder()
            .WithQuantity(10)
            .WithLotSize(1000)
            .WithHedgeDesignation(_purchaseContractId, HedgedContractType.Purchase, 0.5m)
            .Build();

        // Act
        var hedgedQuantity = contract.GetHedgedQuantity();

        // Assert
        // 10 lots * 1000 MT/lot * 0.5 hedge ratio = 5000 MT
        hedgedQuantity.Should().Be(5000m);
    }

    [Fact]
    public void GetHedgedQuantity_WhenNotDesignated_ReturnsZero()
    {
        // Arrange
        var contract = PaperContractBuilder.CreateBasicOpen();

        // Act
        var hedgedQuantity = contract.GetHedgedQuantity();

        // Assert
        hedgedQuantity.Should().Be(0m);
    }

    [Fact]
    public void GetHedgedQuantity_WithFullHedgeRatio_ReturnsFullQuantity()
    {
        // Arrange
        var contract = new PaperContractBuilder()
            .WithQuantity(10)
            .WithLotSize(1000)
            .WithHedgeDesignation(_purchaseContractId, HedgedContractType.Purchase, 1.0m)
            .Build();

        // Act
        var hedgedQuantity = contract.GetHedgedQuantity();

        // Assert
        // 10 lots * 1000 MT/lot * 1.0 hedge ratio = 10000 MT
        hedgedQuantity.Should().Be(10000m);
    }

    #endregion

    #region Builder Integration Tests

    [Fact]
    public void PaperContractBuilder_CreateWithHedge_CreatesDesignatedContract()
    {
        // Act
        var contract = PaperContractBuilder.CreateWithHedge(_purchaseContractId, HedgedContractType.Purchase);

        // Assert
        contract.Should().NotBeNull();
        contract.IsDesignatedHedge.Should().BeTrue();
        contract.HedgedContractId.Should().Be(_purchaseContractId);
        contract.HedgedContractType.Should().Be(HedgedContractType.Purchase);
        contract.Status.Should().Be(PaperContractStatus.Open);
    }

    [Fact]
    public void PaperContractBuilder_WithHedgeDesignation_SetsAllFields()
    {
        // Act
        var contract = new PaperContractBuilder()
            .WithContractNumber("PC-HEDGE-001")
            .WithProductType("WTI")
            .WithContractMonth("JAN26")
            .AsShort()
            .WithQuantity(20)
            .WithLotSize(500)
            .WithEntryPrice(72.00m)
            .WithHedgeDesignation(_salesContractId, HedgedContractType.Sales, 0.75m)
            .Build();

        // Assert
        contract.ContractNumber.Should().Be("PC-HEDGE-001");
        contract.ProductType.Should().Be("WTI");
        contract.Position.Should().Be(PositionType.Short);
        contract.IsDesignatedHedge.Should().BeTrue();
        contract.HedgedContractId.Should().Be(_salesContractId);
        contract.HedgedContractType.Should().Be(HedgedContractType.Sales);
        contract.HedgeRatio.Should().Be(0.75m);
    }

    #endregion
}
