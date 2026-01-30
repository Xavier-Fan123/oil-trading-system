using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Infrastructure.Data;
using OilTrading.IntegrationTests.Infrastructure;
using Xunit;

namespace OilTrading.IntegrationTests.Data;

public class DatabaseIntegrationTests : IClassFixture<InMemoryWebApplicationFactory>
{
    private readonly InMemoryWebApplicationFactory _factory;

    public DatabaseIntegrationTests(InMemoryWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Database_ShouldCreateAllTables_Successfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act & Assert - Verify database is created and can query all entity sets
        // InMemory database doesn't have information_schema, so we test by querying each DbSet
        var usersCount = await context.Users.CountAsync();
        var productsCount = await context.Products.CountAsync();
        var tradingPartnersCount = await context.TradingPartners.CountAsync();
        var purchaseContractsCount = await context.PurchaseContracts.CountAsync();
        var salesContractsCount = await context.SalesContracts.CountAsync();
        var shippingOperationsCount = await context.ShippingOperations.CountAsync();

        // All queries should complete successfully (counts can be 0)
        usersCount.Should().BeGreaterThanOrEqualTo(0);
        productsCount.Should().BeGreaterThanOrEqualTo(0);
        tradingPartnersCount.Should().BeGreaterThanOrEqualTo(0);
        purchaseContractsCount.Should().BeGreaterThanOrEqualTo(0);
        salesContractsCount.Should().BeGreaterThanOrEqualTo(0);
        shippingOperationsCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task PurchaseContract_WithValueObjects_ShouldPersistCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var contractNumber = ContractNumber.Create(2025, ContractType.CARGO, 1);
        var quantity = new Quantity(10000, QuantityUnit.BBL);
        
        var contract = new PurchaseContract(
            contractNumber,
            ContractType.CARGO,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            quantity);

        // Act
        contract.SetRowVersion(new byte[] { 0 });
        context.PurchaseContracts.Add(contract);
        await context.SaveChangesAsync();

        // Clear context to ensure fresh read from database
        context.ChangeTracker.Clear();

        // Assert
        var savedContract = await context.PurchaseContracts
            .FirstOrDefaultAsync(c => c.Id == contract.Id);

        savedContract.Should().NotBeNull();
        savedContract!.ContractNumber.Value.Should().Be("ITGR-2025-CARGO-B0001");
        savedContract.ContractQuantity.Value.Should().Be(10000);
        savedContract.ContractQuantity.Unit.Should().Be(QuantityUnit.BBL);
        savedContract.Status.Should().Be(ContractStatus.Draft);
        savedContract.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Product_WithComplexProperties_ShouldPersistCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = new Product
        {
            Name = "Test Crude Oil",
            Code = "TESTCR",
            ProductName = "Test Crude Oil",
            ProductCode = "TESTCR",
            Type = ProductType.CrudeOil,
            Grade = "Light Sweet",
            Specification = "API 35, Sulfur 0.5%",
            UnitOfMeasure = "BBL",
            Density = 840.5m,
            Origin = "Test Field"
        };

        // Act
        product.SetRowVersion(new byte[] { 0 });
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Clear context for fresh read
        context.ChangeTracker.Clear();

        // Assert
        var savedProduct = await context.Products
            .FirstOrDefaultAsync(p => p.ProductCode == "TESTCR");

        savedProduct.Should().NotBeNull();
        savedProduct!.ProductName.Should().Be("Test Crude Oil");
        savedProduct.ProductCode.Should().Be("TESTCR");
        savedProduct.Type.Should().Be(ProductType.CrudeOil);
        savedProduct.Grade.Should().Be("Light Sweet");
        savedProduct.Specification.Should().Be("API 35, Sulfur 0.5%");
        savedProduct.UnitOfMeasure.Should().Be("BBL");
        savedProduct.Density.Should().Be(840.5m);
        savedProduct.Origin.Should().Be("Test Field");
        savedProduct.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDelete_ShouldBeExcludedFromQueries_ByDefault()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = new Product
        {
            Name = "Test Product for Deletion",
            Code = "TESTDEL",
            ProductName = "Test Product for Deletion",
            ProductCode = "TESTDEL",
            Type = ProductType.RefinedProducts,
            Grade = "Test Grade",
            Specification = "Test Spec",
            UnitOfMeasure = "MT",
            Density = 900.0m,
            Origin = "Test Origin"
        };

        product.SetRowVersion(new byte[] { 0 });
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Act - Soft delete the product
        product.SoftDelete("test-user");
        await context.SaveChangesAsync();

        // Clear context for fresh read
        context.ChangeTracker.Clear();

        // Assert - Product should not be returned by default query
        var normalQuery = await context.Products
            .Where(p => p.ProductCode == "TESTDEL")
            .FirstOrDefaultAsync();

        normalQuery.Should().BeNull();

        // But should be returned when ignoring query filters
        var ignoredFilterQuery = await context.Products
            .IgnoreQueryFilters()
            .Where(p => p.ProductCode == "TESTDEL")
            .FirstOrDefaultAsync();

        ignoredFilterQuery.Should().NotBeNull();
        ignoredFilterQuery!.IsDeleted.Should().BeTrue();
        ignoredFilterQuery.DeletedBy.Should().Be("test-user");
        ignoredFilterQuery.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task TradingPartner_WithEnumProperties_ShouldPersistCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tradingPartner = new TradingPartner
        {
            Name = "Test Trading Company",
            Code = "TTC001",
            CompanyName = "Test Trading Company",
            CompanyCode = "TTC001",
            Type = TradingPartnerType.Both,
            PartnerType = TradingPartnerType.Both,
            ContactEmail = "test@company.com",
            ContactPhone = "+1-555-0123"
        };

        // Act
        tradingPartner.SetRowVersion(new byte[] { 0 });
        context.TradingPartners.Add(tradingPartner);
        await context.SaveChangesAsync();

        // Clear context for fresh read
        context.ChangeTracker.Clear();

        // Assert
        var saved = await context.TradingPartners
            .FirstOrDefaultAsync(tp => tp.Code == "TTC001");

        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test Trading Company");
        saved.Code.Should().Be("TTC001");
        saved.Type.Should().Be(TradingPartnerType.Both);
        saved.ContactEmail.Should().Be("test@company.com");
        saved.ContactPhone.Should().Be("+1-555-0123");
    }

    [Fact]
    public async Task Database_Indexes_ShouldWork_ForQueries()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Use unique codes to avoid conflicts with other tests
        var uniqueSuffix = Guid.NewGuid().ToString()[..8];
        var codeA = $"PRODA-{uniqueSuffix}";
        var codeB = $"PRODB-{uniqueSuffix}";
        var codeC = $"PRODC-{uniqueSuffix}";

        // Create multiple products
        var products = new[]
        {
            new Product { Name = "Product A", Code = codeA, ProductName = "Product A", ProductCode = codeA, Type = ProductType.CrudeOil, Grade = "Grade A", Specification = "Spec A", UnitOfMeasure = "BBL", Density = 800m, Origin = "Origin A" },
            new Product { Name = "Product B", Code = codeB, ProductName = "Product B", ProductCode = codeB, Type = ProductType.RefinedProducts, Grade = "Grade B", Specification = "Spec B", UnitOfMeasure = "MT", Density = 900m, Origin = "Origin B" },
            new Product { Name = "Product C", Code = codeC, ProductName = "Product C", ProductCode = codeC, Type = ProductType.CrudeOil, Grade = "Grade C", Specification = "Spec C", UnitOfMeasure = "BBL", Density = 850m, Origin = "Origin C" }
        };

        foreach (var p in products) { p.SetRowVersion(new byte[] { 0 }); }
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Act & Assert - Test indexed queries
        var codeQuery = await context.Products
            .Where(p => p.ProductCode == codeB)
            .FirstOrDefaultAsync();

        codeQuery.Should().NotBeNull();
        codeQuery!.ProductName.Should().Be("Product B");

        var typeQuery = await context.Products
            .Where(p => p.Type == ProductType.CrudeOil && p.ProductCode.StartsWith("PROD"))
            .CountAsync();

        typeQuery.Should().BeGreaterThanOrEqualTo(2);

        var activeQuery = await context.Products
            .Where(p => p.IsActive && p.ProductCode.Contains(uniqueSuffix))
            .CountAsync();

        activeQuery.Should().Be(3);
    }

    [Fact(Skip = "InMemory database does not support concurrency tokens")]
    public async Task ConcurrencyToken_ShouldPreventConcurrentUpdates()
    {
        // Arrange
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();
        var context1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var context2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = new Product
        {
            Name = "Concurrency Test Product",
            Code = "CONC001",
            ProductName = "Concurrency Test Product",
            ProductCode = "CONC001",
            Type = ProductType.RefinedProducts,
            Grade = "Test Grade",
            Specification = "Test Spec",
            UnitOfMeasure = "MT",
            Density = 950.0m,
            Origin = "Test Origin"
        };

        product.SetRowVersion(new byte[] { 0 });
        context1.Products.Add(product);
        await context1.SaveChangesAsync();
        var productId = product.Id;

        // Load the same entity in two contexts
        var product1 = await context1.Products.FindAsync(productId);
        var product2 = await context2.Products.FindAsync(productId);

        // Act - Modify both entities
        product1!.Name = "Updated by Context 1";
        product1.ProductName = "Updated by Context 1";
        product2!.Name = "Updated by Context 2";
        product2.ProductName = "Updated by Context 2";

        // First update should succeed
        await context1.SaveChangesAsync();

        // Second update should fail due to concurrency conflict
        var act = async () => await context2.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}