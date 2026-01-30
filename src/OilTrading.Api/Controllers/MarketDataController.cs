using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.MarketData;
using OilTrading.Application.Queries.MarketData;
using OilTrading.Application.DTOs;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Entities;

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
    /// <param name="productCode">Product code (e.g., BRENT, WTI, MGO)</param>
    /// <param name="startDate">Start date for price history</param>
    /// <param name="endDate">End date for price history</param>
    /// <param name="priceType">Optional: Price type filter (Spot, FuturesSettlement, FuturesClose, Index, Spread)</param>
    /// <param name="contractMonth">Optional: Contract month filter (e.g., JAN25, FEB25)</param>
    /// <returns>Historical prices for the product with optional filters</returns>
    [HttpGet("history/{productCode}")]
    [ProducesResponseType(typeof(IEnumerable<MarketPriceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPriceHistory(
        string productCode,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? priceType,
        [FromQuery] string? contractMonth,
        [FromQuery] string? region)  // NEW: Region filter for spot prices
    {
        var query = new GetPriceHistoryQuery
        {
            ProductCode = productCode,
            StartDate = startDate ?? DateTime.UtcNow.AddMonths(-1),
            EndDate = endDate ?? DateTime.UtcNow,
            PriceType = priceType,
            ContractMonth = contractMonth,
            Region = region  // NEW: Include region in query
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

    /// <summary>
    /// Get prices for settlement calculation by product, contract month and date range
    /// Used to calculate settlement benchmark prices using historical market data
    /// </summary>
    /// <param name="productCode">Product code (e.g., BRENT, WTI)</param>
    /// <param name="contractMonth">Contract month identifier (e.g., OCT25, NOV25)</param>
    /// <param name="startDate">Start date for benchmark period</param>
    /// <param name="endDate">End date for benchmark period</param>
    /// <param name="priceType">Price type: 0=Spot, 1=FuturesSettlement, 2=ForwardCurve</param>
    /// <returns>Historical prices for the contract month within date range</returns>
    [HttpGet("settlement-prices")]
    [ProducesResponseType(typeof(IEnumerable<MarketPriceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSettlementPrices(
        [FromQuery] string productCode,
        [FromQuery] string contractMonth,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int priceType = 0)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return BadRequest(new { error = "Product code is required" });

        if (string.IsNullOrWhiteSpace(contractMonth))
            return BadRequest(new { error = "Contract month is required" });

        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        if (start >= end)
            return BadRequest(new { error = "Start date must be before end date" });

        var query = new GetSettlementPricesQuery
        {
            ProductCode = productCode,
            ContractMonth = contractMonth,
            StartDate = start,
            EndDate = end,
            PriceType = (MarketPriceType)priceType
        };

        try
        {
            var result = await _mediator.Send(query);

            if (result == null || !result.Any())
                return NotFound(new { error = "No market prices found for the specified criteria", productCode, contractMonth, startDate = start, endDate = end });

            _logger.LogInformation(
                "Settlement prices retrieved: Product={ProductCode}, ContractMonth={ContractMonth}, Count={Count}, DateRange={StartDate}-{EndDate}",
                productCode, contractMonth, result.Count(), start, end);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement prices for ProductCode={ProductCode}, ContractMonth={ContractMonth}", productCode, contractMonth);
            return BadRequest(new { error = "Error retrieving settlement prices", details = ex.Message });
        }
    }

    /// <summary>
    /// Get price statistics (min, max, average, std dev) for settlement calculations
    /// Used to analyze price volatility and validate pricing calculations
    /// </summary>
    /// <param name="productCode">Product code (e.g., BRENT, WTI)</param>
    /// <param name="contractMonth">Contract month identifier</param>
    /// <param name="priceType">Price type: 0=Spot, 1=FuturesSettlement, 2=ForwardCurve</param>
    /// <returns>Price statistics including min, max, average, standard deviation</returns>
    [HttpGet("statistics/{productCode}/{contractMonth}")]
    [ProducesResponseType(typeof(PriceStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPriceStatistics(
        string productCode,
        string contractMonth,
        [FromQuery] int priceType = 0)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return BadRequest(new { error = "Product code is required" });

        if (string.IsNullOrWhiteSpace(contractMonth))
            return BadRequest(new { error = "Contract month is required" });

        try
        {
            var query = new GetPriceStatisticsQuery
            {
                ProductCode = productCode,
                ContractMonth = contractMonth,
                PriceType = (MarketPriceType)priceType
            };

            var result = await _mediator.Send(query);

            if (result == null)
                return NotFound(new { error = "No price statistics available for the specified criteria" });

            _logger.LogInformation(
                "Price statistics retrieved: Product={ProductCode}, ContractMonth={ContractMonth}, Min={Min}, Max={Max}, Avg={Avg}",
                productCode, contractMonth, result.MinPrice, result.MaxPrice, result.AveragePrice);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price statistics for ProductCode={ProductCode}, ContractMonth={ContractMonth}", productCode, contractMonth);
            return BadRequest(new { error = "Error retrieving price statistics", details = ex.Message });
        }
    }

    /// <summary>
    /// Get latest price for a specific product and contract month
    /// Used for real-time settlement price reference
    /// </summary>
    /// <param name="productCode">Product code (e.g., BRENT, WTI)</param>
    /// <param name="contractMonth">Contract month identifier</param>
    /// <param name="priceType">Price type: 0=Spot, 1=FuturesSettlement, 2=ForwardCurve</param>
    /// <returns>Latest market price for the contract month</returns>
    [HttpGet("latest/{productCode}/{contractMonth}")]
    [ProducesResponseType(typeof(MarketPriceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestContractMonthPrice(
        string productCode,
        string contractMonth,
        [FromQuery] int priceType = 0)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return BadRequest(new { error = "Product code is required" });

        if (string.IsNullOrWhiteSpace(contractMonth))
            return BadRequest(new { error = "Contract month is required" });

        var query = new GetLatestContractMonthPriceQuery
        {
            ProductCode = productCode,
            ContractMonth = contractMonth,
            PriceType = (MarketPriceType)priceType
        };

        try
        {
            var result = await _mediator.Send(query);

            if (result == null)
                return NotFound(new { error = "No price found for the specified product and contract month" });

            _logger.LogInformation(
                "Latest contract month price retrieved: Product={ProductCode}, ContractMonth={ContractMonth}, Price={Price}, Date={Date}",
                productCode, contractMonth, result.Price, result.PriceDate);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest price for ProductCode={ProductCode}, ContractMonth={ContractMonth}", productCode, contractMonth);
            return BadRequest(new { error = "Error retrieving latest price", details = ex.Message });
        }
    }

    /// <summary>
    /// Get available contract months for a specific product and price type
    /// Used to populate contract month dropdowns in UI
    /// </summary>
    /// <param name="productCode">Product code (e.g., BRENT, WTI)</param>
    /// <param name="priceType">Optional: Price type filter (FuturesSettlement, FuturesClose, Index, Spread)</param>
    /// <returns>List of available contract months for the product/price type combination</returns>
    [HttpGet("contract-months/{productCode}")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAvailableContractMonths(
        string productCode,
        [FromQuery] string? priceType = null)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return BadRequest(new { error = "Product code is required" });

        try
        {
            // Query all prices for this product with optional price type filter
            var query = new GetPriceHistoryQuery
            {
                ProductCode = productCode,
                StartDate = DateTime.UtcNow.AddYears(-5), // Get last 5 years of contract months
                EndDate = DateTime.UtcNow,
                PriceType = priceType
            };

            var prices = await _mediator.Send(query);

            // Extract unique contract months, excluding null values
            var contractMonths = prices
                .Where(p => !string.IsNullOrEmpty(p.ContractMonth))
                .Select(p => p.ContractMonth)
                .Distinct()
                .OrderBy(cm => cm) // Sort chronologically (JAN25, FEB25, etc.)
                .ToList();

            _logger.LogInformation(
                "Available contract months retrieved: Product={ProductCode}, PriceType={PriceType}, Count={Count}",
                productCode, priceType ?? "All", contractMonths.Count);

            return Ok(contractMonths);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contract months for ProductCode={ProductCode}, PriceType={PriceType}", productCode, priceType ?? "All");
            return BadRequest(new { error = "Error retrieving contract months", details = ex.Message });
        }
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