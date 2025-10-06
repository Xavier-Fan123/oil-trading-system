using FluentAssertions;
using Moq;
using OilTrading.Application.Commands.SalesContracts;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using Xunit;

namespace OilTrading.UnitTests.Application.Commands;

public class ContractMatchingCommandHandlerTests
{
    private readonly Mock<ISalesContractRepository> _mockSalesContractRepository;
    private readonly Mock<IPurchaseContractRepository> _mockPurchaseContractRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;

    public ContractMatchingCommandHandlerTests()
    {
        _mockSalesContractRepository = new Mock<ISalesContractRepository>();
        _mockPurchaseContractRepository = new Mock<IPurchaseContractRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
    }

    #region LinkSalesContractToPurchaseCommandHandler Tests

    [Fact]
    public async Task LinkContracts_WithValidContracts_ShouldLinkSuccessfully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var purchaseContractId = Guid.NewGuid();
        var salesContractId = Guid.NewGuid();

        var purchaseContract = CreatePurchaseContract(purchaseContractId, productId, quantity: 10000m);
        var salesContract = CreateSalesContract(salesContractId, productId, quantity: 5000m);

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdWithIncludesAsync(purchaseContractId, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchaseContract);

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(salesContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesContract);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new LinkSalesContractToPurchaseCommandHandler(
            _mockSalesContractRepository.Object,
            _mockPurchaseContractRepository.Object,
            _mockUnitOfWork.Object);

        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = salesContractId,
            PurchaseContractId = purchaseContractId
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        salesContract.LinkedPurchaseContractId.Should().Be(purchaseContractId);
    }

    [Fact]
    public async Task LinkContracts_WithNonExistentSalesContract_ShouldThrowNotFoundException()
    {
        // Arrange
        var purchaseContractId = Guid.NewGuid();
        var salesContractId = Guid.NewGuid();

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(salesContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalesContract?)null);

        var handler = new LinkSalesContractToPurchaseCommandHandler(
            _mockSalesContractRepository.Object,
            _mockPurchaseContractRepository.Object,
            _mockUnitOfWork.Object);

        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = salesContractId,
            PurchaseContractId = purchaseContractId
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            async () => await handler.Handle(command, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkContracts_WithNonExistentPurchaseContract_ShouldThrowNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var purchaseContractId = Guid.NewGuid();
        var salesContractId = Guid.NewGuid();

        var salesContract = CreateSalesContract(salesContractId, productId, quantity: 5000m);

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(salesContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesContract);

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdWithIncludesAsync(purchaseContractId, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseContract?)null);

        var handler = new LinkSalesContractToPurchaseCommandHandler(
            _mockSalesContractRepository.Object,
            _mockPurchaseContractRepository.Object,
            _mockUnitOfWork.Object);

        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = salesContractId,
            PurchaseContractId = purchaseContractId
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            async () => await handler.Handle(command, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkContracts_WithDifferentProducts_ShouldThrowDomainException()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var purchaseContractId = Guid.NewGuid();
        var salesContractId = Guid.NewGuid();

        var purchaseContract = CreatePurchaseContract(purchaseContractId, productId1, quantity: 10000m);
        var salesContract = CreateSalesContract(salesContractId, productId2, quantity: 5000m);

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdWithIncludesAsync(purchaseContractId, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchaseContract);

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(salesContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesContract);

        var handler = new LinkSalesContractToPurchaseCommandHandler(
            _mockSalesContractRepository.Object,
            _mockPurchaseContractRepository.Object,
            _mockUnitOfWork.Object);

        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = salesContractId,
            PurchaseContractId = purchaseContractId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            async () => await handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("same product");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkContracts_WhenSalesQuantityExceedsPurchaseQuantity_ShouldThrowDomainException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var purchaseContractId = Guid.NewGuid();
        var salesContractId = Guid.NewGuid();

        // Purchase: 5000 MT, Sales: 10000 MT (exceeds available)
        var purchaseContract = CreatePurchaseContract(purchaseContractId, productId, quantity: 5000m);
        var salesContract = CreateSalesContract(salesContractId, productId, quantity: 10000m);

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdWithIncludesAsync(purchaseContractId, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchaseContract);

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(salesContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesContract);

        var handler = new LinkSalesContractToPurchaseCommandHandler(
            _mockSalesContractRepository.Object,
            _mockPurchaseContractRepository.Object,
            _mockUnitOfWork.Object);

        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = salesContractId,
            PurchaseContractId = purchaseContractId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            async () => await handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("exceeds available purchase quantity");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkContracts_WithIncompatibleQuantityUnits_ShouldThrowDomainException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var purchaseContractId = Guid.NewGuid();
        var salesContractId = Guid.NewGuid();

        // Purchase in MT, Sales in BBL - incompatible units
        var purchaseContract = CreatePurchaseContract(purchaseContractId, productId, quantity: 10000m, unit: QuantityUnit.MT);
        var salesContract = CreateSalesContract(salesContractId, productId, quantity: 5000m, unit: QuantityUnit.BBL);

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdWithIncludesAsync(purchaseContractId, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchaseContract);

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(salesContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesContract);

        var handler = new LinkSalesContractToPurchaseCommandHandler(
            _mockSalesContractRepository.Object,
            _mockPurchaseContractRepository.Object,
            _mockUnitOfWork.Object);

        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = salesContractId,
            PurchaseContractId = purchaseContractId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            async () => await handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("compatible quantity units");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkContracts_WhenSalesContractAlreadyLinkedToAnother_ShouldThrowDomainException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var purchaseContractId = Guid.NewGuid();
        var otherPurchaseContractId = Guid.NewGuid();
        var salesContractId = Guid.NewGuid();

        var purchaseContract = CreatePurchaseContract(purchaseContractId, productId, quantity: 10000m);
        var salesContract = CreateSalesContract(salesContractId, productId, quantity: 5000m);

        // Sales contract already linked to another purchase contract
        salesContract.LinkToPurchaseContract(otherPurchaseContractId);

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdWithIncludesAsync(purchaseContractId, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchaseContract);

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(salesContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesContract);

        var handler = new LinkSalesContractToPurchaseCommandHandler(
            _mockSalesContractRepository.Object,
            _mockPurchaseContractRepository.Object,
            _mockUnitOfWork.Object);

        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = salesContractId,
            PurchaseContractId = purchaseContractId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            async () => await handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("already linked to another purchase contract");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkContracts_WithInactivePurchaseContract_ShouldThrowDomainException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var purchaseContractId = Guid.NewGuid();
        var salesContractId = Guid.NewGuid();

        var purchaseContract = CreatePurchaseContract(purchaseContractId, productId, quantity: 10000m);
        purchaseContract.Status = ContractStatus.Draft; // Not active

        var salesContract = CreateSalesContract(salesContractId, productId, quantity: 5000m);

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdWithIncludesAsync(purchaseContractId, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchaseContract);

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(salesContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesContract);

        var handler = new LinkSalesContractToPurchaseCommandHandler(
            _mockSalesContractRepository.Object,
            _mockPurchaseContractRepository.Object,
            _mockUnitOfWork.Object);

        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = salesContractId,
            PurchaseContractId = purchaseContractId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            async () => await handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("not available for linking");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkContracts_WithPartiallyAllocatedPurchaseContract_ShouldLinkToRemainingQuantity()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var purchaseContractId = Guid.NewGuid();
        var salesContractId1 = Guid.NewGuid();
        var salesContractId2 = Guid.NewGuid();

        // Purchase contract with 10000 MT, already has 6000 MT linked
        var purchaseContract = CreatePurchaseContract(purchaseContractId, productId, quantity: 10000m);
        var existingSalesContract = CreateSalesContract(salesContractId1, productId, quantity: 6000m);
        existingSalesContract.LinkToPurchaseContract(purchaseContractId);
        purchaseContract.LinkedSalesContracts.Add(existingSalesContract);

        // New sales contract with 3000 MT (within remaining 4000 MT)
        var newSalesContract = CreateSalesContract(salesContractId2, productId, quantity: 3000m);

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdWithIncludesAsync(purchaseContractId, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchaseContract);

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(salesContractId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newSalesContract);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new LinkSalesContractToPurchaseCommandHandler(
            _mockSalesContractRepository.Object,
            _mockPurchaseContractRepository.Object,
            _mockUnitOfWork.Object);

        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = salesContractId2,
            PurchaseContractId = purchaseContractId
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        newSalesContract.LinkedPurchaseContractId.Should().Be(purchaseContractId);
    }

    #endregion

    #region UnlinkSalesContractFromPurchaseCommandHandler Tests

    [Fact]
    public async Task UnlinkContracts_WithValidLinkedContract_ShouldUnlinkSuccessfully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var purchaseContractId = Guid.NewGuid();
        var salesContractId = Guid.NewGuid();

        var salesContract = CreateSalesContract(salesContractId, productId, quantity: 5000m);
        salesContract.LinkToPurchaseContract(purchaseContractId);

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(salesContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesContract);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UnlinkSalesContractFromPurchaseCommandHandler(
            _mockSalesContractRepository.Object,
            _mockUnitOfWork.Object);

        var command = new UnlinkSalesContractFromPurchaseCommand
        {
            SalesContractId = salesContractId
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        salesContract.LinkedPurchaseContractId.Should().BeNull();
    }

    [Fact]
    public async Task UnlinkContracts_WithNonExistentSalesContract_ShouldThrowNotFoundException()
    {
        // Arrange
        var salesContractId = Guid.NewGuid();

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(salesContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalesContract?)null);

        var handler = new UnlinkSalesContractFromPurchaseCommandHandler(
            _mockSalesContractRepository.Object,
            _mockUnitOfWork.Object);

        var command = new UnlinkSalesContractFromPurchaseCommand
        {
            SalesContractId = salesContractId
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            async () => await handler.Handle(command, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnlinkContracts_WithUnlinkedContract_ShouldCompleteSuccessfully()
    {
        // Arrange - Sales contract not linked to any purchase contract
        var productId = Guid.NewGuid();
        var salesContractId = Guid.NewGuid();

        var salesContract = CreateSalesContract(salesContractId, productId, quantity: 5000m);
        // No link established

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(salesContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesContract);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UnlinkSalesContractFromPurchaseCommandHandler(
            _mockSalesContractRepository.Object,
            _mockUnitOfWork.Object);

        var command = new UnlinkSalesContractFromPurchaseCommand
        {
            SalesContractId = salesContractId
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - Should complete without error
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        salesContract.LinkedPurchaseContractId.Should().BeNull();
    }

    #endregion

    #region Command Validation Tests

    [Fact]
    public void LinkSalesContractToPurchaseCommand_WithEmptySalesContractId_ShouldFailValidation()
    {
        // Arrange
        var validator = new LinkSalesContractToPurchaseCommandValidator();
        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = Guid.Empty,
            PurchaseContractId = Guid.NewGuid()
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SalesContractId");
    }

    [Fact]
    public void LinkSalesContractToPurchaseCommand_WithEmptyPurchaseContractId_ShouldFailValidation()
    {
        // Arrange
        var validator = new LinkSalesContractToPurchaseCommandValidator();
        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = Guid.NewGuid(),
            PurchaseContractId = Guid.Empty
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PurchaseContractId");
    }

    [Fact]
    public void LinkSalesContractToPurchaseCommand_WithValidIds_ShouldPassValidation()
    {
        // Arrange
        var validator = new LinkSalesContractToPurchaseCommandValidator();
        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = Guid.NewGuid(),
            PurchaseContractId = Guid.NewGuid()
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private PurchaseContract CreatePurchaseContract(
        Guid id,
        Guid productId,
        decimal quantity,
        QuantityUnit unit = QuantityUnit.MT)
    {
        var traderId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        return new PurchaseContract(
            contractNumber: new ContractNumber($"PC-{id.ToString()[..8]}"),
            traderId: traderId,
            supplierId: supplierId,
            productId: productId,
            contractQuantity: new Quantity(quantity, unit),
            contractPrice: Money.USD(75m),
            deliveryStart: DateTime.UtcNow.AddMonths(1),
            deliveryEnd: DateTime.UtcNow.AddMonths(2))
        {
            Id = id,
            Status = ContractStatus.Active
        };
    }

    private SalesContract CreateSalesContract(
        Guid id,
        Guid productId,
        decimal quantity,
        QuantityUnit unit = QuantityUnit.MT)
    {
        var traderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();

        return new SalesContract(
            contractNumber: new ContractNumber($"SC-{id.ToString()[..8]}"),
            traderId: traderId,
            buyerId: buyerId,
            productId: productId,
            contractQuantity: new Quantity(quantity, unit),
            contractPrice: Money.USD(80m),
            deliveryStart: DateTime.UtcNow.AddMonths(1),
            deliveryEnd: DateTime.UtcNow.AddMonths(2))
        {
            Id = id,
            Status = ContractStatus.Active
        };
    }

    #endregion
}
