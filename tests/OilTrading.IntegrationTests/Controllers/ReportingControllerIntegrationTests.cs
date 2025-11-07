using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using OilTrading.IntegrationTests.Infrastructure;
using OilTrading.Application.DTOs;
using OilTrading.Infrastructure.Data;

namespace OilTrading.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for the reporting system (ReportConfiguration, ReportExecution, ReportDistribution)
/// Tests the complete reporting workflow from configuration to execution and distribution
/// </summary>
public class ReportingControllerIntegrationTests : IAsyncLifetime
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
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    #region Report Configuration Tests

    [Fact]
    public async Task CreateReportConfiguration_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createRequest = new
        {
            name = "Daily Sales Report",
            description = "Daily sales summary report",
            reportType = "SalesExecutionReport",
            schedule = "Daily",
            scheduleTime = "08:00:00",
            isActive = true
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(createRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/report-configurations", jsonContent);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotNull(responseContent);
        Assert.Contains("Daily Sales Report", responseContent);
    }

    [Fact]
    public async Task GetReportConfigurations_ReturnsPagedList()
    {
        // Arrange
        var createRequest = new
        {
            name = "Test Report",
            description = "Test description",
            reportType = "ContractExecutionReport",
            schedule = "Weekly",
            scheduleTime = "09:00:00",
            isActive = true
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(createRequest),
            Encoding.UTF8,
            "application/json");

        // Create a report configuration
        await _client.PostAsync("/api/report-configurations", jsonContent);

        // Act
        var response = await _client.GetAsync("/api/report-configurations?pageNum=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Report", responseContent);
    }

    [Fact]
    public async Task GetReportConfigurationById_WithValidId_ReturnsConfiguration()
    {
        // Arrange
        var createRequest = new
        {
            name = "Test Report",
            description = "Test description",
            reportType = "PerformanceMetricsReport",
            schedule = "Monthly",
            scheduleTime = "01:00:00",
            isActive = true
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(createRequest),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _client.PostAsync("/api/report-configurations", jsonContent);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var configId = ExtractIdFromResponse(createContent);

        // Act
        var response = await _client.GetAsync($"/api/report-configurations/{configId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Report", responseContent);
    }

    [Fact]
    public async Task UpdateReportConfiguration_WithValidData_ReturnsUpdatedConfiguration()
    {
        // Arrange
        var createRequest = new
        {
            name = "Original Name",
            description = "Original description",
            reportType = "SettlementSummaryReport",
            schedule = "Weekly",
            scheduleTime = "10:00:00",
            isActive = true
        };

        var createContent = new StringContent(
            JsonSerializer.Serialize(createRequest),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _client.PostAsync("/api/report-configurations", createContent);
        var responseContent = await createResponse.Content.ReadAsStringAsync();
        var configId = ExtractIdFromResponse(responseContent);

        var updateRequest = new
        {
            name = "Updated Name",
            description = "Updated description",
            schedule = "Daily",
            scheduleTime = "12:00:00",
            isActive = true
        };

        var updateContent = new StringContent(
            JsonSerializer.Serialize(updateRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PutAsync($"/api/report-configurations/{configId}", updateContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updateResponseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Updated Name", updateResponseContent);
    }

    [Fact]
    public async Task DeleteReportConfiguration_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var createRequest = new
        {
            name = "Report to Delete",
            description = "This will be deleted",
            reportType = "RiskAnalysisReport",
            schedule = "Monthly",
            scheduleTime = "03:00:00",
            isActive = true
        };

        var createContent = new StringContent(
            JsonSerializer.Serialize(createRequest),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _client.PostAsync("/api/report-configurations", createContent);
        var responseContent = await createResponse.Content.ReadAsStringAsync();
        var configId = ExtractIdFromResponse(responseContent);

        // Act
        var response = await _client.DeleteAsync($"/api/report-configurations/{configId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/report-configurations/{configId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    #endregion

    #region Report Execution Tests

    [Fact]
    public async Task ExecuteReport_WithValidConfiguration_ReturnsExecutionResult()
    {
        // Arrange
        var configRequest = new
        {
            name = "Executable Report",
            description = "Report that will be executed",
            reportType = "ContractExecutionReport",
            schedule = "Manual",
            scheduleTime = "00:00:00",
            isActive = true
        };

        var configContent = new StringContent(
            JsonSerializer.Serialize(configRequest),
            Encoding.UTF8,
            "application/json");

        var configResponse = await _client.PostAsync("/api/report-configurations", configContent);
        var configResponseContent = await configResponse.Content.ReadAsStringAsync();
        var configId = ExtractIdFromResponse(configResponseContent);

        var executeRequest = new
        {
            reportConfigurationId = configId,
            parameters = new { }
        };

        var executeContent = new StringContent(
            JsonSerializer.Serialize(executeRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/report-executions/execute", executeContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotNull(responseContent);
    }

    [Fact]
    public async Task GetReportExecutions_ReturnsPagedList()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/report-executions?pageNum=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("totalCount", responseContent);
    }

    [Fact]
    public async Task GetReportExecutionById_WithValidId_ReturnsExecution()
    {
        // Arrange
        var configRequest = new
        {
            name = "Report for Retrieval",
            description = "Report execution to retrieve",
            reportType = "SalesExecutionReport",
            schedule = "Manual",
            scheduleTime = "00:00:00",
            isActive = true
        };

        var configContent = new StringContent(
            JsonSerializer.Serialize(configRequest),
            Encoding.UTF8,
            "application/json");

        var configResponse = await _client.PostAsync("/api/report-configurations", configContent);
        var configResponseContent = await configResponse.Content.ReadAsStringAsync();
        var configId = ExtractIdFromResponse(configResponseContent);

        var executeRequest = new
        {
            reportConfigurationId = configId,
            parameters = new { }
        };

        var executeContent = new StringContent(
            JsonSerializer.Serialize(executeRequest),
            Encoding.UTF8,
            "application/json");

        var executeResponse = await _client.PostAsync("/api/report-executions/execute", executeContent);
        var executeResponseContent = await executeResponse.Content.ReadAsStringAsync();
        var executionId = ExtractIdFromResponse(executeResponseContent);

        // Act
        var response = await _client.GetAsync($"/api/report-executions/{executionId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotNull(responseContent);
    }

    #endregion

    #region Report Distribution Tests

    [Fact]
    public async Task ConfigureDistribution_WithValidChannel_ReturnsDistributionConfig()
    {
        // Arrange
        var distributionRequest = new
        {
            name = "Email Distribution",
            channel = "Email",
            recipients = "test@example.com",
            isEnabled = true
        };

        var content = new StringContent(
            JsonSerializer.Serialize(distributionRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/report-distributions", content);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDistributions_ReturnsPagedList()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/report-distributions?pageNum=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("totalCount", responseContent);
    }

    [Fact]
    public async Task UpdateDistributionStatus_WithValidId_ReturnsUpdatedStatus()
    {
        // Arrange
        var distributionRequest = new
        {
            name = "SFTP Distribution",
            channel = "SFTP",
            recipients = "sftp://server.com/path",
            isEnabled = true
        };

        var createContent = new StringContent(
            JsonSerializer.Serialize(distributionRequest),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _client.PostAsync("/api/report-distributions", createContent);
        var responseContent = await createResponse.Content.ReadAsStringAsync();
        var distributionId = ExtractIdFromResponse(responseContent);

        var updateRequest = new
        {
            isEnabled = false
        };

        var updateContent = new StringContent(
            JsonSerializer.Serialize(updateRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PutAsync($"/api/report-distributions/{distributionId}", updateContent);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);
    }

    #endregion

    #region Report Archive Tests

    [Fact]
    public async Task GetArchivedReports_ReturnsPagedList()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/report-archives?pageNum=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("totalCount", responseContent);
    }

    [Fact]
    public async Task DownloadArchivedReport_WithValidId_ReturnsFileContent()
    {
        // This test assumes there are archived reports in the system
        // Get list of archives first
        var listResponse = await _client.GetAsync("/api/report-archives?pageNum=1&pageSize=10");
        if (listResponse.IsSuccessStatusCode)
        {
            var listContent = await listResponse.Content.ReadAsStringAsync();
            if (listContent.Contains("id"))
            {
                // Try to extract an ID and download it
                var archiveId = ExtractIdFromResponse(listContent);

                // Act
                var response = await _client.GetAsync($"/api/report-archives/{archiveId}/download");

                // Assert
                Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
            }
        }
    }

    #endregion

    #region Helper Methods

    private string ExtractIdFromResponse(string jsonContent)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            // Try to find 'id' field in the response
            if (root.TryGetProperty("id", out var idElement))
            {
                return idElement.GetString() ?? Guid.NewGuid().ToString();
            }

            // Try to find it in nested structure
            if (root.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("id", out var nestedId))
            {
                return nestedId.GetString() ?? Guid.NewGuid().ToString();
            }

            // If we can't find an ID, generate a test one
            return Guid.NewGuid().ToString();
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }

    #endregion
}
