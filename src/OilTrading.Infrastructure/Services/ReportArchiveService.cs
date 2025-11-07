using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Services;

/// <summary>
/// Service for managing report archiving, retention, and cleanup
/// </summary>
public class ReportArchiveService : IReportArchiveService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportArchiveService> _logger;

    public ReportArchiveService(
        ApplicationDbContext context,
        ILogger<ReportArchiveService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Archive a completed report execution
    /// </summary>
    public async Task<bool> ArchiveAsync(
        Guid executionId,
        string storageLocation,
        int retentionDays = 90,
        bool isCompressed = false)
    {
        _logger.LogInformation("Archiving report execution: {ExecutionId}, RetentionDays: {RetentionDays}", executionId, retentionDays);

        var execution = await _context.ReportExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId && !e.IsDeleted);

        if (execution == null)
        {
            _logger.LogWarning("Report execution not found for archiving: {ExecutionId}", executionId);
            return false;
        }

        var archive = new ReportArchive
        {
            Id = Guid.NewGuid(),
            ExecutionId = executionId,
            ArchiveDate = DateTime.UtcNow,
            RetentionDays = retentionDays,
            ExpiryDate = DateTime.UtcNow.AddDays(retentionDays),
            StorageLocation = storageLocation,
            IsCompressed = isCompressed,
            FileSize = execution.FileSizeBytes ?? 0,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow
        };

        _context.ReportArchives.Add(archive);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Report archived successfully: {ArchiveId}, ExpiryDate: {ExpiryDate}", archive.Id, archive.ExpiryDate);
        return true;
    }

    /// <summary>
    /// Get a specific archive by ID
    /// </summary>
    public async Task<ReportArchive?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving archive: {ArchiveId}", id);

        var archive = await _context.ReportArchives
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (archive == null)
        {
            _logger.LogWarning("Archive not found: {ArchiveId}", id);
        }

        return archive;
    }

    /// <summary>
    /// Get all archives for a specific report configuration
    /// </summary>
    public async Task<(List<ReportArchive> items, int totalCount)> GetByConfigAsync(
        Guid configId,
        int page = 1,
        int pageSize = 10)
    {
        _logger.LogInformation("Retrieving archives for config: {ConfigId}, page {Page}", configId, page);

        var query = _context.ReportArchives
            .Where(a => a.ReportExecution!.ReportConfigId == configId && !a.IsDeleted)
            .OrderByDescending(a => a.ArchiveDate);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} archives for config {ConfigId}", items.Count, configId);
        return (items, totalCount);
    }

    /// <summary>
    /// Get archives by execution ID
    /// </summary>
    public async Task<ReportArchive?> GetByExecutionAsync(Guid executionId)
    {
        _logger.LogInformation("Retrieving archive for execution: {ExecutionId}", executionId);

        var archive = await _context.ReportArchives
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ExecutionId == executionId && !a.IsDeleted);

        if (archive == null)
        {
            _logger.LogWarning("Archive not found for execution: {ExecutionId}", executionId);
        }

        return archive;
    }

    /// <summary>
    /// Retrieve an archived report (restore or make available)
    /// </summary>
    public async Task<bool> RetrieveAsync(Guid archiveId)
    {
        _logger.LogInformation("Retrieving archive: {ArchiveId}", archiveId);

        var archive = await _context.ReportArchives
            .FirstOrDefaultAsync(a => a.Id == archiveId && !a.IsDeleted);

        if (archive == null)
        {
            _logger.LogWarning("Archive not found for retrieval: {ArchiveId}", archiveId);
            return false;
        }

        // Check if archive has expired
        if (archive.ExpiryDate < DateTime.UtcNow)
        {
            _logger.LogWarning("Archive has expired: {ArchiveId}, ExpiryDate: {ExpiryDate}", archiveId, archive.ExpiryDate);
            return false;
        }

        // TODO: Implement actual file retrieval/restore logic
        _logger.LogInformation("Archive retrieved successfully: {ArchiveId}", archiveId);
        return true;
    }

    /// <summary>
    /// Get all expired archives ready for cleanup
    /// </summary>
    public async Task<List<ReportArchive>> GetExpiredAsync()
    {
        _logger.LogInformation("Retrieving expired archives");

        var now = DateTime.UtcNow;
        var expiredArchives = await _context.ReportArchives
            .Where(a => a.ExpiryDate <= now && !a.IsDeleted)
            .OrderByDescending(a => a.ExpiryDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} expired archives", expiredArchives.Count);
        return expiredArchives;
    }

    /// <summary>
    /// Cleanup expired archives
    /// </summary>
    public async Task<int> CleanupExpiredAsync()
    {
        _logger.LogInformation("Starting cleanup of expired archives");

        var expiredArchives = await GetExpiredAsync();

        if (expiredArchives.Count == 0)
        {
            _logger.LogInformation("No expired archives to cleanup");
            return 0;
        }

        int cleanedCount = 0;

        foreach (var archive in expiredArchives)
        {
            try
            {
                // TODO: Implement actual file deletion logic
                // await _fileService.DeleteAsync(archive.StorageLocation);

                archive.IsDeleted = true;
                _context.ReportArchives.Update(archive);
                cleanedCount++;

                _logger.LogInformation("Archive cleaned up: {ArchiveId}", archive.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up archive: {ArchiveId}", archive.Id);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Archive cleanup completed. Cleaned: {Count}", cleanedCount);
        return cleanedCount;
    }

    /// <summary>
    /// Configure retention policy for a report configuration
    /// </summary>
    public async Task<bool> ConfigureRetentionAsync(Guid configId, int retentionDays)
    {
        _logger.LogInformation("Configuring retention for config: {ConfigId}, Days: {Days}", configId, retentionDays);

        // TODO: Store retention configuration at the ReportConfiguration level
        // For now, this is informational - actual retention is set per archive

        if (retentionDays <= 0)
        {
            _logger.LogWarning("Invalid retention days: {Days}", retentionDays);
            return false;
        }

        _logger.LogInformation("Retention configured: {ConfigId}, Days: {Days}", configId, retentionDays);
        return true;
    }

    /// <summary>
    /// Get archive statistics
    /// </summary>
    public async Task<ArchiveStatistics> GetStatisticsAsync()
    {
        _logger.LogInformation("Retrieving archive statistics");

        var totalArchives = await _context.ReportArchives
            .CountAsync(a => !a.IsDeleted);

        var expiredCount = await _context.ReportArchives
            .CountAsync(a => a.ExpiryDate <= DateTime.UtcNow && !a.IsDeleted);

        var totalSize = await _context.ReportArchives
            .Where(a => !a.IsDeleted)
            .SumAsync(a => a.FileSize);

        var compressedCount = await _context.ReportArchives
            .CountAsync(a => a.IsCompressed && !a.IsDeleted);

        var stats = new ArchiveStatistics
        {
            TotalArchives = totalArchives,
            ExpiredArchives = expiredCount,
            TotalSizeBytes = totalSize,
            CompressedArchives = compressedCount,
            RetrievedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Archive statistics: Total={Total}, Expired={Expired}, Size={Size}",
            stats.TotalArchives, stats.ExpiredArchives, stats.TotalSizeBytes);

        return stats;
    }

    /// <summary>
    /// Extend retention period for an archive
    /// </summary>
    public async Task<bool> ExtendRetentionAsync(Guid archiveId, int additionalDays)
    {
        _logger.LogInformation("Extending retention for archive: {ArchiveId}, AdditionalDays: {Days}", archiveId, additionalDays);

        var archive = await _context.ReportArchives
            .FirstOrDefaultAsync(a => a.Id == archiveId && !a.IsDeleted);

        if (archive == null)
        {
            _logger.LogWarning("Archive not found for retention extension: {ArchiveId}", archiveId);
            return false;
        }

        archive.ExpiryDate = archive.ExpiryDate.AddDays(additionalDays);
        archive.RetentionDays += additionalDays;

        _context.ReportArchives.Update(archive);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Archive retention extended: {ArchiveId}, NewExpiryDate: {ExpiryDate}", archiveId, archive.ExpiryDate);
        return true;
    }

    /// <summary>
    /// Mark archive as deleted
    /// </summary>
    public async Task<bool> MarkAsDeletedAsync(Guid archiveId)
    {
        _logger.LogInformation("Marking archive as deleted: {ArchiveId}", archiveId);

        var archive = await _context.ReportArchives
            .FirstOrDefaultAsync(a => a.Id == archiveId && !a.IsDeleted);

        if (archive == null)
        {
            _logger.LogWarning("Archive not found for deletion: {ArchiveId}", archiveId);
            return false;
        }

        archive.IsDeleted = true;
        _context.ReportArchives.Update(archive);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Archive marked as deleted: {ArchiveId}", archiveId);
        return true;
    }
}

/// <summary>
/// Archive statistics DTO
/// </summary>
public class ArchiveStatistics
{
    public int TotalArchives { get; set; }
    public int ExpiredArchives { get; set; }
    public long TotalSizeBytes { get; set; }
    public int CompressedArchives { get; set; }
    public DateTime RetrievedAt { get; set; }
}

/// <summary>
/// Interface for report archive service
/// </summary>
public interface IReportArchiveService
{
    Task<bool> ArchiveAsync(
        Guid executionId,
        string storageLocation,
        int retentionDays = 90,
        bool isCompressed = false);

    Task<ReportArchive?> GetByIdAsync(Guid id);
    Task<(List<ReportArchive> items, int totalCount)> GetByConfigAsync(Guid configId, int page = 1, int pageSize = 10);
    Task<ReportArchive?> GetByExecutionAsync(Guid executionId);
    Task<bool> RetrieveAsync(Guid archiveId);
    Task<List<ReportArchive>> GetExpiredAsync();
    Task<int> CleanupExpiredAsync();
    Task<bool> ConfigureRetentionAsync(Guid configId, int retentionDays);
    Task<ArchiveStatistics> GetStatisticsAsync();
    Task<bool> ExtendRetentionAsync(Guid archiveId, int additionalDays);
    Task<bool> MarkAsDeletedAsync(Guid archiveId);
}
