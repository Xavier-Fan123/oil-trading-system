namespace OilTrading.Core.Entities;

/// <summary>
/// Represents a schedule for automated report execution.
/// </summary>
public class ReportSchedule
{
    public Guid Id { get; set; }
    public Guid ReportConfigId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string Frequency { get; set; } = string.Empty; // Once, Daily, Weekly, Monthly, Quarterly, Annually
    public int? DayOfWeek { get; set; } // 0-6 (Sunday-Saturday) for weekly
    public int? DayOfMonth { get; set; } // 1-31 for monthly
    public string? Time { get; set; } // HH:mm format
    public string? Timezone { get; set; } // IANA timezone
    public DateTime? NextRunDate { get; set; }
    public DateTime? LastRunDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual ReportConfiguration? ReportConfig { get; set; }
}
