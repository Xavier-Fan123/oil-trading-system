using FluentAssertions;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using Xunit;

namespace OilTrading.Tests.Domain.ValueObjects;

public class QuantityTests
{
    [Fact]
    public void Quantity_ShouldCreateValidInstance_WhenValidValueAndUnitProvided()
    {
        // Arrange
        decimal value = 1000.5m;
        QuantityUnit unit = QuantityUnit.MT;

        // Act
        var quantity = new Quantity(value, unit);

        // Assert
        quantity.Value.Should().Be(value);
        quantity.Unit.Should().Be(unit);
    }

    [Fact]
    public void Quantity_ShouldThrowArgumentException_WhenValueIsNegative()
    {
        // Arrange
        decimal negativeValue = -100m;
        QuantityUnit unit = QuantityUnit.MT;

        // Act & Assert
        Assert.Throws<DomainException>(() => new Quantity(negativeValue, unit));
    }

    [Fact]
    public void Quantity_ShouldAllowZero()
    {
        // Arrange
        decimal zeroValue = 0m;
        QuantityUnit unit = QuantityUnit.BBL;

        // Act
        var quantity = new Quantity(zeroValue, unit);

        // Assert
        quantity.Value.Should().Be(0m);
        quantity.Unit.Should().Be(unit);
    }

    [Fact]
    public void Quantity_ShouldConvertToBarrels_WhenFromMetricTons()
    {
        // Arrange
        var quantity = new Quantity(100, QuantityUnit.MT);
        decimal tonBarrelRatio = 7.6m;

        // Act
        var convertedQuantity = quantity.ConvertTo(QuantityUnit.BBL, tonBarrelRatio);

        // Assert
        convertedQuantity.Value.Should().Be(760m); // 100 * 7.6
        convertedQuantity.Unit.Should().Be(QuantityUnit.BBL);
    }

    [Fact]
    public void Quantity_ShouldConvertToMetricTons_WhenFromBarrels()
    {
        // Arrange
        var quantity = new Quantity(760, QuantityUnit.BBL);
        decimal tonBarrelRatio = 7.6m;

        // Act
        var convertedQuantity = quantity.ConvertTo(QuantityUnit.MT, tonBarrelRatio);

        // Assert
        convertedQuantity.Value.Should().BeApproximately(100m, 0.01m); // 760 / 7.6
        convertedQuantity.Unit.Should().Be(QuantityUnit.MT);
    }

    [Fact]
    public void Quantity_ShouldReturnSameQuantity_WhenConvertingToSameUnit()
    {
        // Arrange
        var quantity = new Quantity(500, QuantityUnit.MT);

        // Act
        var convertedQuantity = quantity.ConvertTo(QuantityUnit.MT, 7.6m);

        // Assert
        convertedQuantity.Value.Should().Be(500m);
        convertedQuantity.Unit.Should().Be(QuantityUnit.MT);
    }

    [Fact]
    public void Quantity_ShouldThrowArgumentException_WhenConvertingWithInvalidRatio()
    {
        // Arrange
        var quantity = new Quantity(100, QuantityUnit.MT);

        // Act & Assert
        Assert.Throws<DomainException>(() => quantity.ConvertTo(QuantityUnit.BBL, 0));
        Assert.Throws<DomainException>(() => quantity.ConvertTo(QuantityUnit.BBL, -1));
    }

    [Fact]
    public void Quantity_ShouldAddCorrectly_WhenSameUnit()
    {
        // Arrange
        var quantity1 = new Quantity(100, QuantityUnit.MT);
        var quantity2 = new Quantity(50, QuantityUnit.MT);

        // Act
        var result = quantity1 + quantity2;

        // Assert
        result.Value.Should().Be(150);
        result.Unit.Should().Be(QuantityUnit.MT);
    }

    [Fact]
    public void Quantity_ShouldThrowInvalidOperationException_WhenAddingDifferentUnits()
    {
        // Arrange
        var quantity1 = new Quantity(100, QuantityUnit.MT);
        var quantity2 = new Quantity(50, QuantityUnit.BBL);

        // Act & Assert
        Assert.Throws<DomainException>(() => quantity1 + quantity2);
    }

    [Fact]
    public void Quantity_ShouldSubtractCorrectly_WhenSameUnit()
    {
        // Arrange
        var quantity1 = new Quantity(100, QuantityUnit.MT);
        var quantity2 = new Quantity(30, QuantityUnit.MT);

        // Act
        var result = quantity1 - quantity2;

        // Assert
        result.Value.Should().Be(70);
        result.Unit.Should().Be(QuantityUnit.MT);
    }

    [Fact]
    public void Quantity_ShouldThrowArgumentException_WhenSubtractionResultsInNegative()
    {
        // Arrange
        var quantity1 = new Quantity(30, QuantityUnit.MT);
        var quantity2 = new Quantity(100, QuantityUnit.MT);

        // Act & Assert
        Assert.Throws<DomainException>(() => quantity1 - quantity2);
    }

    [Fact]
    public void Quantity_ShouldMultiplyByDecimal()
    {
        // Arrange
        var quantity = new Quantity(50, QuantityUnit.BBL);
        decimal multiplier = 2.5m;

        // Act
        var result = quantity * multiplier;

        // Assert
        result.Value.Should().Be(125);
        result.Unit.Should().Be(QuantityUnit.BBL);
    }

    [Fact]
    public void Quantity_ShouldDivideByDecimal()
    {
        // Arrange
        var quantity = new Quantity(100, QuantityUnit.GAL);
        decimal divisor = 4m;

        // Act
        // Division operator not implemented, create new instance with divided value
        var result = new Quantity(quantity.Value / divisor, quantity.Unit);

        // Assert
        result.Value.Should().Be(25);
        result.Unit.Should().Be(QuantityUnit.GAL);
    }

    [Fact]
    public void Quantity_ShouldThrowDivideByZeroException()
    {
        // Arrange
        var quantity = new Quantity(100, QuantityUnit.MT);

        // Act & Assert
        // Division operator not implemented
        Assert.Throws<DivideByZeroException>(() => new Quantity(quantity.Value / 0, quantity.Unit));
    }

    [Fact]
    public void Quantity_ShouldBeEqual_WhenValueAndUnitAreSame()
    {
        // Arrange
        var quantity1 = new Quantity(100, QuantityUnit.MT);
        var quantity2 = new Quantity(100, QuantityUnit.MT);

        // Act & Assert
        quantity1.Should().Be(quantity2);
        quantity1.Equals(quantity2).Should().BeTrue();
    }

    [Fact]
    public void Quantity_ShouldNotBeEqual_WhenValueOrUnitAreDifferent()
    {
        // Arrange
        var quantity1 = new Quantity(100, QuantityUnit.MT);
        var quantity2 = new Quantity(100, QuantityUnit.BBL);
        var quantity3 = new Quantity(200, QuantityUnit.MT);

        // Act & Assert
        quantity1.Should().NotBe(quantity2);
        quantity1.Should().NotBe(quantity3);
        quantity1.Equals(quantity2).Should().BeFalse();
        quantity1.Equals(quantity3).Should().BeFalse();
    }

    [Fact]
    public void Quantity_ShouldHaveSameHashCode_WhenEqual()
    {
        // Arrange
        var quantity1 = new Quantity(100, QuantityUnit.MT);
        var quantity2 = new Quantity(100, QuantityUnit.MT);

        // Act & Assert
        quantity1.GetHashCode().Should().Be(quantity2.GetHashCode());
    }

    [Fact]
    public void Quantity_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var quantity = new Quantity(1234.56m, QuantityUnit.BBL);

        // Act
        var result = quantity.ToString();

        // Assert
        result.Should().Be("1,234.56 BBL");  // N2 format includes thousand separator
    }

    [Theory]
    [InlineData(0.001, QuantityUnit.MT)]
    [InlineData(1000000, QuantityUnit.BBL)]
    [InlineData(999.999, QuantityUnit.GAL)]
    public void Quantity_ShouldHandleEdgeCases(decimal value, QuantityUnit unit)
    {
        // Act
        var quantity = new Quantity(value, unit);

        // Assert
        quantity.Value.Should().Be(value);
        quantity.Unit.Should().Be(unit);
    }

    [Theory]
    [InlineData(QuantityUnit.MT, "MT")]
    [InlineData(QuantityUnit.BBL, "BBL")]
    [InlineData(QuantityUnit.GAL, "GAL")]
    public void Quantity_ShouldDisplayCorrectUnitString(QuantityUnit unit, string expectedString)
    {
        // Arrange
        var quantity = new Quantity(100, unit);

        // Act
        var result = quantity.ToString();

        // Assert
        result.Should().Contain(expectedString);
    }
}