using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediatR;
using OilTrading.Application.Commands.MarketData;
using OilTrading.Application.Queries.MarketData;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/market-data")]
public class MarketDataController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MarketDataController> _logger;
    private readonly IVaRCalculationService _varService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IProductCodeResolverService _productCodeResolver;

    public MarketDataController(
        IMediator mediator,
        ILogger<MarketDataController> logger,
        IVaRCalculationService varService,
        ApplicationDbContext dbContext,
        IProductCodeResolverService productCodeResolver)
    {
        _mediator = mediator;
        _logger = logger;
        _varService = varService;
        _dbContext = dbContext;
        _productCodeResolver = productCodeResolver;
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

        // Default to XGroup if no file type specified (X-Group unified format is the standard)
        if (string.IsNullOrEmpty(fileType))
        {
            fileType = "XGroup";
            _logger.LogInformation("No file type specified, defaulting to XGroup");
        }
        else if (fileType != "XGroup")
        {
            _logger.LogWarning("Invalid file type: {FileType}. Only XGroup format is supported.", fileType);
            return BadRequest(new { error = "Only 'XGroup' file type is supported. Please use the X-Group unified format (7-column Excel file)." });
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
    [ProducesResponseType(typeof(OilTrading.Core.ValueObjects.PriceStatistics), StatusCodes.Status200OK)]
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

    /// <summary>
    /// Calculate VaR metrics for a specific product
    /// </summary>
    /// <param name="productCode">Product code (e.g., "SG380", "MF 0.5")</param>
    /// <param name="days">Lookback period in days (default 252 = 1 year)</param>
    /// <returns>VaR metrics including 1-day and 10-day VaR at 95% and 99% confidence</returns>
    [HttpGet("var-metrics/{productCode}")]
    [ProducesResponseType(typeof(VaRMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVaRMetrics(
        string productCode,
        [FromQuery] int days = 252)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return BadRequest(new { error = "Product code is required" });

        if (days < 30 || days > 1000)
            return BadRequest(new { error = "Days must be between 30 and 1000" });

        try
        {
            _logger.LogInformation("Calculating VaR metrics for {ProductCode} with {Days} day lookback", productCode, days);

            var result = await _varService.CalculateVaRAsync(productCode, days);

            _logger.LogInformation(
                "VaR calculated for {ProductCode}: 1D-95%={Var1Day95:F2}, AnnualVol={AnnualVol:P2}",
                productCode, result.Var1Day95, result.AnnualizedVolatility);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Insufficient data for VaR calculation: {ProductCode}", productCode);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating VaR for {ProductCode}", productCode);
            return StatusCode(500, new { error = "Error calculating VaR metrics", details = ex.Message });
        }
    }

    /// <summary>
    /// Get available price benchmarks from market data for floating pricing contracts.
    /// Returns distinct products with latest prices, grouped by category.
    /// </summary>
    [HttpGet("available-benchmarks")]
    [ResponseCache(Duration = 300)] // 5-minute cache
    [ProducesResponseType(typeof(IEnumerable<AvailableBenchmarkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableBenchmarks()
    {
        try
        {
            // Product display name and category mapping for known product codes
            var productInfo = new Dictionary<string, (string DisplayName, string Category, string Unit)>(StringComparer.OrdinalIgnoreCase)
            {
                // Fuel Oil
                ["SG380"] = ("MOPS FO 380cst FOB Sg", "Fuel Oil", "MT"),
                ["SG180"] = ("MOPS FO 180cst FOB Sg", "Fuel Oil", "MT"),
                ["BUNKER_SPORE"] = ("HSFO 380 Singapore", "Fuel Oil", "MT"),
                ["BUNKER_HK"] = ("HSFO 380 Hong Kong", "Fuel Oil", "MT"),
                ["FUEL_OIL_35_RTDM"] = ("HSFO 3.5% Rotterdam", "Fuel Oil", "MT"),
                // Marine Fuel
                ["MF 0.5"] = ("MOPS Marine Fuel 0.5%", "Marine Fuel", "MT"),
                ["MGO"] = ("Marine Gas Oil", "Marine Fuel", "MT"),
                // Gasoil
                ["GO 10ppm"] = ("Gas Oil 10ppm", "Gasoil", "MT"),
                ["MOPS_GASOIL"] = ("Gasoil 0.1% Singapore", "Gasoil", "MT"),
                ["GASOIL_FUTURES"] = ("ICE Gasoil Futures", "Gasoil", "MT"),
                // Crude Oil
                ["BRENT_CRUDE"] = ("Brent Crude (Platts)", "Crude Oil", "BBL"),
                ["BRENT"] = ("ICE Brent Futures", "Crude Oil", "BBL"),
                ["Brt Fut"] = ("ICE Brent Future", "Crude Oil", "BBL"),
                ["WTI"] = ("WTI NYMEX", "Crude Oil", "BBL"),
                // Gasoline
                ["GASOLINE_92"] = ("Gasoline 92 RON", "Gasoline", "BBL"),
                ["GASOLINE_95"] = ("Gasoline 95 RON", "Gasoline", "BBL"),
                ["GASOLINE_97"] = ("Gasoline 97 RON", "Gasoline", "BBL"),
                // Jet Fuel
                ["JET_FUEL"] = ("Jet Fuel/Kerosene", "Jet Fuel", "BBL"),
            };

            // Query distinct product codes with their latest prices from MarketPrice table
            // Group by ProductCode and PriceType (Spot vs Futures)
            var latestPrices = await _dbContext.MarketPrices
                .GroupBy(mp => new { mp.ProductCode, mp.IsSettlement })
                .Select(g => new
                {
                    ProductCode = g.Key.ProductCode,
                    IsSettlement = g.Key.IsSettlement,
                    LatestPrice = g.OrderByDescending(p => p.PriceDate).Select(p => p.Price).FirstOrDefault(),
                    LatestDate = g.Max(p => p.PriceDate),
                    Currency = g.Select(p => p.Currency).FirstOrDefault() ?? "USD",
                    DataSource = g.Select(p => p.DataSource).FirstOrDefault(),
                })
                .ToListAsync();

            var benchmarks = latestPrices.Select(lp =>
            {
                var priceType = lp.IsSettlement ? "Futures" : "Spot";
                var info = productInfo.GetValueOrDefault(lp.ProductCode);
                var displayName = info.DisplayName ?? _productCodeResolver.GetDisplayName(lp.ProductCode) ?? lp.ProductCode;
                var category = info.Category ?? _productCodeResolver.GetAssetClass(lp.ProductCode) ?? "Other";
                var unit = info.Unit ?? "MT";

                return new AvailableBenchmarkDto
                {
                    ProductCode = lp.ProductCode,
                    DisplayName = displayName,
                    Category = category,
                    PriceType = priceType,
                    Unit = unit,
                    Currency = lp.Currency,
                    LatestPrice = lp.LatestPrice,
                    LatestDate = lp.LatestDate,
                    DataSource = lp.DataSource,
                };
            })
            .OrderBy(b => b.Category)
            .ThenBy(b => b.DisplayName)
            .ThenBy(b => b.PriceType)
            .ToList();

            _logger.LogInformation("Available benchmarks retrieved: {Count} benchmarks from market data", benchmarks.Count);
            return Ok(benchmarks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available benchmarks");
            return StatusCode(500, new { error = "Error retrieving available benchmarks", details = ex.Message });
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