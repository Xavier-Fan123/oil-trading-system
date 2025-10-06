using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OilTrading.Application.Commands.FinancialReports;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using Xunit;

namespace OilTrading.Tests.Application.Commands.FinancialReports;

public class DeleteFinancialReportCommandHandlerTests : IDisposable
{
    private readonly Mock<IFinancialReportRepository> _mockFinancialReportRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<DeleteFinancialReportCommandHandler>> _mockLogger;
    private readonly DeleteFinancialReportCommandHandler _handler;

    // Test data
    private readonly Guid _reportId = Guid.NewGuid();
    private readonly Guid _tradingPartnerId = Guid.NewGuid();
    private readonly FinancialReport _existingReport;
    private readonly TradingPartner _tradingPartner;

    public DeleteFinancialReportCommandHandlerTests()
    {
        _mockFinancialReportRepository = new Mock<IFinancialReportRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<DeleteFinancialReportCommandHandler>>();

        _handler = new DeleteFinancialReportCommandHandler(
            _mockFinancialReportRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);

        // Setup test entities
        _tradingPartner = new TradingPartner
        {
            CompanyName = "Test Trading Company",
            CompanyCode = "TTC",
            Type = TradingPartnerType.Supplier,
            IsActive = true
        };
        SetEntityId(_tradingPartner, _tradingPartnerId);

        _existingReport = new FinancialReport(
            _tradingPartnerId,
            DateTime.UtcNow.AddDays(-90),
            DateTime.UtcNow.AddDays(-1));
        SetEntityId(_existingReport, _reportId);
        
        _existingReport.UpdateFinancialPosition(5000m, 3000m, 2000m, 3000m, 1500m);
        _existingReport.UpdatePerformanceData(10000m, 1000m, 1200m);
        _existingReport.SetCreated("test.user", DateTime.UtcNow.AddDays(-7));

        // Set up navigation property
        var tradingPartnerProperty = typeof(FinancialReport).GetProperty(nameof(FinancialReport.TradingPartner));
        tradingPartnerProperty?.SetValue(_existingReport, _tradingPartner);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidReportId_ShouldDeleteReportSuccessfully()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = "Test deletion"
        };

        SetupSuccessfulDeletion();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        // Verify repository calls
        _mockFinancialReportRepository.Verify(x => x.DeleteAsync(
            It.Is<FinancialReport>(r => r.Id == _reportId),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutDeletionReason_ShouldStillDeleteSuccessfully()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = null
        };

        SetupSuccessfulDeletion();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        _mockFinancialReportRepository.Verify(x => x.DeleteAsync(
            It.IsAny<FinancialReport>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldLogDeletionReason()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = "Report data was incorrect"
        };

        SetupSuccessfulDeletion();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Report data was incorrect")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoDeletionReason_ShouldLogDefaultMessage()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = null
        };

        SetupSuccessfulDeletion();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No reason provided")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SuccessfulDeletion_ShouldLogTradingPartnerDetails()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = "Test reason"
        };

        SetupSuccessfulDeletion();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Test Trading Company") && v.ToString()!.Contains("TTC")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Error Cases

    [Fact]
    public async Task Handle_WithNonExistentReport_ShouldThrowNotFoundException()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = "Test deletion"
        };

        _mockFinancialReportRepository
            .Setup(x => x.GetByIdWithTradingPartnerAsync(_reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinancialReport?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain("FinancialReport");
        exception.Message.Should().Contain(_reportId.ToString());

        // Verify no deletion attempts were made
        _mockFinancialReportRepository.Verify(x => x.DeleteAsync(
            It.IsAny<FinancialReport>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDatabaseDeletionFailure_ShouldThrowOriginalException()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = "Test deletion"
        };

        _mockFinancialReportRepository
            .Setup(x => x.GetByIdWithTradingPartnerAsync(_reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingReport);

        _mockFinancialReportRepository
            .Setup(x => x.DeleteAsync(It.IsAny<FinancialReport>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database deletion failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain("Database deletion failed");
    }

    [Fact]
    public async Task Handle_WithSaveChangesFailure_ShouldThrowOriginalException()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = "Test deletion"
        };

        _mockFinancialReportRepository
            .Setup(x => x.GetByIdWithTradingPartnerAsync(_reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingReport);

        _mockFinancialReportRepository
            .Setup(x => x.DeleteAsync(It.IsAny<FinancialReport>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Save changes failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain("Save changes failed");
    }

    [Fact]
    public async Task Handle_WithUnexpectedException_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = "Test deletion"
        };

        _mockFinancialReportRepository
            .Setup(x => x.GetByIdWithTradingPartnerAsync(_reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingReport);

        _mockFinancialReportRepository
            .Setup(x => x.DeleteAsync(It.IsAny<FinancialReport>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain("Unexpected error");

        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred while deleting financial report")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Logging Verification

    [Fact]
    public async Task Handle_SuccessfulDeletion_ShouldLogInitiationMessage()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter@company.com",
            DeletionReason = "Test deletion"
        };

        SetupSuccessfulDeletion();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Deleting financial report") && 
                    v.ToString()!.Contains(_reportId.ToString()) &&
                    v.ToString()!.Contains("test.deleter@company.com")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SuccessfulDeletion_ShouldLogReportDetails()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = "Data accuracy concerns"
        };

        SetupSuccessfulDeletion();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Verify detailed log with trading partner and period information
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Test Trading Company") &&
                    v.ToString()!.Contains("TTC") &&
                    v.ToString()!.Contains("Data accuracy concerns")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SuccessfulDeletion_ShouldLogCompletionMessage()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = "Test deletion"
        };

        SetupSuccessfulDeletion();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("deleted successfully") &&
                    v.ToString()!.Contains(_reportId.ToString()) &&
                    v.ToString()!.Contains("Test Trading Company")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Repository Integration Tests

    [Fact]
    public async Task Handle_ShouldCallRepositoriesInCorrectOrder()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = "Test deletion"
        };

        var callOrder = new List<string>();

        _mockFinancialReportRepository
            .Setup(x => x.GetByIdWithTradingPartnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("GetReport"))
            .ReturnsAsync(_existingReport);

        _mockFinancialReportRepository
            .Setup(x => x.DeleteAsync(It.IsAny<FinancialReport>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("DeleteReport"))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChanges"))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        callOrder.Should().Equal("GetReport", "DeleteReport", "SaveChanges");
    }

    [Fact]
    public async Task Handle_ShouldPassCorrectReportToDeleteMethod()
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = "Test deletion"
        };

        SetupSuccessfulDeletion();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockFinancialReportRepository.Verify(x => x.DeleteAsync(
            It.Is<FinancialReport>(r => 
                r.Id == _reportId &&
                r.TradingPartnerId == _tradingPartnerId &&
                r.TotalAssets == 5000m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WithEmptyOrNullDeletedBy_ShouldStillProcessSuccessfully(string? deletedBy)
    {
        // Arrange
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = deletedBy!,
            DeletionReason = "Test deletion"
        };

        SetupSuccessfulDeletion();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        _mockFinancialReportRepository.Verify(x => x.DeleteAsync(
            It.IsAny<FinancialReport>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithLongDeletionReason_ShouldHandleCorrectly()
    {
        // Arrange
        var longReason = new string('A', 1000); // Very long deletion reason
        var command = new DeleteFinancialReportCommand
        {
            Id = _reportId,
            DeletedBy = "test.deleter",
            DeletionReason = longReason
        };

        SetupSuccessfulDeletion();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(longReason)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulDeletion()
    {
        _mockFinancialReportRepository
            .Setup(x => x.GetByIdWithTradingPartnerAsync(_reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingReport);

        _mockFinancialReportRepository
            .Setup(x => x.DeleteAsync(It.IsAny<FinancialReport>(), It.IsAny<CancellationToken>()))
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