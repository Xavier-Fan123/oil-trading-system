using FluentAssertions;
using OilTrading.Core.ValueObjects;
using Xunit;

namespace OilTrading.UnitTests.Core.ValueObjects;

public class QuantityTests
{
    [Theory]
    [InlineData(1000, QuantityUnit.BBL)]
    [InlineData(500.5, QuantityUnit.MT)]
    [InlineData(1, QuantityUnit.GAL)]
    public void Quantity_ShouldBeCreated_WithValidData(decimal value, QuantityUnit unit)
    {
        // Act
        var quantity = new Quantity(value, unit);
        
        // Assert
        quantity.Value.Should().Be(value);
        quantity.Unit.Should().Be(unit);
    }

    [Theory]
    [InlineData(-100, QuantityUnit.MT)]
    public void Quantity_WithInvalidData_ShouldThrowException(decimal value, QuantityUnit unit)
    {
        // Act & Assert
        var action = () => new Quantity(value, unit);
        action.Should().Throw<OilTrading.Core.Common.DomainException>();
    }

    [Fact]
    public void Quantity_Addition_ShouldWork_WithSameUnit()
    {
        // Arrange
        var quantity1 = new Quantity(1000, QuantityUnit.BBL);
        var quantity2 = new Quantity(500, QuantityUnit.BBL);
        
        // Act
        var result = quantity1.Add(quantity2);
        
        // Assert
        result.Value.Should().Be(1500);
        result.Unit.Should().Be(QuantityUnit.BBL);
    }

    [Fact]
    public void Quantity_Addition_ShouldThrow_WithDifferentUnits()
    {
        // Arrange
        var quantity1 = new Quantity(1000, QuantityUnit.BBL);
        var quantity2 = new Quantity(500, QuantityUnit.MT);
        
        // Act & Assert
        var action = () => quantity1.Add(quantity2);
        action.Should().Throw<OilTrading.Core.Common.DomainException>()
            .WithMessage("*Cannot add different quantity units*");
    }

    [Fact]
    public void Quantity_Subtraction_ShouldWork_WithSameUnit()
    {
        // Arrange
        var quantity1 = new Quantity(1000, QuantityUnit.BBL);
        var quantity2 = new Quantity(300, QuantityUnit.BBL);
        
        // Act
        var result = quantity1.Subtract(quantity2);
        
        // Assert
        result.Value.Should().Be(700);
        result.Unit.Should().Be(QuantityUnit.BBL);
    }

    [Fact]
    public void Quantity_Equality_ShouldWork_Correctly()
    {
        // Arrange
        var quantity1 = new Quantity(1000, QuantityUnit.BBL);
        var quantity2 = new Quantity(1000, QuantityUnit.BBL);
        var quantity3 = new Quantity(1000, QuantityUnit.MT);
        var quantity4 = new Quantity(500, QuantityUnit.BBL);
        
        // Assert
        quantity1.Should().Be(quantity2);
        quantity1.Should().NotBe(quantity3);
        quantity1.Should().NotBe(quantity4);
    }

    [Fact]
    public void Quantity_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var quantity = new Quantity(1000.5m, QuantityUnit.BBL);
        
        // Act
        var result = quantity.ToString();
        
        // Assert
        result.Should().Contain("1000.5");
        result.Should().Contain("BBL");
    }

    [Fact]
    public void Quantity_Multiply_ShouldWork_WithScalar()
    {
        // Arrange
        var quantity = new Quantity(100, QuantityUnit.BBL);
        
        // Act
        var result = quantity.Multiply(2.5m);
        
        // Assert
        result.Value.Should().Be(250);
        result.Unit.Should().Be(QuantityUnit.BBL);
    }

    [Fact]
    public void Quantity_StaticFactories_ShouldWork()
    {
        // Act
        var barrels = Quantity.Barrels(1000);
        var metricTons = Quantity.MetricTons(500);
        
        // Assert
        barrels.Value.Should().Be(1000);
        barrels.Unit.Should().Be(QuantityUnit.BBL);
        metricTons.Value.Should().Be(500);
        metricTons.Unit.Should().Be(QuantityUnit.MT);
    }

    [Theory]
    [InlineData(1000, QuantityUnit.BBL, 7.6, 131.58, QuantityUnit.MT)]  // 1000 BBL = ~131.58 MT
    [InlineData(100, QuantityUnit.MT, 7.6, 760, QuantityUnit.BBL)]      // 100 MT = 760 BBL
    public void Quantity_Convert_ShouldWork_BetweenBBLAndMT(
        decimal inputValue, 
        QuantityUnit inputUnit, 
        decimal conversionFactor,
        decimal expectedValue, 
        QuantityUnit expectedUnit)
    {
        // Arrange
        var quantity = new Quantity(inputValue, inputUnit);
        
        // Act
        var result = quantity.ConvertTo(expectedUnit, conversionFactor);
        
        // Assert
        result.Value.Should().BeApproximately(expectedValue, 0.1m);
        result.Unit.Should().Be(expectedUnit);
    }

    [Fact]
    public void Quantity_Convert_WithUnsupportedUnits_ShouldThrowException()
    {
        // Arrange
        var quantity = new Quantity(1000, QuantityUnit.BBL);
        
        // Act & Assert
        var action = () => quantity.ConvertTo(QuantityUnit.GAL, 1);
        action.Should().Throw<OilTrading.Core.Common.DomainException>();
    }

    [Fact]
    public void Quantity_OperatorOverloads_ShouldWork()
    {
        // Arrange
        var quantity1 = new Quantity(1000, QuantityUnit.BBL);
        var quantity2 = new Quantity(500, QuantityUnit.BBL);
        
        // Act
        var sum = quantity1 + quantity2;
        var difference = quantity1 - quantity2;
        var product = quantity1 * 2;
        
        // Assert
        sum.Value.Should().Be(1500);
        sum.Unit.Should().Be(QuantityUnit.BBL);
        difference.Value.Should().Be(500);
        difference.Unit.Should().Be(QuantityUnit.BBL);
        product.Value.Should().Be(2000);
        product.Unit.Should().Be(QuantityUnit.BBL);
    }
}