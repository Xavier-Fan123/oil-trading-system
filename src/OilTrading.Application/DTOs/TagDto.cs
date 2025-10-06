using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.DTOs;

/// <summary>
/// 标签DTO - Tag Data Transfer Object
/// </summary>
public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = string.Empty;
    public TagCategory Category { get; set; }
    public string CategoryDisplayName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? MutuallyExclusiveTags { get; set; }
    public int? MaxUsagePerEntity { get; set; }
    public string? AllowedContractStatuses { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// 标签摘要DTO - Tag Summary DTO
/// </summary>
public class TagSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = string.Empty;
    public TagCategory Category { get; set; }
    public string CategoryDisplayName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int UsageCount { get; set; }
    public bool IsActive { get; set; }
    
    // Additional fields for TradeGroup context
    public DateTime AssignedAt { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
}

/// <summary>
/// 创建标签DTO - Create Tag DTO
/// </summary>
public class CreateTagDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public TagCategory Category { get; set; }
    public int Priority { get; set; } = 0;
}

/// <summary>
/// 更新标签DTO - Update Tag DTO
/// </summary>
public class UpdateTagDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int? Priority { get; set; }
}

/// <summary>
/// 合同标签DTO - Contract Tag DTO
/// </summary>
public class ContractTagDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public Guid TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string TagColor { get; set; } = string.Empty;
    public TagCategory TagCategory { get; set; }
    public string? Notes { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}

/// <summary>
/// 添加合同标签DTO - Add Contract Tag DTO
/// </summary>
public class AddContractTagDto
{
    public Guid TagId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// 标签使用统计DTO - Tag Usage Statistics DTO
/// </summary>
public class TagUsageStatisticsDto
{
    public int TotalTags { get; set; }
    public int ActiveTags { get; set; }
    public int UnusedTags { get; set; }
    public Dictionary<string, int> TagsByCategory { get; set; } = new();
    public List<TagUsageInfoDto> MostUsedTags { get; set; } = new();
    public List<TagUsageInfoDto> RecentlyUsedTags { get; set; } = new();
}

/// <summary>
/// 标签使用信息DTO - Tag Usage Info DTO
/// </summary>
public class TagUsageInfoDto
{
    public Guid TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string CategoryDisplayName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// 标签搜索请求DTO - Tag Search Request DTO
/// </summary>
public class TagSearchRequestDto
{
    public string? SearchTerm { get; set; }
    public TagCategory? Category { get; set; }
    public bool? IsActive { get; set; }
    public int? MinUsageCount { get; set; }
    public int? MaxUsageCount { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 标签验证结果DTO - Tag Validation Result DTO
/// </summary>
public class TagValidationResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> ConflictingTags { get; set; } = new();
}

/// <summary>
/// 预定义标签信息DTO - Predefined Tag Info DTO
/// </summary>
public class PredefinedTagInfoDto
{
    public TagCategory Category { get; set; }
    public string CategoryDisplayName { get; set; } = string.Empty;
    public string CategoryDescription { get; set; } = string.Empty;
    public string DefaultColor { get; set; } = string.Empty;
    public List<string> PredefinedNames { get; set; } = new();
}