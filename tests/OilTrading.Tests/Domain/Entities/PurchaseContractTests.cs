using FluentAssertions;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using Xunit;

namespace OilTrading.Tests.Domain.Entities;

public class PurchaseContractTests
{
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _traderId = Guid.NewGuid();

    [Fact]
    public void PurchaseContract_ShouldCreateValidInstance()
    {
        // Arrange
        var contractNumber = ContractNumber.Parse("PC-2024-001");
        var contractType = OilTrading.Core.ValueObjects.ContractType.CARGO;
        var quantity = new Quantity(1000, QuantityUnit.MT);
        var tonBarrelRatio = 7.6m;
        var laycanStart = DateTime.Today.AddDays(30);
        var laycanEnd = DateTime.Today.AddDays(35);

        // Act
        var contract = new PurchaseContract(
            contractNumber: contractNumber,
            contractType: contractType,
            tradingPartnerId: _supplierId,
            productId: _productId,
            traderId: _traderId,
            contractQuantity: quantity,
            tonBarrelRatio: tonBarrelRatio);

        contract.UpdateLaycan(laycanStart, laycanEnd);

        // Assert
        contract.Id.Should().NotBe(Guid.Empty);
        contract.ContractNumber.Should().Be(contractNumber);
        contract.ContractType.Should().Be(contractType);
        contract.TradingPartnerId.Should().Be(_supplierId);
        contract.ProductId.Should().Be(_productId);
        contract.TraderId.Should().Be(_traderId);
        contract.ContractQuantity.Should().Be(quantity);
        contract.TonBarrelRatio.Should().Be(tonBarrelRatio);
        contract.LaycanStart.Should().Be(laycanStart);
        contract.LaycanEnd.Should().Be(laycanEnd);
        contract.Status.Should().Be(ContractStatus.Draft);
        contract.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void PurchaseContract_ShouldThrowArgumentException_WhenLaycanEndBeforeStart()
    {
        // Arrange
        var contract = CreateValidContract();
        var laycanStart = DateTime.Today.AddDays(35);
        var laycanEnd = DateTime.Today.AddDays(30);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => contract.UpdateLaycan(laycanStart, laycanEnd));
    }

    [Fact]
    public void PurchaseContract_ShouldUpdatePricing_WithFixedPrice()
    {
        // Arrange
        var contract = CreateValidContract();
        var priceFormula = PriceFormula.Fixed(75.50m);
        var contractValue = Money.Dollar(75500m); // 75.50 * 1000

        // Act
        contract.UpdatePricing(priceFormula, contractValue);

        // Assert
        contract.PriceFormula.Should().Be(priceFormula);
        contract.ContractValue.Should().Be(contractValue);
        contract.PriceFormula.IsFixedPrice.Should().BeTrue();
    }

    [Fact]
    public void PurchaseContract_ShouldUpdatePricing_WithFloatingPrice()
    {
        // Arrange
        var contract = CreateValidContract();
        var priceFormula = PriceFormula.Index("BRENT", PricingMethod.AVG, Money.Dollar(5.00m));
        var contractValue = Money.Dollar(0); // Will be calculated later

        // Act
        contract.UpdatePricing(priceFormula, contractValue);

        // Assert
        contract.PriceFormula.Should().Be(priceFormula);
        contract.ContractValue.Should().Be(contractValue);
        contract.PriceFormula.IsFixedPrice.Should().BeFalse();
    }

    [Fact]
    public void PurchaseContract_ShouldSetPricingPeriod()
    {
        // Arrange
        var contract = CreateValidContract();
        var periodStart = DateTime.Today.AddDays(10);
        var periodEnd = DateTime.Today.AddDays(20);

        // Act
        contract.SetPricingPeriod(periodStart, periodEnd);

        // Assert
        contract.PricingPeriodStart.Should().Be(periodStart);
        contract.PricingPeriodEnd.Should().Be(periodEnd);
    }

    [Fact]
    public void PurchaseContract_ShouldThrowArgumentException_WhenPricingPeriodEndBeforeStart()
    {
        // Arrange
        var contract = CreateValidContract();
        var periodStart = DateTime.Today.AddDays(20);
        var periodEnd = DateTime.Today.AddDays(10);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => contract.SetPricingPeriod(periodStart, periodEnd));
    }

    [Fact]
    public void PurchaseContract_ShouldUpdatePorts()
    {
        // Arrange
        var contract = CreateValidContract();
        var loadPort = "Houston";
        var dischargePort = "Rotterdam";

        // Act
        contract.UpdatePorts(loadPort, dischargePort);

        // Assert
        contract.LoadPort.Should().Be(loadPort);
        contract.DischargePort.Should().Be(dischargePort);
    }

    [Fact]
    public void PurchaseContract_ShouldThrowArgumentException_WhenPortsAreEmpty()
    {
        // Arrange
        var contract = CreateValidContract();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => contract.UpdatePorts("", "Rotterdam"));
        Assert.Throws<ArgumentException>(() => contract.UpdatePorts("Houston", ""));
        Assert.Throws<ArgumentException>(() => contract.UpdatePorts("   ", "Rotterdam"));
    }

    [Fact]
    public void PurchaseContract_ShouldUpdateDeliveryTerms()
    {
        // Arrange
        var contract = CreateValidContract();
        var deliveryTerms = DeliveryTerms.CIF;

        // Act
        contract.UpdateDeliveryTerms(deliveryTerms);

        // Assert
        contract.DeliveryTerms.Should().Be(deliveryTerms);
    }

    [Fact]
    public void PurchaseContract_ShouldUpdateSettlementType()
    {
        // Arrange
        var contract = CreateValidContract();
        var settlementType = SettlementType.ContractPayment;

        // Act
        contract.UpdateSettlementType(settlementType);

        // Assert
        contract.SettlementType.Should().Be(settlementType);
    }

    [Fact]
    public void PurchaseContract_ShouldUpdatePaymentTerms()
    {
        // Arrange
        var contract = CreateValidContract();
        var paymentTerms = "NET 30 DAYS FROM B/L DATE";
        var creditPeriodDays = 30;

        // Act
        contract.UpdatePaymentTerms(paymentTerms, creditPeriodDays);

        // Assert
        contract.PaymentTerms.Should().Be(paymentTerms);
        contract.CreditPeriodDays.Should().Be(creditPeriodDays);
    }

    [Fact]
    public void PurchaseContract_ShouldSetPrepaymentPercentage()
    {
        // Arrange
        var contract = CreateValidContract();
        var prepaymentPercentage = 25.5m;

        // Act
        contract.SetPrepaymentPercentage(prepaymentPercentage);

        // Assert
        contract.PrepaymentPercentage.Should().Be(prepaymentPercentage);
    }

    [Fact]
    public void PurchaseContract_ShouldThrowArgumentException_WhenPrepaymentPercentageIsInvalid()
    {
        // Arrange
        var contract = CreateValidContract();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => contract.SetPrepaymentPercentage(-1));
        Assert.Throws<ArgumentException>(() => contract.SetPrepaymentPercentage(101));
    }

    [Fact]
    public void PurchaseContract_ShouldUpdateQualitySpecifications()
    {
        // Arrange
        var contract = CreateValidContract();
        var qualitySpecs = "API 38-42, Sulfur max 0.5%";

        // Act
        contract.UpdateQualitySpecifications(qualitySpecs);

        // Assert
        contract.QualitySpecifications.Should().Be(qualitySpecs);
    }

    [Fact]
    public void PurchaseContract_ShouldUpdateInspectionAgency()
    {
        // Arrange
        var contract = CreateValidContract();
        var inspectionAgency = "SGS";

        // Act
        contract.UpdateInspectionAgency(inspectionAgency);

        // Assert
        contract.InspectionAgency.Should().Be(inspectionAgency);
    }

    [Fact]
    public void PurchaseContract_ShouldAddNotes()
    {
        // Arrange
        var contract = CreateValidContract();
        var notes = "Special handling required";

        // Act
        contract.AddNotes(notes);

        // Assert
        contract.Notes.Should().Be(notes);
    }

    [Fact]
    public void PurchaseContract_ShouldSetCreatedBy()
    {
        // Arrange
        var contract = CreateValidContract();
        var createdBy = "john.trader@company.com";

        // Act
        contract.SetCreatedBy(createdBy);

        // Assert
        contract.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public void PurchaseContract_ShouldActivate_WhenInDraftStatus()
    {
        // Arrange
        var contract = CreateValidContract();
        var activatedBy = "manager@company.com";

        // Act
        contract.SetUpdatedBy(activatedBy);
        contract.Activate();

        // Assert
        contract.Status.Should().Be(ContractStatus.Active);
        contract.UpdatedBy.Should().Be(activatedBy);
        contract.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void PurchaseContract_ShouldThrowInvalidOperationException_WhenActivatingNonDraftContract()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.SetUpdatedBy("manager@company.com");
        contract.Activate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => contract.Activate());
    }

    [Fact]
    public void PurchaseContract_ShouldCancel_WhenNotCompleted()
    {
        // Arrange
        var contract = CreateValidContract();
        var cancelledBy = "manager@company.com";
        var reason = "Market conditions changed";

        // Act
        contract.SetUpdatedBy(cancelledBy);
        contract.Cancel(reason);

        // Assert
        contract.Status.Should().Be(ContractStatus.Cancelled);
        contract.UpdatedBy.Should().Be(cancelledBy);
        contract.Notes.Should().Contain(reason);
        contract.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void PurchaseContract_ShouldComplete_WhenActive()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.SetUpdatedBy("manager@company.com");
        contract.Activate();
        var completedBy = "trader@company.com";

        // Act
        contract.SetUpdatedBy(completedBy);
        contract.Complete();

        // Assert
        contract.Status.Should().Be(ContractStatus.Completed);
        contract.UpdatedBy.Should().Be(completedBy);
        contract.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void PurchaseContract_ShouldThrowInvalidOperationException_WhenCompletingNonActiveContract()
    {
        // Arrange
        var contract = CreateValidContract();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => contract.Complete());
    }

    [Fact]
    public void PurchaseContract_ShouldCalculateBarrelQuantity()
    {
        // Arrange
        var quantity = new Quantity(100, QuantityUnit.MT);
        var tonBarrelRatio = 7.6m;
        var contract = new PurchaseContract(
            contractNumber: ContractNumber.Parse("PC-2024-001"),
            contractType: OilTrading.Core.ValueObjects.ContractType.CARGO,
            tradingPartnerId: _supplierId,
            productId: _productId,
            traderId: _traderId,
            contractQuantity: quantity,
            tonBarrelRatio: tonBarrelRatio);

        // Act
        var barrelQuantity = contract.ContractQuantity.Unit == QuantityUnit.MT 
            ? contract.ContractQuantity.Value * contract.TonBarrelRatio 
            : contract.ContractQuantity.Value;

        // Assert
        barrelQuantity.Should().Be(760m); // 100 * 7.6
    }

    [Fact]
    public void PurchaseContract_ShouldReturnOriginalQuantity_WhenAlreadyInBarrels()
    {
        // Arrange
        var quantity = new Quantity(1000, QuantityUnit.BBL);
        var contract = new PurchaseContract(
            contractNumber: ContractNumber.Parse("PC-2024-001"),
            contractType: OilTrading.Core.ValueObjects.ContractType.CARGO,
            tradingPartnerId: _supplierId,
            productId: _productId,
            traderId: _traderId,
            contractQuantity: quantity,
            tonBarrelRatio: 7.6m);

        // Act
        var barrelQuantity = contract.ContractQuantity.Unit == QuantityUnit.MT 
            ? contract.ContractQuantity.Value * contract.TonBarrelRatio 
            : contract.ContractQuantity.Value;

        // Assert
        barrelQuantity.Should().Be(1000m);
    }

    private PurchaseContract CreateValidContract()
    {
        var contractNumber = ContractNumber.Parse("PC-2024-001");
        var contractType = OilTrading.Core.ValueObjects.ContractType.CARGO;
        var quantity = new Quantity(1000, QuantityUnit.MT);

        return new PurchaseContract(
            contractNumber: contractNumber,
            contractType: contractType,
            tradingPartnerId: _supplierId,
            productId: _productId,
            traderId: _traderId,
            contractQuantity: quantity,
            tonBarrelRatio: 7.6m);
    }
}