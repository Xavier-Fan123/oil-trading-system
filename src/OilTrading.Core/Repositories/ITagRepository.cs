using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Repositories;

/// <summary>
/// 标签仓储接口 - Tag Repository Interface
/// </summary>
public interface ITagRepository : IRepository<Tag>
{
    /// <summary>
    /// 根据名称获取标签
    /// </summary>
    Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据分类获取标签列表
    /// </summary>
    Task<IEnumerable<Tag>> GetByCategoryAsync(TagCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取活跃的标签列表
    /// </summary>
    Task<IEnumerable<Tag>> GetActiveTagsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索标签（支持名称和描述模糊搜索）
    /// </summary>
    Task<IEnumerable<Tag>> SearchTagsAsync(string searchTerm, TagCategory? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最常用的标签
    /// </summary>
    Task<IEnumerable<Tag>> GetMostUsedTagsAsync(int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取未使用的标签
    /// </summary>
    Task<IEnumerable<Tag>> GetUnusedTagsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查标签名称是否唯一
    /// </summary>
    Task<bool> IsNameUniqueAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新标签使用计数
    /// </summary>
    Task UpdateUsageCountsAsync(Dictionary<Guid, int> usageCounts, CancellationToken cancellationToken = default);
}

/// <summary>
/// 合同标签仓储接口 - Contract Tag Repository Interface
/// </summary>
public interface IContractTagRepository : IRepository<ContractTag>
{
    /// <summary>
    /// 获取合同的所有标签
    /// </summary>
    Task<IEnumerable<ContractTag>> GetByContractAsync(Guid contractId, string contractType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取标签的所有合同关联
    /// </summary>
    Task<IEnumerable<ContractTag>> GetByTagAsync(Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查合同是否已有指定标签
    /// </summary>
    Task<bool> ContractHasTagAsync(Guid contractId, string contractType, Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除合同的指定标签
    /// </summary>
    Task RemoveContractTagAsync(Guid contractId, string contractType, Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除合同的所有标签
    /// </summary>
    Task RemoveAllContractTagsAsync(Guid contractId, string contractType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据标签搜索合同
    /// </summary>
    Task<IEnumerable<ContractTag>> FindContractsByTagsAsync(IEnumerable<Guid> tagIds, string? contractType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取合同标签统计信息
    /// </summary>
    Task<Dictionary<Guid, int>> GetTagUsageStatisticsAsync(CancellationToken cancellationToken = default);
}