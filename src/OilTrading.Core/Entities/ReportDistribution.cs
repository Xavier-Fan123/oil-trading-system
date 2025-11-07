namespace OilTrading.Core.Entities;

/// <summary>
/// Represents a distribution channel configuration for report delivery.
/// Supports Email, SFTP, and Webhook distribution channels.
/// </summary>
public class ReportDistribution
{
    public Guid Id { get; set; }
    public Guid ReportConfigId { get; set; }
    public string ChannelName { get; set; } = string.Empty;
    public string ChannelType { get; set; } = string.Empty; // Email, SFTP, Webhook
    public string ChannelConfiguration { get; set; } = "{}"; // JSON string with channel-specific configuration
    public bool IsEnabled { get; set; } = true;

    // Test tracking
    public DateTime? LastTestedDate { get; set; }
    public string? LastTestStatus { get; set; } // Success, Failed
    public string? LastTestMessage { get; set; }

    // Retry configuration
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 300;

    // Audit fields
    public DateTime CreatedDate { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual ReportConfiguration? ReportConfig { get; set; }
}
