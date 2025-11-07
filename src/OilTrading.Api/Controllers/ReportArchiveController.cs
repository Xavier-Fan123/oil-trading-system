using Microsoft.AspNetCore.Mvc;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;
using OilTrading.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace OilTrading.Api.Controllers;

/// <summary>
/// API controller for managing archived reports
/// Handles archival, retrieval, and deletion of historical report executions
/// </summary>
[ApiController]
[Route("api/report-archives")]
[Produces("application/json")]
public class ReportArchiveController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ReportArchiveController> _logger;

    public ReportArchiveController(
        ApplicationDbContext dbContext,
        ILogger<ReportArchiveController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Query Endpoints

    /// <summary>
    /// Gets all archived reports with pagination
    /// </summary>
    /// <param name="pageNum">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paged list of archived reports</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ReportArchiveDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArchives([FromQuery] int pageNum = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageNum < 1) pageNum = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _dbContext.ReportArchives.AsQueryable();
            var totalCount = await query.CountAsync();

            var archives = await query
                .OrderByDescending(x => x.ArchiveDate)
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = archives.Select(a => MapToDto(a)).ToList();
            var totalPages = (totalCount + pageSize - 1) / pageSize;

            _logger.LogInformation("Retrieved {Count} report archives for page {PageNum}",
                dtos.Count, pageNum);

            return Ok(new PagedResultDto<ReportArchiveDto>(
                dtos,
                pageNum,
                pageSize,
                totalCount,
                totalPages
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report archives");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving report archives" });
        }
    }

    /// <summary>
    /// Gets a specific archived report by ID
    /// </summary>
    /// <param name="id">Report archive ID</param>
    /// <returns>Archive details if found</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReportArchiveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArchive(Guid id)
    {
        try
        {
            var archive = await _dbContext.ReportArchives
                .FirstOrDefaultAsync(x => x.Id == id);

            if (archive == null)
            {
                _logger.LogInformation("Report archive not found: {ArchiveId}", id);
                return NotFound(new { error = "Report archive not found" });
            }

            var dto = MapToDto(archive);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report archive: {ArchiveId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the report archive" });
        }
    }

    #endregion

    #region Command Endpoints

    /// <summary>
    /// Downloads an archived report file
    /// </summary>
    /// <param name="id">Report archive ID</param>
    /// <returns>Archive file content if available</returns>
    [HttpPost("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/octet-stream")]
    public async Task<IActionResult> DownloadArchive(Guid id)
    {
        try
        {
            var archive = await _dbContext.ReportArchives
                .Include(x => x.ReportExecution)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (archive == null)
            {
                _logger.LogInformation("Report archive not found for download: {ArchiveId}", id);
                return NotFound(new { error = "Report archive not found" });
            }

            // Check if archive has expired
            if (DateTime.UtcNow > archive.ExpiryDate)
            {
                _logger.LogWarning("Attempted to download expired archive {ArchiveId}", id);
                return BadRequest(new { error = "This archive has expired and is no longer available" });
            }

            // In a real implementation, this would retrieve the actual archived file from storage
            // For now, return a simulated response
            var fileName = $"report_archive_{archive.Id:N}.csv";
            var fileContent = System.Text.Encoding.UTF8.GetBytes(
                $"Archived Report Data\n" +
                $"Archive ID: {archive.Id}\n" +
                $"Execution ID: {archive.ExecutionId}\n" +
                $"Archived at: {archive.ArchiveDate:O}\n" +
                $"Expires at: {archive.ExpiryDate:O}\n" +
                $"Retention Days: {archive.RetentionDays}\n" +
                $"File Size: {archive.FileSize} bytes\n" +
                $"Compressed: {archive.IsCompressed}\n");

            _logger.LogInformation("Downloaded report archive {ArchiveId}: {FileName}",
                id, fileName);

            return File(fileContent, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading report archive: {ArchiveId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while downloading the archive" });
        }
    }

    /// <summary>
    /// Restores an archived report from archive
    /// </summary>
    /// <param name="id">Report archive ID</param>
    /// <returns>Restored report execution details</returns>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(ReportExecutionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreArchive(Guid id)
    {
        try
        {
            var archive = await _dbContext.ReportArchives
                .Include(x => x.ReportExecution)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (archive == null)
            {
                _logger.LogInformation("Report archive not found for restore: {ArchiveId}", id);
                return NotFound(new { error = "Report archive not found" });
            }

            // Check if archive has expired
            if (DateTime.UtcNow > archive.ExpiryDate)
            {
                _logger.LogWarning("Attempted to restore expired archive {ArchiveId}", id);
                return BadRequest(new { error = "This archive has expired and cannot be restored" });
            }

            // Get the execution that was archived
            var execution = archive.ReportExecution;
            if (execution == null)
            {
                _logger.LogWarning("Associated execution not found for archive {ArchiveId}", id);
                return NotFound(new { error = "Associated report execution not found" });
            }

            _logger.LogInformation("Restored report archive {ArchiveId} from execution {ExecutionId}",
                id, execution.Id);

            // Return the execution details
            var dto = new ReportExecutionDto(
                execution.Id,
                execution.ReportConfigId,
                null,
                execution.ExecutionStartTime,
                execution.ExecutionEndTime,
                execution.Status,
                execution.RecordsProcessed,
                execution.ErrorMessage,
                (int?)(execution.DurationSeconds.HasValue ? execution.DurationSeconds * 1000 : null),
                (int?)execution.FileSizeBytes,
                execution.OutputFileName,
                execution.OutputFilePath,
                execution.ExecutedBy?.ToString()
            );

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring report archive: {ArchiveId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while restoring the archive" });
        }
    }

    /// <summary>
    /// Deletes an archived report permanently
    /// </summary>
    /// <param name="id">Report archive ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteArchive(Guid id)
    {
        try
        {
            var archive = await _dbContext.ReportArchives
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (archive == null)
            {
                _logger.LogInformation("Report archive not found for deletion: {ArchiveId}", id);
                return NotFound(new { error = "Report archive not found" });
            }

            // Note: In a production system, you would also delete the associated file from storage
            _dbContext.ReportArchives.Remove(archive);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Deleted report archive {ArchiveId}. Freed {FileSize} bytes",
                archive.Id, archive.FileSize);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report archive: {ArchiveId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting the archive" });
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Maps ReportArchive entity to DTO
    /// </summary>
    private static ReportArchiveDto MapToDto(ReportArchive archive)
    {
        return new ReportArchiveDto(
            archive.Id,
            archive.ExecutionId,
            archive.ArchiveDate,
            archive.RetentionDays,
            archive.ExpiryDate,
            archive.StorageLocation,
            archive.IsCompressed,
            archive.FileSize
        );
    }

    #endregion
}
