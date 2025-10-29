using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Commands.PurchaseContracts;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;
using OilTrading.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace OilTrading.Tests.Integration;

public class PurchaseContractControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public PurchaseContractControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove the default DbContext
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                // Remove the real IRealTimeRiskMonitoringService if it exists
                var riskServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRealTimeRiskMonitoringService));
                if (riskServiceDescriptor != null)
                {
                    services.Remove(riskServiceDescriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_PurchaseContract");
                });

                // Register mock IRealTimeRiskMonitoringService for RiskCheckAttribute
                services.AddScoped<IRealTimeRiskMonitoringService, MockRealTimeRiskMonitoringService>();

                // Ensure the database is created
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.EnsureCreated();

                // Seed data
                SeedTestData(context);
            });
        });

        _client = _factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task GetPurchaseContracts_ShouldReturnPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/purchase-contracts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {content}");

        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task GetPurchaseContracts_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 10;

        // Act
        var response = await _client.GetAsync($"/api/v2/purchase-contracts?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<JsonElement>(content, options);
        
        if (result.TryGetProperty("pageNumber", out var pageNumberElement))
        {
            pageNumberElement.GetInt32().Should().Be(pageNumber);
        }
        
        if (result.TryGetProperty("pageSize", out var pageSizeElement))
        {
            pageSizeElement.GetInt32().Should().Be(pageSize);
        }
    }

    [Fact]
    public async Task CreatePurchaseContract_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var command = new CreatePurchaseContractDto
        {
            ContractType = ContractType.CARGO,
            SupplierId = GetTestTradingPartnerId(),
            ProductId = GetTestProductId(),
            TraderId = GetTestUserId(),
            Quantity = 1000,
            QuantityUnit = QuantityUnit.MT,
            TonBarrelRatio = 7.6m,
            PricingType = PricingType.Fixed,
            FixedPrice = 75.50m,
            DeliveryTerms = DeliveryTerms.FOB,
            LaycanStart = DateTime.Today.AddDays(30),
            LaycanEnd = DateTime.Today.AddDays(35),
            LoadPort = "Houston",
            DischargePort = "Rotterdam",
            SettlementType = ContractPaymentMethod.TT,
            CreditPeriodDays = 30,
            PrepaymentPercentage = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v2/purchase-contracts", command);

        // Debug output
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Response: {errorContent}");
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = response.Headers.Location?.ToString();
        location.Should().NotBeNullOrEmpty();
        location.Should().Contain("/api/v2/purchase-contracts/");
    }

    [Fact]
    public async Task CreatePurchaseContract_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreatePurchaseContractDto
        {
            // Missing required fields
            Quantity = -1000, // Invalid quantity
            QuantityUnit = QuantityUnit.MT,
            TonBarrelRatio = 7.6m,
            PricingType = PricingType.Fixed,
            DeliveryTerms = DeliveryTerms.FOB,
            LaycanStart = DateTime.Today.AddDays(30),
            LaycanEnd = DateTime.Today.AddDays(35),
            LoadPort = "Houston",
            DischargePort = "Rotterdam",
            SettlementType = ContractPaymentMethod.TT,
            CreditPeriodDays = 30,
            PrepaymentPercentage = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v2/purchase-contracts", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetPurchaseContractById_WithValidId_ShouldReturnContract()
    {
        // Arrange
        var contractId = await CreateTestContract();

        // Act
        var response = await _client.GetAsync($"/api/v2/purchase-contracts/{contractId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        if (result.TryGetProperty("id", out var idElement))
        {
            idElement.GetString().Should().Be(contractId.ToString());
        }
    }

    [Fact]
    public async Task GetPurchaseContractById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v2/purchase-contracts/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePurchaseContract_WithValidData_ShouldReturnNoContent()
    {
        // Arrange
        var contractId = await CreateTestContract();
        var updateDto = new UpdatePurchaseContractDto
        {
            Quantity = 1500,
            QuantityUnit = QuantityUnit.MT,
            TonBarrelRatio = 7.6m,
            PricingType = PricingType.Fixed,
            FixedPrice = 80.00m,
            DeliveryTerms = DeliveryTerms.CIF,
            LaycanStart = DateTime.Today.AddDays(25),
            LaycanEnd = DateTime.Today.AddDays(30),
            LoadPort = "Houston",
            DischargePort = "Amsterdam",
            SettlementType = ContractPaymentMethod.LC,
            CreditPeriodDays = 45,
            PrepaymentPercentage = 10
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v2/purchase-contracts/{contractId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ActivatePurchaseContract_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var contractId = await CreateTestContract();

        // Act
        var response = await _client.PostAsync($"/api/v2/purchase-contracts/{contractId}/activate", null);

        // Debug output
        if (response.StatusCode != HttpStatusCode.NoContent)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Response: {errorContent}");
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetAvailableQuantity_WithValidId_ShouldReturnQuantityInfo()
    {
        // Arrange
        var contractId = await CreateTestContract();

        // Act
        var response = await _client.GetAsync($"/api/v2/purchase-contracts/{contractId}/available-quantity");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result.TryGetProperty("contractId", out var contractIdElement).Should().BeTrue();
        result.TryGetProperty("totalQuantity", out var totalQuantityElement).Should().BeTrue();
        result.TryGetProperty("availableQuantity", out var availableQuantityElement).Should().BeTrue();
    }

    [Fact]
    public async Task GetPurchaseContracts_WithFilters_ShouldFilterCorrectly()
    {
        // Arrange
        var supplierId = GetTestTradingPartnerId();

        // Act
        var response = await _client.GetAsync($"/api/v2/purchase-contracts?supplierId={supplierId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert - In test environment, Redis may not be available, so accept either OK or ServiceUnavailable
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadAsStringAsync();
        // Health check returns JSON with status field
        content.Should().Contain("status");
    }

    private async Task<Guid> CreateTestContract()
    {
        var command = new CreatePurchaseContractDto
        {
            ContractType = ContractType.CARGO,
            SupplierId = GetTestTradingPartnerId(),
            ProductId = GetTestProductId(),
            TraderId = GetTestUserId(),
            Quantity = 1000,
            QuantityUnit = QuantityUnit.MT,
            TonBarrelRatio = 7.6m,
            PricingType = PricingType.Fixed,
            FixedPrice = 75.50m,
            DeliveryTerms = DeliveryTerms.FOB,
            LaycanStart = DateTime.Today.AddDays(30),
            LaycanEnd = DateTime.Today.AddDays(35),
            LoadPort = "Houston",
            DischargePort = "Rotterdam",
            SettlementType = ContractPaymentMethod.TT,
            CreditPeriodDays = 30,
            PrepaymentPercentage = 0,
            PaymentTerms = "TT 30 days after B/L date" // Required for activation
        };

        var response = await _client.PostAsJsonAsync("/api/v2/purchase-contracts", command);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return Guid.Parse(content.Trim('"'));
    }

    private static void SeedTestData(ApplicationDbContext context)
    {
        // Seed test users
        var testUser = new OilTrading.Core.Entities.User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "test_hash",
            Role = OilTrading.Core.Entities.UserRole.Trader,
            IsActive = true
        };
        context.Users.Add(testUser);

        // Seed test trading partner
        var testPartner = new OilTrading.Core.Entities.TradingPartner
        {
            Code = "TEST_SUPPLIER",
            Name = "Test Supplier",
            CompanyName = "Test Supplier Ltd",
            CompanyCode = "TSL",
            Type = OilTrading.Core.Entities.TradingPartnerType.Supplier,
            ContactEmail = "supplier@test.com",
            ContactPhone = "+1234567890",
            Address = "Test Address",
            Country = "Test Country",
            TaxId = "TEST123",
            IsActive = true,
            CreditLimit = 1000000m,
            CreditRating = "A"
        };
        context.TradingPartners.Add(testPartner);

        // Seed test product
        var testProduct = new OilTrading.Core.Entities.Product
        {
            Code = "TEST_CRUDE",
            Name = "Test Crude Oil",
            ProductName = "Test Crude Oil",
            ProductCode = "TEST_CRUDE",
            Type = OilTrading.Core.Entities.ProductType.CrudeOil,
            Grade = "Light Sweet",
            Specification = "Test Specification",
            UnitOfMeasure = "BBL",
            Density = 850.0m,
            Origin = "Test Origin",
            IsActive = true
        };
        context.Products.Add(testProduct);

        context.SaveChanges();
    }

    private Guid GetTestUserId()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return context.Users.First(u => u.Email == "test@example.com").Id;
    }

    private Guid GetTestTradingPartnerId()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return context.TradingPartners.First(tp => tp.Code == "TEST_SUPPLIER").Id;
    }

    private Guid GetTestProductId()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return context.Products.First(p => p.Code == "TEST_CRUDE").Id;
    }
}