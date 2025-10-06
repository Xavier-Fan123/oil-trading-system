using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OilTrading.Application.Commands.FinancialReports;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Application.DTOs;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using Xunit;

namespace OilTrading.Tests.Application.Commands.FinancialReports;

public class CreateFinancialReportCommandHandlerTests : IDisposable
{
    private readonly Mock<IFinancialReportRepository> _mockFinancialReportRepository;
    private readonly Mock<ITradingPartnerRepository> _mockTradingPartnerRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CreateFinancialReportCommandHandler>> _mockLogger;
    private readonly CreateFinancialReportCommandHandler _handler;

    // Test data
    private readonly Guid _tradingPartnerId = Guid.NewGuid();
    private readonly DateTime _validStartDate = DateTime.UtcNow.AddDays(-90);
    private readonly DateTime _validEndDate = DateTime.UtcNow.AddDays(-1);
    private readonly TradingPartner _activeTradingPartner;

    public CreateFinancialReportCommandHandlerTests()
    {
        _mockFinancialReportRepository = new Mock<IFinancialReportRepository>();
        _mockTradingPartnerRepository = new Mock<ITradingPartnerRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<CreateFinancialReportCommandHandler>>();

        _handler = new CreateFinancialReportCommandHandler(
            _mockFinancialReportRepository.Object,
            _mockTradingPartnerRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);

        // Setup test trading partner
        _activeTradingPartner = new TradingPartner
        {
            CompanyName = "Test Trading Company",
            CompanyCode = "TTC",
            Type = TradingPartnerType.Supplier,
            IsActive = true
        };
        SetEntityId(_activeTradingPartner, _tradingPartnerId);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateFinancialReportSuccessfully()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupSuccessfulScenario();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TradingPartnerId.Should().Be(_tradingPartnerId);
        result.ReportStartDate.Should().Be(command.ReportStartDate);
        result.ReportEndDate.Should().Be(command.ReportEndDate);
        result.TotalAssets.Should().Be(command.TotalAssets);
        result.CreatedBy.Should().Be(command.CreatedBy);

        // Verify repository calls
        _mockFinancialReportRepository.Verify(x => x.AddAsync(
            It.Is<FinancialReport>(r => 
                r.TradingPartnerId == _tradingPartnerId &&
                r.ReportStartDate == command.ReportStartDate &&
                r.ReportEndDate == command.ReportEndDate &&
                r.TotalAssets == command.TotalAssets),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCompleteFinancialData_ShouldSetAllFinancialFields()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = 10000m;
        command.TotalLiabilities = 6000m;
        command.NetAssets = 4000m;
        command.CurrentAssets = 5000m;
        command.CurrentLiabilities = 3000m;
        command.Revenue = 20000m;
        command.NetProfit = 2000m;
        command.OperatingCashFlow = 2500m;
        
        SetupSuccessfulScenario();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalAssets.Should().Be(10000m);
        result.TotalLiabilities.Should().Be(6000m);
        result.NetAssets.Should().Be(4000m);
        result.CurrentAssets.Should().Be(5000m);
        result.CurrentLiabilities.Should().Be(3000m);
        result.Revenue.Should().Be(20000m);
        result.NetProfit.Should().Be(2000m);
        result.OperatingCashFlow.Should().Be(2500m);
        
        // Computed properties should be calculated
        result.CurrentRatio.Should().BeApproximately(1.67m, 0.01m);
        result.DebtToAssetRatio.Should().Be(0.6m);
        result.ROE.Should().Be(0.5m);
        result.ROA.Should().Be(0.2m);
    }

    [Fact]
    public async Task Handle_WithPartialFinancialData_ShouldAllowNullValues()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = 5000m;
        command.TotalLiabilities = null;
        command.NetAssets = null;
        command.CurrentAssets = 3000m;
        command.CurrentLiabilities = null;
        
        SetupSuccessfulScenario();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalAssets.Should().Be(5000m);
        result.TotalLiabilities.Should().BeNull();
        result.NetAssets.Should().BeNull();
        result.CurrentAssets.Should().Be(3000m);
        result.CurrentLiabilities.Should().BeNull();
        
        // Computed properties that require null values should be null
        result.CurrentRatio.Should().BeNull();
        result.DebtToAssetRatio.Should().BeNull();
        result.ROE.Should().BeNull();
        result.ROA.Should().NotBeNull(); // Can calculate with available data
    }

    [Fact]
    public async Task Handle_WithAuditFields_ShouldSetAuditInformation()
    {
        // Arrange
        var command = CreateValidCommand();
        command.CreatedBy = "test.user@company.com";
        
        SetupSuccessfulScenario();
        var beforeCreation = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CreatedBy.Should().Be("test.user@company.com");
        result.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        result.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
        
        // Verify entity was created with correct audit info
        _mockFinancialReportRepository.Verify(x => x.AddAsync(
            It.Is<FinancialReport>(r => 
                r.CreatedBy == "test.user@company.com" &&
                r.CreatedAt >= beforeCreation &&
                r.CreatedAt <= DateTime.UtcNow),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Error Cases

    [Fact]
    public async Task Handle_WithNonExistentTradingPartner_ShouldThrowNotFoundException()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradingPartner?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain($"TradingPartner");
        exception.Message.Should().Contain(_tradingPartnerId.ToString());
    }

    [Fact]
    public async Task Handle_WithInactiveTradingPartner_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var command = CreateValidCommand();
        var inactiveTradingPartner = new TradingPartner
        {
            CompanyName = "Inactive Company",
            CompanyCode = "IC",
            Type = TradingPartnerType.Supplier,
            IsActive = false
        };
        SetEntityId(inactiveTradingPartner, _tradingPartnerId);
        
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveTradingPartner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.ErrorCode.Should().Be("INACTIVE_TRADING_PARTNER");
        exception.Message.Should().Contain("inactive trading partner");
        exception.Message.Should().Contain("Inactive Company");
    }

    [Fact]
    public async Task Handle_WithOverlappingReportPeriod_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var command = CreateValidCommand();
        
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_activeTradingPartner);

        _mockFinancialReportRepository
            .Setup(x => x.HasOverlappingReportAsync(
                _tradingPartnerId,
                command.ReportStartDate,
                command.ReportEndDate,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.ErrorCode.Should().Be("OVERLAPPING_REPORT_PERIOD");
        exception.Message.Should().Contain("overlaps with the specified period");
        exception.Message.Should().Contain(_activeTradingPartner.CompanyName);
    }

    [Fact]
    public async Task Handle_WithDomainValidationError_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var command = CreateValidCommand();
        command.ReportStartDate = DateTime.UtcNow.AddDays(-1);
        command.ReportEndDate = DateTime.UtcNow.AddDays(-2); // Invalid: start after end
        
        SetupSuccessfulScenario();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.ErrorCode.Should().Be("DOMAIN_VALIDATION_ERROR");
        exception.Message.Should().Contain("start date must be before end date");
    }

    [Fact]
    public async Task Handle_WithDatabaseSaveFailure_ShouldThrowOriginalException()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupSuccessfulScenario();
        
        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain("Database connection failed");
    }

    #endregion

    #region Business Logic Edge Cases

    [Theory]
    [InlineData(-100)] // Negative assets
    [InlineData(-50)] // Negative current assets
    public async Task Handle_WithNegativeAssets_ShouldThrowBusinessRuleException(decimal negativeValue)
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = negativeValue == -100 ? negativeValue : 1000;
        command.CurrentAssets = negativeValue == -50 ? negativeValue : 500;
        
        SetupSuccessfulScenario();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.ErrorCode.Should().Be("DOMAIN_VALIDATION_ERROR");
        exception.Message.Should().Contain("cannot be negative");
    }

    [Fact]
    public async Task Handle_WithCurrentAssetsExceedingTotal_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalAssets = 1000;
        command.CurrentAssets = 1500; // Exceeds total assets
        
        SetupSuccessfulScenario();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.ErrorCode.Should().Be("DOMAIN_VALIDATION_ERROR");
        exception.Message.Should().Contain("Current assets cannot exceed total assets");
    }

    [Fact]
    public async Task Handle_WithCurrentLiabilitiesExceedingTotal_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var command = CreateValidCommand();
        command.TotalLiabilities = 500;
        command.CurrentLiabilities = 800; // Exceeds total liabilities
        
        SetupSuccessfulScenario();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.ErrorCode.Should().Be("DOMAIN_VALIDATION_ERROR");
        exception.Message.Should().Contain("Current liabilities cannot exceed total liabilities");
    }

    #endregion

    #region Logging Verification

    [Fact]
    public async Task Handle_SuccessfulCreation_ShouldLogAppropriateMessages()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupSuccessfulScenario();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Creating financial report")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Financial report created successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DomainValidationError_ShouldLogWarning()
    {
        // Arrange
        var command = CreateValidCommand();
        command.ReportStartDate = DateTime.UtcNow.AddDays(-1);
        command.ReportEndDate = DateTime.UtcNow.AddDays(-2);
        SetupSuccessfulScenario();

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Domain validation failed")),
                It.IsAny<DomainException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UnexpectedError_ShouldLogError()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupSuccessfulScenario();
        
        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error occurred")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Repository Integration Tests

    [Fact]
    public async Task Handle_ShouldCallRepositoriesInCorrectOrder()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupSuccessfulScenario();
        var callOrder = new List<string>();

        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("GetTradingPartner"))
            .ReturnsAsync(_activeTradingPartner);

        _mockFinancialReportRepository
            .Setup(x => x.HasOverlappingReportAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("CheckOverlapping"))
            .ReturnsAsync(false);

        _mockFinancialReportRepository
            .Setup(x => x.AddAsync(It.IsAny<FinancialReport>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddReport"))
            .ReturnsAsync((FinancialReport r, CancellationToken ct) => r);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChanges"))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        callOrder.Should().Equal("GetTradingPartner", "CheckOverlapping", "AddReport", "SaveChanges");
    }

    #endregion

    #region Helper Methods

    private CreateFinancialReportCommand CreateValidCommand()
    {
        return new CreateFinancialReportCommand
        {
            TradingPartnerId = _tradingPartnerId,
            ReportStartDate = _validStartDate,
            ReportEndDate = _validEndDate,
            TotalAssets = 5000m,
            TotalLiabilities = 3000m,
            NetAssets = 2000m,
            CurrentAssets = 3000m,
            CurrentLiabilities = 1500m,
            Revenue = 10000m,
            NetProfit = 1000m,
            OperatingCashFlow = 1200m,
            CreatedBy = "test.user"
        };
    }

    private void SetupSuccessfulScenario()
    {
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_activeTradingPartner);

        _mockFinancialReportRepository
            .Setup(x => x.HasOverlappingReportAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockFinancialReportRepository
            .Setup(x => x.AddAsync(It.IsAny<FinancialReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinancialReport r, CancellationToken ct) => r);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private static void SetEntityId(BaseEntity entity, Guid id)
    {
        var idProperty = typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id));
        idProperty?.SetValue(entity, id);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #endregion
}