using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Services;

/// <summary>
/// Service for managing report executions, tracking, and file operations
/// </summary>
public class ReportExecutionService : IReportExecutionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportExecutionService> _logger;

    public ReportExecutionService(
        ApplicationDbContext context,
        ILogger<ReportExecutionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create and execute a report for the given configuration
    /// </summary>
    public async Task<ReportExecution> ExecuteAsync(Guid configId, Guid? executedBy = null)
    {
        _logger.LogInformation("Executing report: {ConfigId}", configId);

        var config = await _context.ReportConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == configId && !c.IsDeleted);

        if (config == null)
        {
            throw new InvalidOperationException($"Report configuration not found: {configId}");
        }

        var execution = new ReportExecution
        {
            Id = Guid.NewGuid(),
            ReportConfigId = configId,
            ExecutionStartTime = DateTime.UtcNow,
            Status = "Running",
            ExecutedBy = executedBy,
            IsScheduled = false,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow,
            SuccessfulDistributions = 0,
            FailedDistributions = 0
        };

        _context.ReportExecutions.Add(execution);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Report execution started: {ExecutionId}", execution.Id);
        return execution;
    }

    /// <summary>
    /// Get a specific report execution by ID
    /// </summary>
    public async Task<ReportExecution?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving report execution: {ExecutionId}", id);

        var execution = await _context.ReportExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (execution == null)
        {
            _logger.LogWarning("Report execution not found: {ExecutionId}", id);
        }

        return execution;
    }

    /// <summary>
    /// Get all executions for a specific report configuration with pagination
    /// </summary>
    public async Task<(List<ReportExecution> items, int totalCount)> GetByConfigAsync(
        Guid configId,
        int page = 1,
        int pageSize = 10)
    {
        _logger.LogInformation("Retrieving executions for config: {ConfigId}, page {Page}", configId, page);

        var query = _context.ReportExecutions
            .Where(e => e.ReportConfigId == configId && !e.IsDeleted)
            .OrderByDescending(e => e.ExecutionStartTime);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} executions for config {ConfigId}", items.Count, configId);
        return (items, totalCount);
    }

    /// <summary>
    /// Update the status of a report execution
    /// </summary>
    public async Task<bool> UpdateStatusAsync(Guid id, string status, string? errorMessage = null)
    {
        _logger.LogInformation("Updating execution status: {ExecutionId}, Status: {Status}", id, status);

        var execution = await _context.ReportExecutions
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (execution == null)
        {
            _logger.LogWarning("Report execution not found for status update: {ExecutionId}", id);
            return false;
        }

        execution.Status = status;
        execution.ErrorMessage = errorMessage;

        if (status == "Completed" || status == "Failed")
        {
            execution.ExecutionEndTime = DateTime.UtcNow;

            if (execution.ExecutionStartTime != null)
            {
                var duration = execution.ExecutionEndTime.Value - execution.ExecutionStartTime;
                execution.DurationSeconds = (double)duration.TotalSeconds;
            }
        }

        _context.ReportExecutions.Update(execution);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Execution status updated: {ExecutionId}, Status: {Status}", id, status);
        return true;
    }

    /// <summary>
    /// Update execution metrics after report generation
    /// </summary>
    public async Task<bool> UpdateMetricsAsync(
        Guid id,
        int? recordsProcessed = null,
        int? totalRecords = null,
        long? fileSizeBytes = null,
        string? outputFileName = null,
        string? outputFilePath = null,
        string? outputFileFormat = null)
    {
        _logger.LogInformation("Updating execution metrics: {ExecutionId}", id);

        var execution = await _context.ReportExecutions
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (execution == null)
        {
            _logger.LogWarning("Report execution not found for metrics update: {ExecutionId}", id);
            return false;
        }

        if (recordsProcessed.HasValue)
            execution.RecordsProcessed = recordsProcessed;
        if (totalRecords.HasValue)
            execution.TotalRecords = totalRecords;
        if (fileSizeBytes.HasValue)
            execution.FileSizeBytes = fileSizeBytes;
        if (!string.IsNullOrEmpty(outputFileName))
            execution.OutputFileName = outputFileName;
        if (!string.IsNullOrEmpty(outputFilePath))
            execution.OutputFilePath = outputFilePath;
        if (!string.IsNullOrEmpty(outputFileFormat))
            execution.OutputFileFormat = outputFileFormat;

        _context.ReportExecutions.Update(execution);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Execution metrics updated: {ExecutionId}", id);
        return true;
    }

    /// <summary>
    /// Update distribution tracking after sending
    /// </summary>
    public async Task<bool> UpdateDistributionStatusAsync(
        Guid id,
        int successfulDistributions,
        int failedDistributions)
    {
        _logger.LogInformation("Updating distribution status: {ExecutionId}", id);

        var execution = await _context.ReportExecutions
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (execution == null)
        {
            _logger.LogWarning("Report execution not found for distribution update: {ExecutionId}", id);
            return false;
        }

        execution.SuccessfulDistributions = successfulDistributions;
        execution.FailedDistributions = failedDistributions;

        _context.ReportExecutions.Update(execution);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Distribution status updated: {ExecutionId}", id);
        return true;
    }

    /// <summary>
    /// Mark a scheduled execution
    /// </summary>
    public async Task<bool> MarkAsScheduledAsync(Guid id)
    {
        _logger.LogInformation("Marking execution as scheduled: {ExecutionId}", id);

        var execution = await _context.ReportExecutions
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (execution == null)
        {
            _logger.LogWarning("Report execution not found for scheduled marking: {ExecutionId}", id);
            return false;
        }

        execution.IsScheduled = true;
        _context.ReportExecutions.Update(execution);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Execution marked as scheduled: {ExecutionId}", id);
        return true;
    }

    /// <summary>
    /// Soft delete a report execution
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Deleting report execution: {ExecutionId}", id);

        var execution = await _context.ReportExecutions
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (execution == null)
        {
            _logger.LogWarning("Report execution not found for deletion: {ExecutionId}", id);
            return false;
        }

        execution.IsDeleted = true;
        _context.ReportExecutions.Update(execution);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Report execution deleted: {ExecutionId}", id);
        return true;
    }

    /// <summary>
    /// Get executions by status
    /// </summary>
    public async Task<List<ReportExecution>> GetByStatusAsync(string status)
    {
        _logger.LogInformation("Retrieving executions by status: {Status}", status);

        var executions = await _context.ReportExecutions
            .Where(e => e.Status == status && !e.IsDeleted)
            .OrderByDescending(e => e.ExecutionStartTime)
            .ToListAsync();

        _logger.LogInformation("Found {Count} executions with status {Status}", executions.Count, status);
        return executions;
    }

    /// <summary>
    /// Get pending executions (not yet completed or failed)
    /// </summary>
    public async Task<List<ReportExecution>> GetPendingAsync()
    {
        _logger.LogInformation("Retrieving pending executions");

        var executions = await _context.ReportExecutions
            .Where(e => (e.Status == "Pending" || e.Status == "Running") && !e.IsDeleted)
            .OrderByDescending(e => e.ExecutionStartTime)
            .ToListAsync();

        _logger.LogInformation("Found {Count} pending executions", executions.Count);
        return executions;
    }

    /// <summary>
    /// Get failed executions for retry
    /// </summary>
    public async Task<List<ReportExecution>> GetFailedAsync()
    {
        _logger.LogInformation("Retrieving failed executions");

        var executions = await _context.ReportExecutions
            .Where(e => e.Status == "Failed" && !e.IsDeleted)
            .OrderByDescending(e => e.ExecutionStartTime)
            .ToListAsync();

        _logger.LogInformation("Found {Count} failed executions", executions.Count);
        return executions;
    }

    /// <summary>
    /// Count executions by configuration
    /// </summary>
    public async Task<int> GetCountByConfigAsync(Guid configId)
    {
        _logger.LogInformation("Counting executions for config: {ConfigId}", configId);

        var count = await _context.ReportExecutions
            .CountAsync(e => e.ReportConfigId == configId && !e.IsDeleted);

        _logger.LogInformation("Config {ConfigId} has {Count} executions", configId, count);
        return count;
    }
}

/// <summary>
/// Interface for report execution service
/// </summary>
public interface IReportExecutionService
{
    Task<ReportExecution> ExecuteAsync(Guid configId, Guid? executedBy = null);
    Task<ReportExecution?> GetByIdAsync(Guid id);
    Task<(List<ReportExecution> items, int totalCount)> GetByConfigAsync(Guid configId, int page = 1, int pageSize = 10);
    Task<bool> UpdateStatusAsync(Guid id, string status, string? errorMessage = null);
    Task<bool> UpdateMetricsAsync(
        Guid id,
        int? recordsProcessed = null,
        int? totalRecords = null,
        long? fileSizeBytes = null,
        string? outputFileName = null,
        string? outputFilePath = null,
        string? outputFileFormat = null);
    Task<bool> UpdateDistributionStatusAsync(Guid id, int successfulDistributions, int failedDistributions);
    Task<bool> MarkAsScheduledAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
    Task<List<ReportExecution>> GetByStatusAsync(string status);
    Task<List<ReportExecution>> GetPendingAsync();
    Task<List<ReportExecution>> GetFailedAsync();
    Task<int> GetCountByConfigAsync(Guid configId);
}
