using FluentAssertions;
using OilTrading.Core.ValueObjects;
using Xunit;

namespace OilTrading.UnitTests.Core.ValueObjects;

public class MoneyTests
{
    [Theory]
    [InlineData(100.50, "USD")]
    [InlineData(0, "EUR")]
    [InlineData(999999.99, "GBP")]
    public void Money_ShouldBeCreated_WithValidData(decimal amount, string currency)
    {
        // Act
        var money = new Money(amount, currency);
        
        // Assert
        money.Amount.Should().Be(amount);
        money.Currency.Should().Be(currency);
    }

    [Theory]
    [InlineData(-1, "USD")]
    [InlineData(100, "")]
    [InlineData(100, null)]
    public void Money_WithInvalidData_ShouldThrowException(decimal amount, string currency)
    {
        // Act & Assert
        var action = () => new Money(amount, currency);
        action.Should().Throw<OilTrading.Core.Common.DomainException>();
    }

    [Fact]
    public void Money_Addition_ShouldWork_WithSameCurrency()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "USD");
        
        // Act
        var result = money1 + money2;
        
        // Assert
        result.Amount.Should().Be(150);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Addition_ShouldThrow_WithDifferentCurrencies()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "EUR");
        
        // Act & Assert
        var action = () => money1 + money2;
        action.Should().Throw<OilTrading.Core.Common.DomainException>()
            .WithMessage("Cannot add different currencies*");
    }

    [Fact]
    public void Money_Subtraction_ShouldWork_WithSameCurrency()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(30, "USD");
        
        // Act
        var result = money1 - money2;
        
        // Assert
        result.Amount.Should().Be(70);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Equality_ShouldWork_Correctly()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");
        var money3 = new Money(100, "EUR");
        var money4 = new Money(50, "USD");
        
        // Assert
        money1.Should().Be(money2);
        money1.Should().NotBe(money3);
        money1.Should().NotBe(money4);
        (money1 == money2).Should().BeTrue();
        (money1 != money3).Should().BeTrue();
    }

    [Fact]
    public void Money_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var money = new Money(123.45m, "USD");
        
        // Act
        var result = money.ToString();
        
        // Assert
        result.Should().Be("123.45 USD");
    }

    [Fact]
    public void Money_Multiply_ShouldWork_WithScalar()
    {
        // Arrange
        var money = new Money(50, "USD");
        
        // Act
        var result = money * 3;
        
        // Assert
        result.Amount.Should().Be(150);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Divide_ShouldWork_WithScalar()
    {
        // Arrange
        var money = new Money(150, "USD");
        
        // Act
        var result = money / 3;
        
        // Assert
        result.Amount.Should().Be(50);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_DivideByZero_ShouldThrowException()
    {
        // Arrange
        var money = new Money(100, "USD");
        
        // Act & Assert
        var action = () => money / 0;
        action.Should().Throw<OilTrading.Core.Common.DomainException>()
            .WithMessage("Cannot divide by zero");
    }
}