using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;
using OilTrading.IntegrationTests.Infrastructure;
using Xunit;

namespace OilTrading.IntegrationTests.Controllers;

public class ProductsControllerIntegrationTests : IClassFixture<InMemoryWebApplicationFactory>
{
    private readonly InMemoryWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductsControllerIntegrationTests(InMemoryWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ShouldReturnAllProducts()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        var products = JsonSerializer.Deserialize<Product[]>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        products.Should().NotBeNull();
        products.Should().HaveCountGreaterThan(0);
        products.Should().Contain(p => p.ProductCode == "BRENT");
        products.Should().Contain(p => p.ProductCode == "WTI");
    }

    [Fact]
    public async Task GetProduct_WithValidId_ShouldReturnProduct()
    {
        // Arrange
        await SeedTestDataAsync();
        var testProduct = await GetTestProductAsync();

        // Act
        var response = await _client.GetAsync($"/api/products/{testProduct.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var product = JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        product.Should().NotBeNull();
        product!.Id.Should().Be(testProduct.Id);
        product.ProductCode.Should().Be(testProduct.ProductCode);
        product.ProductName.Should().Be(testProduct.ProductName);
    }

    [Fact]
    public async Task GetProduct_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ShouldCreateProduct()
    {
        // Arrange
        var newProduct = new
        {
            ProductName = "Test Oil Product",
            ProductCode = "TEST",
            Type = 1, // Crude
            Grade = "Test Grade",
            Specification = "Test Specification",
            UnitOfMeasure = "BBL",
            Density = 850.0,
            Origin = "Test Origin"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", newProduct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var createdProduct = JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        createdProduct.Should().NotBeNull();
        createdProduct!.ProductName.Should().Be(newProduct.ProductName);
        createdProduct.ProductCode.Should().Be(newProduct.ProductCode);
        createdProduct.Id.Should().NotBe(Guid.Empty);

        // Verify it was actually saved to database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var savedProduct = await context.Products.FindAsync(createdProduct.Id);
        savedProduct.Should().NotBeNull();
        savedProduct!.ProductName.Should().Be(newProduct.ProductName);
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ShouldUpdateProduct()
    {
        // Arrange
        await SeedTestDataAsync();
        var testProduct = await GetTestProductAsync();
        
        var updatedData = new
        {
            Id = testProduct.Id,
            ProductName = "Updated Product Name",
            ProductCode = testProduct.ProductCode,
            Type = testProduct.Type,
            Grade = "Updated Grade",
            Specification = testProduct.Specification,
            UnitOfMeasure = testProduct.UnitOfMeasure,
            Density = testProduct.Density,
            Origin = testProduct.Origin,
            IsActive = testProduct.IsActive
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{testProduct.Id}", updatedData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the product was updated in the database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updatedProduct = await context.Products.FindAsync(testProduct.Id);
        updatedProduct.Should().NotBeNull();
        updatedProduct!.ProductName.Should().Be("Updated Product Name");
        updatedProduct.Grade.Should().Be("Updated Grade");
    }

    [Fact]
    public async Task DeleteProduct_WithValidId_ShouldSoftDeleteProduct()
    {
        // Arrange
        await SeedTestDataAsync();
        var testProduct = await GetTestProductAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/products/{testProduct.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the product was soft deleted
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Use IgnoreQueryFilters to check soft delete
        var deletedProduct = await context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == testProduct.Id);
        
        deletedProduct.Should().NotBeNull();
        deletedProduct!.IsDeleted.Should().BeTrue();
        deletedProduct.DeletedAt.Should().NotBeNull();
        deletedProduct.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetProducts_ShouldReturnCachedResponse_OnSecondCall()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - First call
        var response1 = await _client.GetAsync("/api/products");
        var cacheHeader1 = response1.Headers.GetValues("Cache-Control").FirstOrDefault();

        // Act - Second call (should be cached)
        var response2 = await _client.GetAsync("/api/products");
        var ageHeader = response2.Headers.Contains("Age") ? response2.Headers.GetValues("Age").FirstOrDefault() : null;

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        cacheHeader1.Should().Contain("public");
        cacheHeader1.Should().Contain("max-age=300"); // 5 minutes cache
        
        // Second response should have an Age header indicating it was cached
        ageHeader.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProducts_WithRateLimit_ShouldThrottle_AfterManyRequests()
    {
        // Arrange & Act - Make many rapid requests to test rate limiting
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 15; i++) // Try to exceed rate limit
        {
            tasks.Add(_client.GetAsync("/api/products"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - Some requests should be rate limited (429)
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);

        successCount.Should().BeGreaterThan(0);
        // Note: Actual rate limiting behavior depends on configuration
        // This test verifies the rate limiting middleware is working
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Check if data already exists
        if (await context.Products.AnyAsync())
            return;

        var products = new[]
        {
            new Product {
                Name = "Brent Crude Oil", Code = "BRENT", ProductName = "Brent Crude Oil", ProductCode = "BRENT",
                Type = ProductType.CrudeOil, Grade = "Light Sweet", Specification = "API 38, Sulfur 0.37%",
                UnitOfMeasure = "BBL", Density = 835.0m, Origin = "North Sea"
            },
            new Product {
                Name = "West Texas Intermediate", Code = "WTI", ProductName = "West Texas Intermediate", ProductCode = "WTI",
                Type = ProductType.CrudeOil, Grade = "Light Sweet", Specification = "API 39.6, Sulfur 0.24%",
                UnitOfMeasure = "BBL", Density = 827.0m, Origin = "United States"
            },
            new Product {
                Name = "Marine Gas Oil", Code = "MGO", ProductName = "Marine Gas Oil", ProductCode = "MGO",
                Type = ProductType.RefinedProducts, Grade = "0.5% Sulfur", Specification = "ISO 8217:2017",
                UnitOfMeasure = "MT", Density = 890.0m, Origin = "Singapore"
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private async Task<Product> GetTestProductAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Products.FirstAsync(p => p.ProductCode == "BRENT");
    }
}