using FluentAssertions;
using Moq;
using OilTrading.Application.Commands.PaperContracts;
using OilTrading.Application.DTOs;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using OilTrading.Core.Repositories;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OilTrading.UnitTests.Application.PaperContracts;

/// <summary>
/// Unit tests for Paper Contract command handlers
/// Tests: CreatePaperContractCommand, UpdateMTMCommand, ClosePositionCommand
/// Tests hedge designation functionality for Data Lineage Enhancement v2.18.0
/// </summary>
public class PaperContractCommandHandlerTests
{
    private readonly Mock<IPaperContractRepository> _mockPaperContractRepository;
    private readonly Mock<IMarketDataRepository> _mockMarketDataRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CreatePaperContractCommandHandler>> _mockCreateLogger;
    private readonly Mock<ILogger<UpdateMTMCommandHandler>> _mockMTMLogger;
    private readonly Mock<ILogger<ClosePositionCommandHandler>> _mockCloseLogger;
    private const string TestUser = "test-user";

    public PaperContractCommandHandlerTests()
    {
        _mockPaperContractRepository = new Mock<IPaperContractRepository>();
        _mockMarketDataRepository = new Mock<IMarketDataRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCreateLogger = new Mock<ILogger<CreatePaperContractCommandHandler>>();
        _mockMTMLogger = new Mock<ILogger<UpdateMTMCommandHandler>>();
        _mockCloseLogger = new Mock<ILogger<ClosePositionCommandHandler>>();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    #region CreatePaperContractCommand Tests

    [Fact]
    public async Task Handle_CreatePaperContractCommand_WithValidData_ShouldCreateContract()
    {
        // Arrange
        var command = new CreatePaperContractCommand
        {
            ContractMonth = "JAN25",
            ProductType = "Brent",
            Position = "Long",
            Quantity = 10,
            LotSize = 1000,
            EntryPrice = 75.50m,
            TradeDate = DateTime.UtcNow.AddDays(-1),
            TradeReference = "TR-001",
            CounterpartyName = "Test Counterparty",
            Notes = "Test paper contract",
            CreatedBy = TestUser
        };

        PaperContract? capturedContract = null;
        _mockPaperContractRepository
            .Setup(r => r.AddAsync(It.IsAny<PaperContract>(), It.IsAny<CancellationToken>()))
            .Callback<PaperContract, CancellationToken>((c, ct) => capturedContract = c)
            .ReturnsAsync((PaperContract c, CancellationToken ct) => c);

        var handler = new CreatePaperContractCommandHandler(
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockCreateLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ContractMonth.Should().Be("JAN25");
        result.ProductType.Should().Be("Brent");
        result.Position.Should().Be("Long");
        result.Quantity.Should().Be(10);
        result.LotSize.Should().Be(1000);
        result.EntryPrice.Should().Be(75.50m);
        result.Status.Should().Be("Open");
        _mockPaperContractRepository.Verify(r => r.AddAsync(It.IsAny<PaperContract>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreatePaperContractCommand_ShortPosition_ShouldSetCorrectPositionType()
    {
        // Arrange
        var command = new CreatePaperContractCommand
        {
            ContractMonth = "FEB25",
            ProductType = "380cst",
            Position = "Short",
            Quantity = 5,
            LotSize = 1000,
            EntryPrice = 450m,
            TradeDate = DateTime.UtcNow,
            CreatedBy = TestUser
        };

        _mockPaperContractRepository
            .Setup(r => r.AddAsync(It.IsAny<PaperContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaperContract c, CancellationToken ct) => c);

        var handler = new CreatePaperContractCommandHandler(
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockCreateLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Position.Should().Be("Short");
    }

    [Fact]
    public async Task Handle_CreatePaperContractCommand_InvalidPositionType_ShouldThrowException()
    {
        // Arrange
        var command = new CreatePaperContractCommand
        {
            ContractMonth = "MAR25",
            ProductType = "Brent",
            Position = "InvalidPosition",
            Quantity = 10,
            LotSize = 1000,
            EntryPrice = 75m,
            TradeDate = DateTime.UtcNow,
            CreatedBy = TestUser
        };

        var handler = new CreatePaperContractCommandHandler(
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockCreateLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("Invalid position type");
    }

    [Theory]
    [InlineData("Brent", "JAN25")]
    [InlineData("380cst", "FEB25")]
    [InlineData("0.5%", "MAR25")]
    [InlineData("Gasoil", "APR25")]
    public async Task Handle_CreatePaperContractCommand_DifferentProducts_ShouldCreateSuccessfully(
        string productType,
        string contractMonth)
    {
        // Arrange
        var command = new CreatePaperContractCommand
        {
            ContractMonth = contractMonth,
            ProductType = productType,
            Position = "Long",
            Quantity = 10,
            LotSize = 1000,
            EntryPrice = 100m,
            TradeDate = DateTime.UtcNow,
            CreatedBy = TestUser
        };

        _mockPaperContractRepository
            .Setup(r => r.AddAsync(It.IsAny<PaperContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaperContract c, CancellationToken ct) => c);

        var handler = new CreatePaperContractCommandHandler(
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockCreateLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ProductType.Should().Be(productType);
        result.ContractMonth.Should().Be(contractMonth);
    }

    #endregion

    #region ClosePositionCommand Tests

    [Fact]
    public async Task Handle_ClosePositionCommand_WithValidData_ShouldClosePosition()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        var closingPrice = 80m;
        var closeDate = DateTime.UtcNow;

        var command = new ClosePositionCommand
        {
            ContractId = contract.Id,
            ClosingPrice = closingPrice,
            CloseDate = closeDate,
            ClosedBy = TestUser
        };

        _mockPaperContractRepository
            .Setup(r => r.GetByIdAsync(contract.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        _mockPaperContractRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PaperContract>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ClosePositionCommandHandler(
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockCloseLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Closed");
        result.CurrentPrice.Should().Be(closingPrice);
        result.SettlementDate.Should().Be(closeDate);
        result.RealizedPnL.Should().NotBeNull();
        // Long position: (80 - 75) * 10 * 1000 = 50,000
        result.RealizedPnL!.Value.Should().Be(50000m);
        _mockPaperContractRepository.Verify(r => r.UpdateAsync(contract, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ClosePositionCommand_ShortPosition_ShouldCalculateNegativePnL()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Short, 75m, 10);
        var closingPrice = 80m; // Price went up, short loses
        var closeDate = DateTime.UtcNow;

        var command = new ClosePositionCommand
        {
            ContractId = contract.Id,
            ClosingPrice = closingPrice,
            CloseDate = closeDate,
            ClosedBy = TestUser
        };

        _mockPaperContractRepository
            .Setup(r => r.GetByIdAsync(contract.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        _mockPaperContractRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PaperContract>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ClosePositionCommandHandler(
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockCloseLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.RealizedPnL.Should().NotBeNull();
        // Short position: (80 - 75) * 10 * 1000 * -1 = -50,000
        result.RealizedPnL!.Value.Should().Be(-50000m);
    }

    [Fact]
    public async Task Handle_ClosePositionCommand_ContractNotFound_ShouldReturnNull()
    {
        // Arrange
        var command = new ClosePositionCommand
        {
            ContractId = Guid.NewGuid(),
            ClosingPrice = 80m,
            CloseDate = DateTime.UtcNow,
            ClosedBy = TestUser
        };

        _mockPaperContractRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaperContract?)null);

        var handler = new ClosePositionCommandHandler(
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockCloseLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ClosePositionCommand_AlreadyClosed_ShouldThrowException()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.ClosePosition(80m, DateTime.UtcNow); // Already closed

        var command = new ClosePositionCommand
        {
            ContractId = contract.Id,
            ClosingPrice = 85m,
            CloseDate = DateTime.UtcNow,
            ClosedBy = TestUser
        };

        _mockPaperContractRepository
            .Setup(r => r.GetByIdAsync(contract.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        var handler = new ClosePositionCommandHandler(
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockCloseLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    #endregion

    #region PaperContract Entity - Hedge Designation Tests

    [Fact]
    public void PaperContract_DesignateAsHedge_ShouldSetHedgeProperties()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        var hedgedContractId = Guid.NewGuid();

        // Act
        contract.DesignateAsHedge(hedgedContractId, HedgedContractType.Purchase, 1.0m, TestUser);

        // Assert
        contract.IsDesignatedHedge.Should().BeTrue();
        contract.HedgedContractId.Should().Be(hedgedContractId);
        contract.HedgedContractType.Should().Be(HedgedContractType.Purchase);
        contract.HedgeRatio.Should().Be(1.0m);
        contract.HedgeDesignationDate.Should().NotBeNull();
        contract.HedgeDesignationDate!.Value.Date.Should().Be(DateTime.UtcNow.Date);
    }

    [Fact]
    public void PaperContract_DesignateAsHedge_WithSalesContract_ShouldSetCorrectType()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Short, 75m, 10);
        var hedgedContractId = Guid.NewGuid();

        // Act
        contract.DesignateAsHedge(hedgedContractId, HedgedContractType.Sales, 1.0m, TestUser);

        // Assert
        contract.HedgedContractType.Should().Be(HedgedContractType.Sales);
    }

    [Fact]
    public void PaperContract_DesignateAsHedge_WithCustomRatio_ShouldSetRatio()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        var hedgedContractId = Guid.NewGuid();

        // Act
        contract.DesignateAsHedge(hedgedContractId, HedgedContractType.Purchase, 0.5m, TestUser);

        // Assert
        contract.HedgeRatio.Should().Be(0.5m);
    }

    [Fact]
    public void PaperContract_DesignateAsHedge_WhenClosed_ShouldThrowException()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.ClosePosition(80m, DateTime.UtcNow);

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            contract.DesignateAsHedge(Guid.NewGuid(), HedgedContractType.Purchase, 1.0m, TestUser));
    }

    [Fact]
    public void PaperContract_DesignateAsHedge_WithInvalidRatio_ShouldThrowException()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            contract.DesignateAsHedge(Guid.NewGuid(), HedgedContractType.Purchase, 0m, TestUser));
        Assert.Throws<DomainException>(() =>
            contract.DesignateAsHedge(Guid.NewGuid(), HedgedContractType.Purchase, 15m, TestUser));
    }

    [Fact]
    public void PaperContract_RemoveHedgeDesignation_ShouldClearHedgeProperties()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.DesignateAsHedge(Guid.NewGuid(), HedgedContractType.Purchase, 1.0m, TestUser);

        // Act
        contract.RemoveHedgeDesignation("Strategy change", TestUser);

        // Assert
        contract.IsDesignatedHedge.Should().BeFalse();
        contract.HedgedContractId.Should().BeNull();
        contract.HedgedContractType.Should().BeNull();
        contract.HedgeDesignationDate.Should().BeNull();
        contract.HedgeEffectiveness.Should().BeNull();
        contract.Notes.Should().Contain("Strategy change");
    }

    [Fact]
    public void PaperContract_RemoveHedgeDesignation_WhenNotDesignated_ShouldThrowException()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            contract.RemoveHedgeDesignation("Some reason", TestUser));
    }

    [Fact]
    public void PaperContract_RemoveHedgeDesignation_WithoutReason_ShouldThrowException()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.DesignateAsHedge(Guid.NewGuid(), HedgedContractType.Purchase, 1.0m, TestUser);

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            contract.RemoveHedgeDesignation("", TestUser));
    }

    [Fact]
    public void PaperContract_UpdateHedgeEffectiveness_ShouldSetValue()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.DesignateAsHedge(Guid.NewGuid(), HedgedContractType.Purchase, 1.0m, TestUser);

        // Act
        contract.UpdateHedgeEffectiveness(85.5m, TestUser);

        // Assert
        contract.HedgeEffectiveness.Should().Be(85.5m);
    }

    [Fact]
    public void PaperContract_UpdateHedgeEffectiveness_WhenNotDesignated_ShouldThrowException()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            contract.UpdateHedgeEffectiveness(90m, TestUser));
    }

    [Fact]
    public void PaperContract_UpdateHedgeEffectiveness_InvalidValue_ShouldThrowException()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.DesignateAsHedge(Guid.NewGuid(), HedgedContractType.Purchase, 1.0m, TestUser);

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            contract.UpdateHedgeEffectiveness(-10m, TestUser));
        Assert.Throws<DomainException>(() =>
            contract.UpdateHedgeEffectiveness(150m, TestUser));
    }

    [Fact]
    public void PaperContract_UpdateHedgeRatio_ShouldUpdateValue()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.DesignateAsHedge(Guid.NewGuid(), HedgedContractType.Purchase, 1.0m, TestUser);

        // Act
        contract.UpdateHedgeRatio(1.5m, TestUser);

        // Assert
        contract.HedgeRatio.Should().Be(1.5m);
    }

    [Fact]
    public void PaperContract_GetHedgedQuantity_ShouldCalculateCorrectly()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.DesignateAsHedge(Guid.NewGuid(), HedgedContractType.Purchase, 0.5m, TestUser);

        // Act
        var hedgedQuantity = contract.GetHedgedQuantity();

        // Assert
        // Quantity * LotSize * HedgeRatio = 10 * 1000 * 0.5 = 5000
        hedgedQuantity.Should().Be(5000m);
    }

    [Fact]
    public void PaperContract_GetHedgedQuantity_WhenNotDesignated_ShouldReturnZero()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);

        // Act
        var hedgedQuantity = contract.GetHedgedQuantity();

        // Assert
        hedgedQuantity.Should().Be(0);
    }

    [Fact]
    public void PaperContract_CanBeDesignatedAsHedge_OpenAndNotDesignated_ShouldReturnTrue()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);

        // Act
        var canBeDesignated = contract.CanBeDesignatedAsHedge();

        // Assert
        canBeDesignated.Should().BeTrue();
    }

    [Fact]
    public void PaperContract_CanBeDesignatedAsHedge_AlreadyDesignated_ShouldReturnFalse()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.DesignateAsHedge(Guid.NewGuid(), HedgedContractType.Purchase, 1.0m, TestUser);

        // Act
        var canBeDesignated = contract.CanBeDesignatedAsHedge();

        // Assert
        canBeDesignated.Should().BeFalse();
    }

    [Fact]
    public void PaperContract_CanBeDesignatedAsHedge_WhenClosed_ShouldReturnFalse()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.ClosePosition(80m, DateTime.UtcNow);

        // Act
        var canBeDesignated = contract.CanBeDesignatedAsHedge();

        // Assert
        canBeDesignated.Should().BeFalse();
    }

    #endregion

    #region PaperContract Entity - P&L Calculation Tests

    [Fact]
    public void PaperContract_UpdateMTM_ShouldCalculateUnrealizedPnL()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);

        // Act
        contract.UpdateMTM(80m, DateTime.UtcNow);

        // Assert
        contract.CurrentPrice.Should().Be(80m);
        contract.LastMTMDate.Should().NotBeNull();
        contract.UnrealizedPnL.Should().NotBeNull();
        // Long: (80 - 75) * 10 * 1000 = 50,000
        contract.UnrealizedPnL!.Value.Should().Be(50000m);
    }

    [Fact]
    public void PaperContract_UpdateMTM_ShortPosition_ShouldCalculateCorrectly()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Short, 75m, 10);

        // Act
        contract.UpdateMTM(70m, DateTime.UtcNow); // Price went down, short profits

        // Assert
        contract.UnrealizedPnL.Should().NotBeNull();
        // Short: (70 - 75) * 10 * 1000 * -1 = 50,000
        contract.UnrealizedPnL!.Value.Should().Be(50000m);
    }

    [Fact]
    public void PaperContract_GetUnrealizedPnL_WhenOpen_ShouldCalculate()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.CurrentPrice = 80m;

        // Act
        var pnl = contract.GetUnrealizedPnL();

        // Assert
        pnl.Should().Be(50000m);
    }

    [Fact]
    public void PaperContract_GetUnrealizedPnL_WhenClosed_ShouldReturnZero()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.ClosePosition(80m, DateTime.UtcNow);

        // Act
        var pnl = contract.GetUnrealizedPnL();

        // Assert
        pnl.Should().Be(0);
    }

    [Fact]
    public void PaperContract_GetTotalValue_ShouldCalculateCorrectly()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.CurrentPrice = 80m;

        // Act
        var value = contract.GetTotalValue();

        // Assert
        // abs(10 * 1000 * 80) = 800,000
        value.Should().Be(800000m);
    }

    #endregion

    #region PaperContract Entity - Trade Group Tests

    [Fact]
    public void PaperContract_AssignToTradeGroup_ShouldSetTradeGroupId()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        var tradeGroupId = Guid.NewGuid();

        // Act
        contract.AssignToTradeGroup(tradeGroupId, TestUser);

        // Assert
        contract.TradeGroupId.Should().Be(tradeGroupId);
    }

    [Fact]
    public void PaperContract_AssignToTradeGroup_WhenClosed_ShouldThrowException()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.ClosePosition(80m, DateTime.UtcNow);

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            contract.AssignToTradeGroup(Guid.NewGuid(), TestUser));
    }

    [Fact]
    public void PaperContract_RemoveFromTradeGroup_ShouldClearTradeGroupId()
    {
        // Arrange
        var contract = CreateTestPaperContract(PositionType.Long, 75m, 10);
        contract.AssignToTradeGroup(Guid.NewGuid(), TestUser);

        // Act
        contract.RemoveFromTradeGroup(TestUser);

        // Assert
        contract.TradeGroupId.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private PaperContract CreateTestPaperContract(PositionType position, decimal entryPrice, decimal quantity)
    {
        var contract = new PaperContract
        {
            ContractNumber = $"PC-TEST-{Guid.NewGuid().ToString()[..8]}",
            ContractMonth = "JAN25",
            ProductType = "Brent",
            Position = position,
            Quantity = quantity,
            LotSize = 1000,
            EntryPrice = entryPrice,
            TradeDate = DateTime.UtcNow.AddDays(-5),
            Status = PaperContractStatus.Open
        };
        contract.SetId(Guid.NewGuid());
        return contract;
    }

    #endregion
}
