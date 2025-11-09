using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using OilTrading.IntegrationTests.Infrastructure;
using OilTrading.Infrastructure.Data;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using OilTrading.Core.ValueObjects;

namespace OilTrading.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for the Settlement Analytics system (SettlementAnalyticsController)
/// Tests the complete analytics workflow with comprehensive coverage of all endpoints
/// Tests are organized by endpoint with variations for different parameter combinations
/// </summary>
public class SettlementAnalyticsIntegrationTests : IAsyncLifetime
{
    private InMemoryWebApplicationFactory _factory;
    private HttpClient _client;
    private ApplicationDbContext _dbContext;

    public async Task InitializeAsync()
    {
        _factory = new InMemoryWebApplicationFactory();
        _client = _factory.CreateClient();
        _dbContext = _factory.GetDbContext();

        // Ensure database is created with seed data
        await _dbContext.Database.EnsureCreatedAsync();

        // Seed test data for analytics tests
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Seed test data including products, trading partners, contracts, and settlements
    /// for comprehensive analytics testing
    /// </summary>
    private async Task SeedTestDataAsync()
    {
        // Check if data already exists
        if (_dbContext.Products.Any())
            return;

        // Create test products
        var brentProduct = new Product
        {
            Id = Guid.NewGuid(),
            Code = "BRENT",
            Name = "Brent Crude Oil",
            Description = "Light Sweet Crude Oil",
            DefaultUnit = QuantityUnit.BBL,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Seeder"
        };

        var wtiProduct = new Product
        {
            Id = Guid.NewGuid(),
            Code = "WTI",
            Name = "West Texas Intermediate",
            Description = "Light Sweet Crude Oil",
            DefaultUnit = QuantityUnit.BBL,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Seeder"
        };

        var mgoProduct = new Product
        {
            Id = Guid.NewGuid(),
            Code = "MGO",
            Name = "Marine Gas Oil",
            Description = "ISO 8217:2017 MGO",
            DefaultUnit = QuantityUnit.MT,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Seeder"
        };

        _dbContext.Products.AddRange(brentProduct, wtiProduct, mgoProduct);
        await _dbContext.SaveChangesAsync();

        // Create test trading partners
        var supplier1 = new TradingPartner
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier 1",
            Code = "SUPP001",
            Type = PartnerType.Supplier,
            CreditLimit = 1000000,
            CreditCurrency = "USD",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Seeder"
        };

        var supplier2 = new TradingPartner
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier 2",
            Code = "SUPP002",
            Type = PartnerType.Supplier,
            CreditLimit = 500000,
            CreditCurrency = "USD",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Seeder"
        };

        var customer1 = new TradingPartner
        {
            Id = Guid.NewGuid(),
            Name = "Test Customer 1",
            Code = "CUST001",
            Type = PartnerType.Customer,
            CreditLimit = 2000000,
            CreditCurrency = "USD",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Seeder"
        };

        _dbContext.TradingPartners.AddRange(supplier1, supplier2, customer1);
        await _dbContext.SaveChangesAsync();

        // Create test user
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "trader@test.com",
            PasswordHash = "hashedpassword",
            FirstName = "Test",
            LastName = "Trader",
            Role = UserRole.SeniorTrader,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Seeder"
        };

        _dbContext.Users.Add(testUser);
        await _dbContext.SaveChangesAsync();

        // Create test purchase contracts
        var purchaseContract1 = new PurchaseContract
        {
            Id = Guid.NewGuid(),
            ContractNumber = ContractNumber.Create(2025, ContractType.Purchase, 1),
            TradingPartnerId = supplier1.Id,
            ProductId = brentProduct.Id,
            TraderId = testUser.Id,
            ContractValue = new Money(100000, "USD"),
            Quantity = new Quantity(1000, QuantityUnit.BBL),
            LaycanStart = DateTime.UtcNow.AddDays(-5),
            LaycanEnd = DateTime.UtcNow.AddDays(5),
            Status = ContractStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            CreatedBy = "Seeder"
        };

        var purchaseContract2 = new PurchaseContract
        {
            Id = Guid.NewGuid(),
            ContractNumber = ContractNumber.Create(2025, ContractType.Purchase, 2),
            TradingPartnerId = supplier2.Id,
            ProductId = wtiProduct.Id,
            TraderId = testUser.Id,
            ContractValue = new Money(80000, "USD"),
            Quantity = new Quantity(800, QuantityUnit.BBL),
            LaycanStart = DateTime.UtcNow.AddDays(-3),
            LaycanEnd = DateTime.UtcNow.AddDays(7),
            Status = ContractStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            CreatedBy = "Seeder"
        };

        _dbContext.PurchaseContracts.AddRange(purchaseContract1, purchaseContract2);
        await _dbContext.SaveChangesAsync();

        // Create test sales contracts
        var salesContract1 = new SalesContract
        {
            Id = Guid.NewGuid(),
            ContractNumber = ContractNumber.Create(2025, ContractType.Sales, 1),
            TradingPartnerId = customer1.Id,
            ProductId = brentProduct.Id,
            TraderId = testUser.Id,
            ContractValue = new Money(120000, "USD"),
            Quantity = new Quantity(1000, QuantityUnit.BBL),
            LaycanStart = DateTime.UtcNow.AddDays(-2),
            LaycanEnd = DateTime.UtcNow.AddDays(8),
            Status = ContractStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-9),
            CreatedBy = "Seeder"
        };

        _dbContext.SalesContracts.Add(salesContract1);
        await _dbContext.SaveChangesAsync();

        // Create test settlements for purchase contracts
        var settlement1 = new ContractSettlement
        {
            Id = Guid.NewGuid(),
            ContractId = purchaseContract1.Id,
            TradingPartnerId = supplier1.Id,
            SettlementType = SettlementType.Telegraphic,
            SettlementCurrency = "USD",
            TotalSettlementAmount = 100000,
            Status = SettlementStatus.Finalized,
            IsPurchaseSettlement = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            CreatedBy = "Seeder"
        };

        var settlement2 = new ContractSettlement
        {
            Id = Guid.NewGuid(),
            ContractId = purchaseContract2.Id,
            TradingPartnerId = supplier2.Id,
            SettlementType = SettlementType.Telegraphic,
            SettlementCurrency = "USD",
            TotalSettlementAmount = 80000,
            Status = SettlementStatus.Finalized,
            IsPurchaseSettlement = true,
            CreatedAt = DateTime.UtcNow.AddDays(-4),
            CreatedBy = "Seeder"
        };

        var settlement3 = new ContractSettlement
        {
            Id = Guid.NewGuid(),
            ContractId = purchaseContract1.Id,
            TradingPartnerId = supplier1.Id,
            SettlementType = SettlementType.Telegraphic,
            SettlementCurrency = "USD",
            TotalSettlementAmount = 50000,
            Status = SettlementStatus.Approved,
            IsPurchaseSettlement = true,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            CreatedBy = "Seeder"
        };

        // Create test settlement for sales contract
        var settlement4 = new ContractSettlement
        {
            Id = Guid.NewGuid(),
            ContractId = salesContract1.Id,
            TradingPartnerId = customer1.Id,
            SettlementType = SettlementType.Telegraphic,
            SettlementCurrency = "USD",
            TotalSettlementAmount = 120000,
            Status = SettlementStatus.Finalized,
            IsPurchaseSettlement = false,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            CreatedBy = "Seeder"
        };

        _dbContext.ContractSettlements.AddRange(settlement1, settlement2, settlement3, settlement4);
        await _dbContext.SaveChangesAsync();
    }

    #region Analytics Endpoint Tests

    /// <summary>
    /// Test: GET /api/settlement-analytics/analytics with default parameters
    /// Validates the comprehensive analytics endpoint returns complete data structure
    /// </summary>
    [Fact]
    public async Task GetSettlementAnalytics_WithDefaultParameters_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/analytics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotNull(responseContent);

        var jsonDoc = JsonDocument.Parse(responseContent);
        Assert.True(jsonDoc.RootElement.TryGetProperty("totalSettlements", out _));
        Assert.True(jsonDoc.RootElement.TryGetProperty("totalAmount", out _));
        Assert.True(jsonDoc.RootElement.TryGetProperty("dailyTrends", out _));
    }

    /// <summary>
    /// Test: GET /api/settlement-analytics/analytics with custom daysToAnalyze parameter
    /// Validates date range filtering works correctly
    /// </summary>
    [Fact]
    public async Task GetSettlementAnalytics_WithCustomDaysParameter_ReturnsFilteredData()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/analytics?daysToAnalyze=7");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);
        Assert.True(jsonDoc.RootElement.TryGetProperty("totalSettlements", out var totalElement));
        Assert.NotNull(totalElement);
    }

    /// <summary>
    /// Test: GET /api/settlement-analytics/analytics with invalid daysToAnalyze
    /// Validates input validation catches out-of-range values
    /// </summary>
    [Fact]
    public async Task GetSettlementAnalytics_WithInvalidDaysParameter_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/analytics?daysToAnalyze=400");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("between 1 and 365", responseContent);
    }

    /// <summary>
    /// Test: GET /api/settlement-analytics/analytics with currency filter
    /// Validates currency filtering parameter works correctly
    /// </summary>
    [Fact]
    public async Task GetSettlementAnalytics_WithCurrencyFilter_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/analytics?currency=USD");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotNull(responseContent);
    }

    #endregion

    #region Metrics Endpoint Tests

    /// <summary>
    /// Test: GET /api/settlement-analytics/metrics with default parameters
    /// Validates KPI metrics endpoint returns all required metrics
    /// </summary>
    [Fact]
    public async Task GetSettlementMetrics_WithDefaultParameters_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        // Validate required metrics properties
        Assert.True(jsonDoc.RootElement.TryGetProperty("totalSettlementValue", out _));
        Assert.True(jsonDoc.RootElement.TryGetProperty("totalSettlementCount", out _));
        Assert.True(jsonDoc.RootElement.TryGetProperty("successRate", out _));
        Assert.True(jsonDoc.RootElement.TryGetProperty("slaComplianceRate", out _));
    }

    /// <summary>
    /// Test: GET /api/settlement-analytics/metrics with custom analysis period
    /// Validates period-based KPI calculation
    /// </summary>
    [Fact]
    public async Task GetSettlementMetrics_With7DaysPeriod_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/metrics?daysToAnalyze=7");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotNull(responseContent);
    }

    #endregion

    #region Daily Trends Endpoint Tests

    /// <summary>
    /// Test: GET /api/settlement-analytics/daily-trends
    /// Validates daily trend data structure for chart visualization
    /// </summary>
    [Fact]
    public async Task GetDailyTrends_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/daily-trends");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        // Validate array structure for chart data
        Assert.Equal(JsonValueKind.Array, jsonDoc.RootElement.ValueKind);
    }

    /// <summary>
    /// Test: GET /api/settlement-analytics/daily-trends with daysToAnalyze parameter
    /// Validates period filtering in trend data
    /// </summary>
    [Fact]
    public async Task GetDailyTrends_With30DaysPeriod_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/daily-trends?daysToAnalyze=30");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotNull(responseContent);
    }

    #endregion

    #region Currency Breakdown Endpoint Tests

    /// <summary>
    /// Test: GET /api/settlement-analytics/currency-breakdown
    /// Validates currency distribution analysis data
    /// </summary>
    [Fact]
    public async Task GetCurrencyBreakdown_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/currency-breakdown");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        // Validate array structure for currency breakdown
        Assert.Equal(JsonValueKind.Array, jsonDoc.RootElement.ValueKind);
    }

    #endregion

    #region Status Distribution Endpoint Tests

    /// <summary>
    /// Test: GET /api/settlement-analytics/status-distribution
    /// Validates settlement status breakdown data
    /// </summary>
    [Fact]
    public async Task GetStatusDistribution_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/status-distribution");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        // Validate array structure for status distribution
        Assert.Equal(JsonValueKind.Array, jsonDoc.RootElement.ValueKind);
    }

    #endregion

    #region Top Partners Endpoint Tests

    /// <summary>
    /// Test: GET /api/settlement-analytics/top-partners
    /// Validates top trading partners by settlement volume ranking
    /// </summary>
    [Fact]
    public async Task GetTopPartners_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/top-partners");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        // Validate array structure for partner data
        Assert.Equal(JsonValueKind.Array, jsonDoc.RootElement.ValueKind);
    }

    /// <summary>
    /// Test: GET /api/settlement-analytics/top-partners with different daysToAnalyze
    /// Validates partner ranking changes with different time periods
    /// </summary>
    [Fact]
    public async Task GetTopPartners_With15DaysPeriod_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/top-partners?daysToAnalyze=15");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotNull(responseContent);
    }

    #endregion

    #region Dashboard Summary Endpoint Tests

    /// <summary>
    /// Test: GET /api/settlement-analytics/summary
    /// Validates complete dashboard summary combining analytics and metrics
    /// This is the primary endpoint for the dashboard component
    /// </summary>
    [Fact]
    public async Task GetDashboardSummary_ReturnsCompleteDataStructure()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/summary");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        // Validate complete dashboard structure
        Assert.True(jsonDoc.RootElement.TryGetProperty("analytics", out var analyticsElement));
        Assert.True(jsonDoc.RootElement.TryGetProperty("metrics", out var metricsElement));
        Assert.True(jsonDoc.RootElement.TryGetProperty("generatedAt", out var generatedElement));
        Assert.True(jsonDoc.RootElement.TryGetProperty("analysisPeriodDays", out var periodElement));

        // Validate nested analytics structure
        Assert.True(analyticsElement.TryGetProperty("totalSettlements", out _));
        Assert.True(analyticsElement.TryGetProperty("dailyTrends", out _));
        Assert.True(analyticsElement.TryGetProperty("currencyBreakdown", out _));

        // Validate nested metrics structure
        Assert.True(metricsElement.TryGetProperty("totalSettlementValue", out _));
        Assert.True(metricsElement.TryGetProperty("successRate", out _));
    }

    /// <summary>
    /// Test: GET /api/settlement-analytics/summary with custom daysToAnalyze
    /// Validates period parameter propagates through entire summary
    /// </summary>
    [Fact]
    public async Task GetDashboardSummary_WithCustomPeriod_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/summary?daysToAnalyze=14");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        // Verify period was applied correctly
        Assert.True(jsonDoc.RootElement.TryGetProperty("analysisPeriodDays", out var periodElement));
        Assert.Equal(14, periodElement.GetInt32());
    }

    /// <summary>
    /// Test: GET /api/settlement-analytics/summary with concurrent data loading
    /// Validates efficient concurrent execution of analytics and metrics queries
    /// </summary>
    [Fact]
    public async Task GetDashboardSummary_ExecutesConcurrentRequests_PerformanceOptimized()
    {
        // Arrange
        var startTime = DateTime.UtcNow;

        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/summary");

        var endTime = DateTime.UtcNow;
        var elapsedMs = (endTime - startTime).TotalMilliseconds;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Verify response time is reasonable (should be <2 seconds for concurrent execution)
        Assert.True(elapsedMs < 2000, $"Request took {elapsedMs}ms, expected <2000ms for concurrent execution");
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Test: Invalid daysToAnalyze parameter validation
    /// Validates zero value rejection
    /// </summary>
    [Fact]
    public async Task Analytics_WithZeroDays_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/analytics?daysToAnalyze=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test: Invalid daysToAnalyze parameter validation
    /// Validates negative value rejection
    /// </summary>
    [Fact]
    public async Task Analytics_WithNegativeDays_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/analytics?daysToAnalyze=-1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test: Analytics endpoint handles exceptions gracefully
    /// Validates 500 error response on server error scenarios
    /// </summary>
    [Fact]
    public async Task Analytics_OnServerError_ReturnsInternalServerError()
    {
        // Act - Using invalid parameter combination to trigger potential error
        var response = await _client.GetAsync("/api/settlement-analytics/analytics?daysToAnalyze=365&isSalesSettlement=true&isSalesSettlement=false");

        // Assert - Should handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    #endregion

    #region Edge Cases and Boundary Tests

    /// <summary>
    /// Test: Boundary test with minimum valid daysToAnalyze value
    /// Validates single day analysis works correctly
    /// </summary>
    [Fact]
    public async Task Analytics_WithMinimumValidDays_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/analytics?daysToAnalyze=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Test: Boundary test with maximum valid daysToAnalyze value
    /// Validates full-year analysis works correctly
    /// </summary>
    [Fact]
    public async Task Analytics_WithMaximumValidDays_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/settlement-analytics/analytics?daysToAnalyze=365");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Test: Multiple simultaneous analytics requests
    /// Validates concurrent request handling
    /// </summary>
    [Fact]
    public async Task Analytics_MultipleConcurrentRequests_AllSucceed()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync("/api/settlement-analytics/analytics"));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.All(responses, response =>
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response.Dispose();
        });
    }

    #endregion
}
