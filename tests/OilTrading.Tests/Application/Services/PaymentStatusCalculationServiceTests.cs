using Xunit;
using Moq;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Application.Services;

namespace OilTrading.Tests.Application.Services;

/// <summary>
/// Unit tests for PaymentStatusCalculationService.
/// Tests the calculation of contract payment status based on settlement information.
/// </summary>
public class PaymentStatusCalculationServiceTests
{
    private readonly Mock<IContractSettlementRepository> _settlementRepositoryMock;
    private readonly Mock<IPurchaseContractRepository> _purchaseContractRepositoryMock;
    private readonly Mock<ISalesContractRepository> _salesContractRepositoryMock;
    private readonly PaymentStatusCalculationService _service;

    public PaymentStatusCalculationServiceTests()
    {
        _settlementRepositoryMock = new Mock<IContractSettlementRepository>();
        _purchaseContractRepositoryMock = new Mock<IPurchaseContractRepository>();
        _salesContractRepositoryMock = new Mock<ISalesContractRepository>();

        _service = new PaymentStatusCalculationService(
            _settlementRepositoryMock.Object,
            _purchaseContractRepositoryMock.Object,
            _salesContractRepositoryMock.Object);
    }

    #region Helper Methods

    private ContractSettlement CreateSettlement(
        Guid contractId,
        decimal amount = 10000m,
        DateTime? dueDate = null,
        DateTime? paymentDate = null)
    {
        dueDate ??= DateTime.UtcNow.AddDays(30);

        var settlement = new ContractSettlement(
            contractId,
            "PC-2025-001",
            "EXT-001",
            "BL-123456",
            DocumentType.BillOfLading,
            DateTime.UtcNow,
            "Test");

        // Set amounts
        typeof(ContractSettlement).GetProperty("BenchmarkAmount")?.SetValue(settlement, 5000m);
        typeof(ContractSettlement).GetProperty("CargoValue")?.SetValue(settlement, 5000m);
        typeof(ContractSettlement).GetProperty("TotalSettlementAmount")?.SetValue(settlement, amount);
        typeof(ContractSettlement).GetProperty("ActualPayableDueDate")?.SetValue(settlement, dueDate);
        typeof(ContractSettlement).GetProperty("ActualPaymentDate")?.SetValue(settlement, paymentDate);

        return settlement;
    }

    #endregion

    #region Purchase Contract Payment Status Tests

    [Fact]
    public async Task CalculatePurchaseContractPaymentStatusAsync_NoSettlements_ReturnsNull()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContractSettlement>());

        // Act
        var result = await _service.CalculatePurchaseContractPaymentStatusAsync(contractId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CalculatePurchaseContractPaymentStatusAsync_AllSettlementsPaid_ReturnsPaid()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var paymentDate = DateTime.UtcNow.AddDays(-5);
        var settlements = new List<ContractSettlement>
        {
            CreateSettlement(contractId, 5000m, DateTime.UtcNow.AddDays(30), paymentDate),
            CreateSettlement(contractId, 5000m, DateTime.UtcNow.AddDays(35), paymentDate)
        };

        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        // Act
        var result = await _service.CalculatePurchaseContractPaymentStatusAsync(contractId);

        // Assert
        Assert.Equal(ContractPaymentStatus.Paid, result);
    }

    [Fact]
    public async Task CalculatePurchaseContractPaymentStatusAsync_SomeSettlementsPaid_ReturnsPartiallyPaid()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var paymentDate = DateTime.UtcNow.AddDays(-5);
        var settlements = new List<ContractSettlement>
        {
            CreateSettlement(contractId, 5000m, DateTime.UtcNow.AddDays(30), paymentDate),
            CreateSettlement(contractId, 5000m, DateTime.UtcNow.AddDays(35), null) // Unpaid
        };

        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        // Act
        var result = await _service.CalculatePurchaseContractPaymentStatusAsync(contractId);

        // Assert
        Assert.Equal(ContractPaymentStatus.PartiallyPaid, result);
    }

    [Fact]
    public async Task CalculatePurchaseContractPaymentStatusAsync_DueDatePassed_ReturnsOverdue()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var overdueDate = DateTime.UtcNow.AddDays(-5); // 5 days ago
        var settlements = new List<ContractSettlement>
        {
            CreateSettlement(contractId, 10000m, overdueDate, null)
        };

        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        // Act
        var result = await _service.CalculatePurchaseContractPaymentStatusAsync(contractId);

        // Assert
        Assert.Equal(ContractPaymentStatus.Overdue, result);
    }

    [Fact]
    public async Task CalculatePurchaseContractPaymentStatusAsync_DueDateToday_ReturnsDue()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var today = DateTime.UtcNow;
        var settlements = new List<ContractSettlement>
        {
            CreateSettlement(contractId, 10000m, today, null)
        };

        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        // Act
        var result = await _service.CalculatePurchaseContractPaymentStatusAsync(contractId);

        // Assert
        Assert.Equal(ContractPaymentStatus.Due, result);
    }

    [Fact]
    public async Task CalculatePurchaseContractPaymentStatusAsync_DueDateInFuture_ReturnsNotDue()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var futureDate = DateTime.UtcNow.AddDays(30);
        var settlements = new List<ContractSettlement>
        {
            CreateSettlement(contractId, 10000m, futureDate, null)
        };

        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        // Act
        var result = await _service.CalculatePurchaseContractPaymentStatusAsync(contractId);

        // Assert
        Assert.Equal(ContractPaymentStatus.NotDue, result);
    }

    #endregion

    #region Sales Contract Payment Status Tests

    [Fact]
    public async Task CalculateSalesContractPaymentStatusAsync_NoSettlements_ReturnsNull()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContractSettlement>());

        // Act
        var result = await _service.CalculateSalesContractPaymentStatusAsync(contractId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CalculateSalesContractPaymentStatusAsync_AllSettlementsCollected_ReturnsPaid()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var collectionDate = DateTime.UtcNow.AddDays(-5);
        var settlements = new List<ContractSettlement>
        {
            CreateSettlement(contractId, 5000m, DateTime.UtcNow.AddDays(30), collectionDate),
            CreateSettlement(contractId, 5000m, DateTime.UtcNow.AddDays(35), collectionDate)
        };

        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        // Act
        var result = await _service.CalculateSalesContractPaymentStatusAsync(contractId);

        // Assert
        Assert.Equal(ContractPaymentStatus.Paid, result);
    }

    [Fact]
    public async Task CalculateSalesContractPaymentStatusAsync_SomeSettlementsCollected_ReturnsPartiallyPaid()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var collectionDate = DateTime.UtcNow.AddDays(-5);
        var settlements = new List<ContractSettlement>
        {
            CreateSettlement(contractId, 5000m, DateTime.UtcNow.AddDays(30), collectionDate),
            CreateSettlement(contractId, 5000m, DateTime.UtcNow.AddDays(35), null) // Not collected
        };

        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        // Act
        var result = await _service.CalculateSalesContractPaymentStatusAsync(contractId);

        // Assert
        Assert.Equal(ContractPaymentStatus.PartiallyPaid, result);
    }

    #endregion

    #region Unpaid Amount Tests

    [Fact]
    public async Task GetUnpaidAmountAsync_NoSettlements_ReturnsZero()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContractSettlement>());

        // Act
        var result = await _service.GetUnpaidAmountAsync(contractId);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task GetUnpaidAmountAsync_WithUnpaidSettlements_ReturnsTotalUnpaidAmount()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var settlements = new List<ContractSettlement>
        {
            CreateSettlement(contractId, 5000m, DateTime.UtcNow.AddDays(30), null),
            CreateSettlement(contractId, 3000m, DateTime.UtcNow.AddDays(35), null),
            CreateSettlement(contractId, 2000m, DateTime.UtcNow.AddDays(40), DateTime.UtcNow) // Paid
        };

        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        // Act
        var result = await _service.GetUnpaidAmountAsync(contractId);

        // Assert
        Assert.Equal(8000m, result);
    }

    #endregion

    #region Paid Amount Tests

    [Fact]
    public async Task GetPaidAmountAsync_NoSettlements_ReturnsZero()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContractSettlement>());

        // Act
        var result = await _service.GetPaidAmountAsync(contractId);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task GetPaidAmountAsync_WithPaidSettlements_ReturnsTotalPaidAmount()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var paymentDate = DateTime.UtcNow.AddDays(-5);
        var settlements = new List<ContractSettlement>
        {
            CreateSettlement(contractId, 5000m, DateTime.UtcNow.AddDays(30), paymentDate),
            CreateSettlement(contractId, 3000m, DateTime.UtcNow.AddDays(35), paymentDate),
            CreateSettlement(contractId, 2000m, DateTime.UtcNow.AddDays(40), null) // Unpaid
        };

        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        // Act
        var result = await _service.GetPaidAmountAsync(contractId);

        // Assert
        Assert.Equal(8000m, result);
    }

    #endregion

    #region Earliest Unpaid Due Date Tests

    [Fact]
    public async Task GetEarliestUnpaidDueDateAsync_NoSettlements_ReturnsNull()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContractSettlement>());

        // Act
        var result = await _service.GetEarliestUnpaidDueDateAsync(contractId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetEarliestUnpaidDueDateAsync_WithUnpaidSettlements_ReturnsEarliestDate()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var date1 = DateTime.UtcNow.AddDays(30);
        var date2 = DateTime.UtcNow.AddDays(20); // Earlier
        var date3 = DateTime.UtcNow.AddDays(40);
        var paymentDate = DateTime.UtcNow.AddDays(-5);

        var settlements = new List<ContractSettlement>
        {
            CreateSettlement(contractId, 5000m, date1, null),
            CreateSettlement(contractId, 3000m, date2, null),
            CreateSettlement(contractId, 2000m, date3, paymentDate) // Paid - excluded
        };

        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        // Act
        var result = await _service.GetEarliestUnpaidDueDateAsync(contractId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(date2.Date, result.Value.Date);
    }

    #endregion

    #region Payment Status Details Tests

    [Fact]
    public async Task GetPaymentStatusDetailsAsync_NoSettlements_ReturnsEmptyDetails()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContractSettlement>());

        // Act
        var result = await _service.GetPaymentStatusDetailsAsync(contractId, isPurchaseContract: true);

        // Assert
        Assert.Null(result.PaymentStatus);
        Assert.Equal(0, result.TotalAmount);
        Assert.Equal(0, result.PaidAmount);
        Assert.Equal(0, result.UnpaidAmount);
        Assert.Empty(result.Settlements);
    }

    [Fact]
    public async Task GetPaymentStatusDetailsAsync_WithMultipleSettlements_ReturnsCompleteDetails()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var paymentDate = DateTime.UtcNow.AddDays(-5);
        var dueDate1 = DateTime.UtcNow.AddDays(30);
        var dueDate2 = DateTime.UtcNow.AddDays(35);

        var settlements = new List<ContractSettlement>
        {
            CreateSettlement(contractId, 5000m, dueDate1, paymentDate),
            CreateSettlement(contractId, 3000m, dueDate2, null)
        };

        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        // Act
        var result = await _service.GetPaymentStatusDetailsAsync(contractId, isPurchaseContract: true);

        // Assert
        Assert.Equal(ContractPaymentStatus.PartiallyPaid, result.PaymentStatus);
        Assert.Equal(8000m, result.TotalAmount);
        Assert.Equal(5000m, result.PaidAmount);
        Assert.Equal(3000m, result.UnpaidAmount);
        Assert.Equal(2, result.SettlementCount);
        Assert.Equal(1, result.PaidSettlementCount);
        Assert.Equal(1, result.UnpaidSettlementCount);
        Assert.NotNull(result.EarliestUnpaidDueDate);
        Assert.Equal(2, result.Settlements.Count);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task PaymentStatusCalculation_ComplexScenario_CorrectlyHandlesMultipleSettlements()
    {
        // Scenario: Contract with 3 settlements at different payment stages
        // Settlement 1: Paid 5 days ago
        // Settlement 2: Not paid, due in 30 days
        // Settlement 3: Not paid, due 3 days ago (overdue)

        // Arrange
        var contractId = Guid.NewGuid();
        var pastPaymentDate = DateTime.UtcNow.AddDays(-5);
        var pastDueDate = DateTime.UtcNow.AddDays(-3); // Overdue
        var futureDueDate = DateTime.UtcNow.AddDays(30);

        var settlements = new List<ContractSettlement>
        {
            CreateSettlement(contractId, 5000m, futureDueDate, pastPaymentDate),
            CreateSettlement(contractId, 3000m, futureDueDate, null),
            CreateSettlement(contractId, 2000m, pastDueDate, null)
        };

        _settlementRepositoryMock
            .Setup(r => r.GetByContractIdAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settlements);

        // Act
        var paymentStatus = await _service.CalculatePurchaseContractPaymentStatusAsync(contractId);
        var unpaidAmount = await _service.GetUnpaidAmountAsync(contractId);
        var paidAmount = await _service.GetPaidAmountAsync(contractId);
        var earliestDueDate = await _service.GetEarliestUnpaidDueDateAsync(contractId);
        var details = await _service.GetPaymentStatusDetailsAsync(contractId, isPurchaseContract: true);

        // Assert
        Assert.Equal(ContractPaymentStatus.Overdue, paymentStatus); // Most severe status
        Assert.Equal(5000m, unpaidAmount); // 3000 + 2000
        Assert.Equal(5000m, paidAmount);
        Assert.NotNull(earliestDueDate);
        Assert.Equal(pastDueDate.Date, earliestDueDate.Value.Date);
        Assert.Equal(ContractPaymentStatus.Overdue, details.PaymentStatus);
        Assert.Equal(3, details.SettlementCount);
        Assert.Equal(1, details.PaidSettlementCount);
        Assert.Equal(2, details.UnpaidSettlementCount);
        Assert.NotNull(details.EarliestUnpaidDueDate);
    }

    #endregion
}
