using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using Xunit;

namespace OilTrading.UnitTests.Application.Services;

public class ContractInventoryServiceTests
{
    private readonly Mock<IPurchaseContractRepository> _mockPurchaseContractRepository;
    private readonly Mock<ISalesContractRepository> _mockSalesContractRepository;
    private readonly Mock<IInventoryReservationRepository> _mockReservationRepository;
    private readonly Mock<IRealTimeInventoryService> _mockInventoryService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<ContractInventoryService>> _mockLogger;
    private readonly ContractInventoryService _service;

    public ContractInventoryServiceTests()
    {
        _mockPurchaseContractRepository = new Mock<IPurchaseContractRepository>();
        _mockSalesContractRepository = new Mock<ISalesContractRepository>();
        _mockReservationRepository = new Mock<IInventoryReservationRepository>();
        _mockInventoryService = new Mock<IRealTimeInventoryService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<ContractInventoryService>>();

        _service = new ContractInventoryService(
            _mockPurchaseContractRepository.Object,
            _mockSalesContractRepository.Object,
            _mockReservationRepository.Object,
            _mockInventoryService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    #region ReserveInventoryForContractAsync Tests

    [Fact]
    public async Task ReserveInventory_ForPurchaseContract_WithSufficientInventory_ShouldSucceed()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var productCode = "BRENT";
        var locationCode = "ROTTERDAM";
        var quantity = new Quantity(1000m, QuantityUnit.MT);

        var purchaseContract = CreatePurchaseContract(contractId, productCode, quantity);

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdAsync(contractId, default))
            .ReturnsAsync(purchaseContract);

        _mockReservationRepository
            .Setup(r => r.GetByContractIdAsync(contractId))
            .ReturnsAsync(new List<InventoryReservation>());

        // Mock inventory availability check
        var inventorySnapshot = CreateInventorySnapshot(productCode, availableQuantity: 2000m);
        _mockInventoryService
            .Setup(s => s.GetRealTimeInventoryAsync(productCode, locationCode))
            .ReturnsAsync(inventorySnapshot);

        // Mock inventory reservation
        _mockInventoryService
            .Setup(s => s.ReserveInventoryAsync(It.IsAny<InventoryReservationRequest>()))
            .ReturnsAsync(new InventoryOperationResult { IsSuccessful = true });

        _mockReservationRepository
            .Setup(r => r.AddAsync(It.IsAny<InventoryReservation>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ReserveInventoryForContractAsync(contractId, "Purchase");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.ReservedQuantity.Should().NotBeNull();
        result.ReservedQuantity!.Value.Should().Be(quantity.Value);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ReserveInventory_ForPurchaseContract_WithInsufficientInventory_ShouldFail()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var productCode = "BRENT";
        var locationCode = "ROTTERDAM";
        var requiredQuantity = new Quantity(1000m, QuantityUnit.MT);
        var availableQuantity = 500m; // Less than required

        var purchaseContract = CreatePurchaseContract(contractId, productCode, requiredQuantity);

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdAsync(contractId, default))
            .ReturnsAsync(purchaseContract);

        _mockReservationRepository
            .Setup(r => r.GetByContractIdAsync(contractId))
            .ReturnsAsync(new List<InventoryReservation>());

        // Mock insufficient inventory
        var inventorySnapshot = CreateInventorySnapshot(productCode, availableQuantity);
        _mockInventoryService
            .Setup(s => s.GetRealTimeInventoryAsync(productCode, locationCode))
            .ReturnsAsync(inventorySnapshot);

        // Act
        var result = await _service.ReserveInventoryForContractAsync(contractId, "Purchase");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Insufficient inventory");
        result.AvailableQuantity.Should().NotBeNull();
        result.AvailableQuantity!.Value.Should().Be(availableQuantity);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task ReserveInventory_WithExistingActiveReservation_ShouldFail()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var productCode = "BRENT";
        var quantity = new Quantity(1000m, QuantityUnit.MT);

        var purchaseContract = CreatePurchaseContract(contractId, productCode, quantity);

        // Existing active reservation
        var existingReservation = new InventoryReservation(
            contractId,
            "Purchase",
            productCode,
            "ROTTERDAM",
            quantity,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(29),
            "Existing reservation",
            "System");

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdAsync(contractId, default))
            .ReturnsAsync(purchaseContract);

        _mockReservationRepository
            .Setup(r => r.GetByContractIdAsync(contractId))
            .ReturnsAsync(new List<InventoryReservation> { existingReservation });

        // Act
        var result = await _service.ReserveInventoryForContractAsync(contractId, "Purchase");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Active inventory reservation already exists");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task ReserveInventory_ForNonExistentContract_ShouldFail()
    {
        // Arrange
        var contractId = Guid.NewGuid();

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdAsync(contractId, default))
            .ReturnsAsync((PurchaseContract?)null);

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(contractId, default))
            .ReturnsAsync((SalesContract?)null);

        // Act
        var result = await _service.ReserveInventoryForContractAsync(contractId, "Purchase");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ReserveInventory_ForSalesContract_ShouldNotCheckAvailability()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var productCode = "BRENT";
        var quantity = new Quantity(1000m, QuantityUnit.MT);

        var salesContract = CreateSalesContract(contractId, productCode, quantity);

        _mockSalesContractRepository
            .Setup(r => r.GetByIdAsync(contractId, default))
            .ReturnsAsync(salesContract);

        _mockReservationRepository
            .Setup(r => r.GetByContractIdAsync(contractId))
            .ReturnsAsync(new List<InventoryReservation>());

        _mockReservationRepository
            .Setup(r => r.AddAsync(It.IsAny<InventoryReservation>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ReserveInventoryForContractAsync(contractId, "Sales");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        // Should NOT call inventory availability check for sales contracts
        _mockInventoryService.Verify(s => s.GetRealTimeInventoryAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    #endregion

    #region ReleaseInventoryReservationAsync Tests

    [Fact]
    public async Task ReleaseInventory_WithActiveReservation_ShouldReleaseSuccessfully()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var quantity = new Quantity(1000m, QuantityUnit.MT);

        var reservation = new InventoryReservation(
            contractId,
            "Purchase",
            "BRENT",
            "ROTTERDAM",
            quantity,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(29),
            "Test reservation",
            "System");

        _mockReservationRepository
            .Setup(r => r.GetByContractIdAsync(contractId))
            .ReturnsAsync(new List<InventoryReservation> { reservation });

        _mockInventoryService
            .Setup(s => s.ReleaseReservationAsync(It.IsAny<InventoryReleaseRequest>()))
            .ReturnsAsync(new InventoryOperationResult { IsSuccessful = true });

        _mockReservationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<InventoryReservation>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ReleaseInventoryReservationAsync(contractId, "Contract cancelled");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.ReservedQuantity.Should().NotBeNull();
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ReleaseInventory_WithNoActiveReservation_ShouldFail()
    {
        // Arrange
        var contractId = Guid.NewGuid();

        _mockReservationRepository
            .Setup(r => r.GetByContractIdAsync(contractId))
            .ReturnsAsync(new List<InventoryReservation>());

        // Act
        var result = await _service.ReleaseInventoryReservationAsync(contractId, "Test reason");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No active inventory reservations found");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task ReleaseInventory_WithMultipleActiveReservations_ShouldReleaseAll()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var quantity1 = new Quantity(500m, QuantityUnit.MT);
        var quantity2 = new Quantity(300m, QuantityUnit.MT);

        var reservation1 = new InventoryReservation(
            contractId, "Purchase", "BRENT", "ROTTERDAM", quantity1,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(29), "Reservation 1", "System");

        var reservation2 = new InventoryReservation(
            contractId, "Purchase", "BRENT", "SINGAPORE", quantity2,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(29), "Reservation 2", "System");

        _mockReservationRepository
            .Setup(r => r.GetByContractIdAsync(contractId))
            .ReturnsAsync(new List<InventoryReservation> { reservation1, reservation2 });

        _mockInventoryService
            .Setup(s => s.ReleaseReservationAsync(It.IsAny<InventoryReleaseRequest>()))
            .ReturnsAsync(new InventoryOperationResult { IsSuccessful = true });

        _mockReservationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<InventoryReservation>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ReleaseInventoryReservationAsync(contractId, "Release all");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.ReservedQuantity!.Value.Should().Be(800m); // 500 + 300
        _mockReservationRepository.Verify(r => r.UpdateAsync(It.IsAny<InventoryReservation>()), Times.Exactly(2));
    }

    #endregion

    #region PartialReleaseInventoryAsync Tests

    [Fact]
    public async Task PartialReleaseInventory_WithSufficientReservation_ShouldReleasePartially()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var reservedQuantity = new Quantity(1000m, QuantityUnit.MT);
        var releaseQuantity = new Quantity(400m, QuantityUnit.MT);

        var reservation = new InventoryReservation(
            contractId, "Purchase", "BRENT", "ROTTERDAM", reservedQuantity,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(29), "Test reservation", "System");

        _mockReservationRepository
            .Setup(r => r.GetByContractIdAsync(contractId))
            .ReturnsAsync(new List<InventoryReservation> { reservation });

        _mockInventoryService
            .Setup(s => s.ReleaseReservationAsync(It.IsAny<InventoryReleaseRequest>()))
            .ReturnsAsync(new InventoryOperationResult { IsSuccessful = true });

        _mockReservationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<InventoryReservation>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _service.PartialReleaseInventoryAsync(contractId, releaseQuantity, "Partial release");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.ReservedQuantity!.Value.Should().Be(400m);
        result.Warnings.Should().BeEmpty();
        reservation.GetRemainingQuantity().Value.Should().Be(600m); // 1000 - 400
    }

    [Fact]
    public async Task PartialReleaseInventory_ExceedingReservation_ShouldReleaseOnlyAvailable()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var reservedQuantity = new Quantity(1000m, QuantityUnit.MT);
        var releaseQuantity = new Quantity(1500m, QuantityUnit.MT); // More than reserved

        var reservation = new InventoryReservation(
            contractId, "Purchase", "BRENT", "ROTTERDAM", reservedQuantity,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(29), "Test reservation", "System");

        _mockReservationRepository
            .Setup(r => r.GetByContractIdAsync(contractId))
            .ReturnsAsync(new List<InventoryReservation> { reservation });

        _mockInventoryService
            .Setup(s => s.ReleaseReservationAsync(It.IsAny<InventoryReleaseRequest>()))
            .ReturnsAsync(new InventoryOperationResult { IsSuccessful = true });

        _mockReservationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<InventoryReservation>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _service.PartialReleaseInventoryAsync(contractId, releaseQuantity, "Partial release");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.ReservedQuantity!.Value.Should().Be(1000m); // Can only release what's reserved
        result.Warnings.Should().NotBeEmpty();
        result.Warnings.Should().Contain(w => w.Contains("Could not release full requested quantity"));
    }

    [Fact]
    public async Task PartialReleaseInventory_WithZeroQuantity_ShouldDoNothing()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var releaseQuantity = Quantity.Zero(QuantityUnit.MT);

        var reservation = new InventoryReservation(
            contractId, "Purchase", "BRENT", "ROTTERDAM", new Quantity(1000m, QuantityUnit.MT),
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(29), "Test reservation", "System");

        _mockReservationRepository
            .Setup(r => r.GetByContractIdAsync(contractId))
            .ReturnsAsync(new List<InventoryReservation> { reservation });

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _service.PartialReleaseInventoryAsync(contractId, releaseQuantity, "Zero release");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.ReservedQuantity!.Value.Should().Be(0m);
        // Should not call inventory service for zero release
        _mockInventoryService.Verify(s => s.ReleaseReservationAsync(It.IsAny<InventoryReleaseRequest>()), Times.Never);
    }

    #endregion

    #region CheckInventoryAvailabilityAsync Tests

    [Fact]
    public async Task CheckInventoryAvailability_WithSufficientStock_ShouldReturnAvailable()
    {
        // Arrange
        var productCode = "BRENT";
        var locationCode = "ROTTERDAM";
        var requiredQuantity = new Quantity(1000m, QuantityUnit.MT);
        var availableQuantity = 2000m;

        var inventorySnapshot = CreateInventorySnapshot(productCode, availableQuantity);
        _mockInventoryService
            .Setup(s => s.GetRealTimeInventoryAsync(productCode, locationCode))
            .ReturnsAsync(inventorySnapshot);

        // Act
        var result = await _service.CheckInventoryAvailabilityAsync(productCode, locationCode, requiredQuantity);

        // Assert
        result.IsAvailable.Should().BeTrue();
        result.AvailableQuantity.Value.Should().Be(availableQuantity);
        result.ShortfallQuantity.IsZero().Should().BeTrue();
    }

    [Fact]
    public async Task CheckInventoryAvailability_WithInsufficientStock_ShouldReturnShortfall()
    {
        // Arrange
        var productCode = "BRENT";
        var locationCode = "ROTTERDAM";
        var requiredQuantity = new Quantity(1000m, QuantityUnit.MT);
        var availableQuantity = 600m;

        var inventorySnapshot = CreateInventorySnapshot(productCode, availableQuantity);
        _mockInventoryService
            .Setup(s => s.GetRealTimeInventoryAsync(productCode, locationCode))
            .ReturnsAsync(inventorySnapshot);

        // Act
        var result = await _service.CheckInventoryAvailabilityAsync(productCode, locationCode, requiredQuantity);

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.AvailableQuantity.Value.Should().Be(availableQuantity);
        result.ShortfallQuantity.Value.Should().Be(400m); // 1000 - 600
    }

    [Fact]
    public async Task CheckInventoryAvailability_WithZeroStock_ShouldReturnFullShortfall()
    {
        // Arrange
        var productCode = "BRENT";
        var locationCode = "ROTTERDAM";
        var requiredQuantity = new Quantity(1000m, QuantityUnit.MT);
        var availableQuantity = 0m;

        var inventorySnapshot = CreateInventorySnapshot(productCode, availableQuantity);
        _mockInventoryService
            .Setup(s => s.GetRealTimeInventoryAsync(productCode, locationCode))
            .ReturnsAsync(inventorySnapshot);

        // Act
        var result = await _service.CheckInventoryAvailabilityAsync(productCode, locationCode, requiredQuantity);

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.ShortfallQuantity.Value.Should().Be(requiredQuantity.Value);
    }

    #endregion

    #region ExtendReservationAsync Tests

    [Fact]
    public async Task ExtendReservation_WithValidReservation_ShouldExtendSuccessfully()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var originalExpiryDate = DateTime.UtcNow.AddDays(10);
        var newExpiryDate = DateTime.UtcNow.AddDays(30);

        var reservation = new InventoryReservation(
            Guid.NewGuid(), "Purchase", "BRENT", "ROTTERDAM",
            new Quantity(1000m, QuantityUnit.MT),
            DateTime.UtcNow.AddDays(-1), originalExpiryDate,
            "Test reservation", "System")
        {
            Id = reservationId
        };

        _mockReservationRepository
            .Setup(r => r.GetByIdAsync(reservationId))
            .ReturnsAsync(reservation);

        _mockReservationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<InventoryReservation>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ExtendReservationAsync(reservationId, newExpiryDate, "Need more time");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.ReservationId.Should().Be(reservationId);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ExtendReservation_WithNonExistentReservation_ShouldFail()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _mockReservationRepository
            .Setup(r => r.GetByIdAsync(reservationId))
            .ReturnsAsync((InventoryReservation?)null);

        // Act
        var result = await _service.ExtendReservationAsync(reservationId, DateTime.UtcNow.AddDays(30), "Test");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Reservation not found");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    #endregion

    #region CancelReservationAsync Tests

    [Fact]
    public async Task CancelReservation_WithValidReservation_ShouldCancelSuccessfully()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        var reservation = new InventoryReservation(
            Guid.NewGuid(), "Purchase", "BRENT", "ROTTERDAM",
            new Quantity(1000m, QuantityUnit.MT),
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(29),
            "Test reservation", "System")
        {
            Id = reservationId
        };

        _mockReservationRepository
            .Setup(r => r.GetByIdAsync(reservationId))
            .ReturnsAsync(reservation);

        _mockReservationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<InventoryReservation>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CancelReservationAsync(reservationId, "Contract cancelled");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.ReservationId.Should().Be(reservationId);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CancelReservation_WithNonExistentReservation_ShouldFail()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _mockReservationRepository
            .Setup(r => r.GetByIdAsync(reservationId))
            .ReturnsAsync((InventoryReservation?)null);

        // Act
        var result = await _service.CancelReservationAsync(reservationId, "Test reason");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Reservation not found");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    #endregion

    #region GetActiveReservationsAsync Tests

    [Fact]
    public async Task GetActiveReservations_ShouldReturnOnlyActiveReservations()
    {
        // Arrange
        var activeReservations = new List<InventoryReservation>
        {
            new InventoryReservation(
                Guid.NewGuid(), "Purchase", "BRENT", "ROTTERDAM",
                new Quantity(1000m, QuantityUnit.MT),
                DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(29),
                "Active 1", "System"),
            new InventoryReservation(
                Guid.NewGuid(), "Purchase", "WTI", "SINGAPORE",
                new Quantity(500m, QuantityUnit.MT),
                DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(28),
                "Active 2", "System")
        };

        _mockReservationRepository
            .Setup(r => r.GetActiveReservationsAsync())
            .ReturnsAsync(activeReservations);

        // Act
        var result = await _service.GetActiveReservationsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(activeReservations);
    }

    #endregion

    #region ProcessExpiredReservationsAsync Tests

    [Fact]
    public async Task ProcessExpiredReservations_WithExpiredReservations_ShouldCancelThem()
    {
        // Arrange
        var expiredReservations = new List<InventoryReservation>
        {
            new InventoryReservation(
                Guid.NewGuid(), "Purchase", "BRENT", "ROTTERDAM",
                new Quantity(1000m, QuantityUnit.MT),
                DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-1), // Expired
                "Expired 1", "System"),
            new InventoryReservation(
                Guid.NewGuid(), "Purchase", "WTI", "SINGAPORE",
                new Quantity(500m, QuantityUnit.MT),
                DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddDays(-2), // Expired
                "Expired 2", "System")
        };

        _mockReservationRepository
            .Setup(r => r.GetExpiredReservationsAsync())
            .ReturnsAsync(expiredReservations);

        _mockReservationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<InventoryReservation>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ProcessExpiredReservationsAsync();

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Metadata.Should().ContainKey("ProcessedReservations");
        result.Metadata.Should().ContainKey("TotalExpired");
        _mockReservationRepository.Verify(r => r.UpdateAsync(It.IsAny<InventoryReservation>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ProcessExpiredReservations_WithNoExpiredReservations_ShouldNotSaveChanges()
    {
        // Arrange
        _mockReservationRepository
            .Setup(r => r.GetExpiredReservationsAsync())
            .ReturnsAsync(new List<InventoryReservation>());

        // Act
        var result = await _service.ProcessExpiredReservationsAsync();

        // Assert
        result.IsSuccessful.Should().BeTrue();
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidateContractInventoryRequirements_WithSufficientInventory_ShouldPassValidation()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var productCode = "BRENT";
        var quantity = new Quantity(1000m, QuantityUnit.MT);

        var purchaseContract = CreatePurchaseContract(contractId, productCode, quantity);

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdAsync(contractId))
            .ReturnsAsync(purchaseContract);

        var inventorySnapshot = CreateInventorySnapshot(productCode, availableQuantity: 2000m);
        _mockInventoryService
            .Setup(s => s.GetRealTimeInventoryAsync(productCode, It.IsAny<string>()))
            .ReturnsAsync(inventorySnapshot);

        // Act
        var result = await _service.ValidateContractInventoryRequirementsAsync(contractId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateContractInventoryRequirements_WithInsufficientInventory_ShouldFailValidation()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var productCode = "BRENT";
        var quantity = new Quantity(1000m, QuantityUnit.MT);

        var purchaseContract = CreatePurchaseContract(contractId, productCode, quantity);

        _mockPurchaseContractRepository
            .Setup(r => r.GetByIdAsync(contractId))
            .ReturnsAsync(purchaseContract);

        var inventorySnapshot = CreateInventorySnapshot(productCode, availableQuantity: 500m);
        _mockInventoryService
            .Setup(s => s.GetRealTimeInventoryAsync(productCode, It.IsAny<string>()))
            .ReturnsAsync(inventorySnapshot);

        // Act
        var result = await _service.ValidateContractInventoryRequirementsAsync(contractId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.First().ErrorCode.Should().Be("INSUFFICIENT_INVENTORY");
    }

    [Fact]
    public async Task ValidateInventoryMovement_WithZeroQuantity_ShouldFailValidation()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var zeroQuantity = Quantity.Zero(QuantityUnit.MT);

        // Act
        var result = await _service.ValidateInventoryMovementAsync(contractId, zeroQuantity, "Receipt");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.First().ErrorCode.Should().Be("INVALID_QUANTITY");
    }

    [Fact]
    public async Task ValidateInventoryMovement_WithNegativeQuantity_ShouldFailValidation()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var negativeQuantity = new Quantity(-100m, QuantityUnit.MT);

        // Act
        var result = await _service.ValidateInventoryMovementAsync(contractId, negativeQuantity, "Receipt");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.First().ErrorMessage.Should().Contain("positive");
    }

    #endregion

    #region Helper Methods

    private PurchaseContract CreatePurchaseContract(Guid contractId, string productCode, Quantity quantity)
    {
        var traderId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        return new PurchaseContract(
            contractNumber: new ContractNumber($"PC-{contractId.ToString()[..8]}"),
            traderId: traderId,
            supplierId: supplierId,
            productId: productId,
            contractQuantity: quantity,
            contractPrice: Money.USD(75m),
            deliveryStart: DateTime.UtcNow.AddMonths(1),
            deliveryEnd: DateTime.UtcNow.AddMonths(2))
        {
            Id = contractId,
            Status = ContractStatus.Active,
            Product = new Product
            {
                Id = productId,
                Code = productCode,
                Name = productCode,
                Category = ProductCategory.Crude
            }
        };
    }

    private SalesContract CreateSalesContract(Guid contractId, string productCode, Quantity quantity)
    {
        var traderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        return new SalesContract(
            contractNumber: new ContractNumber($"SC-{contractId.ToString()[..8]}"),
            traderId: traderId,
            buyerId: buyerId,
            productId: productId,
            contractQuantity: quantity,
            contractPrice: Money.USD(80m),
            deliveryStart: DateTime.UtcNow.AddMonths(1),
            deliveryEnd: DateTime.UtcNow.AddMonths(2))
        {
            Id = contractId,
            Status = ContractStatus.Active,
            Product = new Product
            {
                Id = productId,
                Code = productCode,
                Name = productCode,
                Category = ProductCategory.Crude
            }
        };
    }

    private RealTimeInventorySnapshot CreateInventorySnapshot(string productCode, decimal availableQuantity)
    {
        var productId = Guid.NewGuid();
        return new RealTimeInventorySnapshot
        {
            SnapshotTime = DateTime.UtcNow,
            Positions = new List<InventoryPosition>
            {
                new InventoryPosition
                {
                    ProductId = productId,
                    ProductCode = productCode,
                    AvailableQuantity = new Quantity(availableQuantity, QuantityUnit.MT),
                    TotalQuantity = new Quantity(availableQuantity, QuantityUnit.MT),
                    ReservedQuantity = Quantity.Zero(QuantityUnit.MT)
                }
            }
        };
    }

    #endregion
}
