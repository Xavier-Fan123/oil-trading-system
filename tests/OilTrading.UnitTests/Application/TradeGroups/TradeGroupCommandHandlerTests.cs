using FluentAssertions;
using Moq;
using OilTrading.Application.Commands.TradeGroups;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OilTrading.UnitTests.Application.TradeGroups;

/// <summary>
/// Unit tests for Trade Group command handlers
/// Tests: CreateTradeGroupCommand, CloseTradeGroupCommand, AssignPaperContractToTradeGroupCommand
/// Created for Data Lineage Enhancement v2.18.0 - Trade Group risk aggregation testing
/// </summary>
public class TradeGroupCommandHandlerTests
{
    private readonly Mock<ITradeGroupRepository> _mockTradeGroupRepository;
    private readonly Mock<IPaperContractRepository> _mockPaperContractRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CreateTradeGroupCommandHandler>> _mockCreateLogger;
    private readonly Mock<ILogger<CloseTradeGroupCommandHandler>> _mockCloseLogger;
    private readonly Mock<ILogger<AssignPaperContractToTradeGroupCommandHandler>> _mockAssignLogger;
    private const string TestUser = "test-user";

    public TradeGroupCommandHandlerTests()
    {
        _mockTradeGroupRepository = new Mock<ITradeGroupRepository>();
        _mockPaperContractRepository = new Mock<IPaperContractRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCreateLogger = new Mock<ILogger<CreateTradeGroupCommandHandler>>();
        _mockCloseLogger = new Mock<ILogger<CloseTradeGroupCommandHandler>>();
        _mockAssignLogger = new Mock<ILogger<AssignPaperContractToTradeGroupCommandHandler>>();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    #region CreateTradeGroupCommand Tests

    [Fact]
    public async Task Handle_CreateTradeGroupCommand_WithValidData_ShouldCreateGroup()
    {
        // Arrange
        var command = new CreateTradeGroupCommand
        {
            GroupName = "Calendar Spread Q1-Q2",
            StrategyType = (int)StrategyType.CalendarSpread,
            Description = "Spread between Q1 and Q2 Brent futures",
            CreatedBy = TestUser
        };

        _mockTradeGroupRepository
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeGroup?)null);

        _mockTradeGroupRepository
            .Setup(r => r.AddAsync(It.IsAny<TradeGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeGroup t, CancellationToken ct) => t);

        var handler = new CreateTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockUnitOfWork.Object,
            _mockCreateLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _mockTradeGroupRepository.Verify(r => r.AddAsync(It.IsAny<TradeGroup>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreateTradeGroupCommand_WithRiskParameters_ShouldSetRiskParameters()
    {
        // Arrange
        var command = new CreateTradeGroupCommand
        {
            GroupName = "High Risk Arbitrage",
            StrategyType = (int)StrategyType.Arbitrage,
            ExpectedRiskLevel = (int)RiskLevel.High,
            MaxAllowedLoss = 100000m,
            TargetProfit = 50000m,
            CreatedBy = TestUser
        };

        TradeGroup? capturedGroup = null;
        _mockTradeGroupRepository
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeGroup?)null);

        _mockTradeGroupRepository
            .Setup(r => r.AddAsync(It.IsAny<TradeGroup>(), It.IsAny<CancellationToken>()))
            .Callback<TradeGroup, CancellationToken>((g, ct) => capturedGroup = g)
            .ReturnsAsync((TradeGroup t, CancellationToken ct) => t);

        var handler = new CreateTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockUnitOfWork.Object,
            _mockCreateLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        capturedGroup.Should().NotBeNull();
        capturedGroup!.ExpectedRiskLevel.Should().Be(RiskLevel.High);
        capturedGroup.MaxAllowedLoss.Should().Be(100000m);
        capturedGroup.TargetProfit.Should().Be(50000m);
    }

    [Fact]
    public async Task Handle_CreateTradeGroupCommand_DuplicateName_ShouldThrowException()
    {
        // Arrange
        var existingGroup = new TradeGroup("Existing Group", StrategyType.Directional, "Existing", TestUser);
        var command = new CreateTradeGroupCommand
        {
            GroupName = "Existing Group",
            StrategyType = (int)StrategyType.CalendarSpread,
            CreatedBy = TestUser
        };

        _mockTradeGroupRepository
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        var handler = new CreateTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockUnitOfWork.Object,
            _mockCreateLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Theory]
    [InlineData(StrategyType.Directional, "Directional position")]
    [InlineData(StrategyType.CalendarSpread, "Calendar spread strategy")]
    [InlineData(StrategyType.IntercommoditySpread, "Intercommodity spread")]
    [InlineData(StrategyType.BasisHedge, "Basis hedge")]
    [InlineData(StrategyType.CrackSpread, "Crack spread")]
    public async Task Handle_CreateTradeGroupCommand_AllStrategyTypes_ShouldCreateSuccessfully(
        StrategyType strategyType,
        string description)
    {
        // Arrange
        var command = new CreateTradeGroupCommand
        {
            GroupName = $"Test {strategyType} Group",
            StrategyType = (int)strategyType,
            Description = description,
            CreatedBy = TestUser
        };

        _mockTradeGroupRepository
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeGroup?)null);

        _mockTradeGroupRepository
            .Setup(r => r.AddAsync(It.IsAny<TradeGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeGroup t, CancellationToken ct) => t);

        var handler = new CreateTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockUnitOfWork.Object,
            _mockCreateLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _mockTradeGroupRepository.Verify(r => r.AddAsync(It.Is<TradeGroup>(g =>
            g.StrategyType == strategyType), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CloseTradeGroupCommand Tests

    [Fact]
    public async Task Handle_CloseTradeGroupCommand_NoOpenPositions_ShouldCloseSuccessfully()
    {
        // Arrange
        var tradeGroup = new TradeGroup("Test Close Group", StrategyType.CalendarSpread, "Test", TestUser);
        var command = new CloseTradeGroupCommand
        {
            TradeGroupId = tradeGroup.Id,
            ClosedBy = TestUser
        };

        _mockTradeGroupRepository
            .Setup(r => r.GetWithContractsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tradeGroup);

        _mockTradeGroupRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TradeGroup>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CloseTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockUnitOfWork.Object,
            _mockCloseLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        tradeGroup.Status.Should().Be(TradeGroupStatus.Closed);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CloseTradeGroupCommand_GroupNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var command = new CloseTradeGroupCommand
        {
            TradeGroupId = Guid.NewGuid(),
            ClosedBy = TestUser
        };

        _mockTradeGroupRepository
            .Setup(r => r.GetWithContractsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeGroup?)null);

        var handler = new CloseTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockUnitOfWork.Object,
            _mockCloseLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CloseTradeGroupCommand_WithOpenPositions_ShouldThrowDomainException()
    {
        // Arrange
        var tradeGroup = new TradeGroup("Test Group With Positions", StrategyType.CalendarSpread, "Test", TestUser);

        // Add an open paper contract to the group
        var paperContract = CreateTestPaperContract();
        tradeGroup.PaperContracts.Add(paperContract);

        var command = new CloseTradeGroupCommand
        {
            TradeGroupId = tradeGroup.Id,
            ClosedBy = TestUser
        };

        _mockTradeGroupRepository
            .Setup(r => r.GetWithContractsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tradeGroup);

        var handler = new CloseTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockUnitOfWork.Object,
            _mockCloseLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("open positions");
    }

    [Fact]
    public async Task Handle_CloseTradeGroupCommand_AlreadyClosed_ShouldThrowDomainException()
    {
        // Arrange
        var tradeGroup = new TradeGroup("Already Closed Group", StrategyType.Directional, "Test", TestUser);
        tradeGroup.Close(TestUser); // Close it first

        var command = new CloseTradeGroupCommand
        {
            TradeGroupId = tradeGroup.Id,
            ClosedBy = TestUser
        };

        _mockTradeGroupRepository
            .Setup(r => r.GetWithContractsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tradeGroup);

        var handler = new CloseTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockUnitOfWork.Object,
            _mockCloseLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("already closed");
    }

    #endregion

    #region AssignPaperContractToTradeGroupCommand Tests

    [Fact]
    public async Task Handle_AssignPaperContractCommand_ValidData_ShouldAssignSuccessfully()
    {
        // Arrange
        var tradeGroup = new TradeGroup("Spread Strategy", StrategyType.CalendarSpread, "Test", TestUser);
        var paperContract = CreateTestPaperContract();

        var command = new AssignPaperContractToTradeGroupCommand
        {
            TradeGroupId = tradeGroup.Id,
            PaperContractId = paperContract.Id,
            AssignedBy = TestUser,
            Notes = "Leg 1 of calendar spread"
        };

        _mockTradeGroupRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tradeGroup);

        _mockPaperContractRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paperContract);

        _mockPaperContractRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PaperContract>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new AssignPaperContractToTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockAssignLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        paperContract.TradeGroupId.Should().Be(tradeGroup.Id);
        _mockPaperContractRepository.Verify(r => r.UpdateAsync(paperContract, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AssignPaperContractCommand_TradeGroupNotFound_ShouldReturnFalse()
    {
        // Arrange
        var command = new AssignPaperContractToTradeGroupCommand
        {
            TradeGroupId = Guid.NewGuid(),
            PaperContractId = Guid.NewGuid(),
            AssignedBy = TestUser
        };

        _mockTradeGroupRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeGroup?)null);

        var handler = new AssignPaperContractToTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockAssignLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AssignPaperContractCommand_PaperContractNotFound_ShouldReturnFalse()
    {
        // Arrange
        var tradeGroup = new TradeGroup("Test Group", StrategyType.CalendarSpread, "Test", TestUser);
        var command = new AssignPaperContractToTradeGroupCommand
        {
            TradeGroupId = tradeGroup.Id,
            PaperContractId = Guid.NewGuid(),
            AssignedBy = TestUser
        };

        _mockTradeGroupRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tradeGroup);

        _mockPaperContractRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaperContract?)null);

        var handler = new AssignPaperContractToTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockAssignLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AssignPaperContractCommand_AlreadyAssigned_ShouldReturnTrue()
    {
        // Arrange
        var tradeGroup = new TradeGroup("Test Group", StrategyType.CalendarSpread, "Test", TestUser);
        var paperContract = CreateTestPaperContract();
        paperContract.AssignToTradeGroup(tradeGroup.Id, TestUser); // Already assigned

        var command = new AssignPaperContractToTradeGroupCommand
        {
            TradeGroupId = tradeGroup.Id,
            PaperContractId = paperContract.Id,
            AssignedBy = TestUser
        };

        _mockTradeGroupRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tradeGroup);

        _mockPaperContractRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paperContract);

        var handler = new AssignPaperContractToTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockAssignLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockPaperContractRepository.Verify(r => r.UpdateAsync(It.IsAny<PaperContract>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AssignPaperContractCommand_ReassignToNewGroup_ShouldUpdate()
    {
        // Arrange
        var oldGroup = new TradeGroup("Old Group", StrategyType.Directional, "Old", TestUser);
        var newGroup = new TradeGroup("New Group", StrategyType.CalendarSpread, "New", TestUser);
        var paperContract = CreateTestPaperContract();
        paperContract.AssignToTradeGroup(oldGroup.Id, TestUser); // Assigned to old group

        var command = new AssignPaperContractToTradeGroupCommand
        {
            TradeGroupId = newGroup.Id,
            PaperContractId = paperContract.Id,
            AssignedBy = TestUser
        };

        _mockTradeGroupRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newGroup);

        _mockPaperContractRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paperContract);

        _mockPaperContractRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PaperContract>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new AssignPaperContractToTradeGroupCommandHandler(
            _mockTradeGroupRepository.Object,
            _mockPaperContractRepository.Object,
            _mockUnitOfWork.Object,
            _mockAssignLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        paperContract.TradeGroupId.Should().Be(newGroup.Id);
        _mockPaperContractRepository.Verify(r => r.UpdateAsync(paperContract, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region TradeGroup Entity Tests

    [Fact]
    public void TradeGroup_UpdateInfo_ShouldUpdateProperties()
    {
        // Arrange
        var tradeGroup = new TradeGroup("Original Name", StrategyType.Directional, "Original Description", TestUser);

        // Act
        tradeGroup.UpdateInfo("Updated Name", "Updated Description", TestUser);

        // Assert
        tradeGroup.GroupName.Should().Be("Updated Name");
        tradeGroup.Description.Should().Be("Updated Description");
    }

    [Fact]
    public void TradeGroup_UpdateInfo_WhenClosed_ShouldThrowException()
    {
        // Arrange
        var tradeGroup = new TradeGroup("Test Group", StrategyType.Directional, "Test", TestUser);
        tradeGroup.Close(TestUser);

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            tradeGroup.UpdateInfo("New Name", "New Description", TestUser));
    }

    [Fact]
    public void TradeGroup_SetRiskParameters_ShouldSetAllParameters()
    {
        // Arrange
        var tradeGroup = new TradeGroup("Risk Test Group", StrategyType.BasisHedge, "Risk testing", TestUser);

        // Act
        tradeGroup.SetRiskParameters(RiskLevel.High, 50000m, 25000m, TestUser);

        // Assert
        tradeGroup.ExpectedRiskLevel.Should().Be(RiskLevel.High);
        tradeGroup.MaxAllowedLoss.Should().Be(50000m);
        tradeGroup.TargetProfit.Should().Be(25000m);
    }

    [Fact]
    public void TradeGroup_IsSpreadStrategy_ShouldReturnTrueForSpreads()
    {
        // Arrange & Act & Assert
        new TradeGroup("Calendar", StrategyType.CalendarSpread, null, TestUser).IsSpreadStrategy().Should().BeTrue();
        new TradeGroup("Intercommodity", StrategyType.IntercommoditySpread, null, TestUser).IsSpreadStrategy().Should().BeTrue();
        new TradeGroup("Location", StrategyType.LocationSpread, null, TestUser).IsSpreadStrategy().Should().BeTrue();
        new TradeGroup("Directional", StrategyType.Directional, null, TestUser).IsSpreadStrategy().Should().BeFalse();
    }

    [Fact]
    public void TradeGroup_IsHedgeStrategy_ShouldReturnTrueForHedges()
    {
        // Arrange & Act & Assert
        new TradeGroup("Basis", StrategyType.BasisHedge, null, TestUser).IsHedgeStrategy().Should().BeTrue();
        new TradeGroup("Cross", StrategyType.CrossHedge, null, TestUser).IsHedgeStrategy().Should().BeTrue();
        new TradeGroup("Calendar", StrategyType.CalendarSpread, null, TestUser).IsHedgeStrategy().Should().BeFalse();
    }

    [Fact]
    public void TradeGroup_GetNetPnL_NoPositions_ShouldReturnZero()
    {
        // Arrange
        var tradeGroup = new TradeGroup("Empty Group", StrategyType.Directional, null, TestUser);

        // Act
        var pnl = tradeGroup.GetNetPnL();

        // Assert
        pnl.Should().Be(0);
    }

    [Fact]
    public void TradeGroup_GetTotalValue_NoPositions_ShouldReturnZero()
    {
        // Arrange
        var tradeGroup = new TradeGroup("Empty Group", StrategyType.Directional, null, TestUser);

        // Act
        var value = tradeGroup.GetTotalValue();

        // Assert
        value.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private PaperContract CreateTestPaperContract()
    {
        return new PaperContract
        {
            ContractNumber = $"PC-TEST-{Guid.NewGuid().ToString()[..8]}",
            ContractMonth = "JAN25",
            ProductType = "Brent",
            Position = PositionType.Long,
            Quantity = 10,
            LotSize = 1000,
            EntryPrice = 75.50m,
            TradeDate = DateTime.UtcNow.AddDays(-5),
            Status = PaperContractStatus.Open
        };
    }

    #endregion
}
