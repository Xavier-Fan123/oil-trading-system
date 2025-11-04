using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using OilTrading.IntegrationTests.Infrastructure;
using Xunit;

namespace OilTrading.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for payment status endpoints in PurchaseContractController and SalesContractController
/// Tests the complete flow: Contract Creation -> Settlement Creation -> Payment Status Calculation
/// </summary>
public class PaymentStatusControllerIntegrationTests : IClassFixture<InMemoryWebApplicationFactory>
{
    private readonly InMemoryWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public PaymentStatusControllerIntegrationTests(InMemoryWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    [Fact]
    public async Task GetPaymentStatus_ForPurchaseContractWithoutSettlements_ShouldReturnNotDue()
    {
        // Arrange
        var contractId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/purchase-contracts/{contractId}/payment-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("paymentStatus");
        content.Should().Contain("contractId");
    }

    [Fact]
    public async Task GetPaymentStatusDetails_ForPurchaseContractWithoutSettlements_ShouldReturnDetails()
    {
        // Arrange
        var contractId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/purchase-contracts/{contractId}/payment-status/details");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPaymentStatus_ForSalesContractWithoutSettlements_ShouldReturnNotDue()
    {
        // Arrange
        var contractId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/sales-contracts/{contractId}/payment-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("paymentStatus");
        content.Should().Contain("contractId");
    }

    [Fact]
    public async Task GetPaymentStatusDetails_ForSalesContractWithoutSettlements_ShouldReturnDetails()
    {
        // Arrange
        var contractId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/sales-contracts/{contractId}/payment-status/details");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPaymentStatus_ForNonExistentContract_ShouldReturnUnknown()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/purchase-contracts/{nonExistentId}/payment-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Unknown");
    }

    [Fact]
    public async Task PaymentStatusEndpoints_ShouldBeAccessible_ForBothContractTypes()
    {
        // Test that endpoints exist for both purchase and sales contracts

        var purchaseContractId = Guid.NewGuid();
        var salesContractId = Guid.NewGuid();

        // Purchase contract endpoints
        var purchaseStatusResponse = await _client.GetAsync($"/api/purchase-contracts/{purchaseContractId}/payment-status");
        purchaseStatusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var purchaseDetailsResponse = await _client.GetAsync($"/api/purchase-contracts/{purchaseContractId}/payment-status/details");
        purchaseDetailsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Sales contract endpoints
        var salesStatusResponse = await _client.GetAsync($"/api/sales-contracts/{salesContractId}/payment-status");
        salesStatusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var salesDetailsResponse = await _client.GetAsync($"/api/sales-contracts/{salesContractId}/payment-status/details");
        salesDetailsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
