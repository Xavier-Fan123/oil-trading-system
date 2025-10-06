using FluentAssertions;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using Xunit;

namespace OilTrading.UnitTests.Core.Entities;

public class PurchaseContractTests
{
    [Fact]
    public void PurchaseContract_ShouldBeCreated_WithValidData()
    {
        // Arrange
        var contractNumber = ContractNumber.Create(2025, ContractType.CARGO, 1);
        var tradingPartnerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var traderId = Guid.NewGuid();
        var quantity = new Quantity(10000, QuantityUnit.BBL);
        
        // Act
        var contract = new PurchaseContract(
            contractNumber,
            ContractType.CARGO,
            tradingPartnerId,
            productId,
            traderId,
            quantity);
        
        // Assert
        contract.TradingPartnerId.Should().Be(tradingPartnerId);
        contract.ProductId.Should().Be(productId);
        contract.TraderId.Should().Be(traderId);
        contract.ContractType.Should().Be(ContractType.CARGO);
        contract.ContractQuantity.Should().Be(quantity);
        contract.Status.Should().Be(ContractStatus.Draft);
        contract.IsDeleted.Should().BeFalse();
        contract.ContractNumber.Should().Be(contractNumber);
    }

    [Fact]
    public void PurchaseContract_ShouldGenerateContractNumber_Correctly()
    {
        // Arrange
        var contractNumber = ContractNumber.Create(2025, ContractType.CARGO, 1);
        
        // Act
        var contract = CreateValidPurchaseContract(contractNumber);
        
        // Assert
        contract.ContractNumber.Should().NotBeNull();
        contract.ContractNumber.Value.Should().Be("ITGR-2025-CARGO-B0001");
        contract.ContractNumber.Year.Should().Be("2025");
        contract.ContractNumber.Type.Should().Be(ContractType.CARGO);
        contract.ContractNumber.SerialNumber.Should().Be(1);
    }

    [Fact]
    public void PurchaseContract_SoftDelete_ShouldSetDeletedProperties()
    {
        // Arrange
        var contractNumber = ContractNumber.Create(2025, ContractType.CARGO, 1);
        var contract = CreateValidPurchaseContract(contractNumber);
        var deletedBy = "test-user";
        
        // Act
        contract.SoftDelete(deletedBy);
        
        // Assert
        contract.IsDeleted.Should().BeTrue();
        contract.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        contract.DeletedBy.Should().Be(deletedBy);
    }

    [Fact]
    public void PurchaseContract_CanBeLinkedToSalesContract_ShouldReturnTrueForActiveContract()
    {
        // Arrange
        var contractNumber = ContractNumber.Create(2025, ContractType.CARGO, 1);
        var contract = CreateValidPurchaseContract(contractNumber);
        
        // Use reflection to set status to Active for testing
        var statusProperty = typeof(PurchaseContract).GetProperty("Status");
        statusProperty?.SetValue(contract, ContractStatus.Active);
        
        // Act
        var canBeLinked = contract.CanBeLinkedToSalesContract();
        
        // Assert
        canBeLinked.Should().BeTrue();
    }

    [Fact]
    public void PurchaseContract_GetAvailableQuantity_ShouldReturnFullQuantity_WhenNoLinkedContracts()
    {
        // Arrange
        var contractNumber = ContractNumber.Create(2025, ContractType.CARGO, 1);
        var contract = CreateValidPurchaseContract(contractNumber);
        
        // Act
        var availableQuantity = contract.GetAvailableQuantity();
        
        // Assert
        availableQuantity.Should().Be(contract.ContractQuantity);
    }

    private static PurchaseContract CreateValidPurchaseContract(ContractNumber? contractNumber = null)
    {
        var number = contractNumber ?? ContractNumber.Create(2025, ContractType.CARGO, 1);
        return new PurchaseContract(
            number,
            ContractType.CARGO,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Quantity(10000, QuantityUnit.BBL));
    }
}