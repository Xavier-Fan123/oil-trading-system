using FluentAssertions;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Core.Events;
using Xunit;

namespace OilTrading.Tests.Domain.Entities;

public class SalesContractTests
{
    private readonly Guid _tradingPartnerId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _traderId = Guid.NewGuid();
    private readonly Guid _purchaseContractId = Guid.NewGuid();
    private readonly ContractNumber _contractNumber = ContractNumber.Create(2024, ContractType.CARGO, 1);
    private readonly Quantity _contractQuantity = Quantity.MetricTons(1000);

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateValidSalesContract_WhenValidInputProvided()
    {
        // Act
        var contract = new SalesContract(
            _contractNumber,
            ContractType.CARGO,
            _tradingPartnerId,
            _productId,
            _traderId,
            _contractQuantity);

        // Assert
        contract.Should().NotBeNull();
        contract.Id.Should().NotBeEmpty();
        contract.ContractNumber.Should().Be(_contractNumber);
        contract.ContractType.Should().Be(ContractType.CARGO);
        contract.TradingPartnerId.Should().Be(_tradingPartnerId);
        contract.CustomerId.Should().Be(_tradingPartnerId); // Alias property
        contract.ProductId.Should().Be(_productId);
        contract.TraderId.Should().Be(_traderId);
        contract.ContractQuantity.Should().Be(_contractQuantity);
        contract.Status.Should().Be(ContractStatus.Draft);
        contract.LinkedPurchaseContractId.Should().BeNull();
        contract.TonBarrelRatio.Should().Be(7.6m);
        contract.DeliveryTerms.Should().Be(DeliveryTerms.FOB);
        contract.SettlementType.Should().Be(SettlementType.ContractPayment);
        
        // Should have domain event
        contract.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SalesContractCreatedEvent>();
    }

    [Fact]
    public void Constructor_ShouldCreateWithLinkedPurchaseContract_WhenProvided()
    {
        // Act
        var contract = new SalesContract(
            _contractNumber,
            ContractType.CARGO,
            _tradingPartnerId,
            _productId,
            _traderId,
            _contractQuantity,
            linkedPurchaseContractId: _purchaseContractId);

        // Assert
        contract.LinkedPurchaseContractId.Should().Be(_purchaseContractId);
    }

    [Fact]
    public void Constructor_ShouldCreateWithCustomTonBarrelRatio_WhenProvided()
    {
        // Arrange
        const decimal customRatio = 8.0m;

        // Act
        var contract = new SalesContract(
            _contractNumber,
            ContractType.CARGO,
            _tradingPartnerId,
            _productId,
            _traderId,
            _contractQuantity,
            customRatio);

        // Assert
        contract.TonBarrelRatio.Should().Be(customRatio);
    }

    [Theory]
    [InlineData(null)]
    public void Constructor_ShouldThrowArgumentNullException_WhenContractNumberIsNull(ContractNumber contractNumber)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SalesContract(
            contractNumber,
            ContractType.CARGO,
            _tradingPartnerId,
            _productId,
            _traderId,
            _contractQuantity));
    }

    [Theory]
    [InlineData(null)]
    public void Constructor_ShouldThrowArgumentNullException_WhenContractQuantityIsNull(Quantity contractQuantity)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SalesContract(
            _contractNumber,
            ContractType.CARGO,
            _tradingPartnerId,
            _productId,
            _traderId,
            contractQuantity));
    }

    #endregion

    #region Purchase Contract Linking Tests

    [Fact]
    public void LinkToPurchaseContract_ShouldLinkSuccessfully_WhenContractIsDraft()
    {
        // Arrange
        var contract = CreateValidSalesContract();

        // Act
        contract.LinkToPurchaseContract(_purchaseContractId);

        // Assert
        contract.LinkedPurchaseContractId.Should().Be(_purchaseContractId);
        contract.DomainEvents.Should().Contain(e => e is SalesContractLinkedToPurchaseEvent);
    }

    [Theory]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Completed)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.PendingApproval)]
    public void LinkToPurchaseContract_ShouldThrowDomainException_WhenContractIsNotDraft(ContractStatus status)
    {
        // Arrange
        var contract = CreateValidSalesContract();
        SetContractStatus(contract, status);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.LinkToPurchaseContract(_purchaseContractId));
        exception.Message.Should().Contain($"Cannot link purchase contract when sales contract is in {status} status");
    }

    [Fact]
    public void UnlinkFromPurchaseContract_ShouldUnlinkSuccessfully_WhenLinkedAndDraft()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        contract.LinkToPurchaseContract(_purchaseContractId);

        // Act
        contract.UnlinkFromPurchaseContract();

        // Assert
        contract.LinkedPurchaseContractId.Should().BeNull();
        contract.DomainEvents.Should().Contain(e => e is SalesContractUnlinkedFromPurchaseEvent);
    }

    [Fact]
    public void UnlinkFromPurchaseContract_ShouldThrowDomainException_WhenNotLinked()
    {
        // Arrange
        var contract = CreateValidSalesContract();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.UnlinkFromPurchaseContract());
        exception.Message.Should().Be("Sales contract is not linked to any purchase contract");
    }

    [Theory]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Completed)]
    [InlineData(ContractStatus.Cancelled)]
    public void UnlinkFromPurchaseContract_ShouldThrowDomainException_WhenContractIsNotDraft(ContractStatus status)
    {
        // Arrange
        var contract = CreateValidSalesContract();
        contract.LinkToPurchaseContract(_purchaseContractId);
        SetContractStatus(contract, status);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.UnlinkFromPurchaseContract());
        exception.Message.Should().Contain($"Cannot unlink purchase contract when sales contract is in {status} status");
    }

    #endregion

    #region Pricing Tests

    [Fact]
    public void UpdatePricing_ShouldUpdateSuccessfully_WhenValidInputProvided()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        var priceFormula = PriceFormula.Fixed(75.50m, "USD");
        var contractValue = Money.Dollar(75500); // 1000 MT * $75.50
        var profitMargin = Money.Dollar(5000);

        // Act
        contract.UpdatePricing(priceFormula, contractValue, profitMargin);

        // Assert
        contract.PriceFormula.Should().Be(priceFormula);
        contract.ContractValue.Should().Be(contractValue);
        contract.ProfitMargin.Should().Be(profitMargin);
        contract.DomainEvents.Should().Contain(e => e is SalesContractPricingUpdatedEvent);
    }

    [Fact]
    public void UpdatePricing_ShouldUpdateWithoutProfitMargin_WhenNotProvided()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        var priceFormula = PriceFormula.Fixed(75.50m, "USD");
        var contractValue = Money.Dollar(75500);

        // Act
        contract.UpdatePricing(priceFormula, contractValue);

        // Assert
        contract.PriceFormula.Should().Be(priceFormula);
        contract.ContractValue.Should().Be(contractValue);
        contract.ProfitMargin.Should().BeNull();
    }

    [Theory]
    [InlineData(ContractStatus.Completed)]
    [InlineData(ContractStatus.Cancelled)]
    public void UpdatePricing_ShouldThrowDomainException_WhenContractStatusInvalid(ContractStatus status)
    {
        // Arrange
        var contract = CreateValidSalesContract();
        SetContractStatus(contract, status);
        var priceFormula = PriceFormula.Fixed(75.50m, "USD");
        var contractValue = Money.Dollar(75500);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.UpdatePricing(priceFormula, contractValue));
        exception.Message.Should().Contain($"Cannot update pricing for contract in {status} status");
    }

    [Fact]
    public void UpdatePricing_ShouldThrowArgumentNullException_WhenPriceFormulaIsNull()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        var contractValue = Money.Dollar(75500);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => contract.UpdatePricing(null!, contractValue));
    }

    [Fact]
    public void UpdatePricing_ShouldThrowArgumentNullException_WhenContractValueIsNull()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        var priceFormula = PriceFormula.Fixed(75.50m, "USD");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => contract.UpdatePricing(priceFormula, null!));
    }

    #endregion

    #region Laycan Tests

    [Fact]
    public void UpdateLaycan_ShouldUpdateSuccessfully_WhenValidDatesProvided()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        var laycanStart = DateTime.UtcNow.AddDays(30);
        var laycanEnd = laycanStart.AddDays(10);

        // Act
        contract.UpdateLaycan(laycanStart, laycanEnd);

        // Assert
        contract.LaycanStart.Should().Be(laycanStart);
        contract.LaycanEnd.Should().Be(laycanEnd);
    }

    [Fact]
    public void UpdateLaycan_ShouldThrowDomainException_WhenStartIsAfterEnd()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        var laycanStart = DateTime.UtcNow.AddDays(40);
        var laycanEnd = DateTime.UtcNow.AddDays(30);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.UpdateLaycan(laycanStart, laycanEnd));
        exception.Message.Should().Be("Laycan start must be before laycan end");
    }

    [Fact]
    public void UpdateLaycan_ShouldThrowDomainException_WhenStartIsInPast()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        var laycanStart = DateTime.UtcNow.AddDays(-1);
        var laycanEnd = DateTime.UtcNow.AddDays(10);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.UpdateLaycan(laycanStart, laycanEnd));
        exception.Message.Should().Be("Laycan start cannot be in the past");
    }

    #endregion

    #region Port Updates Tests

    [Fact]
    public void UpdatePorts_ShouldUpdateSuccessfully_WhenValidPortsProvided()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        const string loadPort = "Houston";
        const string dischargePort = "Rotterdam";

        // Act
        contract.UpdatePorts(loadPort, dischargePort);

        // Assert
        contract.LoadPort.Should().Be(loadPort);
        contract.DischargePort.Should().Be(dischargePort);
    }

    [Fact]
    public void UpdatePorts_ShouldTrimWhitespace_WhenPortsHaveSpaces()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        const string loadPort = "  Houston  ";
        const string dischargePort = "  Rotterdam  ";

        // Act
        contract.UpdatePorts(loadPort, dischargePort);

        // Assert
        contract.LoadPort.Should().Be("Houston");
        contract.DischargePort.Should().Be("Rotterdam");
    }

    [Theory]
    [InlineData("", "Rotterdam")]
    [InlineData("   ", "Rotterdam")]
    [InlineData(null, "Rotterdam")]
    public void UpdatePorts_ShouldThrowDomainException_WhenLoadPortIsInvalid(string invalidLoadPort, string dischargePort)
    {
        // Arrange
        var contract = CreateValidSalesContract();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.UpdatePorts(invalidLoadPort, dischargePort));
        exception.Message.Should().Be("Load port cannot be empty");
    }

    [Theory]
    [InlineData("Houston", "")]
    [InlineData("Houston", "   ")]
    [InlineData("Houston", null)]
    public void UpdatePorts_ShouldThrowDomainException_WhenDischargePortIsInvalid(string loadPort, string invalidDischargePort)
    {
        // Arrange
        var contract = CreateValidSalesContract();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.UpdatePorts(loadPort, invalidDischargePort));
        exception.Message.Should().Be("Discharge port cannot be empty");
    }

    #endregion

    #region Contract Status Management Tests

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingApproval)]
    public void Activate_ShouldActivateSuccessfully_WhenValidStatusAndCompleteData(ContractStatus initialStatus)
    {
        // Arrange
        var contract = CreateCompleteValidSalesContract();
        SetContractStatus(contract, initialStatus);

        // Act
        contract.Activate();

        // Assert
        contract.Status.Should().Be(ContractStatus.Active);
        contract.DomainEvents.Should().Contain(e => e is SalesContractActivatedEvent);
    }

    [Theory]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Completed)]
    [InlineData(ContractStatus.Cancelled)]
    public void Activate_ShouldThrowDomainException_WhenInvalidStatus(ContractStatus invalidStatus)
    {
        // Arrange
        var contract = CreateCompleteValidSalesContract();
        SetContractStatus(contract, invalidStatus);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.Activate());
        exception.Message.Should().Contain($"Cannot activate contract from {invalidStatus} status");
    }

    [Fact]
    public void Activate_ShouldThrowDomainException_WhenMissingRequiredData()
    {
        // Arrange
        var contract = CreateValidSalesContract(); // Missing required data for activation

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.Activate());
        exception.Message.Should().Contain("Contract validation failed");
        exception.Message.Should().Contain("Valid price formula is required");
    }

    [Fact]
    public void Complete_ShouldCompleteSuccessfully_WhenActive()
    {
        // Arrange
        var contract = CreateCompleteValidSalesContract();
        contract.Activate();

        // Act
        contract.Complete();

        // Assert
        contract.Status.Should().Be(ContractStatus.Completed);
        contract.DomainEvents.Should().Contain(e => e is SalesContractCompletedEvent);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingApproval)]
    [InlineData(ContractStatus.Completed)]
    [InlineData(ContractStatus.Cancelled)]
    public void Complete_ShouldThrowDomainException_WhenNotActive(ContractStatus invalidStatus)
    {
        // Arrange
        var contract = CreateValidSalesContract();
        SetContractStatus(contract, invalidStatus);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.Complete());
        exception.Message.Should().Contain($"Cannot complete contract from {invalidStatus} status");
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingApproval)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Cancelled)]
    public void Cancel_ShouldCancelSuccessfully_WhenNotCompleted(ContractStatus initialStatus)
    {
        // Arrange
        var contract = CreateValidSalesContract();
        SetContractStatus(contract, initialStatus);
        const string reason = "Customer cancelled order";

        // Act
        contract.Cancel(reason);

        // Assert
        contract.Status.Should().Be(ContractStatus.Cancelled);
        contract.Notes.Should().Contain(reason);
        contract.DomainEvents.Should().Contain(e => e is SalesContractCancelledEvent);
    }

    [Fact]
    public void Cancel_ShouldThrowDomainException_WhenCompleted()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        SetContractStatus(contract, ContractStatus.Completed);
        const string reason = "Test cancellation";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.Cancel(reason));
        exception.Message.Should().Be("Cannot cancel completed contract");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Cancel_ShouldThrowDomainException_WhenReasonIsInvalid(string invalidReason)
    {
        // Arrange
        var contract = CreateValidSalesContract();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.Cancel(invalidReason));
        exception.Message.Should().Be("Cancellation reason is required");
    }

    #endregion

    #region Profit Calculation Tests

    [Fact]
    public void CalculateProfitMargin_ShouldReturnCorrectProfit_WhenBothContractValuesAvailable()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        var purchaseContract = CreateMockPurchaseContract();
        SetLinkedPurchaseContract(contract, purchaseContract);
        
        var salesValue = Money.Dollar(100000);
        var purchaseValue = Money.Dollar(85000);
        
        contract.UpdatePricing(PriceFormula.Fixed(100m), salesValue);
        SetPurchaseContractValue(purchaseContract, purchaseValue);

        // Act
        var profit = contract.CalculateProfitMargin();

        // Assert
        profit.Should().NotBeNull();
        profit!.Amount.Should().Be(15000); // $100,000 - $85,000
        profit.Currency.Should().Be("USD");
    }

    [Fact]
    public void CalculateProfitMargin_ShouldReturnNull_WhenLinkedContractMissing()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        contract.UpdatePricing(PriceFormula.Fixed(100m), Money.Dollar(100000));

        // Act
        var profit = contract.CalculateProfitMargin();

        // Assert
        profit.Should().BeNull();
    }

    [Fact]
    public void CalculateProfitMarginPercentage_ShouldReturnCorrectPercentage_WhenBothValuesAvailable()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        var purchaseContract = CreateMockPurchaseContract();
        SetLinkedPurchaseContract(contract, purchaseContract);
        
        var salesValue = Money.Dollar(110000);
        var purchaseValue = Money.Dollar(100000);
        
        contract.UpdatePricing(PriceFormula.Fixed(110m), salesValue);
        SetPurchaseContractValue(purchaseContract, purchaseValue);

        // Act
        var profitPercentage = contract.CalculateProfitMarginPercentage();

        // Assert
        profitPercentage.Should().NotBeNull();
        profitPercentage.Should().Be(10.0m); // ($110,000 - $100,000) / $100,000 * 100 = 10%
    }

    [Fact]
    public void CalculateProfitMarginPercentage_ShouldReturnNull_WhenNoPurchaseContractValue()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        contract.UpdatePricing(PriceFormula.Fixed(100m), Money.Dollar(100000));

        // Act
        var profitPercentage = contract.CalculateProfitMarginPercentage();

        // Assert
        profitPercentage.Should().BeNull();
    }

    #endregion

    #region Trade Group Management Tests

    [Fact]
    public void AssignToTradeGroup_ShouldAssignSuccessfully_WhenValidStatus()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        var tradeGroupId = Guid.NewGuid();
        const string updatedBy = "TestUser";

        // Act
        contract.AssignToTradeGroup(tradeGroupId, updatedBy);

        // Assert
        contract.TradeGroupId.Should().Be(tradeGroupId);
        contract.DomainEvents.Should().Contain(e => e is ContractAddedToTradeGroupEvent);
    }

    [Theory]
    [InlineData(ContractStatus.Completed)]
    [InlineData(ContractStatus.Cancelled)]
    public void AssignToTradeGroup_ShouldThrowDomainException_WhenInvalidStatus(ContractStatus invalidStatus)
    {
        // Arrange
        var contract = CreateValidSalesContract();
        SetContractStatus(contract, invalidStatus);
        var tradeGroupId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => contract.AssignToTradeGroup(tradeGroupId));
        exception.Message.Should().Contain("Cannot assign completed or cancelled contract to trade group");
    }

    [Fact]
    public void RemoveFromTradeGroup_ShouldRemoveSuccessfully_WhenAssigned()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        var tradeGroupId = Guid.NewGuid();
        contract.AssignToTradeGroup(tradeGroupId);

        // Act
        contract.RemoveFromTradeGroup();

        // Assert
        contract.TradeGroupId.Should().BeNull();
        contract.DomainEvents.Should().Contain(e => e is ContractRemovedFromTradeGroupEvent);
    }

    [Fact]
    public void RemoveFromTradeGroup_ShouldDoNothing_WhenNotAssigned()
    {
        // Arrange
        var contract = CreateValidSalesContract();

        // Act
        contract.RemoveFromTradeGroup();

        // Assert
        contract.TradeGroupId.Should().BeNull();
        // Should not throw exception or add domain events
    }

    #endregion

    #region Tag Management Tests

    [Fact]
    public void HasTag_ShouldReturnTrue_WhenTagExists()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        var tagId = Guid.NewGuid();
        AddMockContractTag(contract, tagId, "High Risk");

        // Act & Assert
        contract.HasTag(tagId).Should().BeTrue();
        contract.HasTag("High Risk").Should().BeTrue();
        contract.HasTag("high risk").Should().BeTrue(); // Case insensitive
    }

    [Fact]
    public void HasTag_ShouldReturnFalse_WhenTagDoesNotExist()
    {
        // Arrange
        var contract = CreateValidSalesContract();

        // Act & Assert
        contract.HasTag(Guid.NewGuid()).Should().BeFalse();
        contract.HasTag("NonExistent").Should().BeFalse();
    }

    [Fact]
    public void IsHighRisk_ShouldReturnTrue_WhenHighRiskTagExists()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        AddMockContractTag(contract, Guid.NewGuid(), "High Risk");

        // Act & Assert
        contract.IsHighRisk().Should().BeTrue();
    }

    [Fact]
    public void IsUrgent_ShouldReturnTrue_WhenUrgentTagExists()
    {
        // Arrange
        var contract = CreateValidSalesContract();
        AddMockContractTag(contract, Guid.NewGuid(), "Urgent");

        // Act & Assert
        contract.IsUrgent().Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private SalesContract CreateValidSalesContract()
    {
        return new SalesContract(
            _contractNumber,
            ContractType.CARGO,
            _tradingPartnerId,
            _productId,
            _traderId,
            _contractQuantity);
    }

    private SalesContract CreateCompleteValidSalesContract()
    {
        var contract = CreateValidSalesContract();
        
        // Add required data for activation
        var priceFormula = PriceFormula.Fixed(75.50m, "USD");
        var contractValue = Money.Dollar(75500);
        contract.UpdatePricing(priceFormula, contractValue);
        
        var laycanStart = DateTime.UtcNow.AddDays(30);
        var laycanEnd = laycanStart.AddDays(10);
        contract.UpdateLaycan(laycanStart, laycanEnd);
        
        contract.UpdatePorts("Houston", "Rotterdam");
        
        SetPaymentTerms(contract, "30 days");
        
        return contract;
    }

    private static void SetContractStatus(SalesContract contract, ContractStatus status)
    {
        var statusProperty = typeof(SalesContract).GetProperty(nameof(SalesContract.Status));
        statusProperty?.SetValue(contract, status);
    }

    private static void SetPaymentTerms(SalesContract contract, string paymentTerms)
    {
        var paymentTermsProperty = typeof(SalesContract).GetProperty(nameof(SalesContract.PaymentTerms));
        paymentTermsProperty?.SetValue(contract, paymentTerms);
    }

    private static PurchaseContract CreateMockPurchaseContract()
    {
        // Create a mock purchase contract for testing
        var contractNumber = ContractNumber.Create(2024, ContractType.CARGO, 2);
        var quantity = Quantity.MetricTons(1000);
        
        return new PurchaseContract(
            contractNumber,
            ContractType.CARGO,
            Guid.NewGuid(), // supplierId
            Guid.NewGuid(), // productId
            Guid.NewGuid(), // traderId
            quantity);
    }

    private static void SetLinkedPurchaseContract(SalesContract salesContract, PurchaseContract purchaseContract)
    {
        var linkedContractProperty = typeof(SalesContract).GetProperty(nameof(SalesContract.LinkedPurchaseContract));
        linkedContractProperty?.SetValue(salesContract, purchaseContract);
    }

    private static void SetPurchaseContractValue(PurchaseContract purchaseContract, Money value)
    {
        var priceFormula = PriceFormula.Fixed(value.Amount / 1000, value.Currency); // Assuming 1000 MT
        purchaseContract.UpdatePricing(priceFormula, value);
    }

    private static void AddMockContractTag(SalesContract contract, Guid tagId, string tagName)
    {
        var tag = new Tag(tagName, TagCategory.RiskLevel);
        var tagIdProperty = typeof(Tag).GetProperty(nameof(Tag.Id));
        tagIdProperty?.SetValue(tag, tagId);

        var contractTag = new ContractTag(contract.Id, nameof(SalesContract), tagId, null, "TestUser");
        
        var contractTagsProperty = typeof(SalesContract).GetProperty(nameof(SalesContract.ContractTags));
        var contractTags = (ICollection<ContractTag>)contractTagsProperty?.GetValue(contract)!;
        contractTags.Add(contractTag);
    }

    #endregion
}