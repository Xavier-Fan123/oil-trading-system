using FluentAssertions;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using Xunit;

namespace OilTrading.Tests.Domain.Entities;

public class ProductTests
{
    #region Constructor and Property Tests

    [Fact]
    public void Product_ShouldHaveDefaultValues_WhenCreated()
    {
        // Act
        var product = new Product();

        // Assert
        product.Should().NotBeNull();
        product.Id.Should().NotBeEmpty();
        product.Name.Should().BeEmpty();
        product.Code.Should().BeEmpty();
        product.ProductName.Should().BeEmpty();
        product.ProductCode.Should().BeEmpty();
        product.Type.Should().Be(ProductType.CrudeOil); // Default enum value
        product.Grade.Should().BeEmpty();
        product.Specification.Should().BeEmpty();
        product.UnitOfMeasure.Should().BeEmpty();
        product.Density.Should().Be(0);
        product.Origin.Should().BeEmpty();
        product.IsActive.Should().BeTrue();
        product.PurchaseContracts.Should().NotBeNull().And.BeEmpty();
        product.SalesContracts.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Product_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var product = new Product();
        const string name = "Brent Crude Oil";
        const string code = "BRENT";
        const string productName = "North Sea Brent";
        const string productCode = "BRENT-001";
        const ProductType type = ProductType.CrudeOil;
        const string grade = "Sweet Light";
        const string specification = "API 38.0°, Sulfur 0.37%";
        const string unitOfMeasure = "BBL";
        const decimal density = 0.835m;
        const string origin = "North Sea";

        // Act
        product.Name = name;
        product.Code = code;
        product.ProductName = productName;
        product.ProductCode = productCode;
        product.Type = type;
        product.Grade = grade;
        product.Specification = specification;
        product.UnitOfMeasure = unitOfMeasure;
        product.Density = density;
        product.Origin = origin;
        product.IsActive = false;

        // Assert
        product.Name.Should().Be(name);
        product.Code.Should().Be(code);
        product.ProductName.Should().Be(productName);
        product.ProductCode.Should().Be(productCode);
        product.Type.Should().Be(type);
        product.Grade.Should().Be(grade);
        product.Specification.Should().Be(specification);
        product.UnitOfMeasure.Should().Be(unitOfMeasure);
        product.Density.Should().Be(density);
        product.Origin.Should().Be(origin);
        product.IsActive.Should().BeFalse();
    }

    #endregion

    #region ProductType Enum Tests

    [Theory]
    [InlineData(ProductType.CrudeOil, 1)]
    [InlineData(ProductType.RefinedProducts, 2)]
    [InlineData(ProductType.NaturalGas, 3)]
    [InlineData(ProductType.Petrochemicals, 4)]
    public void ProductType_ShouldHaveCorrectValues(ProductType type, int expectedValue)
    {
        // Act & Assert
        ((int)type).Should().Be(expectedValue);
    }

    [Fact]
    public void ProductType_ShouldContainAllExpectedValues()
    {
        // Arrange
        var expectedTypes = new[]
        {
            ProductType.CrudeOil,
            ProductType.RefinedProducts,
            ProductType.NaturalGas,
            ProductType.Petrochemicals
        };

        // Act
        var allTypes = Enum.GetValues<ProductType>();

        // Assert
        allTypes.Should().HaveCount(4);
        allTypes.Should().Contain(expectedTypes);
    }

    [Theory]
    [InlineData("CrudeOil", ProductType.CrudeOil)]
    [InlineData("RefinedProducts", ProductType.RefinedProducts)]
    [InlineData("NaturalGas", ProductType.NaturalGas)]
    [InlineData("Petrochemicals", ProductType.Petrochemicals)]
    [InlineData("crudeoil", ProductType.CrudeOil)]
    [InlineData("refinedproducts", ProductType.RefinedProducts)]
    public void ProductType_ShouldParseFromString(string typeString, ProductType expectedType)
    {
        // Act
        var success = Enum.TryParse<ProductType>(typeString, true, out var parsedType);

        // Assert
        success.Should().BeTrue();
        parsedType.Should().Be(expectedType);
    }

    [Fact]
    public void ProductType_ToString_ShouldReturnCorrectNames()
    {
        // Test all product types
        ProductType.CrudeOil.ToString().Should().Be("CrudeOil");
        ProductType.RefinedProducts.ToString().Should().Be("RefinedProducts");
        ProductType.NaturalGas.ToString().Should().Be("NaturalGas");
        ProductType.Petrochemicals.ToString().Should().Be("Petrochemicals");
    }

    [Fact]
    public void ProductType_AllValues_ShouldBeUnique()
    {
        // Arrange
        var allTypes = Enum.GetValues<ProductType>();
        var typeValues = allTypes.Cast<int>().ToArray();

        // Assert
        typeValues.Should().OnlyHaveUniqueItems();
        typeValues.Should().AllSatisfy(value => value.Should().BeGreaterThan(0));
    }

    #endregion

    #region Navigation Properties Tests

    [Fact]
    public void PurchaseContracts_ShouldInitializeAsEmptyCollection()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.PurchaseContracts.Should().NotBeNull();
        product.PurchaseContracts.Should().BeEmpty();
        product.PurchaseContracts.Should().BeAssignableTo<ICollection<PurchaseContract>>();
    }

    [Fact]
    public void SalesContracts_ShouldInitializeAsEmptyCollection()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.SalesContracts.Should().NotBeNull();
        product.SalesContracts.Should().BeEmpty();
        product.SalesContracts.Should().BeAssignableTo<ICollection<SalesContract>>();
    }

    [Fact]
    public void PurchaseContracts_ShouldAllowAddingContracts()
    {
        // Arrange
        var product = new Product();
        var contract = CreateMockPurchaseContract();

        // Act
        product.PurchaseContracts.Add(contract);

        // Assert
        product.PurchaseContracts.Should().ContainSingle();
        product.PurchaseContracts.Should().Contain(contract);
    }

    [Fact]
    public void SalesContracts_ShouldAllowAddingContracts()
    {
        // Arrange
        var product = new Product();
        var contract = CreateMockSalesContract();

        // Act
        product.SalesContracts.Add(contract);

        // Assert
        product.SalesContracts.Should().ContainSingle();
        product.SalesContracts.Should().Contain(contract);
    }

    #endregion

    #region Business Logic Tests

    [Theory]
    [InlineData(ProductType.CrudeOil, "Brent", "Sweet Light", true)]
    [InlineData(ProductType.RefinedProducts, "Gasoline", "Regular 87 Octane", true)]
    [InlineData(ProductType.NaturalGas, "LNG", "Pipeline Quality", true)]
    [InlineData(ProductType.Petrochemicals, "Ethylene", "Polymer Grade", true)]
    public void Product_ShouldRepresentDifferentOilTradingProducts(ProductType type, string name, string grade, bool isActive)
    {
        // Arrange & Act
        var product = new Product
        {
            Name = name,
            Type = type,
            Grade = grade,
            IsActive = isActive
        };

        // Assert
        product.Type.Should().Be(type);
        product.Name.Should().Be(name);
        product.Grade.Should().Be(grade);
        product.IsActive.Should().Be(isActive);
    }

    [Fact]
    public void Product_ShouldSupportCrudeOilProperties()
    {
        // Arrange
        var product = new Product
        {
            Name = "West Texas Intermediate",
            Code = "WTI",
            Type = ProductType.CrudeOil,
            Grade = "Light Sweet",
            Specification = "API 39.6°, Sulfur 0.24%",
            Density = 0.827m,
            Origin = "Cushing, Oklahoma",
            UnitOfMeasure = "BBL"
        };

        // Assert
        product.Type.Should().Be(ProductType.CrudeOil);
        product.Name.Should().Be("West Texas Intermediate");
        product.Code.Should().Be("WTI");
        product.Grade.Should().Be("Light Sweet");
        product.Specification.Should().Contain("API").And.Contain("Sulfur");
        product.Density.Should().BePositive();
        product.Origin.Should().NotBeEmpty();
        product.UnitOfMeasure.Should().Be("BBL");
    }

    [Fact]
    public void Product_ShouldSupportRefinedProductProperties()
    {
        // Arrange
        var product = new Product
        {
            Name = "Jet Fuel",
            Code = "JET-A1",
            Type = ProductType.RefinedProducts,
            Grade = "Aviation Grade",
            Specification = "ASTM D1655, DEF STAN 91-91",
            Density = 0.775m,
            Origin = "Various Refineries",
            UnitOfMeasure = "GAL"
        };

        // Assert
        product.Type.Should().Be(ProductType.RefinedProducts);
        product.Name.Should().Be("Jet Fuel");
        product.Code.Should().Be("JET-A1");
        product.Grade.Should().Be("Aviation Grade");
        product.Specification.Should().Contain("ASTM");
        product.UnitOfMeasure.Should().Be("GAL");
    }

    [Theory]
    [InlineData(0.75)]   // Light crude
    [InlineData(0.85)]   // Medium crude
    [InlineData(0.95)]   // Heavy crude
    [InlineData(1.05)]   // Extra heavy crude
    public void Product_ShouldHandleDifferentDensityValues(decimal density)
    {
        // Arrange & Act
        var product = new Product
        {
            Name = "Test Crude",
            Type = ProductType.CrudeOil,
            Density = density
        };

        // Assert
        product.Density.Should().Be(density);
    }

    [Fact]
    public void Product_ShouldSupportActiveInactiveStates()
    {
        // Arrange
        var activeProduct = new Product { Name = "Active Product", IsActive = true };
        var inactiveProduct = new Product { Name = "Inactive Product", IsActive = false };

        // Assert
        activeProduct.IsActive.Should().BeTrue();
        inactiveProduct.IsActive.Should().BeFalse();
    }

    #endregion

    #region Edge Cases and Validation Tests

    [Fact]
    public void Product_ShouldHandleEmptyStrings()
    {
        // Arrange & Act
        var product = new Product
        {
            Name = "",
            Code = "",
            ProductName = "",
            ProductCode = "",
            Grade = "",
            Specification = "",
            UnitOfMeasure = "",
            Origin = ""
        };

        // Assert
        product.Name.Should().BeEmpty();
        product.Code.Should().BeEmpty();
        product.ProductName.Should().BeEmpty();
        product.ProductCode.Should().BeEmpty();
        product.Grade.Should().BeEmpty();
        product.Specification.Should().BeEmpty();
        product.UnitOfMeasure.Should().BeEmpty();
        product.Origin.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]      // Zero density
    [InlineData(-1)]     // Negative density (invalid but should be handled)
    [InlineData(0.001)]  // Very low density
    [InlineData(999.99)] // Very high density
    public void Product_ShouldHandleEdgeCaseDensityValues(decimal density)
    {
        // Arrange & Act
        var product = new Product
        {
            Name = "Test Product",
            Density = density
        };

        // Assert
        product.Density.Should().Be(density);
    }

    [Fact]
    public void Product_ShouldHandleLongStrings()
    {
        // Arrange
        var longString = new string('A', 1000);
        
        // Act
        var product = new Product
        {
            Name = longString,
            Specification = longString
        };

        // Assert
        product.Name.Should().Be(longString);
        product.Specification.Should().Be(longString);
    }

    #endregion

    #region Entity Inheritance Tests

    [Fact]
    public void Product_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.Should().BeAssignableTo<BaseEntity>();
        product.Id.Should().NotBeEmpty();
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Product_ShouldHaveUniqueId()
    {
        // Arrange & Act
        var product1 = new Product();
        var product2 = new Product();

        // Assert
        product1.Id.Should().NotBe(product2.Id);
    }

    #endregion

    #region Helper Methods

    private static PurchaseContract CreateMockPurchaseContract()
    {
        var contractNumber = ContractNumber.Create(2024, ContractType.CARGO, 1);
        var quantity = Quantity.MetricTons(1000);
        
        return new PurchaseContract(
            contractNumber,
            ContractType.CARGO,
            Guid.NewGuid(), // supplierId
            Guid.NewGuid(), // productId
            Guid.NewGuid(), // traderId
            quantity);
    }

    private static SalesContract CreateMockSalesContract()
    {
        var contractNumber = ContractNumber.Create(2024, ContractType.CARGO, 1);
        var quantity = Quantity.MetricTons(1000);
        
        return new SalesContract(
            contractNumber,
            ContractType.CARGO,
            Guid.NewGuid(), // customerId
            Guid.NewGuid(), // productId
            Guid.NewGuid(), // traderId
            quantity);
    }

    #endregion

    #region Industry-Specific Tests

    [Fact]
    public void Product_ShouldSupportCommonCrudeOilBenchmarks()
    {
        // Test that the Product entity can represent major crude oil benchmarks
        var benchmarks = new[]
        {
            new { Name = "Brent", Origin = "North Sea", Type = ProductType.CrudeOil },
            new { Name = "WTI", Origin = "USA", Type = ProductType.CrudeOil },
            new { Name = "Dubai", Origin = "Middle East", Type = ProductType.CrudeOil },
            new { Name = "Urals", Origin = "Russia", Type = ProductType.CrudeOil }
        };

        foreach (var benchmark in benchmarks)
        {
            var product = new Product
            {
                Name = benchmark.Name,
                Origin = benchmark.Origin,
                Type = benchmark.Type
            };

            product.Name.Should().Be(benchmark.Name);
            product.Origin.Should().Be(benchmark.Origin);
            product.Type.Should().Be(benchmark.Type);
        }
    }

    [Fact]
    public void Product_ShouldSupportCommonRefinedProducts()
    {
        // Test that the Product entity can represent major refined products
        var refinedProducts = new[]
        {
            new { Name = "Gasoline", Grade = "Premium", UoM = "GAL" },
            new { Name = "Diesel", Grade = "Ultra Low Sulfur", UoM = "GAL" },
            new { Name = "Jet Fuel", Grade = "Jet A-1", UoM = "GAL" },
            new { Name = "Fuel Oil", Grade = "380 CST", UoM = "MT" }
        };

        foreach (var refined in refinedProducts)
        {
            var product = new Product
            {
                Name = refined.Name,
                Grade = refined.Grade,
                UnitOfMeasure = refined.UoM,
                Type = ProductType.RefinedProducts
            };

            product.Name.Should().Be(refined.Name);
            product.Grade.Should().Be(refined.Grade);
            product.UnitOfMeasure.Should().Be(refined.UoM);
            product.Type.Should().Be(ProductType.RefinedProducts);
        }
    }

    #endregion
}