using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.MarketData;
using OilTrading.Application.Queries.MarketData;
using OilTrading.Application.DTOs;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/market-data")]
public class MarketDataController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MarketDataController> _logger;

    public MarketDataController(IMediator mediator, ILogger<MarketDataController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    private string GetCurrentUserName()
    {
        try
        {
            return User?.Identity?.Name ?? 
                   HttpContext?.User?.Identity?.Name ?? 
                   "System";
        }
        catch
        {
            return "System";
        }
    }

    /// <summary>
    /// Upload market data from Excel files
    /// </summary>
    /// <returns>Upload result with imported prices</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MarketDataUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMarketData([FromForm] MarketDataUploadRequest request)
    {
        _logger.LogInformation("Upload request received. File: {FileName}, FileType: {FileType}, OverwriteExisting: {OverwriteExisting}", 
            request?.File?.FileName ?? "null", request?.FileType ?? "null", request?.OverwriteExisting ?? false);

        var file = request?.File;
        var fileType = request?.FileType;

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("No file uploaded in request");
            return BadRequest(new { error = "No file uploaded" });
        }

        if (string.IsNullOrEmpty(fileType) || (fileType != "Spot" && fileType != "Futures"))
        {
            _logger.LogWarning("Invalid file type: {FileType}", fileType);
            return BadRequest(new { error = "Invalid file type. Must be 'Spot' or 'Futures'" });
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
            return BadRequest(new { error = "Invalid file format. Only Excel (.xlsx, .xls) and CSV files are supported" });

        // Check file size (max 10MB)
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { error = "File size exceeds maximum allowed size of 10MB" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var command = new UploadMarketDataCommand
        {
            FileName = file.FileName,
            FileType = fileType,
            FileContent = ms.ToArray(),
            UploadedBy = GetCurrentUserName(),
            OverwriteExisting = request?.OverwriteExisting ?? false
        };

        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            _logger.LogInformation(
                "Market data uploaded successfully. File: {FileName}, Type: {FileType}, Records: {RecordCount}", 
                file.FileName, 
                fileType, 
                result.RecordsProcessed
            );
            return Ok(result);
        }

        _logger.LogWarning(
            "Market data upload failed. File: {FileName}, Errors: {Errors}",
            file.FileName,
            string.Join(", ", result.Errors)
        );
        
        // Return a clear error response
        return BadRequest(new { 
            error = "Upload failed", 
            details = result.Errors,
            success = false 
        });
    }

    /// <summary>
    /// Get latest market prices
    /// </summary>
    /// <returns>Latest spot and futures prices</returns>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(LatestPricesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestPrices()
    {
        var query = new GetLatestPricesQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get price history for a specific product
    /// </summary>
    /// <param name="productCode">Product code (e.g., MOPS_380, ICE_BRENT)</param>
    /// <param name="startDate">Start date for price history</param>
    /// <param name="endDate">End date for price history</param>
    /// <returns>Historical prices for the product</returns>
    [HttpGet("history/{productCode}")]
    [ProducesResponseType(typeof(IEnumerable<MarketPriceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPriceHistory(
        string productCode, 
        [FromQuery] DateTime? startDate, 
        [FromQuery] DateTime? endDate)
    {
        var query = new GetPriceHistoryQuery
        {
            ProductCode = productCode,
            StartDate = startDate ?? DateTime.UtcNow.AddMonths(-1),
            EndDate = endDate ?? DateTime.UtcNow
        };
        
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Delete all market data records
    /// </summary>
    /// <param name="reason">Reason for deletion</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("all")]
    [ProducesResponseType(typeof(DeleteMarketDataResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAllMarketData([FromQuery] string? reason = null)
    {
        var command = new DeleteMarketDataCommand
        {
            DeleteType = DeleteMarketDataType.All,
            DeletedBy = GetCurrentUserName(),
            Reason = reason
        };

        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            _logger.LogInformation(
                "All market data deleted successfully. Records deleted: {RecordsDeleted}, User: {User}",
                result.RecordsDeleted,
                GetCurrentUserName());
            return Ok(result);
        }

        _logger.LogWarning(
            "Failed to delete all market data. Errors: {Errors}",
            string.Join(", ", result.Errors));
        
        return BadRequest(result);
    }

    /// <summary>
    /// Delete market data by date range
    /// </summary>
    /// <param name="startDate">Start date for deletion</param>
    /// <param name="endDate">End date for deletion</param>
    /// <param name="reason">Reason for deletion</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("by-date")]
    [ProducesResponseType(typeof(DeleteMarketDataResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteMarketDataByDate(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? reason = null)
    {
        if (startDate >= endDate)
        {
            return BadRequest(new { error = "Start date must be before end date" });
        }

        var command = new DeleteMarketDataCommand
        {
            DeleteType = DeleteMarketDataType.ByDateRange,
            StartDate = startDate,
            EndDate = endDate,
            DeletedBy = GetCurrentUserName(),
            Reason = reason
        };

        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            _logger.LogInformation(
                "Market data deleted by date range. Start: {StartDate}, End: {EndDate}, Records deleted: {RecordsDeleted}, User: {User}",
                startDate, endDate, result.RecordsDeleted, GetCurrentUserName());
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Get count of all market data records
    /// </summary>
    /// <returns>Total count of records</returns>
    [HttpGet("count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMarketDataCount()
    {
        // We need to create a simple query for this, but for now let's use the repository directly
        // This is a temporary solution - ideally we'd create a proper query
        return Ok(new { message = "Count endpoint not implemented yet" });
    }
}

public class MarketDataUploadRequest
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; }

    [FromForm(Name = "fileType")]
    public string FileType { get; set; }

    [FromForm(Name = "overwriteExisting")]
    public bool OverwriteExisting { get; set; } = false;
}