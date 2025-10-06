using FluentAssertions;
using OilTrading.Core.ValueObjects;
using Xunit;

namespace OilTrading.Tests.Domain.ValueObjects;

public class PriceFormulaTests
{
    [Fact]
    public void PriceFormula_ShouldCreateFixedPrice_WhenUsingStaticMethod()
    {
        // Arrange
        decimal fixedPrice = 75.50m;

        // Act
        var priceFormula = PriceFormula.Fixed(fixedPrice);

        // Assert
        priceFormula.IsFixedPrice.Should().BeTrue();
        priceFormula.FixedPrice.Should().Be(fixedPrice);
        priceFormula.Formula.Should().Contain(fixedPrice.ToString());
        priceFormula.Premium.Should().BeNull();
        priceFormula.Discount.Should().BeNull();
    }

    [Fact]
    public void PriceFormula_ShouldCreateFloatingPrice_WithIndexAndPremium()
    {
        // Arrange
        string indexName = "BRENT";
        var premium = Money.Dollar(5.0m);

        // Act
        var priceFormula = PriceFormula.Index(indexName, PricingMethod.AVG, premium);

        // Assert
        priceFormula.IsFixedPrice.Should().BeFalse();
        priceFormula.IndexName.Should().Be(indexName);
        priceFormula.Premium.Should().Be(premium);
        priceFormula.Discount.Should().BeNull();
        priceFormula.Formula.Should().Contain(indexName);
        priceFormula.Formula.Should().Contain("+");
        priceFormula.Formula.Should().Contain("5");
    }

    [Fact]
    public void PriceFormula_ShouldCreateFloatingPrice_WithIndexAndDiscount()
    {
        // Arrange
        string indexName = "WTI";
        var discount = Money.Dollar(2.5m);

        // Act
        // For discount, use negative adjustment
        var priceFormula = PriceFormula.Index(indexName, PricingMethod.AVG, Money.Dollar(-discount.Amount));

        // Assert
        priceFormula.IsFixedPrice.Should().BeFalse();
        priceFormula.IndexName.Should().Be(indexName);
        priceFormula.Premium.Should().BeNull();
        priceFormula.Discount.Should().Be(discount);
        priceFormula.Formula.Should().Contain(indexName);
        priceFormula.Formula.Should().Contain("-");
        priceFormula.Formula.Should().Contain("2.5");
    }

    [Fact]
    public void PriceFormula_ShouldCreateFloatingPrice_WithIndexOnly()
    {
        // Arrange
        string indexName = "DUBAI";

        // Act
        var priceFormula = PriceFormula.Index(indexName);

        // Assert
        priceFormula.IsFixedPrice.Should().BeFalse();
        priceFormula.IndexName.Should().Be(indexName);
        priceFormula.Premium.Should().BeNull();
        priceFormula.Discount.Should().BeNull();
        priceFormula.Formula.Should().Be(indexName);
    }

    [Fact]
    public void PriceFormula_ShouldParseValidFormula_FixedPrice()
    {
        // Arrange
        string formula = "75.50 USD";

        // Act
        var priceFormula = PriceFormula.Parse(formula);

        // Assert
        priceFormula.IsFixedPrice.Should().BeTrue();
        priceFormula.FixedPrice.Should().Be(75.50m);
        priceFormula.Formula.Should().Be(formula);
    }

    [Fact]
    public void PriceFormula_ShouldParseValidFormula_FloatingWithPremium()
    {
        // Arrange
        string formula = "BRENT + 5.00 USD";

        // Act
        var priceFormula = PriceFormula.Parse(formula);

        // Assert
        priceFormula.IsFixedPrice.Should().BeFalse();
        priceFormula.IndexName.Should().Be("BRENT");
        priceFormula.Premium?.Amount.Should().Be(5.00m);
        priceFormula.Premium?.Currency.Should().Be("USD");
        priceFormula.Formula.Should().Be(formula);
    }

    [Fact]
    public void PriceFormula_ShouldParseValidFormula_FloatingWithDiscount()
    {
        // Arrange
        string formula = "WTI - 2.50 USD";

        // Act
        var priceFormula = PriceFormula.Parse(formula);

        // Assert
        priceFormula.IsFixedPrice.Should().BeFalse();
        priceFormula.IndexName.Should().Be("WTI");
        priceFormula.Discount?.Amount.Should().Be(2.50m);
        priceFormula.Discount?.Currency.Should().Be("USD");
        priceFormula.Formula.Should().Be(formula);
    }

    [Fact]
    public void PriceFormula_ShouldParseValidFormula_IndexOnly()
    {
        // Arrange
        string formula = "DUBAI";

        // Act
        var priceFormula = PriceFormula.Parse(formula);

        // Assert
        priceFormula.IsFixedPrice.Should().BeFalse();
        priceFormula.IndexName.Should().Be("DUBAI");
        priceFormula.Premium.Should().BeNull();
        priceFormula.Discount.Should().BeNull();
        priceFormula.Formula.Should().Be(formula);
    }

    [Fact]
    public void PriceFormula_ShouldThrowArgumentException_WhenFormulaIsInvalid()
    {
        // Arrange
        string invalidFormula = "INVALID FORMULA FORMAT";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => PriceFormula.Parse(invalidFormula));
    }

    [Fact]
    public void PriceFormula_ShouldThrowArgumentException_WhenFormulaIsNullOrEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PriceFormula.Parse(null!));
        Assert.Throws<ArgumentException>(() => PriceFormula.Parse(""));
        Assert.Throws<ArgumentException>(() => PriceFormula.Parse("   "));
    }

    [Fact]
    public void PriceFormula_ShouldHaveBasePrice_ForFixedPrice()
    {
        // Arrange
        var priceFormula = PriceFormula.Fixed(80.00m);

        // Act
        var basePrice = priceFormula.BasePrice;

        // Assert
        basePrice?.Amount.Should().Be(80.00m);
        basePrice?.Currency.Should().Be("USD");
    }

    [Fact]
    public void PriceFormula_ShouldCalculateBasePrice_ForFloatingPriceWithPremium()
    {
        // Arrange
        var priceFormula = PriceFormula.Index("BRENT", PricingMethod.AVG, Money.Dollar(5.00m));
        var marketPrices = new Dictionary<string, decimal> { { "BRENT", 70.00m } };

        // Act
        var basePrice = priceFormula.BasePrice;

        // Assert
        basePrice.Should().BeNull(); // Index-based formulas don't have base price
    }

    [Fact]
    public void PriceFormula_ShouldCalculateBasePrice_ForFloatingPriceWithDiscount()
    {
        // Arrange
        var priceFormula = PriceFormula.Index("WTI", PricingMethod.AVG, Money.Dollar(-3.00m));
        var marketPrices = new Dictionary<string, decimal> { { "WTI", 68.00m } };

        // Act
        var basePrice = priceFormula.BasePrice;

        // Assert
        basePrice.Should().BeNull(); // Index-based formulas don't have base price
    }

    [Fact]
    public void PriceFormula_ShouldReturnNull_WhenMarketPriceNotAvailable()
    {
        // Arrange
        var priceFormula = PriceFormula.Index("NONEXISTENT", PricingMethod.AVG);
        var marketPrices = new Dictionary<string, decimal> { { "BRENT", 70.00m } };

        // Act
        var basePrice = priceFormula.BasePrice;

        // Assert
        basePrice.Should().BeNull();
    }

    [Fact]
    public void PriceFormula_ShouldBeEqual_WhenFormulaIsTheSame()
    {
        // Arrange
        var formula1 = PriceFormula.Fixed(75.50m);
        var formula2 = PriceFormula.Fixed(75.50m);

        // Act & Assert
        formula1.Should().Be(formula2);
        (formula1 == formula2).Should().BeTrue();
        (formula1 != formula2).Should().BeFalse();
    }

    [Fact]
    public void PriceFormula_ShouldNotBeEqual_WhenFormulaIsDifferent()
    {
        // Arrange
        var formula1 = PriceFormula.Fixed(75.50m);
        var formula2 = PriceFormula.Fixed(80.00m);

        // Act & Assert
        formula1.Should().NotBe(formula2);
        (formula1 == formula2).Should().BeFalse();
        (formula1 != formula2).Should().BeTrue();
    }

    [Fact]
    public void PriceFormula_ShouldHaveSameHashCode_WhenEqual()
    {
        // Arrange
        var formula1 = PriceFormula.Fixed(75.50m);
        var formula2 = PriceFormula.Fixed(75.50m);

        // Act & Assert
        formula1.GetHashCode().Should().Be(formula2.GetHashCode());
    }

    [Fact]
    public void PriceFormula_ToString_ShouldReturnFormula()
    {
        // Arrange
        var priceFormula = PriceFormula.Index("BRENT", PricingMethod.AVG, Money.Dollar(5.00m));

        // Act
        var result = priceFormula.ToString();

        // Assert
        result.Should().Contain("BRENT");
        result.Should().Contain("+");
        result.Should().Contain("5");
        result.Should().Contain("USD");
    }

    [Theory]
    [InlineData("75.50 USD")]
    [InlineData("BRENT + 5.00 USD")]
    [InlineData("WTI - 2.50 EUR")]
    [InlineData("DUBAI")]
    [InlineData("ICE BRENT + 10.25 USD")]
    public void PriceFormula_ShouldParseAndRecreateCorrectly(string formula)
    {
        // Act
        var priceFormula = PriceFormula.Parse(formula);
        var recreatedFormula = priceFormula.Formula;

        // Assert
        recreatedFormula.Should().Be(formula);
    }

    [Fact]
    public void PriceFormula_ShouldHandleComplexFormula_WithAveraging()
    {
        // Arrange
        string formula = "AVG(BRENT) + 3.50 USD";

        // Act
        var priceFormula = PriceFormula.Parse(formula);

        // Assert
        priceFormula.IsFixedPrice.Should().BeFalse();
        priceFormula.Formula.Should().Be(formula);
        priceFormula.IndexName.Should().Be("AVG(BRENT)");
        priceFormula.Premium?.Amount.Should().Be(3.50m);
    }
}