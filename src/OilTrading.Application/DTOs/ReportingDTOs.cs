namespace OilTrading.Application.DTOs;

/// <summary>
/// Request to create a new report configuration
/// </summary>
public record CreateReportConfigRequest(
    string Name,
    string? Description,
    string ReportType,
    Dictionary<string, object>? Filters,
    List<string>? Columns,
    string ExportFormat,
    bool IncludeMetadata
);

/// <summary>
/// Request to update an existing report configuration
/// </summary>
public record UpdateReportConfigRequest(
    string Name,
    string? Description,
    string ReportType,
    Dictionary<string, object>? Filters,
    List<string>? Columns,
    string ExportFormat,
    bool IncludeMetadata
);

/// <summary>
/// DTO for report configuration response
/// </summary>
public record ReportConfigurationDto(
    Guid Id,
    string Name,
    string? Description,
    string ReportType,
    Dictionary<string, object>? Filters,
    List<string>? Columns,
    string ExportFormat,
    bool IncludeMetadata,
    bool IsActive,
    DateTime CreatedDate,
    string? CreatedBy,
    DateTime? UpdatedDate,
    string? UpdatedBy
);

/// <summary>
/// Request to create a report schedule
/// </summary>
public record CreateScheduleRequest(
    string Frequency,
    string? Time,
    string? Timezone,
    int? DayOfWeek,
    int? DayOfMonth,
    bool Enabled
);

/// <summary>
/// Request to update a report schedule
/// </summary>
public record UpdateScheduleRequest(
    string? Frequency,
    string? Time,
    string? Timezone,
    int? DayOfWeek,
    int? DayOfMonth,
    bool? Enabled
);

/// <summary>
/// DTO for report schedule response
/// </summary>
public record ReportScheduleDto(
    Guid Id,
    Guid ReportConfigId,
    string Frequency,
    string? Time,
    string? Timezone,
    int? DayOfWeek,
    int? DayOfMonth,
    bool Enabled,
    DateTime? NextRunDate,
    DateTime? LastRunDate,
    DateTime CreatedDate
);

/// <summary>
/// Request to create a distribution channel
/// </summary>
public record CreateDistributionRequest(
    string ChannelName,
    string ChannelType,
    Dictionary<string, object>? ChannelConfiguration,
    bool IsEnabled
);

/// <summary>
/// Request to update a distribution channel
/// </summary>
public record UpdateDistributionRequest(
    string? ChannelName,
    Dictionary<string, object>? ChannelConfiguration,
    bool? IsEnabled
);

/// <summary>
/// DTO for distribution channel response
/// </summary>
public record ReportDistributionDto(
    Guid Id,
    Guid ReportConfigId,
    string ChannelName,
    string ChannelType,
    Dictionary<string, object>? ChannelConfiguration,
    bool IsEnabled,
    DateTime? LastTestedDate,
    string? LastTestStatus,
    DateTime CreatedDate
);

/// <summary>
/// DTO for report execution response
/// </summary>
public record ReportExecutionDto(
    Guid Id,
    Guid ReportConfigId,
    Guid? ScheduleId,
    DateTime ExecutionDate,
    DateTime? CompletionDate,
    string Status,
    int? RecordsProcessed,
    string? ErrorMessage,
    int? ExecutionDurationMs,
    int? FileSize,
    string? FileName,
    string? StoragePath,
    string? CreatedBy
);

/// <summary>
/// DTO for report archive response
/// </summary>
public record ReportArchiveDto(
    Guid Id,
    Guid ExecutionId,
    DateTime ArchiveDate,
    int RetentionDays,
    DateTime ExpiryDate,
    string StorageLocation,
    bool IsCompressed,
    long FileSize
);

/// <summary>
/// DTO for template permission
/// </summary>
public record TemplatePermissionDto(
    Guid Id,
    Guid TemplateId,
    Guid UserId,
    int PermissionLevel,
    DateTime CreatedDate
);

/// <summary>
/// Paged result response
/// </summary>
public record PagedResultDto<T>(
    List<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages
);
