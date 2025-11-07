namespace OilTrading.Core.Entities;

/// <summary>
/// Represents an execution history record for a report configuration.
/// Tracks execution details, file output, and status information.
/// </summary>
public class ReportExecution
{
    public Guid Id { get; set; }
    public Guid ReportConfigId { get; set; }
    public DateTime ExecutionStartTime { get; set; }
    public DateTime? ExecutionEndTime { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed, Archived
    public string? ErrorMessage { get; set; }

    // Output file information
    public string? OutputFilePath { get; set; }
    public string? OutputFileName { get; set; }
    public long? FileSizeBytes { get; set; }
    public string OutputFileFormat { get; set; } = "CSV";

    // Execution metrics
    public int? RecordsProcessed { get; set; }
    public int? TotalRecords { get; set; }
    public double? DurationSeconds { get; set; }

    // Distribution tracking
    public int SuccessfulDistributions { get; set; }
    public int FailedDistributions { get; set; }

    // User and scheduling info
    public Guid? ExecutedBy { get; set; }
    public bool IsScheduled { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual ReportConfiguration? ReportConfig { get; set; }
    public virtual User? ExecutedByUser { get; set; }
}
