using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Services;

/// <summary>
/// 标签服务实现 - Tag Service Implementation
/// </summary>
public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;
    private readonly IContractTagRepository _contractTagRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TagService> _logger;

    public TagService(
        ITagRepository tagRepository,
        IContractTagRepository contractTagRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IUnitOfWork unitOfWork,
        ILogger<TagService> logger)
    {
        _tagRepository = tagRepository;
        _contractTagRepository = contractTagRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Tag> CreateTagAsync(string name, TagCategory category, string? description = null, string? color = null, int priority = 0, CancellationToken cancellationToken = default)
    {
        // 检查名称唯一性
        if (!await _tagRepository.IsNameUniqueAsync(name, cancellationToken: cancellationToken))
        {
            throw new DomainException($"Tag with name '{name}' already exists");
        }

        var tag = new Tag(name, category, description, color, priority);
        
        await _tagRepository.AddAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created tag {TagName} in category {Category}", name, category);
        
        return tag;
    }

    public async Task<Tag> UpdateTagAsync(Guid tagId, string? name = null, string? description = null, string? color = null, int? priority = null, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(tagId, cancellationToken);
        if (tag == null)
        {
            throw new NotFoundException($"Tag with ID {tagId} not found");
        }

        // 检查名称唯一性（如果要更改名称）
        if (!string.IsNullOrEmpty(name) && name != tag.Name)
        {
            if (!await _tagRepository.IsNameUniqueAsync(name, tagId, cancellationToken))
            {
                throw new DomainException($"Tag with name '{name}' already exists");
            }
            tag.Rename(name);
        }

        // 更新其他属性
        tag.UpdateDetails(description, color, priority);

        await _tagRepository.UpdateAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated tag {TagId}", tagId);
        
        return tag;
    }

    public async Task DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(tagId, cancellationToken);
        if (tag == null)
        {
            throw new NotFoundException($"Tag with ID {tagId} not found");
        }

        // 检查是否有合同使用此标签
        var contractTags = await _contractTagRepository.GetByTagAsync(tagId, cancellationToken);
        if (contractTags.Any())
        {
            throw new DomainException($"Cannot delete tag '{tag.Name}' because it is used by {contractTags.Count()} contracts");
        }

        await _tagRepository.DeleteAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted tag {TagName} ({TagId})", tag.Name, tagId);
    }

    public async Task AddTagToContractAsync(Guid contractId, string contractType, Guid tagId, string? notes = null, string assignedBy = "System", CancellationToken cancellationToken = default)
    {
        // 验证标签是否可以应用
        var validationResult = await ValidateTagForContractAsync(tagId, contractId, contractType, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new DomainException(validationResult.ErrorMessage ?? "Tag cannot be applied to this contract");
        }

        // 检查是否已存在
        if (await _contractTagRepository.ContractHasTagAsync(contractId, contractType, tagId, cancellationToken))
        {
            _logger.LogWarning("Contract {ContractId} already has tag {TagId}", contractId, tagId);
            return;
        }

        var contractTag = new ContractTag(contractId, contractType, tagId, notes, assignedBy);
        await _contractTagRepository.AddAsync(contractTag, cancellationToken);

        // 更新标签使用计数
        var tag = await _tagRepository.GetByIdAsync(tagId, cancellationToken);
        if (tag != null)
        {
            tag.IncrementUsage();
            await _tagRepository.UpdateAsync(tag, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added tag {TagId} to contract {ContractId} ({ContractType})", tagId, contractId, contractType);
    }

    public async Task RemoveTagFromContractAsync(Guid contractId, string contractType, Guid tagId, CancellationToken cancellationToken = default)
    {
        await _contractTagRepository.RemoveContractTagAsync(contractId, contractType, tagId, cancellationToken);

        // 更新标签使用计数
        var tag = await _tagRepository.GetByIdAsync(tagId, cancellationToken);
        if (tag != null)
        {
            tag.DecrementUsage();
            await _tagRepository.UpdateAsync(tag, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed tag {TagId} from contract {ContractId} ({ContractType})", tagId, contractId, contractType);
    }

    public async Task<IEnumerable<Tag>> GetContractTagsAsync(Guid contractId, string contractType, CancellationToken cancellationToken = default)
    {
        var contractTags = await _contractTagRepository.GetByContractAsync(contractId, contractType, cancellationToken);
        return contractTags.Select(ct => ct.Tag);
    }

    public async Task<IEnumerable<Guid>> FindContractsByTagsAsync(IEnumerable<Guid> tagIds, string? contractType = null, CancellationToken cancellationToken = default)
    {
        var contractTags = await _contractTagRepository.FindContractsByTagsAsync(tagIds, contractType, cancellationToken);
        return contractTags.Select(ct => ct.ContractId).Distinct();
    }

    public async Task<TagValidationResult> ValidateTagForContractAsync(Guid tagId, Guid contractId, string contractType, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(tagId, cancellationToken);
        if (tag == null)
        {
            return TagValidationResult.Failure("Tag not found");
        }

        if (!tag.IsActive)
        {
            return TagValidationResult.Failure("Tag is not active");
        }

        // 获取合同状态
        ContractStatus? contractStatus = null;
        if (contractType.Equals("PurchaseContract", StringComparison.OrdinalIgnoreCase))
        {
            var contract = await _purchaseContractRepository.GetByIdAsync(contractId, cancellationToken);
            contractStatus = contract?.Status;
        }
        else if (contractType.Equals("SalesContract", StringComparison.OrdinalIgnoreCase))
        {
            var contract = await _salesContractRepository.GetByIdAsync(contractId, cancellationToken);
            contractStatus = contract?.Status;
        }

        if (contractStatus.HasValue && !tag.CanBeAppliedToContractStatus(contractStatus.Value))
        {
            return TagValidationResult.Failure($"Tag '{tag.Name}' cannot be applied to contracts in '{contractStatus}' status");
        }

        // 检查是否达到最大使用限制
        var existingTags = await _contractTagRepository.GetByContractAsync(contractId, contractType, cancellationToken);
        var currentUsageCount = existingTags.Count(ct => ct.TagId == tagId);
        
        if (tag.HasReachedMaxUsage(currentUsageCount))
        {
            return TagValidationResult.Failure($"Tag '{tag.Name}' has reached maximum usage limit");
        }

        // 检查互斥标签
        var existingTagNames = existingTags.Select(ct => ct.Tag.Name);
        if (tag.IsConflictWith(existingTagNames))
        {
            var result = TagValidationResult.Failure($"Tag '{tag.Name}' conflicts with existing tags");
            result.ConflictingTags = existingTagNames.Where(name => 
                tag.MutuallyExclusiveTags?.Split(',').Contains(name, StringComparer.OrdinalIgnoreCase) == true).ToList();
            return result;
        }

        return TagValidationResult.Success();
    }

    public async Task<IEnumerable<Tag>> GetPredefinedTagsAsync(TagCategory? category = null, CancellationToken cancellationToken = default)
    {
        if (category.HasValue)
        {
            return await _tagRepository.GetByCategoryAsync(category.Value, cancellationToken);
        }
        
        return await _tagRepository.GetActiveTagsAsync(cancellationToken);
    }

    public async Task CreatePredefinedTagsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating predefined tags");

        var categoriesToCreate = Enum.GetValues<TagCategory>()
            .Where(c => c != TagCategory.Custom);

        foreach (var category in categoriesToCreate)
        {
            var predefinedNames = category.GetPredefinedTags();
            
            foreach (var tagName in predefinedNames)
            {
                // 检查是否已存在
                var existingTag = await _tagRepository.GetByNameAsync(tagName, cancellationToken);
                if (existingTag == null)
                {
                    var tag = new Tag(tagName, category, category.GetDescription());
                    await _tagRepository.AddAsync(tag, cancellationToken);
                    
                    _logger.LogDebug("Created predefined tag: {TagName} ({Category})", tagName, category);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Completed creating predefined tags");
    }

    public async Task<TagUsageStatistics> GetTagUsageStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var allTags = await _tagRepository.GetAllAsync(cancellationToken);
        var activeTags = allTags.Where(t => t.IsActive);
        var unusedTags = allTags.Where(t => t.UsageCount == 0);

        var tagsByCategory = allTags.GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var mostUsedTags = activeTags
            .OrderByDescending(t => t.UsageCount)
            .Take(10)
            .Select(t => new TagUsageInfo
            {
                TagId = t.Id,
                TagName = t.Name,
                Category = t.Category,
                UsageCount = t.UsageCount,
                LastUsedAt = t.LastUsedAt
            }).ToList();

        var recentlyUsedTags = activeTags
            .Where(t => t.LastUsedAt.HasValue)
            .OrderByDescending(t => t.LastUsedAt)
            .Take(10)
            .Select(t => new TagUsageInfo
            {
                TagId = t.Id,
                TagName = t.Name,
                Category = t.Category,
                UsageCount = t.UsageCount,
                LastUsedAt = t.LastUsedAt
            }).ToList();

        return new TagUsageStatistics
        {
            TotalTags = allTags.Count(),
            ActiveTags = activeTags.Count(),
            UnusedTags = unusedTags.Count(),
            TagsByCategory = tagsByCategory,
            MostUsedTags = mostUsedTags,
            RecentlyUsedTags = recentlyUsedTags
        };
    }

    public async Task SynchronizeTagUsageCountsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Synchronizing tag usage counts");

        var usageStats = await _contractTagRepository.GetTagUsageStatisticsAsync(cancellationToken);
        await _tagRepository.UpdateUsageCountsAsync(usageStats, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Synchronized usage counts for {Count} tags", usageStats.Count);
    }
}