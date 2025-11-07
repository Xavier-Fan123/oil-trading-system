using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Services;

/// <summary>
/// Service for managing report configurations (CRUD operations, search, clone)
/// </summary>
public class ReportConfigurationService : IReportConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportConfigurationService> _logger;

    public ReportConfigurationService(
        ApplicationDbContext context,
        ILogger<ReportConfigurationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new report configuration
    /// </summary>
    public async Task<ReportConfiguration> CreateAsync(
        string name,
        string reportType,
        string exportFormat,
        string? description = null,
        string? filterJson = null,
        string? columnsJson = null,
        bool includeMetadata = false,
        Guid? createdBy = null)
    {
        _logger.LogInformation("Creating report configuration: {Name}", name);

        var config = new ReportConfiguration
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            ReportType = reportType,
            FilterJson = filterJson,
            ColumnsJson = columnsJson,
            ExportFormat = exportFormat,
            IncludeMetadata = includeMetadata,
            IsActive = true,
            IsDeleted = false,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            RowVersion = []
        };

        _context.ReportConfigurations.Add(config);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Report configuration created successfully: {ConfigId}", config.Id);
        return config;
    }

    /// <summary>
    /// Get report configuration by ID
    /// </summary>
    public async Task<ReportConfiguration?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving report configuration: {ConfigId}", id);

        var config = await _context.ReportConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (config == null)
        {
            _logger.LogWarning("Report configuration not found: {ConfigId}", id);
        }

        return config;
    }

    /// <summary>
    /// Get all active report configurations with pagination
    /// </summary>
    public async Task<(List<ReportConfiguration> items, int totalCount)> GetAllAsync(int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Retrieving report configurations: page {Page}, pageSize {PageSize}", page, pageSize);

        var query = _context.ReportConfigurations
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreatedDate);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} report configurations", items.Count);
        return (items, totalCount);
    }

    /// <summary>
    /// Update an existing report configuration
    /// </summary>
    public async Task<ReportConfiguration?> UpdateAsync(
        Guid id,
        string? name = null,
        string? description = null,
        string? reportType = null,
        string? filterJson = null,
        string? columnsJson = null,
        string? exportFormat = null,
        bool? includeMetadata = null,
        bool? isActive = null,
        Guid? updatedBy = null)
    {
        _logger.LogInformation("Updating report configuration: {ConfigId}", id);

        var config = await _context.ReportConfigurations
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (config == null)
        {
            _logger.LogWarning("Report configuration not found for update: {ConfigId}", id);
            return null;
        }

        if (!string.IsNullOrEmpty(name))
            config.Name = name;
        if (description != null)
            config.Description = description;
        if (!string.IsNullOrEmpty(reportType))
            config.ReportType = reportType;
        if (filterJson != null)
            config.FilterJson = filterJson;
        if (columnsJson != null)
            config.ColumnsJson = columnsJson;
        if (!string.IsNullOrEmpty(exportFormat))
            config.ExportFormat = exportFormat;
        if (includeMetadata.HasValue)
            config.IncludeMetadata = includeMetadata.Value;
        if (isActive.HasValue)
            config.IsActive = isActive.Value;

        config.UpdatedBy = updatedBy;
        config.UpdatedDate = DateTime.UtcNow;

        _context.ReportConfigurations.Update(config);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Report configuration updated successfully: {ConfigId}", config.Id);
        return config;
    }

    /// <summary>
    /// Soft delete a report configuration
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Deleting report configuration: {ConfigId}", id);

        var config = await _context.ReportConfigurations
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (config == null)
        {
            _logger.LogWarning("Report configuration not found for deletion: {ConfigId}", id);
            return false;
        }

        config.IsDeleted = true;
        config.UpdatedDate = DateTime.UtcNow;

        _context.ReportConfigurations.Update(config);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Report configuration deleted: {ConfigId}", config.Id);
        return true;
    }

    /// <summary>
    /// Clone an existing report configuration
    /// </summary>
    public async Task<ReportConfiguration?> CloneAsync(Guid id, Guid? createdBy = null)
    {
        _logger.LogInformation("Cloning report configuration: {ConfigId}", id);

        var original = await _context.ReportConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (original == null)
        {
            _logger.LogWarning("Report configuration not found for cloning: {ConfigId}", id);
            return null;
        }

        var cloned = new ReportConfiguration
        {
            Id = Guid.NewGuid(),
            Name = $"{original.Name} (Clone)",
            Description = original.Description,
            ReportType = original.ReportType,
            FilterJson = original.FilterJson,
            ColumnsJson = original.ColumnsJson,
            ExportFormat = original.ExportFormat,
            IncludeMetadata = original.IncludeMetadata,
            IsActive = true,
            IsDeleted = false,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            RowVersion = []
        };

        _context.ReportConfigurations.Add(cloned);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Report configuration cloned successfully: {OriginalId} -> {ClonedId}", original.Id, cloned.Id);
        return cloned;
    }

    /// <summary>
    /// Search report configurations by name or description
    /// </summary>
    public async Task<List<ReportConfiguration>> SearchAsync(string searchTerm)
    {
        _logger.LogInformation("Searching report configurations: {SearchTerm}", searchTerm);

        var results = await _context.ReportConfigurations
            .Where(c => !c.IsDeleted &&
                   (c.Name.Contains(searchTerm) ||
                    (c.Description != null && c.Description.Contains(searchTerm))))
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} report configurations matching search term", results.Count);
        return results;
    }

    /// <summary>
    /// Get active report configurations only
    /// </summary>
    public async Task<List<ReportConfiguration>> GetActiveAsync()
    {
        _logger.LogInformation("Retrieving active report configurations");

        var configs = await _context.ReportConfigurations
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} active report configurations", configs.Count);
        return configs;
    }

    /// <summary>
    /// Get report configurations by type
    /// </summary>
    public async Task<List<ReportConfiguration>> GetByTypeAsync(string reportType)
    {
        _logger.LogInformation("Retrieving report configurations by type: {ReportType}", reportType);

        var configs = await _context.ReportConfigurations
            .Where(c => c.ReportType == reportType && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} configurations of type {ReportType}", configs.Count, reportType);
        return configs;
    }
}

/// <summary>
/// Interface for report configuration service
/// </summary>
public interface IReportConfigurationService
{
    Task<ReportConfiguration> CreateAsync(
        string name,
        string reportType,
        string exportFormat,
        string? description = null,
        string? filterJson = null,
        string? columnsJson = null,
        bool includeMetadata = false,
        Guid? createdBy = null);

    Task<ReportConfiguration?> GetByIdAsync(Guid id);
    Task<(List<ReportConfiguration> items, int totalCount)> GetAllAsync(int page = 1, int pageSize = 10);
    Task<ReportConfiguration?> UpdateAsync(
        Guid id,
        string? name = null,
        string? description = null,
        string? reportType = null,
        string? filterJson = null,
        string? columnsJson = null,
        string? exportFormat = null,
        bool? includeMetadata = null,
        bool? isActive = null,
        Guid? updatedBy = null);
    Task<bool> DeleteAsync(Guid id);
    Task<ReportConfiguration?> CloneAsync(Guid id, Guid? createdBy = null);
    Task<List<ReportConfiguration>> SearchAsync(string searchTerm);
    Task<List<ReportConfiguration>> GetActiveAsync();
    Task<List<ReportConfiguration>> GetByTypeAsync(string reportType);
}
