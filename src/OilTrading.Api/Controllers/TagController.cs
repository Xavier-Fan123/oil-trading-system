using Microsoft.AspNetCore.Mvc;
using OilTrading.Application.Services;
using OilTrading.Application.DTOs;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Entities;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/tags")]
[Produces("application/json")]
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly ILogger<TagController> _logger;

    public TagController(ITagService tagService, ILogger<TagController> logger)
    {
        _tagService = tagService;
        _logger = logger;
    }

    private string GetCurrentUserName()
    {
        try
        {
            return User?.Identity?.Name ?? 
                   HttpContext?.User?.Identity?.Name ?? 
                   "System";
        }
        catch
        {
            return "System";
        }
    }

    /// <summary>
    /// 获取所有标签
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TagSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTags([FromQuery] TagSearchRequestDto? searchRequest = null)
    {
        // 简化版本，返回所有活跃标签
        var predefinedTags = await _tagService.GetPredefinedTagsAsync();
        var tagDtos = predefinedTags.Select(t => new TagSummaryDto
        {
            Id = t.Id,
            Name = t.Name,
            Color = t.Color,
            Category = t.Category,
            CategoryDisplayName = t.Category.GetDisplayName(),
            UsageCount = t.UsageCount,
            IsActive = t.IsActive
        });

        return Ok(tagDtos);
    }

    /// <summary>
    /// 根据ID获取标签详情
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTag(Guid id)
    {
        // 这里需要实现GetByIdAsync方法
        // 目前返回NotFound作为占位符
        return NotFound($"Tag with ID {id} not found");
    }

    /// <summary>
    /// 根据分类获取标签
    /// </summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<TagSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTagsByCategory(TagCategory category)
    {
        var tags = await _tagService.GetPredefinedTagsAsync(category);
        var tagDtos = tags.Select(t => new TagSummaryDto
        {
            Id = t.Id,
            Name = t.Name,
            Color = t.Color,
            Category = t.Category,
            CategoryDisplayName = t.Category.GetDisplayName(),
            UsageCount = t.UsageCount,
            IsActive = t.IsActive
        });

        return Ok(tagDtos);
    }

    /// <summary>
    /// 创建新标签
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagDto dto)
    {
        try
        {
            var tag = await _tagService.CreateTagAsync(
                dto.Name, 
                dto.Category, 
                dto.Description, 
                dto.Color, 
                dto.Priority);

            var tagDto = MapToTagDto(tag);

            _logger.LogInformation("Created tag {TagName} in category {Category}", dto.Name, dto.Category);

            return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tagDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tag {TagName}", dto.Name);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 更新标签
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTag(Guid id, [FromBody] UpdateTagDto dto)
    {
        try
        {
            var tag = await _tagService.UpdateTagAsync(
                id, 
                dto.Name, 
                dto.Description, 
                dto.Color, 
                dto.Priority);

            var tagDto = MapToTagDto(tag);

            _logger.LogInformation("Updated tag {TagId}", id);

            return Ok(tagDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tag {TagId}", id);
            
            if (ex.Message.Contains("not found"))
                return NotFound(ex.Message);
            
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 删除标签
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteTag(Guid id)
    {
        try
        {
            await _tagService.DeleteTagAsync(id);

            _logger.LogInformation("Deleted tag {TagId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete tag {TagId}", id);
            
            if (ex.Message.Contains("not found"))
                return NotFound(ex.Message);
            
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 为合同添加标签
    /// </summary>
    [HttpPost("contracts/{contractId:guid}/tags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddTagToContract(
        Guid contractId, 
        [FromQuery] string contractType,
        [FromBody] AddContractTagDto dto)
    {
        try
        {
            await _tagService.AddTagToContractAsync(
                contractId, 
                contractType, 
                dto.TagId, 
                dto.Notes, 
                GetCurrentUserName());

            _logger.LogInformation("Added tag {TagId} to contract {ContractId} ({ContractType})", 
                dto.TagId, contractId, contractType);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add tag {TagId} to contract {ContractId}", 
                dto.TagId, contractId);
            
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 从合同移除标签
    /// </summary>
    [HttpDelete("contracts/{contractId:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveTagFromContract(
        Guid contractId, 
        Guid tagId,
        [FromQuery] string contractType)
    {
        try
        {
            await _tagService.RemoveTagFromContractAsync(contractId, contractType, tagId);

            _logger.LogInformation("Removed tag {TagId} from contract {ContractId} ({ContractType})", 
                tagId, contractId, contractType);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove tag {TagId} from contract {ContractId}", 
                tagId, contractId);
            
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 获取合同的所有标签
    /// </summary>
    [HttpGet("contracts/{contractId:guid}/tags")]
    [ProducesResponseType(typeof(IEnumerable<TagSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractTags(
        Guid contractId, 
        [FromQuery] string contractType)
    {
        var tags = await _tagService.GetContractTagsAsync(contractId, contractType);
        var tagDtos = tags.Select(t => new TagSummaryDto
        {
            Id = t.Id,
            Name = t.Name,
            Color = t.Color,
            Category = t.Category,
            CategoryDisplayName = t.Category.GetDisplayName(),
            UsageCount = t.UsageCount,
            IsActive = t.IsActive
        });

        return Ok(tagDtos);
    }

    /// <summary>
    /// 验证标签是否可以应用于合同
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(TagValidationResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateTagForContract([FromBody] ValidateTagRequest request)
    {
        try
        {
            var result = await _tagService.ValidateTagForContractAsync(
                request.TagId, 
                request.ContractId, 
                request.ContractType);

            var dto = new TagValidationResultDto
            {
                IsValid = result.IsValid,
                ErrorMessage = result.ErrorMessage,
                Warnings = result.Warnings,
                ConflictingTags = result.ConflictingTags
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate tag {TagId} for contract {ContractId}", 
                request.TagId, request.ContractId);
            
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 获取预定义标签信息
    /// </summary>
    [HttpGet("predefined")]
    [ProducesResponseType(typeof(IEnumerable<PredefinedTagInfoDto>), StatusCodes.Status200OK)]
    public IActionResult GetPredefinedTagInfo()
    {
        var categories = Enum.GetValues<TagCategory>()
            .Where(c => c != TagCategory.Custom);

        var infos = categories.Select(category => new PredefinedTagInfoDto
        {
            Category = category,
            CategoryDisplayName = category.GetDisplayName(),
            CategoryDescription = category.GetDescription(),
            DefaultColor = category.GetDefaultColor(),
            PredefinedNames = category.GetPredefinedTags().ToList()
        });

        return Ok(infos);
    }

    /// <summary>
    /// 创建所有预定义标签
    /// </summary>
    [HttpPost("predefined/create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePredefinedTags()
    {
        try
        {
            await _tagService.CreatePredefinedTagsAsync();

            _logger.LogInformation("Created predefined tags");

            return Ok(new { Message = "Predefined tags created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create predefined tags");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 获取标签使用统计
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(TagUsageStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTagUsageStatistics()
    {
        try
        {
            var stats = await _tagService.GetTagUsageStatisticsAsync();

            var dto = new TagUsageStatisticsDto
            {
                TotalTags = stats.TotalTags,
                ActiveTags = stats.ActiveTags,
                UnusedTags = stats.UnusedTags,
                TagsByCategory = stats.TagsByCategory.ToDictionary(
                    kvp => kvp.Key.GetDisplayName(), 
                    kvp => kvp.Value),
                MostUsedTags = stats.MostUsedTags.Select(t => new TagUsageInfoDto
                {
                    TagId = t.TagId,
                    TagName = t.TagName,
                    CategoryDisplayName = t.Category.GetDisplayName(),
                    UsageCount = t.UsageCount,
                    LastUsedAt = t.LastUsedAt
                }).ToList(),
                RecentlyUsedTags = stats.RecentlyUsedTags.Select(t => new TagUsageInfoDto
                {
                    TagId = t.TagId,
                    TagName = t.TagName,
                    CategoryDisplayName = t.Category.GetDisplayName(),
                    UsageCount = t.UsageCount,
                    LastUsedAt = t.LastUsedAt
                }).ToList()
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tag usage statistics");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 同步标签使用计数
    /// </summary>
    [HttpPost("sync-usage-counts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SynchronizeTagUsageCounts()
    {
        try
        {
            await _tagService.SynchronizeTagUsageCountsAsync();

            _logger.LogInformation("Synchronized tag usage counts");

            return Ok(new { Message = "Tag usage counts synchronized successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synchronize tag usage counts");
            return BadRequest(ex.Message);
        }
    }

    private static TagDto MapToTagDto(Tag tag)
    {
        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            Color = tag.Color,
            Category = tag.Category,
            CategoryDisplayName = tag.Category.GetDisplayName(),
            Priority = tag.Priority,
            IsActive = tag.IsActive,
            UsageCount = tag.UsageCount,
            LastUsedAt = tag.LastUsedAt,
            MutuallyExclusiveTags = tag.MutuallyExclusiveTags,
            MaxUsagePerEntity = tag.MaxUsagePerEntity,
            AllowedContractStatuses = tag.AllowedContractStatuses,
            CreatedAt = tag.CreatedAt,
            CreatedBy = tag.CreatedBy,
            UpdatedAt = tag.UpdatedAt,
            UpdatedBy = tag.UpdatedBy
        };
    }
}

/// <summary>
/// 标签验证请求
/// </summary>
public class ValidateTagRequest
{
    public Guid TagId { get; set; }
    public Guid ContractId { get; set; }
    public string ContractType { get; set; } = string.Empty;
}