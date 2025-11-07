using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;
using System.Linq.Expressions;

namespace OilTrading.Infrastructure.Repositories;

public class SettlementTemplateRepository : Repository<SettlementTemplate>, ISettlementTemplateRepository
{
    public SettlementTemplateRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<SettlementTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.Name == name && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SettlementTemplate>> GetByCreatorAsync(Guid creatorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.CreatedByUserId == creatorId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SettlementTemplate>> GetPublicTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsPublic && t.IsActive && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SettlementTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SettlementTemplate>> GetAccessibleTemplatesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Get public templates + private templates from user + templates shared with user
        var accessibleTemplates = await _dbSet
            .Where(t =>
                (t.IsPublic && t.IsActive && !t.IsDeleted) ||  // Public templates
                (t.CreatedByUserId == userId && !t.IsDeleted) ||  // User's own templates
                t.Permissions.Any(p => p.UserId == userId && !t.IsDeleted)  // Shared templates
            )
            .Distinct()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return accessibleTemplates;
    }

    public async Task<IReadOnlyList<SettlementTemplate>> GetRecentlyUsedAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.LastUsedAt.HasValue && !t.IsDeleted)
            .OrderByDescending(t => t.LastUsedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SettlementTemplate>> GetMostUsedAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.TimesUsed)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<SettlementTemplate>> GetPagedAsync(
        Expression<Func<SettlementTemplate, bool>>? filter = null,
        Expression<Func<SettlementTemplate, object>>? orderBy = null,
        bool orderByDescending = false,
        int page = 1,
        int pageSize = 20,
        string[]? includeProperties = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Apply filter
        if (filter != null)
        {
            query = query.Where(filter);
        }

        // Apply includes
        if (includeProperties != null)
        {
            foreach (var include in includeProperties)
            {
                query = query.Include(include);
            }
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply ordering
        if (orderBy != null)
        {
            query = orderByDescending
                ? query.OrderByDescending(orderBy)
                : query.OrderBy(orderBy);
        }
        else
        {
            query = query.OrderByDescending(t => t.CreatedAt);
        }

        // Apply paging
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SettlementTemplate>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<SettlementTemplate?> GetByIdWithIncludesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Usages)
            .Include(t => t.Permissions)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    public async Task<bool> TemplateNameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(t => t.Name == name && !t.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SettlementTemplateUsage>> GetUsagesAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _context.SettlementTemplateUsages
            .Where(u => u.TemplateId == templateId)
            .OrderByDescending(u => u.AppliedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddUsageAsync(SettlementTemplateUsage usage, CancellationToken cancellationToken = default)
    {
        await _context.SettlementTemplateUsages.AddAsync(usage, cancellationToken);
    }

    public async Task<IReadOnlyList<SettlementTemplatePermission>> GetPermissionsAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _context.SettlementTemplatePermissions
            .Where(p => p.TemplateId == templateId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UserHasAccessAsync(Guid templateId, Guid userId, CancellationToken cancellationToken = default)
    {
        var template = await _dbSet.FirstOrDefaultAsync(t => t.Id == templateId && !t.IsDeleted, cancellationToken);

        if (template == null)
            return false;

        // Creator always has access
        if (template.CreatedByUserId == userId)
            return true;

        // Public templates are accessible to all
        if (template.IsPublic)
            return true;

        // Check explicit permissions
        return await _context.SettlementTemplatePermissions
            .AnyAsync(p => p.TemplateId == templateId && p.UserId == userId, cancellationToken);
    }

    public async Task<bool> UserCanEditAsync(Guid templateId, Guid userId, CancellationToken cancellationToken = default)
    {
        var template = await _dbSet.FirstOrDefaultAsync(t => t.Id == templateId && !t.IsDeleted, cancellationToken);

        if (template == null)
            return false;

        // Creator can always edit
        if (template.CreatedByUserId == userId)
            return true;

        // Check if user has Edit or Admin permission
        return await _context.SettlementTemplatePermissions
            .AnyAsync(p => p.TemplateId == templateId && p.UserId == userId &&
                (p.PermissionLevel == TemplatePermissionLevel.Edit || p.PermissionLevel == TemplatePermissionLevel.Admin),
                cancellationToken);
    }

    public async Task AddOrUpdatePermissionAsync(SettlementTemplatePermission permission, CancellationToken cancellationToken = default)
    {
        var existing = await _context.SettlementTemplatePermissions
            .FirstOrDefaultAsync(p => p.TemplateId == permission.TemplateId && p.UserId == permission.UserId, cancellationToken);

        if (existing != null)
        {
            existing.PermissionLevel = permission.PermissionLevel;
            existing.GrantedAt = permission.GrantedAt;
            existing.GrantedBy = permission.GrantedBy;
            _context.SettlementTemplatePermissions.Update(existing);
        }
        else
        {
            await _context.SettlementTemplatePermissions.AddAsync(permission, cancellationToken);
        }
    }

    public async Task RemovePermissionAsync(Guid templateId, Guid userId, CancellationToken cancellationToken = default)
    {
        var permission = await _context.SettlementTemplatePermissions
            .FirstOrDefaultAsync(p => p.TemplateId == templateId && p.UserId == userId, cancellationToken);

        if (permission != null)
        {
            _context.SettlementTemplatePermissions.Remove(permission);
        }
    }

    public async Task<IReadOnlyList<SettlementTemplate>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        return await _dbSet
            .Where(t =>
                (t.Name.ToLower().Contains(lowerSearchTerm) ||
                 t.Description.ToLower().Contains(lowerSearchTerm)) &&
                !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task DeactivateMultipleAsync(IEnumerable<Guid> templateIds, string deactivatedBy, CancellationToken cancellationToken = default)
    {
        var templates = await _dbSet
            .Where(t => templateIds.Contains(t.Id) && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var template in templates)
        {
            template.Deactivate(deactivatedBy);
        }

        _dbSet.UpdateRange(templates);
    }

    public async Task<Dictionary<Guid, int>> GetUsageStatsAsync(IEnumerable<Guid> templateIds, CancellationToken cancellationToken = default)
    {
        return await _context.SettlementTemplateUsages
            .Where(u => templateIds.Contains(u.TemplateId))
            .GroupBy(u => u.TemplateId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count(),
                cancellationToken);
    }
}
