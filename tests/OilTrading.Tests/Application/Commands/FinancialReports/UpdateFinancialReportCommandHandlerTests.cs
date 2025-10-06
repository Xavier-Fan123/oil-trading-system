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

public class UpdateFinancialReportCommandHandlerTests : IDisposable
{
    private readonly Mock<IFinancialReportRepository> _mockFinancialReportRepository;
    private readonly Mock<ITradingPartnerRepository> _mockTradingPartnerRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<UpdateFinancialReportCommandHandler>> _mockLogger;
    private readonly UpdateFinancialReportCommandHandler _handler;

    // Test data
    private readonly Guid _reportId = Guid.NewGuid();
    private readonly Guid _tradingPartnerId = Guid.NewGuid();
    private readonly DateTime _originalStartDate = DateTime.UtcNow.AddDays(-180);
    private readonly DateTime _originalEndDate = DateTime.UtcNow.AddDays(-90);
    private readonly DateTime _newStartDate = DateTime.UtcNow.AddDays(-120);
    private readonly DateTime _newEndDate = DateTime.UtcNow.AddDays(-30);
    private readonly FinancialReport _existingReport;
    private readonly TradingPartner _activeTradingPartner;

    public UpdateFinancialReportCommandHandlerTests()
    {
        _mockFinancialReportRepository = new Mock<IFinancialReportRepository>();
        _mockTradingPartnerRepository = new Mock<ITradingPartnerRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<UpdateFinancialReportCommandHandler>>();

        _handler = new UpdateFinancialReportCommandHandler(
            _mockFinancialReportRepository.Object,
            _mockTradingPartnerRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);

        // Setup test entities
        _activeTradingPartner = new TradingPartner
        {
            CompanyName = "Test Trading Company",
            CompanyCode = "TTC",
            Type = TradingPartnerType.Supplier,
            IsActive = true
        };
        SetEntityId(_activeTradingPartner, _tradingPartnerId);

        _existingReport = new FinancialReport(_tradingPartnerId, _originalStartDate, _originalEndDate);
        SetEntityId(_existingReport, _reportId);
        _existingReport.UpdateFinancialPosition(5000m, 3000m, 2000m, 3000m, 1500m);
        _existingReport.UpdatePerformanceData(10000m, 1000m, 1200m);
        _existingReport.SetCreated("original.user", DateTime.UtcNow.AddDays(-7));

        // Set up navigation property
        var tradingPartnerProperty = typeof(FinancialReport).GetProperty(nameof(FinancialReport.TradingPartner));
        tradingPartnerProperty?.SetValue(_existingReport, _activeTradingPartner);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateFinancialReportSuccessfully()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        SetupSuccessfulScenario();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(_reportId);
        result.ReportStartDate.Should().Be(command.ReportStartDate);
        result.ReportEndDate.Should().Be(command.ReportEndDate);
        result.TotalAssets.Should().Be(command.TotalAssets);
        result.UpdatedBy.Should().Be(command.UpdatedBy);

        // Verify repository calls
        _mockFinancialReportRepository.Verify(x => x.UpdateAsync(
            It.Is<FinancialReport>(r => 
                r.Id == _reportId &&
                r.ReportStartDate == command.ReportStartDate &&
                r.ReportEndDate == command.ReportEndDate &&
                r.TotalAssets == command.TotalAssets &&
                r.UpdatedBy == command.UpdatedBy),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUpdatedFinancialData_ShouldUpdateAllFinancialFields()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.TotalAssets = 15000m;
        command.TotalLiabilities = 9000m;
        command.NetAssets = 6000m;
        command.CurrentAssets = 8000m;
        command.CurrentLiabilities = 4000m;
        command.Revenue = 25000m;
        command.NetProfit = 3000m;
        command.OperatingCashFlow = 3500m;
        
        SetupSuccessfulScenario();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalAssets.Should().Be(15000m);
        result.TotalLiabilities.Should().Be(9000m);
        result.NetAssets.Should().Be(6000m);
        result.CurrentAssets.Should().Be(8000m);
        result.CurrentLiabilities.Should().Be(4000m);
        result.Revenue.Should().Be(25000m);
        result.NetProfit.Should().Be(3000m);
        result.OperatingCashFlow.Should().Be(3500m);
        
        // Computed properties should be recalculated
        result.CurrentRatio.Should().Be(2.0m);
        result.DebtToAssetRatio.Should().Be(0.6m);
        result.ROE.Should().Be(0.5m);
        result.ROA.Should().Be(0.2m);
    }

    [Fact]
    public async Task Handle_WithReportPeriodUpdate_ShouldUpdateDatesAndRecalculateProperties()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.ReportStartDate = DateTime.UtcNow.AddDays(-365); // Make it annual
        command.ReportEndDate = DateTime.UtcNow.AddDays(-1);
        
        SetupSuccessfulScenario();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ReportStartDate.Should().Be(command.ReportStartDate);
        result.ReportEndDate.Should().Be(command.ReportEndDate);
        result.ReportYear.Should().Be(command.ReportStartDate.Year);
        result.IsAnnualReport.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAuditFieldUpdate_ShouldUpdateAuditInformation()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.UpdatedBy = "new.user@company.com";
        
        SetupSuccessfulScenario();
        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UpdatedBy.Should().Be("new.user@company.com");
        result.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        result.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
        
        // Original creation fields should remain unchanged
        result.CreatedBy.Should().Be("original.user");
        result.CreatedAt.Should().Be(_existingReport.CreatedAt);
    }

    [Fact]
    public async Task Handle_WithPartialUpdate_ShouldUpdateOnlySpecifiedFields()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.TotalAssets = 7000m; // Update only this field
        command.TotalLiabilities = null; // Clear this field
        // Keep other fields the same
        
        SetupSuccessfulScenario();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalAssets.Should().Be(7000m);
        result.TotalLiabilities.Should().BeNull();
        result.Revenue.Should().Be(command.Revenue); // Should be updated
        
        // Computed ratios should be recalculated based on new values
        result.DebtToAssetRatio.Should().BeNull(); // No total liabilities
    }

    #endregion

    #region Error Cases

    [Fact]
    public async Task Handle_WithNonExistentReport_ShouldThrowNotFoundException()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        _mockFinancialReportRepository
            .Setup(x => x.GetByIdWithTradingPartnerAsync(_reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinancialReport?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain("FinancialReport");
        exception.Message.Should().Contain(_reportId.ToString());
    }

    [Fact]
    public async Task Handle_WithInactiveTradingPartner_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        var inactiveTradingPartner = new TradingPartner
        {
            CompanyName = "Inactive Company",
            CompanyCode = "IC",
            Type = TradingPartnerType.Supplier,
            IsActive = false
        };
        SetEntityId(inactiveTradingPartner, _tradingPartnerId);

        var reportWithInactivePartner = new FinancialReport(_tradingPartnerId, _originalStartDate, _originalEndDate);
        SetEntityId(reportWithInactivePartner, _reportId);
        var tradingPartnerProperty = typeof(FinancialReport).GetProperty(nameof(FinancialReport.TradingPartner));
        tradingPartnerProperty?.SetValue(reportWithInactivePartner, inactiveTradingPartner);

        _mockFinancialReportRepository
            .Setup(x => x.GetByIdWithTradingPartnerAsync(_reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportWithInactivePartner);

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
        var command = CreateValidUpdateCommand();
        
        _mockFinancialReportRepository
            .Setup(x => x.GetByIdWithTradingPartnerAsync(_reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingReport);

        _mockFinancialReportRepository
            .Setup(x => x.HasOverlappingReportAsync(
                _tradingPartnerId,
                command.ReportStartDate,
                command.ReportEndDate,
                _reportId,
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
        var command = CreateValidUpdateCommand();
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
    public async Task Handle_WithDatabaseUpdateFailure_ShouldThrowOriginalException()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        SetupSuccessfulScenario();
        
        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database update failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain("Database update failed");
    }

    #endregion

    #region Business Logic Edge Cases

    [Theory]
    [InlineData(-100)] // Negative assets
    [InlineData(-50)] // Negative current assets
    public async Task Handle_WithNegativeAssets_ShouldThrowBusinessRuleException(decimal negativeValue)
    {
        // Arrange
        var command = CreateValidUpdateCommand();
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
        var command = CreateValidUpdateCommand();
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
    public async Task Handle_ClearingAllFinancialData_ShouldAllowNullValues()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        command.TotalAssets = null;
        command.TotalLiabilities = null;
        command.NetAssets = null;
        command.CurrentAssets = null;
        command.CurrentLiabilities = null;
        command.Revenue = null;
        command.NetProfit = null;
        command.OperatingCashFlow = null;
        
        SetupSuccessfulScenario();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalAssets.Should().BeNull();
        result.TotalLiabilities.Should().BeNull();
        result.NetAssets.Should().BeNull();
        result.CurrentAssets.Should().BeNull();
        result.CurrentLiabilities.Should().BeNull();
        result.Revenue.Should().BeNull();
        result.NetProfit.Should().BeNull();
        result.OperatingCashFlow.Should().BeNull();
        
        // All computed ratios should be null
        result.CurrentRatio.Should().BeNull();
        result.DebtToAssetRatio.Should().BeNull();
        result.ROE.Should().BeNull();
        result.ROA.Should().BeNull();
    }

    #endregion

    #region Logging Verification

    [Fact]
    public async Task Handle_SuccessfulUpdate_ShouldLogAppropriateMessages()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        SetupSuccessfulScenario();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updating financial report")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Financial report") && v.ToString()!.Contains("updated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DomainValidationError_ShouldLogWarning()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Domain validation failed while updating")),
                It.IsAny<DomainException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UnexpectedError_ShouldLogError()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error occurred while updating")),
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
        var command = CreateValidUpdateCommand();
        SetupSuccessfulScenario();
        var callOrder = new List<string>();

        _mockFinancialReportRepository
            .Setup(x => x.GetByIdWithTradingPartnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("GetReport"))
            .ReturnsAsync(_existingReport);

        _mockFinancialReportRepository
            .Setup(x => x.HasOverlappingReportAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("CheckOverlapping"))
            .ReturnsAsync(false);

        _mockFinancialReportRepository
            .Setup(x => x.UpdateAsync(It.IsAny<FinancialReport>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("UpdateReport"))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChanges"))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        callOrder.Should().Equal("GetReport", "CheckOverlapping", "UpdateReport", "SaveChanges");
    }

    #endregion

    #region Helper Methods

    private UpdateFinancialReportCommand CreateValidUpdateCommand()
    {
        return new UpdateFinancialReportCommand
        {
            Id = _reportId,
            ReportStartDate = _newStartDate,
            ReportEndDate = _newEndDate,
            TotalAssets = 8000m,
            TotalLiabilities = 4500m,
            NetAssets = 3500m,
            CurrentAssets = 4000m,
            CurrentLiabilities = 2000m,
            Revenue = 15000m,
            NetProfit = 1800m,
            OperatingCashFlow = 2200m,
            UpdatedBy = "test.updater"
        };
    }

    private void SetupSuccessfulScenario()
    {
        _mockFinancialReportRepository
            .Setup(x => x.GetByIdWithTradingPartnerAsync(_reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingReport);

        _mockFinancialReportRepository
            .Setup(x => x.HasOverlappingReportAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockFinancialReportRepository
            .Setup(x => x.UpdateAsync(It.IsAny<FinancialReport>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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