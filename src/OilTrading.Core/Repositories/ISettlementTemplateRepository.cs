using OilTrading.Core.Entities;
using OilTrading.Core.Common;
using System.Linq.Expressions;

namespace OilTrading.Core.Repositories;

public interface ISettlementTemplateRepository : IRepository<SettlementTemplate>
{
    /// <summary>
    /// Get template by name
    /// </summary>
    Task<SettlementTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all templates created by a specific user
    /// </summary>
    Task<IReadOnlyList<SettlementTemplate>> GetByCreatorAsync(Guid creatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all public templates (shared with all users)
    /// </summary>
    Task<IReadOnlyList<SettlementTemplate>> GetPublicTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active templates
    /// </summary>
    Task<IReadOnlyList<SettlementTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get templates accessible by a specific user (public + private + shared)
    /// </summary>
    Task<IReadOnlyList<SettlementTemplate>> GetAccessibleTemplatesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get most recently used templates
    /// </summary>
    Task<IReadOnlyList<SettlementTemplate>> GetRecentlyUsedAsync(int count = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get most frequently used templates
    /// </summary>
    Task<IReadOnlyList<SettlementTemplate>> GetMostUsedAsync(int count = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paged results with filtering
    /// </summary>
    Task<PagedResult<SettlementTemplate>> GetPagedAsync(
        Expression<Func<SettlementTemplate, bool>>? filter = null,
        Expression<Func<SettlementTemplate, object>>? orderBy = null,
        bool orderByDescending = false,
        int page = 1,
        int pageSize = 20,
        string[]? includeProperties = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get template by ID with all related data (usages, permissions)
    /// </summary>
    Task<SettlementTemplate?> GetByIdWithIncludesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if template name is unique
    /// </summary>
    Task<bool> TemplateNameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get template usages
    /// </summary>
    Task<IReadOnlyList<SettlementTemplateUsage>> GetUsagesAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add template usage record
    /// </summary>
    Task AddUsageAsync(SettlementTemplateUsage usage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get template permissions
    /// </summary>
    Task<IReadOnlyList<SettlementTemplatePermission>> GetPermissionsAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has permission to access template
    /// </summary>
    Task<bool> UserHasAccessAsync(Guid templateId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user can edit template
    /// </summary>
    Task<bool> UserCanEditAsync(Guid templateId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add or update template permission
    /// </summary>
    Task AddOrUpdatePermissionAsync(SettlementTemplatePermission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove template permission
    /// </summary>
    Task RemovePermissionAsync(Guid templateId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search templates by name and description
    /// </summary>
    Task<IReadOnlyList<SettlementTemplate>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate multiple templates
    /// </summary>
    Task DeactivateMultipleAsync(IEnumerable<Guid> templateIds, string deactivatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get template usage statistics
    /// </summary>
    Task<Dictionary<Guid, int>> GetUsageStatsAsync(IEnumerable<Guid> templateIds, CancellationToken cancellationToken = default);
}
