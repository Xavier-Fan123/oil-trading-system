using Microsoft.AspNetCore.Mvc;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;
using OilTrading.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace OilTrading.Api.Controllers;

/// <summary>
/// API controller for managing report executions
/// Handles execution of reports and retrieval of execution results
/// </summary>
[ApiController]
[Route("api/report-executions")]
[Produces("application/json")]
public class ReportExecutionController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ReportExecutionController> _logger;

    public ReportExecutionController(
        ApplicationDbContext dbContext,
        ILogger<ReportExecutionController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Query Endpoints

    /// <summary>
    /// Gets all report executions with pagination
    /// </summary>
    /// <param name="pageNum">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paged list of report executions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ReportExecutionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExecutions([FromQuery] int pageNum = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageNum < 1) pageNum = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _dbContext.ReportExecutions.Where(x => !x.IsDeleted).AsQueryable();
            var totalCount = await query.CountAsync();

            var executions = await query
                .OrderByDescending(x => x.ExecutionStartTime)
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = executions.Select(e => MapToDto(e)).ToList();
            var totalPages = (totalCount + pageSize - 1) / pageSize;

            _logger.LogInformation("Retrieved {Count} report executions for page {PageNum}",
                dtos.Count, pageNum);

            return Ok(new PagedResultDto<ReportExecutionDto>(
                dtos,
                pageNum,
                pageSize,
                totalCount,
                totalPages
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report executions");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving report executions" });
        }
    }

    /// <summary>
    /// Gets a specific report execution by ID
    /// </summary>
    /// <param name="id">Report execution ID</param>
    /// <returns>Report execution details if found</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReportExecutionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExecution(Guid id)
    {
        try
        {
            var execution = await _dbContext.ReportExecutions
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (execution == null)
            {
                _logger.LogInformation("Report execution not found: {ExecutionId}", id);
                return NotFound(new { error = "Report execution not found" });
            }

            var dto = MapToDto(execution);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report execution: {ExecutionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the report execution" });
        }
    }

    #endregion

    #region Command Endpoints

    /// <summary>
    /// Executes a report based on configuration
    /// </summary>
    /// <param name="request">Report execution request with configuration ID and parameters</param>
    /// <returns>Created report execution with ID</returns>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ReportExecutionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteReport([FromBody] ExecuteReportRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate that the report configuration exists
            var configuration = await _dbContext.ReportConfigurations
                .FirstOrDefaultAsync(x => x.Id == request.ReportConfigurationId && !x.IsDeleted);

            if (configuration == null)
            {
                _logger.LogWarning("Report configuration not found for execution: {ConfigurationId}",
                    request.ReportConfigurationId);
                return NotFound(new { error = "Report configuration not found" });
            }

            // Create execution record
            var execution = new ReportExecution
            {
                Id = Guid.NewGuid(),
                ReportConfigId = request.ReportConfigurationId,
                ExecutionStartTime = DateTime.UtcNow,
                Status = "Running",
                OutputFileFormat = request.OutputFormat ?? "CSV",
                IsScheduled = request.IsScheduled,
                ExecutedBy = Guid.Empty,
                CreatedDate = DateTime.UtcNow,
                IsDeleted = false
            };

            _dbContext.ReportExecutions.Add(execution);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Started execution of report configuration {ConfigurationId}: {ConfigurationName}",
                configuration.Id, configuration.Name);

            // Simulate execution completion
            execution.ExecutionEndTime = DateTime.UtcNow.AddSeconds(5);
            execution.Status = "Completed";
            execution.RecordsProcessed = 1500;
            execution.TotalRecords = 1500;
            execution.DurationSeconds = 5.0;
            execution.OutputFileName = $"report_{execution.Id:N}.csv";
            execution.OutputFilePath = $"/reports/{execution.Id:N}.csv";
            execution.FileSizeBytes = 125000;
            execution.SuccessfulDistributions = 0;
            execution.FailedDistributions = 0;

            _dbContext.ReportExecutions.Update(execution);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Completed execution of report {ExecutionId} in {Duration}s",
                execution.Id, execution.DurationSeconds);

            var dto = MapToDto(execution);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing report");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while executing the report" });
        }
    }

    /// <summary>
    /// Downloads the result file of a report execution
    /// </summary>
    /// <param name="id">Report execution ID</param>
    /// <returns>Report file content if available</returns>
    [HttpPost("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/octet-stream")]
    public async Task<IActionResult> DownloadExecution(Guid id)
    {
        try
        {
            var execution = await _dbContext.ReportExecutions
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (execution == null)
            {
                _logger.LogInformation("Report execution not found for download: {ExecutionId}", id);
                return NotFound(new { error = "Report execution not found" });
            }

            if (execution.Status != "Completed")
            {
                return BadRequest(new { error = "Report execution has not completed yet" });
            }

            if (string.IsNullOrWhiteSpace(execution.OutputFileName))
            {
                return BadRequest(new { error = "No file available for this execution" });
            }

            // In a real implementation, this would retrieve the actual file from storage
            // For now, return a simulated response
            var fileContent = System.Text.Encoding.UTF8.GetBytes(
                $"Report Data\n" +
                $"Execution ID: {execution.Id}\n" +
                $"Configuration ID: {execution.ReportConfigId}\n" +
                $"Executed at: {execution.ExecutionStartTime:O}\n" +
                $"Completed at: {execution.ExecutionEndTime:O}\n" +
                $"Records Processed: {execution.RecordsProcessed}\n" +
                $"Duration: {execution.DurationSeconds} seconds\n" +
                $"Format: {execution.OutputFileFormat}\n");

            _logger.LogInformation("Downloaded report execution {ExecutionId}: {FileName}",
                id, execution.OutputFileName);

            return File(fileContent, "application/octet-stream", execution.OutputFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading report execution: {ExecutionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while downloading the report" });
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Maps ReportExecution entity to DTO
    /// </summary>
    private static ReportExecutionDto MapToDto(ReportExecution execution)
    {
        return new ReportExecutionDto(
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
    }

    #endregion
}

/// <summary>
/// Request to execute a report
/// </summary>
public class ExecuteReportRequest
{
    public Guid ReportConfigurationId { get; set; }
    public string? OutputFormat { get; set; }
    public bool IsScheduled { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}
