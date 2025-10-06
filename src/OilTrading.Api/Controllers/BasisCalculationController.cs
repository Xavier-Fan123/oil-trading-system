using Microsoft.AspNetCore.Mvc;
using OilTrading.Application.Services;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BasisCalculationController : ControllerBase
{
    private readonly IBasisCalculationService _basisCalculationService;
    private readonly ILogger<BasisCalculationController> _logger;

    public BasisCalculationController(
        IBasisCalculationService basisCalculationService,
        ILogger<BasisCalculationController> logger)
    {
        _basisCalculationService = basisCalculationService;
        _logger = logger;
    }

    /// <summary>
    /// Calculate basis (spread) between spot and futures prices
    /// </summary>
    [HttpGet("calculate")]
    public async Task<ActionResult<decimal>> CalculateBasis(
        [FromQuery] string productType,
        [FromQuery] DateTime valuationDate,
        [FromQuery] string futuresContract)
    {
        try
        {
            var basis = await _basisCalculationService.CalculateBasisAsync(productType, valuationDate, futuresContract);
            return Ok(basis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating basis for {ProductType} vs {FuturesContract} on {Date}",
                productType, futuresContract, valuationDate);
            return BadRequest($"Error calculating basis: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculate basis for multiple futures contracts
    /// </summary>
    [HttpPost("calculate-multiple")]
    public async Task<ActionResult<Dictionary<string, decimal>>> CalculateMultipleBasis(
        [FromBody] MultipleBasisRequest request)
    {
        try
        {
            var results = await _basisCalculationService.CalculateMultipleBasisAsync(
                request.ProductType, 
                request.ValuationDate, 
                request.FuturesContracts);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating multiple basis for {ProductType}", request.ProductType);
            return BadRequest($"Error calculating multiple basis: {ex.Message}");
        }
    }

    /// <summary>
    /// Get basis history for analysis
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<BasisHistoryDto[]>> GetBasisHistory(
        [FromQuery] string productType,
        [FromQuery] string futuresContract,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var history = await _basisCalculationService.GetBasisHistoryAsync(
                productType, futuresContract, startDate, endDate);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting basis history for {ProductType} vs {FuturesContract}",
                productType, futuresContract);
            return BadRequest($"Error getting basis history: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculate basis-adjusted price using futures price + basis
    /// </summary>
    [HttpPost("adjusted-price")]
    public async Task<ActionResult<decimal>> CalculateBasisAdjustedPrice(
        [FromBody] BasisAdjustedPriceRequest request)
    {
        try
        {
            var adjustedPrice = await _basisCalculationService.CalculateBasisAdjustedPriceAsync(
                request.FuturesPrice, 
                request.ProductType, 
                request.ValuationDate, 
                request.FuturesContract);
            return Ok(adjustedPrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating basis-adjusted price for {ProductType}", request.ProductType);
            return BadRequest($"Error calculating basis-adjusted price: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate if basis is within expected range
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<BasisValidationResult>> ValidateBasis(
        [FromBody] BasisValidationRequest request)
    {
        try
        {
            var validation = await _basisCalculationService.ValidateBasisAsync(
                request.ProductType, 
                request.CalculatedBasis, 
                request.ValuationDate);
            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating basis for {ProductType}", request.ProductType);
            return BadRequest($"Error validating basis: {ex.Message}");
        }
    }
}

public class MultipleBasisRequest
{
    public string ProductType { get; set; } = string.Empty;
    public DateTime ValuationDate { get; set; }
    public string[] FuturesContracts { get; set; } = Array.Empty<string>();
}

public class BasisAdjustedPriceRequest
{
    public decimal FuturesPrice { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public DateTime ValuationDate { get; set; }
    public string FuturesContract { get; set; } = string.Empty;
}

public class BasisValidationRequest
{
    public string ProductType { get; set; } = string.Empty;
    public decimal CalculatedBasis { get; set; }
    public DateTime ValuationDate { get; set; }
}