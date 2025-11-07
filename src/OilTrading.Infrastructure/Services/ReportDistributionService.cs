using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;
using System.Text.Json;

namespace OilTrading.Infrastructure.Services;

/// <summary>
/// Service for managing report distribution channels (Email, SFTP, Webhook)
/// </summary>
public class ReportDistributionService : IReportDistributionService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailDistributionService _emailService;
    private readonly ISftpDistributionService _sftpService;
    private readonly IWebhookDistributionService _webhookService;
    private readonly ILogger<ReportDistributionService> _logger;

    public ReportDistributionService(
        ApplicationDbContext context,
        IEmailDistributionService emailService,
        ISftpDistributionService sftpService,
        IWebhookDistributionService webhookService,
        ILogger<ReportDistributionService> logger)
    {
        _context = context;
        _emailService = emailService;
        _sftpService = sftpService;
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new distribution channel
    /// </summary>
    public async Task<ReportDistribution> CreateAsync(
        Guid reportConfigId,
        string channelType,
        string channelName,
        string channelConfiguration,
        bool isEnabled = true,
        Guid? createdBy = null)
    {
        _logger.LogInformation("Creating distribution channel: {ConfigId}, Type: {ChannelType}", reportConfigId, channelType);

        var config = await _context.ReportConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == reportConfigId && !c.IsDeleted);

        if (config == null)
        {
            throw new InvalidOperationException($"Report configuration not found: {reportConfigId}");
        }

        var distribution = new ReportDistribution
        {
            Id = Guid.NewGuid(),
            ReportConfigId = reportConfigId,
            ChannelType = channelType,
            ChannelName = channelName,
            ChannelConfiguration = channelConfiguration,
            IsEnabled = isEnabled,
            MaxRetries = 3,
            RetryDelaySeconds = 300,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = createdBy,
            IsDeleted = false
        };

        _context.ReportDistributions.Add(distribution);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Distribution channel created: {DistributionId}", distribution.Id);
        return distribution;
    }

    /// <summary>
    /// Get a specific distribution channel by ID
    /// </summary>
    public async Task<ReportDistribution?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving distribution channel: {DistributionId}", id);

        var distribution = await _context.ReportDistributions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (distribution == null)
        {
            _logger.LogWarning("Distribution channel not found: {DistributionId}", id);
        }

        return distribution;
    }

    /// <summary>
    /// Get all distribution channels for a report configuration
    /// </summary>
    public async Task<List<ReportDistribution>> GetByConfigAsync(Guid configId)
    {
        _logger.LogInformation("Retrieving distribution channels for config: {ConfigId}", configId);

        var distributions = await _context.ReportDistributions
            .Where(d => d.ReportConfigId == configId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} distribution channels for config {ConfigId}", distributions.Count, configId);
        return distributions;
    }

    /// <summary>
    /// Update a distribution channel
    /// </summary>
    public async Task<ReportDistribution?> UpdateAsync(
        Guid id,
        string? channelName = null,
        string? channelConfiguration = null,
        bool? isEnabled = null,
        int? maxRetries = null,
        int? retryDelaySeconds = null,
        Guid? updatedBy = null)
    {
        _logger.LogInformation("Updating distribution channel: {DistributionId}", id);

        var distribution = await _context.ReportDistributions
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (distribution == null)
        {
            _logger.LogWarning("Distribution channel not found for update: {DistributionId}", id);
            return null;
        }

        if (!string.IsNullOrEmpty(channelName))
            distribution.ChannelName = channelName;
        if (!string.IsNullOrEmpty(channelConfiguration))
            distribution.ChannelConfiguration = channelConfiguration;
        if (isEnabled.HasValue)
            distribution.IsEnabled = isEnabled.Value;
        if (maxRetries.HasValue)
            distribution.MaxRetries = maxRetries.Value;
        if (retryDelaySeconds.HasValue)
            distribution.RetryDelaySeconds = retryDelaySeconds.Value;

        distribution.UpdatedBy = updatedBy;
        distribution.UpdatedDate = DateTime.UtcNow;

        _context.ReportDistributions.Update(distribution);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Distribution channel updated: {DistributionId}", distribution.Id);
        return distribution;
    }

    /// <summary>
    /// Test a distribution channel
    /// </summary>
    public async Task<(bool success, string message)> TestChannelAsync(Guid id)
    {
        _logger.LogInformation("Testing distribution channel: {DistributionId}", id);

        var distribution = await GetByIdAsync(id);
        if (distribution == null)
        {
            _logger.LogWarning("Distribution channel not found for testing: {DistributionId}", id);
            return (false, "Distribution channel not found");
        }

        bool testResult = false;
        string testMessage = "";

        try
        {
            testResult = distribution.ChannelType.ToLower() switch
            {
                "email" => await _emailService.TestAsync(distribution.ChannelConfiguration),
                "sftp" => await _sftpService.TestAsync(distribution.ChannelConfiguration),
                "webhook" => await _webhookService.TestAsync(distribution.ChannelConfiguration),
                _ => throw new InvalidOperationException($"Unknown channel type: {distribution.ChannelType}")
            };

            testMessage = testResult ? "Test successful" : "Test failed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing distribution channel: {DistributionId}", id);
            testMessage = $"Test error: {ex.Message}";
            testResult = false;
        }

        // Update test status
        distribution = await _context.ReportDistributions
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (distribution != null)
        {
            distribution.LastTestedDate = DateTime.UtcNow;
            distribution.LastTestStatus = testResult ? "Success" : "Failed";
            distribution.LastTestMessage = testMessage;
            _context.ReportDistributions.Update(distribution);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Channel test result: {DistributionId}, Success: {Success}", id, testResult);
        return (testResult, testMessage);
    }

    /// <summary>
    /// Send a report via distribution channel
    /// </summary>
    public async Task<(bool success, string message)> SendAsync(Guid id, byte[] fileContent, string fileName)
    {
        _logger.LogInformation("Sending report via distribution channel: {DistributionId}", id);

        var distribution = await GetByIdAsync(id);
        if (distribution == null || !distribution.IsEnabled)
        {
            _logger.LogWarning("Distribution channel unavailable: {DistributionId}", id);
            return (false, "Distribution channel not found or disabled");
        }

        try
        {
            var success = distribution.ChannelType.ToLower() switch
            {
                "email" => await _emailService.SendAsync(distribution, fileContent, fileName),
                "sftp" => await _sftpService.SendAsync(distribution, fileContent, fileName),
                "webhook" => await _webhookService.SendAsync(distribution, fileContent, fileName),
                _ => throw new InvalidOperationException($"Unknown channel type: {distribution.ChannelType}")
            };

            var message = success ? "Report sent successfully" : "Failed to send report";
            _logger.LogInformation("Send result: {DistributionId}, Success: {Success}", id, success);
            return (success, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending report via channel: {DistributionId}", id);
            return (false, $"Send error: {ex.Message}");
        }
    }

    /// <summary>
    /// Soft delete a distribution channel
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Deleting distribution channel: {DistributionId}", id);

        var distribution = await _context.ReportDistributions
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (distribution == null)
        {
            _logger.LogWarning("Distribution channel not found for deletion: {DistributionId}", id);
            return false;
        }

        distribution.IsDeleted = true;
        distribution.UpdatedDate = DateTime.UtcNow;

        _context.ReportDistributions.Update(distribution);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Distribution channel deleted: {DistributionId}", id);
        return true;
    }

    /// <summary>
    /// Get enabled distribution channels by type
    /// </summary>
    public async Task<List<ReportDistribution>> GetEnabledByTypeAsync(Guid configId, string channelType)
    {
        _logger.LogInformation("Retrieving enabled {ChannelType} distributions for config: {ConfigId}", channelType, configId);

        var distributions = await _context.ReportDistributions
            .Where(d => d.ReportConfigId == configId &&
                   d.ChannelType == channelType &&
                   d.IsEnabled &&
                   !d.IsDeleted)
            .OrderByDescending(d => d.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} enabled {ChannelType} distributions", distributions.Count, channelType);
        return distributions;
    }

    /// <summary>
    /// Get all enabled distribution channels for a configuration
    /// </summary>
    public async Task<List<ReportDistribution>> GetEnabledByConfigAsync(Guid configId)
    {
        _logger.LogInformation("Retrieving all enabled distributions for config: {ConfigId}", configId);

        var distributions = await _context.ReportDistributions
            .Where(d => d.ReportConfigId == configId &&
                   d.IsEnabled &&
                   !d.IsDeleted)
            .OrderByDescending(d => d.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} enabled distributions for config {ConfigId}", distributions.Count, configId);
        return distributions;
    }
}

/// <summary>
/// Email distribution service
/// </summary>
public class EmailDistributionService : IEmailDistributionService
{
    private readonly ILogger<EmailDistributionService> _logger;

    public EmailDistributionService(ILogger<EmailDistributionService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> TestAsync(string configuration)
    {
        _logger.LogInformation("Testing email distribution configuration");
        // TODO: Implement email configuration validation
        await Task.Delay(100); // Simulate async operation
        return true;
    }

    public async Task<bool> SendAsync(ReportDistribution distribution, byte[] fileContent, string fileName)
    {
        _logger.LogInformation("Sending report via email: {ChannelName}", distribution.ChannelName);

        try
        {
            // Parse email configuration
            var config = JsonSerializer.Deserialize<EmailConfig>(distribution.ChannelConfiguration) ??
                        throw new InvalidOperationException("Invalid email configuration");

            // TODO: Implement actual email sending via SMTP
            _logger.LogInformation("Email would be sent to: {Recipients}", config.Recipients);

            await Task.Delay(100); // Simulate async operation
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email: {ChannelName}", distribution.ChannelName);
            return false;
        }
    }

    public async Task<bool> RetryAsync(Guid executionId, int attemptNumber)
    {
        _logger.LogInformation("Retrying email distribution: {ExecutionId}, Attempt: {Attempt}", executionId, attemptNumber);
        await Task.Delay(100 * attemptNumber); // Exponential backoff
        return true;
    }

    private class EmailConfig
    {
        public string? Recipients { get; set; }
        public string? Subject { get; set; }
        public string? SmtpServer { get; set; }
        public int SmtpPort { get; set; }
    }
}

/// <summary>
/// SFTP distribution service
/// </summary>
public class SftpDistributionService : ISftpDistributionService
{
    private readonly ILogger<SftpDistributionService> _logger;

    public SftpDistributionService(ILogger<SftpDistributionService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> TestAsync(string configuration)
    {
        _logger.LogInformation("Testing SFTP distribution configuration");
        // TODO: Implement SFTP connection test
        await Task.Delay(100); // Simulate async operation
        return true;
    }

    public async Task<bool> SendAsync(ReportDistribution distribution, byte[] fileContent, string fileName)
    {
        _logger.LogInformation("Sending report via SFTP: {ChannelName}", distribution.ChannelName);

        try
        {
            // Parse SFTP configuration
            var config = JsonSerializer.Deserialize<SftpConfig>(distribution.ChannelConfiguration) ??
                        throw new InvalidOperationException("Invalid SFTP configuration");

            // TODO: Implement actual SFTP file transfer
            _logger.LogInformation("File would be uploaded to: {Host}:{Path}", config.Host, config.RemotePath);

            await Task.Delay(100); // Simulate async operation
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending via SFTP: {ChannelName}", distribution.ChannelName);
            return false;
        }
    }

    public async Task<bool> RetryAsync(Guid executionId, int attemptNumber)
    {
        _logger.LogInformation("Retrying SFTP distribution: {ExecutionId}, Attempt: {Attempt}", executionId, attemptNumber);
        await Task.Delay(100 * attemptNumber); // Exponential backoff
        return true;
    }

    private class SftpConfig
    {
        public string? Host { get; set; }
        public int Port { get; set; }
        public string? Username { get; set; }
        public string? RemotePath { get; set; }
    }
}

/// <summary>
/// Webhook distribution service
/// </summary>
public class WebhookDistributionService : IWebhookDistributionService
{
    private readonly ILogger<WebhookDistributionService> _logger;

    public WebhookDistributionService(ILogger<WebhookDistributionService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> TestAsync(string configuration)
    {
        _logger.LogInformation("Testing webhook distribution configuration");
        // TODO: Implement webhook URL validation
        await Task.Delay(100); // Simulate async operation
        return true;
    }

    public async Task<bool> SendAsync(ReportDistribution distribution, byte[] fileContent, string fileName)
    {
        _logger.LogInformation("Sending report via webhook: {ChannelName}", distribution.ChannelName);

        try
        {
            // Parse webhook configuration
            var config = JsonSerializer.Deserialize<WebhookConfig>(distribution.ChannelConfiguration) ??
                        throw new InvalidOperationException("Invalid webhook configuration");

            // TODO: Implement actual webhook POST request with file content
            _logger.LogInformation("Webhook POST would be sent to: {Url}", config.Url);

            await Task.Delay(100); // Simulate async operation
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending via webhook: {ChannelName}", distribution.ChannelName);
            return false;
        }
    }

    public async Task<bool> RetryAsync(Guid executionId, int attemptNumber)
    {
        _logger.LogInformation("Retrying webhook distribution: {ExecutionId}, Attempt: {Attempt}", executionId, attemptNumber);
        await Task.Delay(100 * attemptNumber); // Exponential backoff
        return true;
    }

    private class WebhookConfig
    {
        public string? Url { get; set; }
        public string? AuthHeader { get; set; }
        public string? Method { get; set; }
    }
}

/// <summary>
/// Interface for report distribution service
/// </summary>
public interface IReportDistributionService
{
    Task<ReportDistribution> CreateAsync(
        Guid reportConfigId,
        string channelType,
        string channelName,
        string channelConfiguration,
        bool isEnabled = true,
        Guid? createdBy = null);
    Task<ReportDistribution?> GetByIdAsync(Guid id);
    Task<List<ReportDistribution>> GetByConfigAsync(Guid configId);
    Task<ReportDistribution?> UpdateAsync(
        Guid id,
        string? channelName = null,
        string? channelConfiguration = null,
        bool? isEnabled = null,
        int? maxRetries = null,
        int? retryDelaySeconds = null,
        Guid? updatedBy = null);
    Task<(bool success, string message)> TestChannelAsync(Guid id);
    Task<(bool success, string message)> SendAsync(Guid id, byte[] fileContent, string fileName);
    Task<bool> DeleteAsync(Guid id);
    Task<List<ReportDistribution>> GetEnabledByTypeAsync(Guid configId, string channelType);
    Task<List<ReportDistribution>> GetEnabledByConfigAsync(Guid configId);
}

public interface IEmailDistributionService
{
    Task<bool> TestAsync(string configuration);
    Task<bool> SendAsync(ReportDistribution distribution, byte[] fileContent, string fileName);
    Task<bool> RetryAsync(Guid executionId, int attemptNumber);
}

public interface ISftpDistributionService
{
    Task<bool> TestAsync(string configuration);
    Task<bool> SendAsync(ReportDistribution distribution, byte[] fileContent, string fileName);
    Task<bool> RetryAsync(Guid executionId, int attemptNumber);
}

public interface IWebhookDistributionService
{
    Task<bool> TestAsync(string configuration);
    Task<bool> SendAsync(ReportDistribution distribution, byte[] fileContent, string fileName);
    Task<bool> RetryAsync(Guid executionId, int attemptNumber);
}
