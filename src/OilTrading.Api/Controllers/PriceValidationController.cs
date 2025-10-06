using Microsoft.AspNetCore.Mvc;
using OilTrading.Application.Services;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PriceValidationController : ControllerBase
{
    private readonly IPriceValidationService _priceValidationService;
    private readonly ILogger<PriceValidationController> _logger;

    public PriceValidationController(
        IPriceValidationService priceValidationService,
        ILogger<PriceValidationController> logger)
    {
        _priceValidationService = priceValidationService;
        _logger = logger;
    }

    /// <summary>
    /// Validate a single price point
    /// </summary>
    [HttpPost("validate-price")]
    public async Task<ActionResult<PriceValidationResult>> ValidatePrice(
        [FromBody] ValidatePriceRequest request)
    {
        try
        {
            var result = await _priceValidationService.ValidatePriceAsync(
                request.ProductType, 
                request.Price, 
                request.PriceDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating price for {ProductType}", request.ProductType);
            return BadRequest($"Error validating price: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate multiple prices in a series
    /// </summary>
    [HttpPost("validate-series")]
    public async Task<ActionResult<List<PriceValidationResult>>> ValidatePriceSeries(
        [FromBody] ValidatePriceSeriesRequest request)
    {
        try
        {
            var results = await _priceValidationService.ValidatePriceSeriesAsync(
                request.ProductType, 
                request.Prices);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating price series for {ProductType}", request.ProductType);
            return BadRequest($"Error validating price series: {ex.Message}");
        }
    }

    /// <summary>
    /// Detect price anomalies in historical data
    /// </summary>
    [HttpGet("anomalies")]
    public async Task<ActionResult<List<PriceAnomalyResult>>> DetectAnomalies(
        [FromQuery] string productType,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var anomalies = await _priceValidationService.DetectPriceAnomaliesAsync(
                productType, startDate, endDate);
            return Ok(anomalies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting anomalies for {ProductType}", productType);
            return BadRequest($"Error detecting anomalies: {ex.Message}");
        }
    }

    /// <summary>
    /// Get price volatility metrics
    /// </summary>
    [HttpGet("volatility")]
    public async Task<ActionResult<PriceVolatilityMetrics>> GetVolatilityMetrics(
        [FromQuery] string productType,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var metrics = await _priceValidationService.GetVolatilityMetricsAsync(
                productType, startDate, endDate);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting volatility metrics for {ProductType}", productType);
            return BadRequest($"Error getting volatility metrics: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate price change between two prices
    /// </summary>
    [HttpPost("validate-change")]
    public async Task<ActionResult<PriceChangeValidation>> ValidatePriceChange(
        [FromBody] ValidatePriceChangeRequest request)
    {
        try
        {
            var validation = await _priceValidationService.ValidatePriceChangeAsync(
                request.ProductType, 
                request.OldPrice, 
                request.NewPrice, 
                request.ChangeDate);
            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating price change for {ProductType}", request.ProductType);
            return BadRequest($"Error validating price change: {ex.Message}");
        }
    }

    /// <summary>
    /// Get validation configuration for a product type
    /// </summary>
    [HttpGet("config/{productType}")]
    public async Task<ActionResult<PriceValidationConfig>> GetValidationConfig(string productType)
    {
        try
        {
            var config = await _priceValidationService.GetValidationConfigAsync(productType);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting validation config for {ProductType}", productType);
            return BadRequest($"Error getting validation config: {ex.Message}");
        }
    }

    /// <summary>
    /// Update validation configuration for a product type
    /// </summary>
    [HttpPut("config/{productType}")]
    public async Task<ActionResult> UpdateValidationConfig(
        string productType,
        [FromBody] PriceValidationConfig config)
    {
        try
        {
            await _priceValidationService.UpdateValidationConfigAsync(productType, config);
            return Ok(new { Message = $"Validation configuration updated for {productType}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating validation config for {ProductType}", productType);
            return BadRequest($"Error updating validation config: {ex.Message}");
        }
    }

    /// <summary>
    /// Get price validation summary for dashboard
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<PriceValidationSummary>> GetValidationSummary(
        [FromQuery] string[] productTypes,
        [FromQuery] DateTime? date)
    {
        try
        {
            var summaryDate = date ?? DateTime.Today;
            var summary = new PriceValidationSummary
            {
                Date = summaryDate,
                ProductSummaries = new List<ProductValidationSummary>()
            };

            foreach (var productType in productTypes)
            {
                var anomalies = await _priceValidationService.DetectPriceAnomaliesAsync(
                    productType, summaryDate.AddDays(-7), summaryDate);
                
                var volatilityMetrics = await _priceValidationService.GetVolatilityMetricsAsync(
                    productType, summaryDate.AddDays(-30), summaryDate);

                summary.ProductSummaries.Add(new ProductValidationSummary
                {
                    ProductType = productType,
                    AnomalyCount = anomalies.Count,
                    HighSeverityAnomalies = anomalies.Count(a => a.Severity > 0.7m),
                    CurrentVolatility = volatilityMetrics.AnnualizedVolatility,
                    ValidationStatus = anomalies.Any(a => a.Severity > 0.8m) ? "Critical" : 
                                    anomalies.Any(a => a.Severity > 0.5m) ? "Warning" : "Normal"
                });
            }

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting validation summary");
            return BadRequest($"Error getting validation summary: {ex.Message}");
        }
    }
}

public class ValidatePriceRequest
{
    public string ProductType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime PriceDate { get; set; }
}

public class ValidatePriceSeriesRequest
{
    public string ProductType { get; set; } = string.Empty;
    public Dictionary<DateTime, decimal> Prices { get; set; } = new();
}

public class ValidatePriceChangeRequest
{
    public string ProductType { get; set; } = string.Empty;
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTime ChangeDate { get; set; }
}

public class PriceValidationSummary
{
    public DateTime Date { get; set; }
    public List<ProductValidationSummary> ProductSummaries { get; set; } = new();
}

public class ProductValidationSummary
{
    public string ProductType { get; set; } = string.Empty;
    public int AnomalyCount { get; set; }
    public int HighSeverityAnomalies { get; set; }
    public decimal CurrentVolatility { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
}