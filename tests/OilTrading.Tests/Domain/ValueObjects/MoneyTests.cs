using FluentAssertions;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using Xunit;

namespace OilTrading.Tests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Money_ShouldCreateValidInstance_WhenValidAmountAndCurrencyProvided()
    {
        // Arrange
        decimal amount = 100.50m;
        string currency = "USD";

        // Act
        var money = new Money(amount, currency);

        // Assert
        money.Amount.Should().Be(amount);
        money.Currency.Should().Be(currency);
    }

    [Fact]
    public void Money_ShouldThrowArgumentException_WhenCurrencyIsNullOrEmpty()
    {
        // Arrange
        decimal amount = 100.50m;

        // Act & Assert
        Assert.Throws<DomainException>(() => new Money(amount, null!));
        Assert.Throws<DomainException>(() => new Money(amount, ""));
        Assert.Throws<DomainException>(() => new Money(amount, "   "));
    }

    [Fact]
    public void Money_ShouldCreateUSD_WhenUsingStaticMethod()
    {
        // Arrange
        decimal amount = 75.25m;

        // Act
        var money = Money.Dollar(amount);

        // Assert
        money.Amount.Should().Be(amount);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_ShouldCreateEUR_WhenUsingStaticMethod()
    {
        // Arrange
        decimal amount = 85.75m;

        // Act
        var money = Money.Euro(amount);

        // Assert
        money.Amount.Should().Be(amount);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Money_ShouldAddCorrectly_WhenSameCurrency()
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
    public void Money_ShouldThrowInvalidOperationException_WhenAddingDifferentCurrencies()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "EUR");

        // Act & Assert
        Assert.Throws<DomainException>(() => money1 + money2);
    }

    [Fact]
    public void Money_ShouldSubtractCorrectly_WhenSameCurrency()
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
    public void Money_ShouldMultiplyByDecimal()
    {
        // Arrange
        var money = new Money(50, "USD");
        decimal multiplier = 2.5m;

        // Act
        var result = money * multiplier;

        // Assert
        result.Amount.Should().Be(125);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_ShouldDivideByDecimal()
    {
        // Arrange
        var money = new Money(100, "USD");
        decimal divisor = 4m;

        // Act
        var result = money / divisor;

        // Assert
        result.Amount.Should().Be(25);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_ShouldThrowDivideByZeroException()
    {
        // Arrange
        var money = new Money(100, "USD");

        // Act & Assert
        Assert.Throws<DomainException>(() => money / 0);
    }

    [Fact]
    public void Money_ShouldBeEqual_WhenAmountAndCurrencyAreSame()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");

        // Act & Assert
        money1.Should().Be(money2);
        money1.Equals(money2).Should().BeTrue();
    }

    [Fact]
    public void Money_ShouldNotBeEqual_WhenAmountOrCurrencyAreDifferent()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "EUR");
        var money3 = new Money(200, "USD");

        // Act & Assert
        money1.Should().NotBe(money2);
        money1.Should().NotBe(money3);
        money1.Equals(money2).Should().BeFalse();
        money1.Equals(money3).Should().BeFalse();
    }

    [Fact]
    public void Money_ShouldHaveSameHashCode_WhenEqual()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");

        // Act & Assert
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    [Fact]
    public void Money_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var money = new Money(1234.56m, "USD");

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Be("1,234.56 USD");  // N2 format includes thousand separator
    }

    [Theory]
    [InlineData(0, "USD")]
    [InlineData(0.01, "EUR")]
    [InlineData(1000000, "GBP")]
    public void Money_ShouldHandleEdgeCases(decimal amount, string currency)
    {
        // Act
        var money = new Money(amount, currency);

        // Assert
        money.Amount.Should().Be(amount);
        money.Currency.Should().Be(currency);
    }
}