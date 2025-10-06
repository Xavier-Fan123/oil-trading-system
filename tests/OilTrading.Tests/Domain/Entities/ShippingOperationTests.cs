using FluentAssertions;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Core.Events;
using Xunit;

namespace OilTrading.Tests.Domain.Entities;

public class ShippingOperationTests
{
    private readonly Guid _contractId = Guid.NewGuid();
    private readonly string _shippingNumber = "SHIP-2024-001";
    private readonly string _vesselName = "MT Prosperity";
    private readonly Quantity _plannedQuantity = Quantity.MetricTons(5000);
    private readonly DateTime _loadPortETA = DateTime.UtcNow.AddDays(30);
    private readonly DateTime _dischargePortETA = DateTime.UtcNow.AddDays(45);
    private const string LoadPort = "Houston";
    private const string DischargePort = "Rotterdam";

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateValidShippingOperation_WhenValidInputProvided()
    {
        // Act
        var operation = new ShippingOperation(
            _shippingNumber,
            _contractId,
            _vesselName,
            _plannedQuantity,
            _loadPortETA,
            _dischargePortETA,
            LoadPort,
            DischargePort);

        // Assert
        operation.Should().NotBeNull();
        operation.Id.Should().NotBeEmpty();
        operation.ShippingNumber.Should().Be(_shippingNumber.ToUpper());
        operation.ContractId.Should().Be(_contractId);
        operation.VesselName.Should().Be(_vesselName);
        operation.PlannedQuantity.Should().Be(_plannedQuantity);
        operation.LoadPortETA.Should().Be(_loadPortETA);
        operation.DischargePortETA.Should().Be(_dischargePortETA);
        operation.LoadPort.Should().Be(LoadPort);
        operation.DischargePort.Should().Be(DischargePort);
        operation.Status.Should().Be(ShippingStatus.Planned);
        operation.ActualQuantity.Should().BeNull();
        
        // Should have domain event
        operation.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ShippingOperationCreatedEvent>();
    }

    [Fact]
    public void Constructor_ShouldCreateWithoutPorts_WhenPortsNotProvided()
    {
        // Act
        var operation = new ShippingOperation(
            _shippingNumber,
            _contractId,
            _vesselName,
            _plannedQuantity,
            _loadPortETA,
            _dischargePortETA);

        // Assert
        operation.LoadPort.Should().BeNull();
        operation.DischargePort.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldTrimAndUppercaseShippingNumber()
    {
        // Arrange
        const string untrimmedShippingNumber = "  ship-2024-001  ";

        // Act
        var operation = new ShippingOperation(
            untrimmedShippingNumber,
            _contractId,
            _vesselName,
            _plannedQuantity,
            _loadPortETA,
            _dischargePortETA);

        // Assert
        operation.ShippingNumber.Should().Be("SHIP-2024-001");
    }

    [Fact]
    public void Constructor_ShouldTrimVesselName()
    {
        // Arrange
        const string untrimmedVesselName = "  MT Prosperity  ";

        // Act
        var operation = new ShippingOperation(
            _shippingNumber,
            _contractId,
            untrimmedVesselName,
            _plannedQuantity,
            _loadPortETA,
            _dischargePortETA);

        // Assert
        operation.VesselName.Should().Be("MT Prosperity");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_ShouldThrowDomainException_WhenShippingNumberIsInvalid(string invalidShippingNumber)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new ShippingOperation(
            invalidShippingNumber,
            _contractId,
            _vesselName,
            _plannedQuantity,
            _loadPortETA,
            _dischargePortETA));

        exception.Message.Should().Be("Shipping number cannot be empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_ShouldThrowDomainException_WhenVesselNameIsInvalid(string invalidVesselName)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new ShippingOperation(
            _shippingNumber,
            _contractId,
            invalidVesselName,
            _plannedQuantity,
            _loadPortETA,
            _dischargePortETA));

        exception.Message.Should().Be("Vessel name cannot be empty");
    }

    [Fact]
    public void Constructor_ShouldThrowDomainException_WhenLoadETAIsAfterDischargeETA()
    {
        // Arrange
        var loadETA = DateTime.UtcNow.AddDays(45);
        var dischargeETA = DateTime.UtcNow.AddDays(30);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new ShippingOperation(
            _shippingNumber,
            _contractId,
            _vesselName,
            _plannedQuantity,
            loadETA,
            dischargeETA));

        exception.Message.Should().Be("Load port ETA must be before discharge port ETA");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenPlannedQuantityIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ShippingOperation(
            _shippingNumber,
            _contractId,
            _vesselName,
            null!,
            _loadPortETA,
            _dischargePortETA));
    }

    #endregion

    #region Vessel Details Update Tests

    [Fact]
    public void UpdateVesselDetails_ShouldUpdateSuccessfully_WhenValidInputProvided()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        const string newVesselName = "MT Excellence";
        const string imoNumber = "IMO9234567";
        const decimal vesselCapacity = 50000m;
        const string updatedBy = "TestUser";

        // Act
        operation.UpdateVesselDetails(newVesselName, imoNumber, vesselCapacity, updatedBy);

        // Assert
        operation.VesselName.Should().Be(newVesselName);
        operation.IMONumber.Should().Be(imoNumber);
        operation.VesselCapacity.Should().Be(vesselCapacity);
        operation.DomainEvents.Should().Contain(e => e is ShippingVesselChangedEvent);
    }

    [Fact]
    public void UpdateVesselDetails_ShouldUpdateWithoutOptionalFields_WhenNotProvided()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        const string newVesselName = "MT Excellence";

        // Act
        operation.UpdateVesselDetails(newVesselName);

        // Assert
        operation.VesselName.Should().Be(newVesselName);
        operation.IMONumber.Should().BeNull();
        operation.VesselCapacity.Should().BeNull();
    }

    [Fact]
    public void UpdateVesselDetails_ShouldThrowDomainException_WhenStatusIsDischarged()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        SetShippingStatus(operation, ShippingStatus.Discharged);
        const string newVesselName = "MT Excellence";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => operation.UpdateVesselDetails(newVesselName));
        exception.Message.Should().Be("Cannot update vessel details for discharged shipment");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateVesselDetails_ShouldThrowDomainException_WhenVesselNameIsInvalid(string invalidVesselName)
    {
        // Arrange
        var operation = CreateValidShippingOperation();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => operation.UpdateVesselDetails(invalidVesselName));
        exception.Message.Should().Be("Vessel name cannot be empty");
    }

    #endregion

    #region Schedule Update Tests

    [Fact]
    public void UpdateSchedule_ShouldUpdateSuccessfully_WhenValidDatesProvided()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        var newLoadETA = DateTime.UtcNow.AddDays(35);
        var newDischargeETA = DateTime.UtcNow.AddDays(50);
        const string updatedBy = "TestUser";

        // Act
        operation.UpdateSchedule(newLoadETA, newDischargeETA, updatedBy);

        // Assert
        operation.LoadPortETA.Should().Be(newLoadETA);
        operation.DischargePortETA.Should().Be(newDischargeETA);
        operation.DomainEvents.Should().Contain(e => e is ShippingScheduleUpdatedEvent);
    }

    [Fact]
    public void UpdateSchedule_ShouldThrowDomainException_WhenStatusIsDischarged()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        SetShippingStatus(operation, ShippingStatus.Discharged);
        var newLoadETA = DateTime.UtcNow.AddDays(35);
        var newDischargeETA = DateTime.UtcNow.AddDays(50);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => operation.UpdateSchedule(newLoadETA, newDischargeETA));
        exception.Message.Should().Be("Cannot update schedule for discharged shipment");
    }

    [Fact]
    public void UpdateSchedule_ShouldThrowDomainException_WhenLoadETAIsAfterDischargeETA()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        var newLoadETA = DateTime.UtcNow.AddDays(50);
        var newDischargeETA = DateTime.UtcNow.AddDays(35);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => operation.UpdateSchedule(newLoadETA, newDischargeETA));
        exception.Message.Should().Be("Load port ETA must be before discharge port ETA");
    }

    #endregion

    #region Loading Process Tests

    [Fact]
    public void StartLoading_ShouldStartLoadingSuccessfully_WhenPlanned()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        var loadPortATA = DateTime.UtcNow.AddDays(30);
        var norDate = DateTime.UtcNow.AddDays(29);
        const string updatedBy = "LoadingMaster";

        // Act
        operation.StartLoading(loadPortATA, norDate, updatedBy);

        // Assert
        operation.Status.Should().Be(ShippingStatus.Loading);
        operation.LoadPortATA.Should().Be(loadPortATA);
        operation.NoticeOfReadinessDate.Should().Be(norDate);
        operation.DomainEvents.Should().Contain(e => e is ShippingLoadingStartedEvent);
    }

    [Fact]
    public void StartLoading_ShouldUseATAAsNOR_WhenNORNotProvided()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        var loadPortATA = DateTime.UtcNow.AddDays(30);

        // Act
        operation.StartLoading(loadPortATA);

        // Assert
        operation.NoticeOfReadinessDate.Should().Be(loadPortATA);
    }

    [Theory]
    [InlineData(ShippingStatus.Loading)]
    [InlineData(ShippingStatus.InTransit)]
    [InlineData(ShippingStatus.Discharged)]
    [InlineData(ShippingStatus.Cancelled)]
    public void StartLoading_ShouldThrowDomainException_WhenNotPlanned(ShippingStatus invalidStatus)
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        SetShippingStatus(operation, invalidStatus);
        var loadPortATA = DateTime.UtcNow.AddDays(30);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => operation.StartLoading(loadPortATA));
        exception.Message.Should().Contain($"Cannot start loading from {invalidStatus} status");
    }

    [Fact]
    public void CompletedLoading_ShouldCompleteLoadingSuccessfully_WhenLoading()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        operation.StartLoading(DateTime.UtcNow.AddDays(30));
        
        var blDate = DateTime.UtcNow.AddDays(31);
        var actualQuantity = Quantity.MetricTons(4950);
        const string updatedBy = "LoadingMaster";

        // Act
        operation.CompletedLoading(blDate, actualQuantity, updatedBy);

        // Assert
        operation.Status.Should().Be(ShippingStatus.InTransit);
        operation.BillOfLadingDate.Should().Be(blDate);
        operation.ActualQuantity.Should().Be(actualQuantity);
        operation.DomainEvents.Should().Contain(e => e is ShippingLoadingCompletedEvent);
    }

    [Theory]
    [InlineData(ShippingStatus.Planned)]
    [InlineData(ShippingStatus.InTransit)]
    [InlineData(ShippingStatus.Discharged)]
    [InlineData(ShippingStatus.Cancelled)]
    public void CompletedLoading_ShouldThrowDomainException_WhenNotLoading(ShippingStatus invalidStatus)
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        SetShippingStatus(operation, invalidStatus);
        
        var blDate = DateTime.UtcNow.AddDays(31);
        var actualQuantity = Quantity.MetricTons(4950);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => operation.CompletedLoading(blDate, actualQuantity));
        exception.Message.Should().Contain($"Cannot complete loading from {invalidStatus} status");
    }

    [Fact]
    public void CompletedLoading_ShouldThrowDomainException_WhenActualQuantityIsZero()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        operation.StartLoading(DateTime.UtcNow.AddDays(30));
        
        var blDate = DateTime.UtcNow.AddDays(31);
        var zeroQuantity = Quantity.MetricTons(0);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => operation.CompletedLoading(blDate, zeroQuantity));
        exception.Message.Should().Be("Actual quantity must be greater than zero");
    }

    #endregion

    #region Discharge Process Tests

    [Fact]
    public void CompleteDischarge_ShouldCompleteDischargeSuccessfully_WhenInTransit()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        operation.StartLoading(DateTime.UtcNow.AddDays(30));
        operation.CompletedLoading(DateTime.UtcNow.AddDays(31), Quantity.MetricTons(4950));
        
        var dischargeATA = DateTime.UtcNow.AddDays(45);
        var codDate = DateTime.UtcNow.AddDays(46);
        const string updatedBy = "DischargeMaster";

        // Act
        operation.CompleteDischarge(dischargeATA, codDate, updatedBy);

        // Assert
        operation.Status.Should().Be(ShippingStatus.Discharged);
        operation.DischargePortATA.Should().Be(dischargeATA);
        operation.CertificateOfDischargeDate.Should().Be(codDate);
        operation.DomainEvents.Should().Contain(e => e is ShippingDischargeCompletedEvent);
    }

    [Theory]
    [InlineData(ShippingStatus.Planned)]
    [InlineData(ShippingStatus.Loading)]
    [InlineData(ShippingStatus.Discharged)]
    [InlineData(ShippingStatus.Cancelled)]
    public void CompleteDischarge_ShouldThrowDomainException_WhenNotInTransit(ShippingStatus invalidStatus)
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        SetShippingStatus(operation, invalidStatus);
        
        var dischargeATA = DateTime.UtcNow.AddDays(45);
        var codDate = DateTime.UtcNow.AddDays(46);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => operation.CompleteDischarge(dischargeATA, codDate));
        exception.Message.Should().Contain($"Cannot complete discharge from {invalidStatus} status");
    }

    #endregion

    #region Cancellation Tests

    [Theory]
    [InlineData(ShippingStatus.Planned)]
    [InlineData(ShippingStatus.Loading)]
    [InlineData(ShippingStatus.InTransit)]
    public void Cancel_ShouldCancelSuccessfully_WhenNotDischarged(ShippingStatus status)
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        SetShippingStatus(operation, status);
        const string reason = "Weather conditions";
        const string updatedBy = "OperationsManager";

        // Act
        operation.Cancel(reason, updatedBy);

        // Assert
        operation.Status.Should().Be(ShippingStatus.Cancelled);
        operation.Notes.Should().Contain(reason);
        operation.DomainEvents.Should().Contain(e => e is ShippingOperationCancelledEvent);
    }

    [Fact]
    public void Cancel_ShouldThrowDomainException_WhenDischarged()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        SetShippingStatus(operation, ShippingStatus.Discharged);
        const string reason = "Test cancellation";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => operation.Cancel(reason));
        exception.Message.Should().Be("Cannot cancel discharged shipment");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Cancel_ShouldThrowDomainException_WhenReasonIsInvalid(string invalidReason)
    {
        // Arrange
        var operation = CreateValidShippingOperation();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => operation.Cancel(invalidReason));
        exception.Message.Should().Be("Cancellation reason is required");
    }

    #endregion

    #region Quantity Update Tests

    [Fact]
    public void UpdateQuantity_ShouldUpdateSuccessfully_WhenPlanned()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        var newQuantity = Quantity.MetricTons(5500);
        const string updatedBy = "PlanningTeam";

        // Act
        operation.UpdateQuantity(newQuantity, updatedBy);

        // Assert
        operation.PlannedQuantity.Should().Be(newQuantity);
        operation.DomainEvents.Should().Contain(e => e is ShippingQuantityUpdatedEvent);
    }

    [Theory]
    [InlineData(ShippingStatus.Loading)]
    [InlineData(ShippingStatus.InTransit)]
    [InlineData(ShippingStatus.Discharged)]
    [InlineData(ShippingStatus.Cancelled)]
    public void UpdateQuantity_ShouldThrowDomainException_WhenNotPlanned(ShippingStatus invalidStatus)
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        SetShippingStatus(operation, invalidStatus);
        var newQuantity = Quantity.MetricTons(5500);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => operation.UpdateQuantity(newQuantity));
        exception.Message.Should().Contain($"Cannot update quantity for {invalidStatus} shipment");
    }

    [Fact]
    public void UpdateQuantity_ShouldThrowDomainException_WhenQuantityIsZero()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        var zeroQuantity = Quantity.MetricTons(0);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => operation.UpdateQuantity(zeroQuantity));
        exception.Message.Should().Be("Planned quantity must be greater than zero");
    }

    #endregion

    #region Variance and Analysis Tests

    [Fact]
    public void GetVariancePercentage_ShouldReturnCorrectPercentage_WhenActualQuantitySet()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        operation.StartLoading(DateTime.UtcNow.AddDays(30));
        
        // Planned: 5000 MT, Actual: 4950 MT
        var actualQuantity = Quantity.MetricTons(4950);
        operation.CompletedLoading(DateTime.UtcNow.AddDays(31), actualQuantity);

        // Act
        var variance = operation.GetVariancePercentage();

        // Assert
        variance.Should().BeApproximately(-1.0m, 0.01m); // (4950 - 5000) / 5000 * 100 = -1%
    }

    [Fact]
    public void GetVariancePercentage_ShouldReturnZero_WhenActualQuantityNotSet()
    {
        // Arrange
        var operation = CreateValidShippingOperation();

        // Act
        var variance = operation.GetVariancePercentage();

        // Assert
        variance.Should().Be(0);
    }

    [Theory]
    [InlineData(4950, 5.0, false)] // -1% variance, within 5% tolerance
    [InlineData(4750, 5.0, true)]  // -5% variance, exactly at tolerance
    [InlineData(4700, 5.0, true)]  // -6% variance, exceeds tolerance
    [InlineData(5250, 5.0, true)]  // +5% variance, exactly at tolerance
    [InlineData(5300, 5.0, true)]  // +6% variance, exceeds tolerance
    public void IsOverTolerance_ShouldReturnCorrectResult_ForDifferentVariances(decimal actualValue, decimal tolerance, bool expectedOverTolerance)
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        operation.StartLoading(DateTime.UtcNow.AddDays(30));
        operation.CompletedLoading(DateTime.UtcNow.AddDays(31), Quantity.MetricTons(actualValue));

        // Act
        var isOverTolerance = operation.IsOverTolerance(tolerance);

        // Assert
        isOverTolerance.Should().Be(expectedOverTolerance);
    }

    [Fact]
    public void GetTransitTime_ShouldReturnActualTransitTime_WhenActualDatesAvailable()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        var loadATA = DateTime.UtcNow.AddDays(30);
        var dischargeATA = DateTime.UtcNow.AddDays(45);
        
        SetActualDates(operation, loadATA, dischargeATA);

        // Act
        var transitTime = operation.GetTransitTime();

        // Assert
        transitTime.Should().Be(TimeSpan.FromDays(15));
    }

    [Fact]
    public void GetTransitTime_ShouldReturnPlannedTransitTime_WhenActualDatesNotAvailable()
    {
        // Arrange
        var operation = CreateValidShippingOperation();

        // Act
        var transitTime = operation.GetTransitTime();

        // Assert
        transitTime.Should().Be(_dischargePortETA - _loadPortETA);
    }

    [Fact]
    public void IsDelayed_ShouldReturnTrue_WhenLoadingDelayed()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        var delayedLoadATA = _loadPortETA.AddDays(2);
        
        SetActualDates(operation, delayedLoadATA, null);

        // Act
        var isDelayed = operation.IsDelayed();

        // Assert
        isDelayed.Should().BeTrue();
    }

    [Fact]
    public void IsDelayed_ShouldReturnTrue_WhenDischargeDelayed()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        var delayedDischargeATA = _dischargePortETA.AddDays(2);
        
        SetActualDates(operation, _loadPortETA, delayedDischargeATA);

        // Act
        var isDelayed = operation.IsDelayed();

        // Assert
        isDelayed.Should().BeTrue();
    }

    [Fact]
    public void IsDelayed_ShouldReturnFalse_WhenOnTime()
    {
        // Arrange
        var operation = CreateValidShippingOperation();
        SetActualDates(operation, _loadPortETA, _dischargePortETA);

        // Act
        var isDelayed = operation.IsDelayed();

        // Assert
        isDelayed.Should().BeFalse();
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var operation = CreateValidShippingOperation();

        // Act
        var result = operation.ToString();

        // Assert
        result.Should().Be($"Shipping {_shippingNumber.ToUpper()} - {_vesselName} (Planned)");
    }

    #endregion

    #region Helper Methods

    private ShippingOperation CreateValidShippingOperation()
    {
        return new ShippingOperation(
            _shippingNumber,
            _contractId,
            _vesselName,
            _plannedQuantity,
            _loadPortETA,
            _dischargePortETA,
            LoadPort,
            DischargePort);
    }

    private static void SetShippingStatus(ShippingOperation operation, ShippingStatus status)
    {
        var statusProperty = typeof(ShippingOperation).GetProperty(nameof(ShippingOperation.Status));
        statusProperty?.SetValue(operation, status);
    }

    private static void SetActualDates(ShippingOperation operation, DateTime? loadATA, DateTime? dischargeATA)
    {
        if (loadATA.HasValue)
        {
            var loadATAProperty = typeof(ShippingOperation).GetProperty(nameof(ShippingOperation.LoadPortATA));
            loadATAProperty?.SetValue(operation, loadATA.Value);
        }
        
        if (dischargeATA.HasValue)
        {
            var dischargeATAProperty = typeof(ShippingOperation).GetProperty(nameof(ShippingOperation.DischargePortATA));
            dischargeATAProperty?.SetValue(operation, dischargeATA.Value);
        }
    }

    #endregion
}