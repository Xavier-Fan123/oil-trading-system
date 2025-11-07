using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

/// <summary>
/// Represents a report configuration for advanced reporting system.
/// </summary>
public class ReportConfiguration
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ReportType { get; set; } = string.Empty; // ContractExecution, SettlementSummary, PaymentStatus, RiskAnalysis, Custom
    public string? FilterJson { get; set; } // Stores filter criteria as JSON
    public string? ColumnsJson { get; set; } // Stores selected columns as JSON
    public string ExportFormat { get; set; } = "CSV"; // CSV, Excel, PDF, JSON
    public bool IncludeMetadata { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    // Audit fields
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Concurrency control
    public byte[] RowVersion { get; set; } = [];

    // Navigation properties
    public virtual ICollection<ReportSchedule> Schedules { get; set; } = [];
    public virtual ICollection<ReportDistribution> Distributions { get; set; } = [];
    public virtual ICollection<ReportExecution> Executions { get; set; } = [];
    public virtual User? CreatedByUser { get; set; }
    public virtual User? UpdatedByUser { get; set; }
}
