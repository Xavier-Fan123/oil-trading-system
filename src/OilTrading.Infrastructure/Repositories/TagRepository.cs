using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class TagRepository : Repository<Tag>, ITagRepository
{
    public TagRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<Tag>> GetByCategoryAsync(TagCategory category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.Category == category && t.IsActive)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Tag>> GetActiveTagsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Priority)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Tag>> SearchTagsAsync(string searchTerm, TagCategory? category = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(t => 
                t.Name.ToLower().Contains(lowerSearchTerm) || 
                (t.Description != null && t.Description.ToLower().Contains(lowerSearchTerm)));
        }

        if (category.HasValue)
        {
            query = query.Where(t => t.Category == category.Value);
        }

        return await query
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Priority)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Tag>> GetMostUsedTagsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive && t.UsageCount > 0)
            .OrderByDescending(t => t.UsageCount)
            .ThenBy(t => t.Name)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Tag>> GetUnusedTagsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive && t.UsageCount == 0)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsNameUniqueAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(t => t.Name == name);
        
        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task UpdateUsageCountsAsync(Dictionary<Guid, int> usageCounts, CancellationToken cancellationToken = default)
    {
        foreach (var kvp in usageCounts)
        {
            var tag = await _dbSet.FindAsync(new object[] { kvp.Key }, cancellationToken);
            if (tag != null)
            {
                tag.UpdateUsageCount(kvp.Value);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class ContractTagRepository : Repository<ContractTag>, IContractTagRepository
{
    public ContractTagRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ContractTag>> GetByContractAsync(Guid contractId, string contractType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ct => ct.Tag)
            .Where(ct => ct.ContractId == contractId && ct.ContractType == contractType)
            .OrderBy(ct => ct.Tag.Category)
            .ThenBy(ct => ct.Tag.Priority)
            .ThenBy(ct => ct.Tag.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ContractTag>> GetByTagAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ct => ct.TagId == tagId)
            .OrderBy(ct => ct.ContractType)
            .ThenBy(ct => ct.ContractId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ContractHasTagAsync(Guid contractId, string contractType, Guid tagId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(ct => ct.ContractId == contractId && ct.ContractType == contractType && ct.TagId == tagId, cancellationToken);
    }

    public async Task RemoveContractTagAsync(Guid contractId, string contractType, Guid tagId, CancellationToken cancellationToken = default)
    {
        var contractTag = await _dbSet
            .FirstOrDefaultAsync(ct => ct.ContractId == contractId && ct.ContractType == contractType && ct.TagId == tagId, cancellationToken);

        if (contractTag != null)
        {
            _dbSet.Remove(contractTag);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveAllContractTagsAsync(Guid contractId, string contractType, CancellationToken cancellationToken = default)
    {
        var contractTags = await _dbSet
            .Where(ct => ct.ContractId == contractId && ct.ContractType == contractType)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(contractTags);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<ContractTag>> FindContractsByTagsAsync(IEnumerable<Guid> tagIds, string? contractType = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(ct => ct.Tag)
            .Where(ct => tagIds.Contains(ct.TagId));

        if (!string.IsNullOrWhiteSpace(contractType))
        {
            query = query.Where(ct => ct.ContractType == contractType);
        }

        return await query
            .OrderBy(ct => ct.ContractType)
            .ThenBy(ct => ct.ContractId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetTagUsageStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .GroupBy(ct => ct.TagId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count(),
                cancellationToken);
    }
}