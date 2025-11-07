using Microsoft.AspNetCore.Mvc;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;
using OilTrading.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace OilTrading.Api.Controllers;

/// <summary>
/// API controller for managing report configurations
/// Handles creation, retrieval, updating, and deletion of report configurations
/// </summary>
[ApiController]
[Route("api/report-configurations")]
[Produces("application/json")]
public class ReportConfigurationController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ReportConfigurationController> _logger;

    public ReportConfigurationController(
        ApplicationDbContext dbContext,
        ILogger<ReportConfigurationController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Query Endpoints

    /// <summary>
    /// Gets all report configurations with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ReportConfigurationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfigurations([FromQuery] int pageNum = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageNum < 1) pageNum = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _dbContext.ReportConfigurations.Where(x => !x.IsDeleted).AsQueryable();
            var totalCount = await query.CountAsync();

            var configurations = await query
                .OrderByDescending(x => x.CreatedDate)
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = configurations.Select(c => MapToDto(c)).ToList();
            var totalPages = (totalCount + pageSize - 1) / pageSize;

            _logger.LogInformation("Retrieved {Count} report configurations for page {PageNum}",
                dtos.Count, pageNum);

            return Ok(new PagedResultDto<ReportConfigurationDto>(
                dtos,
                pageNum,
                pageSize,
                totalCount,
                totalPages
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report configurations");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving report configurations" });
        }
    }

    /// <summary>
    /// Gets a specific report configuration by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReportConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfiguration(Guid id)
    {
        try
        {
            var configuration = await _dbContext.ReportConfigurations
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (configuration == null)
            {
                _logger.LogInformation("Report configuration not found: {ConfigurationId}", id);
                return NotFound(new { error = "Report configuration not found" });
            }

            var dto = MapToDto(configuration);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report configuration: {ConfigurationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the report configuration" });
        }
    }

    #endregion

    #region Command Endpoints

    /// <summary>
    /// Creates a new report configuration
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReportConfigurationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateConfiguration([FromBody] CreateReportConfigRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { error = "Name is required" });

            if (string.IsNullOrWhiteSpace(request.ReportType))
                return BadRequest(new { error = "ReportType is required" });

            var configuration = new ReportConfiguration
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                ReportType = request.ReportType,
                FilterJson = request.Filters != null ? JsonSerializer.Serialize(request.Filters) : null,
                ColumnsJson = request.Columns != null ? JsonSerializer.Serialize(request.Columns) : null,
                ExportFormat = request.ExportFormat,
                IncludeMetadata = request.IncludeMetadata,
                IsActive = request.IsActive,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            _dbContext.ReportConfigurations.Add(configuration);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created report configuration {ConfigurationId}: {ConfigurationName}",
                configuration.Id, configuration.Name);

            var dto = MapToDto(configuration);
            return CreatedAtAction(nameof(GetConfiguration), new { id = configuration.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report configuration");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while creating the report configuration" });
        }
    }

    /// <summary>
    /// Updates an existing report configuration
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ReportConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateConfiguration(Guid id, [FromBody] UpdateReportConfigRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var configuration = await _dbContext.ReportConfigurations
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (configuration == null)
            {
                _logger.LogInformation("Report configuration not found for update: {ConfigurationId}", id);
                return NotFound(new { error = "Report configuration not found" });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
                configuration.Name = request.Name;

            if (request.Description != null)
                configuration.Description = request.Description;

            if (!string.IsNullOrWhiteSpace(request.ReportType))
                configuration.ReportType = request.ReportType;

            if (request.Filters != null)
                configuration.FilterJson = JsonSerializer.Serialize(request.Filters);

            if (request.Columns != null)
                configuration.ColumnsJson = JsonSerializer.Serialize(request.Columns);

            if (!string.IsNullOrWhiteSpace(request.ExportFormat))
                configuration.ExportFormat = request.ExportFormat;

            configuration.IncludeMetadata = request.IncludeMetadata;
            configuration.IsActive = request.IsActive ?? configuration.IsActive;
            configuration.UpdatedDate = DateTime.UtcNow;
            configuration.UpdatedBy = Guid.Empty;

            _dbContext.ReportConfigurations.Update(configuration);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Updated report configuration {ConfigurationId}: {ConfigurationName}",
                configuration.Id, configuration.Name);

            var dto = MapToDto(configuration);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating report configuration: {ConfigurationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating the report configuration" });
        }
    }

    /// <summary>
    /// Deletes a report configuration (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConfiguration(Guid id)
    {
        try
        {
            var configuration = await _dbContext.ReportConfigurations
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (configuration == null)
            {
                _logger.LogInformation("Report configuration not found for deletion: {ConfigurationId}", id);
                return NotFound(new { error = "Report configuration not found" });
            }

            configuration.IsDeleted = true;
            _dbContext.ReportConfigurations.Update(configuration);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Deleted report configuration {ConfigurationId}: {ConfigurationName}",
                configuration.Id, configuration.Name);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report configuration: {ConfigurationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting the report configuration" });
        }
    }

    #endregion

    #region Helper Methods

    private static ReportConfigurationDto MapToDto(ReportConfiguration configuration)
    {
        var filters = configuration.FilterJson != null
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(configuration.FilterJson)
            : null;

        var columns = configuration.ColumnsJson != null
            ? JsonSerializer.Deserialize<List<string>>(configuration.ColumnsJson)
            : null;

        return new ReportConfigurationDto(
            configuration.Id,
            configuration.Name,
            configuration.Description,
            configuration.ReportType,
            filters,
            columns,
            configuration.ExportFormat,
            configuration.IncludeMetadata,
            configuration.IsActive,
            configuration.CreatedDate,
            configuration.CreatedBy?.ToString(),
            configuration.UpdatedDate,
            configuration.UpdatedBy?.ToString()
        );
    }

    #endregion
}

/// <summary>
/// Request to create a report configuration
/// </summary>
public class CreateReportConfigRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string Schedule { get; set; } = "Manual";
    public string ScheduleTime { get; set; } = "00:00:00";
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object>? Filters { get; set; }
    public List<string>? Columns { get; set; }
    public string ExportFormat { get; set; } = "CSV";
    public bool IncludeMetadata { get; set; }
}

/// <summary>
/// Request to update a report configuration
/// </summary>
public class UpdateReportConfigRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ReportType { get; set; }
    public string? Schedule { get; set; }
    public string? ScheduleTime { get; set; }
    public bool? IsActive { get; set; }
    public Dictionary<string, object>? Filters { get; set; }
    public List<string>? Columns { get; set; }
    public string? ExportFormat { get; set; }
    public bool IncludeMetadata { get; set; }
}
