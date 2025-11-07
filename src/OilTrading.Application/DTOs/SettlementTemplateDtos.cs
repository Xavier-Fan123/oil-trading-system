using OilTrading.Core.Entities;

namespace OilTrading.Application.DTOs;

/// <summary>
/// DTO for creating a new settlement template
/// </summary>
public class CreateSettlementTemplateRequest
{
    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Template configuration (JSON string)
    /// </summary>
    public string TemplateConfiguration { get; set; } = string.Empty;

    /// <summary>
    /// Whether template should be public
    /// </summary>
    public bool IsPublic { get; set; } = false;
}

/// <summary>
/// DTO for updating an existing template
/// </summary>
public class UpdateSettlementTemplateRequest
{
    /// <summary>
    /// Template ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Updated template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Updated template description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Updated template configuration (JSON string)
    /// </summary>
    public string TemplateConfiguration { get; set; } = string.Empty;

    /// <summary>
    /// Update public/private status
    /// </summary>
    public bool IsPublic { get; set; }
}

/// <summary>
/// DTO for template response (read)
/// </summary>
public class SettlementTemplateDto
{
    /// <summary>
    /// Template unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User who created the template
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// User name who created the template
    /// </summary>
    public string CreatedByUserName { get; set; } = string.Empty;

    /// <summary>
    /// When template was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Current template version
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Whether template is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether template is public
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// JSON-serialized template configuration
    /// </summary>
    public string TemplateConfiguration { get; set; } = string.Empty;

    /// <summary>
    /// Number of times template was used
    /// </summary>
    public int TimesUsed { get; set; }

    /// <summary>
    /// Last time template was used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Number of users with access to this template
    /// </summary>
    public int SharedWith { get; set; }

    /// <summary>
    /// Whether current user can edit template
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Whether current user can delete template
    /// </summary>
    public bool CanDelete { get; set; }
}

/// <summary>
/// DTO for template list response (summary)
/// </summary>
public class SettlementTemplateSummaryDto
{
    /// <summary>
    /// Template unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Number of times template was used
    /// </summary>
    public int TimesUsed { get; set; }

    /// <summary>
    /// Last time template was used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Whether template is public
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Whether template is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creator user name
    /// </summary>
    public string CreatedByUserName { get; set; } = string.Empty;

    /// <summary>
    /// When template was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for quick-create from template
/// </summary>
public class QuickCreateFromTemplateRequest
{
    /// <summary>
    /// Template ID to use
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Optional overrides for template values
    /// </summary>
    public Dictionary<string, object>? ValueOverrides { get; set; }
}

/// <summary>
/// DTO for template permission
/// </summary>
public class SettlementTemplatePermissionDto
{
    /// <summary>
    /// User ID who has permission
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User name
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Permission level
    /// </summary>
    public int PermissionLevel { get; set; }

    /// <summary>
    /// Permission level name
    /// </summary>
    public string PermissionLevelName { get; set; } = string.Empty;

    /// <summary>
    /// When permission was granted
    /// </summary>
    public DateTime GrantedAt { get; set; }

    /// <summary>
    /// User who granted permission
    /// </summary>
    public string GrantedBy { get; set; } = string.Empty;
}

/// <summary>
/// DTO for sharing template with user
/// </summary>
public class ShareTemplateRequest
{
    /// <summary>
    /// Template ID to share
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// User ID to share with
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Permission level to grant
    /// 0 = View, 1 = Use, 2 = Edit, 3 = Admin
    /// </summary>
    public int PermissionLevel { get; set; } = 1;
}

/// <summary>
/// DTO for template usage record
/// </summary>
public class SettlementTemplateUsageDto
{
    /// <summary>
    /// Template ID that was used
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Settlement created from template
    /// </summary>
    public Guid SettlementId { get; set; }

    /// <summary>
    /// User who applied the template
    /// </summary>
    public string AppliedBy { get; set; } = string.Empty;

    /// <summary>
    /// When template was applied
    /// </summary>
    public DateTime AppliedAt { get; set; }
}

/// <summary>
/// DTO for template statistics
/// </summary>
public class TemplateStatisticsDto
{
    /// <summary>
    /// Template ID
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Total number of times used
    /// </summary>
    public int TotalUsages { get; set; }

    /// <summary>
    /// Last usage date
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Number of users with access
    /// </summary>
    public int SharedWithCount { get; set; }

    /// <summary>
    /// Usage trend (usages per week)
    /// </summary>
    public List<UsageTrendDto> UsageTrend { get; set; } = new();
}

/// <summary>
/// DTO for usage trend data point
/// </summary>
public class UsageTrendDto
{
    /// <summary>
    /// Week start date
    /// </summary>
    public DateTime WeekStart { get; set; }

    /// <summary>
    /// Number of usages that week
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// DTO for bulk template operations
/// </summary>
public class BulkTemplateOperationRequest
{
    /// <summary>
    /// List of template IDs
    /// </summary>
    public List<Guid> TemplateIds { get; set; } = new();

    /// <summary>
    /// Operation to perform: "activate", "deactivate", "delete", "publish", "unpublish"
    /// </summary>
    public string Operation { get; set; } = string.Empty;
}

/// <summary>
/// DTO for bulk template operation result
/// </summary>
public class BulkTemplateOperationResultDto
{
    /// <summary>
    /// Number of successful operations
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed operations
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Details of each operation
    /// </summary>
    public List<BulkTemplateOperationDetailDto> Details { get; set; } = new();
}

/// <summary>
/// DTO for individual bulk operation result
/// </summary>
public class BulkTemplateOperationDetailDto
{
    /// <summary>
    /// Template ID
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Operation status: "success" or "failure"
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? Message { get; set; }
}
