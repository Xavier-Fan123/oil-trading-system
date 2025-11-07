using Microsoft.AspNetCore.Mvc;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;
using OilTrading.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace OilTrading.Api.Controllers;

/// <summary>
/// API controller for managing report distribution configurations
/// Handles setup and management of report delivery channels
/// </summary>
[ApiController]
[Route("api/report-distributions")]
[Produces("application/json")]
public class ReportDistributionController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ReportDistributionController> _logger;

    public ReportDistributionController(
        ApplicationDbContext dbContext,
        ILogger<ReportDistributionController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Query Endpoints

    /// <summary>
    /// Gets all distribution configurations with pagination
    /// </summary>
    /// <param name="pageNum">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paged list of distribution configurations</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ReportDistributionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDistributions([FromQuery] int pageNum = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageNum < 1) pageNum = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _dbContext.ReportDistributions.Where(x => !x.IsDeleted).AsQueryable();
            var totalCount = await query.CountAsync();

            var distributions = await query
                .OrderByDescending(x => x.CreatedDate)
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = distributions.Select(d => MapToDto(d)).ToList();
            var totalPages = (totalCount + pageSize - 1) / pageSize;

            _logger.LogInformation("Retrieved {Count} report distributions for page {PageNum}",
                dtos.Count, pageNum);

            return Ok(new PagedResultDto<ReportDistributionDto>(
                dtos,
                pageNum,
                pageSize,
                totalCount,
                totalPages
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report distributions");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving report distributions" });
        }
    }

    /// <summary>
    /// Gets a specific distribution configuration by ID
    /// </summary>
    /// <param name="id">Distribution configuration ID</param>
    /// <returns>Distribution configuration details if found</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReportDistributionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDistribution(Guid id)
    {
        try
        {
            var distribution = await _dbContext.ReportDistributions
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (distribution == null)
            {
                _logger.LogInformation("Report distribution not found: {DistributionId}", id);
                return NotFound(new { error = "Report distribution not found" });
            }

            var dto = MapToDto(distribution);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report distribution: {DistributionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the report distribution" });
        }
    }

    #endregion

    #region Command Endpoints

    /// <summary>
    /// Creates a new distribution channel configuration
    /// </summary>
    /// <param name="request">Distribution creation request</param>
    /// <returns>Created distribution configuration with ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ReportDistributionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDistribution([FromBody] CreateDistributionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate required fields - accept both old and new property names
            var channelName = !string.IsNullOrWhiteSpace(request.ChannelName) ? request.ChannelName : request.Name;
            var channelType = !string.IsNullOrWhiteSpace(request.ChannelType) ? request.ChannelType : request.Channel;

            if (string.IsNullOrWhiteSpace(channelName))
            {
                return BadRequest(new { error = "Name or ChannelName is required" });
            }

            if (string.IsNullOrWhiteSpace(channelType))
            {
                return BadRequest(new { error = "Channel or ChannelType is required" });
            }

            // Validate channel type
            var validChannelTypes = new[] { "Email", "SFTP", "Webhook", "FTP", "S3", "Azure" };
            if (!validChannelTypes.Contains(channelType))
            {
                return BadRequest(new { error = $"Invalid Channel. Must be one of: {string.Join(", ", validChannelTypes)}" });
            }

            // Create channel configuration from recipients if provided
            var channelConfig = request.ChannelConfiguration ?? new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(request.Recipients))
            {
                channelConfig["recipients"] = request.Recipients;
            }

            var distribution = new ReportDistribution
            {
                Id = Guid.NewGuid(),
                ReportConfigId = request.ReportConfigId ?? Guid.Empty,
                ChannelName = channelName,
                ChannelType = channelType,
                ChannelConfiguration = JsonSerializer.Serialize(channelConfig),
                IsEnabled = request.IsEnabled,
                LastTestedDate = null,
                LastTestStatus = null,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                IsDeleted = false
            };

            _dbContext.ReportDistributions.Add(distribution);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created report distribution {DistributionId}: {ChannelName} ({ChannelType})",
                distribution.Id, distribution.ChannelName, distribution.ChannelType);

            var dto = MapToDto(distribution);
            return CreatedAtAction(nameof(GetDistribution), new { id = distribution.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report distribution");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while creating the report distribution" });
        }
    }

    /// <summary>
    /// Updates an existing distribution channel configuration
    /// </summary>
    /// <param name="id">Distribution configuration ID</param>
    /// <param name="request">Distribution update request</param>
    /// <returns>Updated distribution configuration</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ReportDistributionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDistribution(Guid id, [FromBody] UpdateDistributionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var distribution = await _dbContext.ReportDistributions
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (distribution == null)
            {
                _logger.LogInformation("Report distribution not found for update: {DistributionId}", id);
                return NotFound(new { error = "Report distribution not found" });
            }

            // Update fields - accept both old and new property names
            if (!string.IsNullOrWhiteSpace(request.ChannelName))
            {
                distribution.ChannelName = request.ChannelName;
            }
            else if (!string.IsNullOrWhiteSpace(request.Name))
            {
                distribution.ChannelName = request.Name;
            }

            if (!string.IsNullOrWhiteSpace(request.Channel))
            {
                distribution.ChannelType = request.Channel;
            }

            if (request.ChannelConfiguration != null)
            {
                distribution.ChannelConfiguration = JsonSerializer.Serialize(request.ChannelConfiguration);
            }

            if (!string.IsNullOrWhiteSpace(request.Recipients))
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(distribution.ChannelConfiguration ?? "{}") ?? new Dictionary<string, object>();
                config["recipients"] = request.Recipients;
                distribution.ChannelConfiguration = JsonSerializer.Serialize(config);
            }

            if (request.IsEnabled.HasValue)
            {
                distribution.IsEnabled = request.IsEnabled.Value;
            }

            distribution.UpdatedDate = DateTime.UtcNow;
            distribution.UpdatedBy = Guid.Empty;

            // Update test tracking
            if (distribution.IsEnabled)
            {
                distribution.LastTestedDate = DateTime.UtcNow;
                distribution.LastTestStatus = "Configured"; // In Phase 5+, would actually test the channel
            }

            _dbContext.ReportDistributions.Update(distribution);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Updated report distribution {DistributionId}: {ChannelName}",
                distribution.Id, distribution.ChannelName);

            var dto = MapToDto(distribution);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating report distribution: {DistributionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating the report distribution" });
        }
    }

    /// <summary>
    /// Deletes a distribution channel configuration (soft delete)
    /// </summary>
    /// <param name="id">Distribution configuration ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDistribution(Guid id)
    {
        try
        {
            var distribution = await _dbContext.ReportDistributions
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (distribution == null)
            {
                _logger.LogInformation("Report distribution not found for deletion: {DistributionId}", id);
                return NotFound(new { error = "Report distribution not found" });
            }

            // Soft delete
            distribution.IsDeleted = true;
            _dbContext.ReportDistributions.Update(distribution);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Deleted report distribution {DistributionId}: {ChannelName}",
                distribution.Id, distribution.ChannelName);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report distribution: {DistributionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting the report distribution" });
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Maps ReportDistribution entity to DTO
    /// </summary>
    private static ReportDistributionDto MapToDto(ReportDistribution distribution)
    {
        var config = distribution.ChannelConfiguration != null
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(distribution.ChannelConfiguration)
            : null;

        return new ReportDistributionDto(
            distribution.Id,
            distribution.ReportConfigId,
            distribution.ChannelName,
            distribution.ChannelType,
            config,
            distribution.IsEnabled,
            distribution.LastTestedDate,
            distribution.LastTestStatus,
            distribution.CreatedDate
        );
    }

    #endregion
}

/// <summary>
/// Request to create a distribution channel
/// </summary>
public class CreateDistributionRequest
{
    public Guid? ReportConfigId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string ChannelType { get; set; } = string.Empty;
    public string? Recipients { get; set; }
    public Dictionary<string, object>? ChannelConfiguration { get; set; }
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Request to update a distribution channel
/// </summary>
public class UpdateDistributionRequest
{
    public string? Name { get; set; }
    public string? ChannelName { get; set; }
    public string? Channel { get; set; }
    public string? Recipients { get; set; }
    public Dictionary<string, object>? ChannelConfiguration { get; set; }
    public bool? IsEnabled { get; set; }
}
