using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Application.Queries.FinancialReports;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using Xunit;

namespace OilTrading.Tests.Application.Queries.FinancialReports;

public class GetFinancialReportsByTradingPartnerQueryHandlerTests : IDisposable
{
    private readonly Mock<IFinancialReportRepository> _mockFinancialReportRepository;
    private readonly Mock<ITradingPartnerRepository> _mockTradingPartnerRepository;
    private readonly Mock<ILogger<GetFinancialReportsByTradingPartnerQueryHandler>> _mockLogger;
    private readonly GetFinancialReportsByTradingPartnerQueryHandler _handler;

    // Test data
    private readonly Guid _tradingPartnerId = Guid.NewGuid();
    private readonly TradingPartner _tradingPartner;
    private readonly List<FinancialReport> _multiYearReports;

    public GetFinancialReportsByTradingPartnerQueryHandlerTests()
    {
        _mockFinancialReportRepository = new Mock<IFinancialReportRepository>();
        _mockTradingPartnerRepository = new Mock<ITradingPartnerRepository>();
        _mockLogger = new Mock<ILogger<GetFinancialReportsByTradingPartnerQueryHandler>>();

        _handler = new GetFinancialReportsByTradingPartnerQueryHandler(
            _mockFinancialReportRepository.Object,
            _mockTradingPartnerRepository.Object,
            _mockLogger.Object);

        // Setup test data
        _tradingPartner = CreateTestTradingPartner();
        _multiYearReports = CreateMultiYearReports();
    }

    #region Success Cases - Basic Retrieval

    [Fact]
    public async Task Handle_WithValidTradingPartnerId_ShouldReturnAllReports()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId);
        SetupBasicScenario();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result.Should().BeInDescendingOrder(r => r.ReportStartDate);
        
        // Verify all reports are mapped correctly
        var firstReport = result.First();
        firstReport.TradingPartnerId.Should().Be(_tradingPartnerId);
        firstReport.ReportYear.Should().Be(2024);
        firstReport.TotalAssets.Should().Be(12000m);
        firstReport.CurrentRatio.Should().BeApproximately(2.0m, 0.01m);
    }

    [Fact]
    public async Task Handle_WithSpecificYear_ShouldReturnOnlyThatYearsReports()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId)
        {
            Year = 2023
        };
        
        var year2023Reports = _multiYearReports.Where(r => r.ReportYear == 2023).ToList();
        SetupYearFilterScenario(2023, year2023Reports);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Q1 and Q4 reports for 2023
        result.Should().OnlyContain(r => r.ReportYear == 2023);
        result.Should().BeInDescendingOrder(r => r.ReportStartDate);
    }

    [Fact]
    public async Task Handle_WithNonExistentYear_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId)
        {
            Year = 2020
        };
        
        SetupYearFilterScenario(2020, new List<FinancialReport>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithIncludeGrowthMetrics_ShouldCalculateGrowthRates()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId)
        {
            IncludeGrowthMetrics = true
        };
        
        SetupGrowthMetricsScenario();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        
        var currentYearReport = result.First(r => r.ReportYear == 2024);
        var previousYearReport = result.First(r => r.ReportYear == 2023);
        
        // 2024 report should have growth metrics calculated
        currentYearReport.RevenueGrowth.Should().Be(20m); // 20% growth
        currentYearReport.NetProfitGrowth.Should().Be(50m); // 50% growth
        currentYearReport.TotalAssetsGrowth.Should().Be(20m); // 20% growth
        
        // 2023 report should also have growth metrics (vs 2022)
        previousYearReport.RevenueGrowth.Should().Be(25m); // 25% growth
        
        // First report chronologically should have no growth metrics
        var oldestReport = result.Last();
        oldestReport.RevenueGrowth.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithoutGrowthMetrics_ShouldNotCalculateGrowth()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId)
        {
            IncludeGrowthMetrics = false
        };
        
        SetupBasicScenario();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyContain(r => r.RevenueGrowth == null);
        result.Should().OnlyContain(r => r.NetProfitGrowth == null);
        result.Should().OnlyContain(r => r.TotalAssetsGrowth == null);
    }

    #endregion

    #region Growth Calculation Tests

    [Theory]
    [InlineData(1000, 1200, 20.0)] // 20% growth
    [InlineData(1000, 800, -20.0)] // 20% decline
    [InlineData(1000, 1000, 0.0)] // No change
    [InlineData(100, 200, 100.0)] // 100% growth
    [InlineData(200, 100, -50.0)] // 50% decline
    public async Task Handle_GrowthCalculation_ShouldCalculateCorrectPercentage(decimal previous, decimal current, decimal expectedGrowth)
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId)
        {
            IncludeGrowthMetrics = true
        };
        
        SetupCustomGrowthScenario(previous, current);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        var currentReport = result.First();
        currentReport.RevenueGrowth.Should().Be(expectedGrowth);
    }

    [Theory]
    [InlineData(0, 100, null)] // Division by zero
    [InlineData(-100, -50, -50.0)] // Negative values
    [InlineData(-100, 50, 150.0)] // Negative to positive
    public async Task Handle_GrowthCalculation_EdgeCases_ShouldHandleCorrectly(decimal previous, decimal current, decimal? expectedGrowth)
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId)
        {
            IncludeGrowthMetrics = true
        };
        
        SetupCustomGrowthScenario(previous, current);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        
        var currentReport = result.First();
        currentReport.RevenueGrowth.Should().Be(expectedGrowth);
    }

    [Fact]
    public async Task Handle_WithNullPreviousValues_ShouldReturnNullGrowth()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId)
        {
            IncludeGrowthMetrics = true
        };
        
        SetupNullValueGrowthScenario();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        
        var currentReport = result.First();
        currentReport.RevenueGrowth.Should().BeNull();
        currentReport.NetProfitGrowth.Should().BeNull();
        currentReport.TotalAssetsGrowth.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNonConsecutiveYears_ShouldCalculateGrowthCorrectly()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId)
        {
            IncludeGrowthMetrics = true
        };
        
        SetupNonConsecutiveYearScenario();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        
        var report2024 = result.First(r => r.ReportYear == 2024);
        report2024.RevenueGrowth.Should().Be(25m); // Growth vs 2022 (skipping 2023)
        
        var report2022 = result.First(r => r.ReportYear == 2022);
        report2022.RevenueGrowth.Should().BeNull(); // No 2021 data
    }

    #endregion

    #region Error Cases

    [Fact]
    public async Task Handle_WithNonExistentTradingPartner_ShouldThrowNotFoundException()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId);
        
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradingPartner?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
        
        exception.Message.Should().Contain("TradingPartner");
        exception.Message.Should().Contain(_tradingPartnerId.ToString());

        // Verify no report retrieval was attempted
        _mockFinancialReportRepository.Verify(x => 
            x.GetByTradingPartnerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithRepositoryException_ShouldThrowOriginalException()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId);
        
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tradingPartner);
            
        _mockFinancialReportRepository
            .Setup(x => x.GetByTradingPartnerIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(query, CancellationToken.None));
        
        exception.Message.Should().Contain("Database connection failed");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithNoReports_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId);
        
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tradingPartner);
            
        _mockFinancialReportRepository
            .Setup(x => x.GetByTradingPartnerIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinancialReport>().AsReadOnly());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithSingleReport_ShouldReturnOneReportWithoutGrowth()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId)
        {
            IncludeGrowthMetrics = true
        };
        
        var singleReport = new List<FinancialReport> { _multiYearReports.First() };
        
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tradingPartner);
            
        _mockFinancialReportRepository
            .Setup(x => x.GetByTradingPartnerIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(singleReport.AsReadOnly());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        
        var report = result.First();
        report.RevenueGrowth.Should().BeNull();
        report.NetProfitGrowth.Should().BeNull();
        report.TotalAssetsGrowth.Should().BeNull();
    }

    [Theory]
    [InlineData(2025)] // Future year
    [InlineData(1900)] // Very old year
    [InlineData(-1)] // Invalid year
    public async Task Handle_WithInvalidYear_ShouldReturnEmptyList(int year)
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId)
        {
            Year = year
        };
        
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tradingPartner);
            
        _mockFinancialReportRepository
            .Setup(x => x.GetByTradingPartnerAndYearAsync(_tradingPartnerId, year, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinancialReport>().AsReadOnly());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region Logging Verification

    [Fact]
    public async Task Handle_SuccessfulRetrieval_ShouldLogRetrievalAndCompletion()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId)
        {
            Year = 2023
        };
        
        SetupYearFilterScenario(2023, _multiYearReports.Where(r => r.ReportYear == 2023).ToList());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Retrieving financial reports") && 
                    v.ToString()!.Contains("2023")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Found") && 
                    v.ToString()!.Contains("financial reports")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithAllYears_ShouldLogAllYearsFilter()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId);
        SetupBasicScenario();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("All years")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Repository Integration Tests

    [Fact]
    public async Task Handle_WithYearFilter_ShouldCallCorrectRepositoryMethod()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId)
        {
            Year = 2023
        };
        
        SetupYearFilterScenario(2023, new List<FinancialReport>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockFinancialReportRepository.Verify(x => 
            x.GetByTradingPartnerAndYearAsync(_tradingPartnerId, 2023, It.IsAny<CancellationToken>()), 
            Times.Once);
            
        _mockFinancialReportRepository.Verify(x => 
            x.GetByTradingPartnerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutYearFilter_ShouldCallCorrectRepositoryMethod()
    {
        // Arrange
        var query = new GetFinancialReportsByTradingPartnerQuery(_tradingPartnerId);
        SetupBasicScenario();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockFinancialReportRepository.Verify(x => 
            x.GetByTradingPartnerIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()), 
            Times.Once);
            
        _mockFinancialReportRepository.Verify(x => 
            x.GetByTradingPartnerAndYearAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    #endregion

    #region Helper Methods

    private TradingPartner CreateTestTradingPartner()
    {
        var partner = new TradingPartner
        {
            CompanyName = "Test Trading Company",
            CompanyCode = "TTC",
            Type = TradingPartnerType.Supplier,
            CreditLimit = 1000000m,
            CurrentExposure = 250000m,
            IsActive = true
        };
        SetEntityId(partner, _tradingPartnerId);
        return partner;
    }

    private List<FinancialReport> CreateMultiYearReports()
    {
        var reports = new List<FinancialReport>();
        
        // 2024 Annual Report
        var report2024 = new FinancialReport(_tradingPartnerId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        report2024.UpdateFinancialPosition(12000m, 6000m, 6000m, 7000m, 3500m);
        report2024.UpdatePerformanceData(15000m, 1500m, 1800m);
        reports.Add(report2024);
        
        // 2023 Q4 Report
        var report2023Q4 = new FinancialReport(_tradingPartnerId, new DateTime(2023, 10, 1), new DateTime(2023, 12, 31));
        report2023Q4.UpdateFinancialPosition(10500m, 5500m, 5000m, 6300m, 3150m);
        report2023Q4.UpdatePerformanceData(12500m, 1000m, 1250m);
        reports.Add(report2023Q4);
        
        // 2023 Q1 Report
        var report2023Q1 = new FinancialReport(_tradingPartnerId, new DateTime(2023, 1, 1), new DateTime(2023, 3, 31));
        report2023Q1.UpdateFinancialPosition(9800m, 4900m, 4900m, 5880m, 2940m);
        report2023Q1.UpdatePerformanceData(12000m, 800m, 1000m);
        reports.Add(report2023Q1);
        
        // 2022 Annual Report
        var report2022 = new FinancialReport(_tradingPartnerId, new DateTime(2022, 1, 1), new DateTime(2022, 12, 31));
        report2022.UpdateFinancialPosition(10000m, 5000m, 5000m, 6000m, 3000m);
        report2022.UpdatePerformanceData(10000m, 800m, 1000m);
        reports.Add(report2022);
        
        return reports;
    }

    private void SetupBasicScenario()
    {
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tradingPartner);
            
        _mockFinancialReportRepository
            .Setup(x => x.GetByTradingPartnerIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_multiYearReports.AsReadOnly());
    }

    private void SetupYearFilterScenario(int year, List<FinancialReport> yearReports)
    {
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tradingPartner);
            
        _mockFinancialReportRepository
            .Setup(x => x.GetByTradingPartnerAndYearAsync(_tradingPartnerId, year, It.IsAny<CancellationToken>()))
            .ReturnsAsync(yearReports.AsReadOnly());
    }

    private void SetupGrowthMetricsScenario()
    {
        var growthReports = new List<FinancialReport>();
        
        // 2024 report
        var current = new FinancialReport(_tradingPartnerId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        current.UpdateFinancialPosition(12000m, 6000m, 6000m, 7000m, 3500m);
        current.UpdatePerformanceData(12000m, 1500m, 1800m); // 20% revenue growth, 50% profit growth
        growthReports.Add(current);
        
        // 2023 report
        var previous = new FinancialReport(_tradingPartnerId, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31));
        previous.UpdateFinancialPosition(10000m, 5000m, 5000m, 6000m, 3000m);
        previous.UpdatePerformanceData(10000m, 1000m, 1200m); // Base for 2024 growth
        growthReports.Add(previous);
        
        // 2022 report
        var older = new FinancialReport(_tradingPartnerId, new DateTime(2022, 1, 1), new DateTime(2022, 12, 31));
        older.UpdateFinancialPosition(8000m, 4000m, 4000m, 4800m, 2400m);
        older.UpdatePerformanceData(8000m, 600m, 800m); // Base for 2023 growth (25% revenue growth)
        growthReports.Add(older);
        
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tradingPartner);
            
        _mockFinancialReportRepository
            .Setup(x => x.GetByTradingPartnerIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(growthReports.AsReadOnly());
    }

    private void SetupCustomGrowthScenario(decimal previousRevenue, decimal currentRevenue)
    {
        var reports = new List<FinancialReport>();
        
        var current = new FinancialReport(_tradingPartnerId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        current.UpdateFinancialPosition(10000m, 5000m, 5000m, 6000m, 3000m);
        current.UpdatePerformanceData(currentRevenue, 1000m, 1200m);
        reports.Add(current);
        
        var previous = new FinancialReport(_tradingPartnerId, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31));
        previous.UpdateFinancialPosition(8000m, 4000m, 4000m, 4800m, 2400m);
        previous.UpdatePerformanceData(previousRevenue, 800m, 1000m);
        reports.Add(previous);
        
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tradingPartner);
            
        _mockFinancialReportRepository
            .Setup(x => x.GetByTradingPartnerIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports.AsReadOnly());
    }

    private void SetupNullValueGrowthScenario()
    {
        var reports = new List<FinancialReport>();
        
        var current = new FinancialReport(_tradingPartnerId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        current.UpdateFinancialPosition(10000m, 5000m, 5000m, 6000m, 3000m);
        current.UpdatePerformanceData(12000m, 1000m, 1200m);
        reports.Add(current);
        
        var previous = new FinancialReport(_tradingPartnerId, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31));
        previous.UpdateFinancialPosition(8000m, 4000m, 4000m, 4800m, 2400m);
        previous.UpdatePerformanceData(null, null, null); // Null values for growth calculation
        reports.Add(previous);
        
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tradingPartner);
            
        _mockFinancialReportRepository
            .Setup(x => x.GetByTradingPartnerIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports.AsReadOnly());
    }

    private void SetupNonConsecutiveYearScenario()
    {
        var reports = new List<FinancialReport>();
        
        // 2024 report
        var report2024 = new FinancialReport(_tradingPartnerId, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        report2024.UpdateFinancialPosition(10000m, 5000m, 5000m, 6000m, 3000m);
        report2024.UpdatePerformanceData(10000m, 1000m, 1200m);
        reports.Add(report2024);
        
        // Skip 2023, add 2022 report
        var report2022 = new FinancialReport(_tradingPartnerId, new DateTime(2022, 1, 1), new DateTime(2022, 12, 31));
        report2022.UpdateFinancialPosition(8000m, 4000m, 4000m, 4800m, 2400m);
        report2022.UpdatePerformanceData(8000m, 800m, 1000m);
        reports.Add(report2022);
        
        _mockTradingPartnerRepository
            .Setup(x => x.GetByIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tradingPartner);
            
        _mockFinancialReportRepository
            .Setup(x => x.GetByTradingPartnerIdAsync(_tradingPartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports.AsReadOnly());
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