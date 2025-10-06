using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

/// <summary>
/// 标签服务接口 - Tag Service Interface
/// </summary>
public interface ITagService
{
    /// <summary>
    /// 创建标签
    /// </summary>
    Task<Tag> CreateTagAsync(string name, TagCategory category, string? description = null, string? color = null, int priority = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新标签
    /// </summary>
    Task<Tag> UpdateTagAsync(Guid tagId, string? name = null, string? description = null, string? color = null, int? priority = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除标签
    /// </summary>
    Task DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为合同添加标签
    /// </summary>
    Task AddTagToContractAsync(Guid contractId, string contractType, Guid tagId, string? notes = null, string assignedBy = "System", CancellationToken cancellationToken = default);

    /// <summary>
    /// 从合同移除标签
    /// </summary>
    Task RemoveTagFromContractAsync(Guid contractId, string contractType, Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取合同的所有标签
    /// </summary>
    Task<IEnumerable<Tag>> GetContractTagsAsync(Guid contractId, string contractType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据标签搜索合同
    /// </summary>
    Task<IEnumerable<Guid>> FindContractsByTagsAsync(IEnumerable<Guid> tagIds, string? contractType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证标签是否可以应用于合同
    /// </summary>
    Task<TagValidationResult> ValidateTagForContractAsync(Guid tagId, Guid contractId, string contractType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取预定义标签
    /// </summary>
    Task<IEnumerable<Tag>> GetPredefinedTagsAsync(TagCategory? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建预定义标签
    /// </summary>
    Task CreatePredefinedTagsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取标签使用统计
    /// </summary>
    Task<TagUsageStatistics> GetTagUsageStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步标签使用计数
    /// </summary>
    Task SynchronizeTagUsageCountsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 标签验证结果
/// </summary>
public class TagValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> ConflictingTags { get; set; } = new();

    public static TagValidationResult Success() => new() { IsValid = true };
    public static TagValidationResult Failure(string errorMessage) => new() { IsValid = false, ErrorMessage = errorMessage };
}

/// <summary>
/// 标签使用统计
/// </summary>
public class TagUsageStatistics
{
    public int TotalTags { get; set; }
    public int ActiveTags { get; set; }
    public int UnusedTags { get; set; }
    public Dictionary<TagCategory, int> TagsByCategory { get; set; } = new();
    public List<TagUsageInfo> MostUsedTags { get; set; } = new();
    public List<TagUsageInfo> RecentlyUsedTags { get; set; } = new();
}

/// <summary>
/// 标签使用信息
/// </summary>
public class TagUsageInfo
{
    public Guid TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public TagCategory Category { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}