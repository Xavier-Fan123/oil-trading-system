namespace OilTrading.Core.Entities;

/// <summary>
/// Represents an archived report execution with retention and storage information
/// </summary>
public class ReportArchive
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public DateTime ArchiveDate { get; set; }
    public int RetentionDays { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public bool IsCompressed { get; set; }
    public long FileSize { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual ReportExecution? ReportExecution { get; set; }
}
