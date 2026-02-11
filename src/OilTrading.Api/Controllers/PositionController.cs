using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.Positions;
using OilTrading.Application.Services;
using OilTrading.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PositionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly INetPositionService _netPositionService;
    private readonly ILogger<PositionController> _logger;

    public PositionController(
        IMediator mediator,
        INetPositionService netPositionService,
        ILogger<PositionController> logger)
    {
        _mediator = mediator;
        _netPositionService = netPositionService;
        _logger = logger;
    }

    /// <summary>
    /// Get current net positions across all products and months
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    public async Task<ActionResult<IEnumerable<object>>> GetCurrentPositions(CancellationToken cancellationToken)
    {
        try
        {
            var positions = await _netPositionService.CalculateRealTimePositionsAsync(cancellationToken);

            // Transform legacy NetPositionDto to frontend-expected format
            var transformedPositions = positions.Select(p => new
            {
                id = $"{p.ProductType}-{p.Month}",
                productType = GetProductTypeEnum(p.ProductType),
                deliveryMonth = p.Month,
                contractMonth = p.ContractMonth,
                netQuantity = p.ContractNetPosition != 0 ? p.ContractNetPosition : p.TotalNetPosition,
                longQuantity = p.PurchaseContractQuantity > 0 ? p.PurchaseContractQuantity : p.PhysicalPurchases,
                shortQuantity = p.SalesContractQuantity > 0 ? p.SalesContractQuantity : p.PhysicalSales,
                unit = "MT",
                averagePrice = p.MarketPrice > 0 ? p.MarketPrice : GetEstimatedPrice(p.ProductType),
                currentPrice = p.MarketPrice > 0 ? p.MarketPrice : GetEstimatedPrice(p.ProductType),
                unrealizedPnL = 0m,
                realizedPnL = 0m,
                totalPnL = 0m,
                positionValue = Math.Abs(p.TotalNetPosition) * (p.MarketPrice > 0 ? p.MarketPrice : GetEstimatedPrice(p.ProductType)),
                positionType = GetPositionType(p.TotalNetPosition),
                currency = "USD",
                lastUpdated = DateTime.UtcNow.ToString("O"),
                riskMetrics = (object?)null,
                // Position optimization: settlement-adjusted and hedge-aware fields
                settledQuantity = p.SettledPurchaseQuantity + p.SettledSalesQuantity,
                matchedQuantity = p.MatchedQuantity,
                adjustedNetExposure = p.AdjustedNetExposure,
                exposureValue = p.ExposureValue
            }).ToList();

            return Ok(transformedPositions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current positions");
            return StatusCode(500, "Internal server error while retrieving positions");
        }
    }

    /// <summary>
    /// Map product type string to enum value
    /// </summary>
    private int GetProductTypeEnum(string productType)
    {
        return productType?.ToLower() switch
        {
            "brent" => 0,
            "wti" => 1,
            "dubai" => 2,
            "mgo" => 3,
            "gasoil" => 4,
            "gasoline" => 5,
            "jetfuel" => 6,
            "naphtha" => 7,
            _ => 0
        };
    }

    /// <summary>
    /// Determine position type from net quantity
    /// </summary>
    private int GetPositionType(decimal netQuantity)
    {
        if (Math.Abs(netQuantity) < 100)
            return 2; // Flat
        return netQuantity > 0 ? 0 : 1; // Long : Short
    }

    /// <summary>
    /// Get estimated market price for a product
    /// </summary>
    private decimal GetEstimatedPrice(string productType)
    {
        return productType?.ToLower() switch
        {
            "brent" => 85m,
            "wti" => 80m,
            "dubai" => 82m,
            "mgo" => 750m,
            "gasoil" => 650m,
            "gasoline" => 3.00m,
            "jetfuel" => 2.80m,
            "naphtha" => 600m,
            _ => 100m
        };
    }

    /// <summary>
    /// Get position summary with key metrics
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(PositionSummaryDto), 200)]
    public async Task<ActionResult<PositionSummaryDto>> GetPositionSummary(CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _netPositionService.GetPositionSummaryAsync(cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving position summary");
            return StatusCode(500, "Internal server error while retrieving position summary");
        }
    }

    /// <summary>
    /// Get P&L analysis for active contracts
    /// </summary>
    [HttpGet("pnl")]
    [ProducesResponseType(typeof(IEnumerable<PnLDto>), 200)]
    public async Task<ActionResult<IEnumerable<PnLDto>>> GetPnL(
        [FromQuery] DateTime? asOfDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var pnl = await _netPositionService.CalculatePnLAsync(asOfDate, cancellationToken);
            return Ok(pnl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating P&L");
            return StatusCode(500, "Internal server error while calculating P&L");
        }
    }

    /// <summary>
    /// Get exposure breakdown by product
    /// </summary>
    [HttpGet("exposure/products")]
    [ProducesResponseType(typeof(IEnumerable<ExposureDto>), 200)]
    public async Task<ActionResult<IEnumerable<ExposureDto>>> GetProductExposure(CancellationToken cancellationToken)
    {
        try
        {
            var exposure = await _netPositionService.CalculateExposureByProductAsync(cancellationToken);
            return Ok(exposure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating product exposure");
            return StatusCode(500, "Internal server error while calculating product exposure");
        }
    }

    /// <summary>
    /// Get exposure breakdown by counterparty
    /// </summary>
    [HttpGet("exposure/counterparties")]
    [ProducesResponseType(typeof(IEnumerable<ExposureDto>), 200)]
    public async Task<ActionResult<IEnumerable<ExposureDto>>> GetCounterpartyExposure(CancellationToken cancellationToken)
    {
        try
        {
            var exposure = await _netPositionService.CalculateExposureByCounterpartyAsync(cancellationToken);
            return Ok(exposure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating counterparty exposure");
            return StatusCode(500, "Internal server error while calculating counterparty exposure");
        }
    }

    /// <summary>
    /// Check if all positions are within defined limits
    /// </summary>
    [HttpGet("limits/check")]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task<ActionResult<bool>> CheckPositionLimits(CancellationToken cancellationToken)
    {
        try
        {
            var withinLimits = await _netPositionService.CheckPositionLimitsAsync(cancellationToken);
            return Ok(withinLimits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking position limits");
            return StatusCode(500, "Internal server error while checking position limits");
        }
    }

    /// <summary>
    /// Force recalculation of all positions (useful after data changes)
    /// </summary>
    [HttpPost("recalculate")]
    [ProducesResponseType(typeof(PositionRecalculationResultDto), 200)]
    public async Task<ActionResult<PositionRecalculationResultDto>> RecalculatePositions(
        [FromBody] RecalculatePositionsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new RecalculatePositionsCommand
            {
                ForceRecalculation = request.ForceRecalculation,
                ProductTypes = request.ProductTypes,
                AsOfDate = request.AsOfDate,
                RequestedBy = request.RequestedBy ?? "API"
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during position recalculation");
            return StatusCode(500, "Internal server error during position recalculation");
        }
    }

    /// <summary>
    /// Get comprehensive position snapshot with optional filtering
    /// </summary>
    [HttpPost("snapshot")]
    [ProducesResponseType(typeof(PositionSnapshotDto), 200)]
    public async Task<ActionResult<PositionSnapshotDto>> GetPositionSnapshot(
        [FromBody] GetPositionSnapshotRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GetPositionSnapshotCommand
            {
                AsOfDate = request.AsOfDate,
                ProductTypes = request.ProductTypes,
                Counterparties = request.Counterparties,
                IncludeBreakdown = request.IncludeBreakdown,
                IncludePnL = request.IncludePnL,
                IncludeExposure = request.IncludeExposure
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating position snapshot");
            return StatusCode(500, "Internal server error while generating position snapshot");
        }
    }

    /// <summary>
    /// Generate detailed P&L report with grouping and filtering options
    /// </summary>
    [HttpPost("pnl/report")]
    [ProducesResponseType(typeof(PnLReportDto), 200)]
    public async Task<ActionResult<PnLReportDto>> GetPnLReport(
        [FromBody] GetPnLReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GetPnLReportCommand
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                AsOfDate = request.AsOfDate,
                ProductTypes = request.ProductTypes,
                Counterparties = request.Counterparties,
                ContractTypes = request.ContractTypes,
                GroupBy = request.GroupBy,
                IncludeRealized = request.IncludeRealized,
                IncludeUnrealized = request.IncludeUnrealized,
                IncludeDetailBreakdown = request.IncludeDetailBreakdown
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating P&L report");
            return StatusCode(500, "Internal server error while generating P&L report");
        }
    }

    /// <summary>
    /// Get position data for a specific product type
    /// </summary>
    [HttpGet("product/{productType}")]
    [ProducesResponseType(typeof(IEnumerable<NetPositionDto>), 200)]
    public async Task<ActionResult<IEnumerable<NetPositionDto>>> GetPositionsByProduct(
        [Required] string productType,
        CancellationToken cancellationToken)
    {
        try
        {
            var positions = await _netPositionService.CalculateRealTimePositionsAsync(cancellationToken);
            var filteredPositions = positions.Where(p => 
                p.ProductType.Equals(productType, StringComparison.OrdinalIgnoreCase));
            
            return Ok(filteredPositions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving positions for product {ProductType}", productType);
            return StatusCode(500, $"Internal server error while retrieving positions for product {productType}");
        }
    }
}

// Request DTOs for API endpoints
public class RecalculatePositionsRequest
{
    public bool ForceRecalculation { get; set; } = false;
    public string[]? ProductTypes { get; set; }
    public DateTime? AsOfDate { get; set; }
    public string? RequestedBy { get; set; }
}

public class GetPositionSnapshotRequest
{
    public DateTime? AsOfDate { get; set; }
    public string[]? ProductTypes { get; set; }
    public string[]? Counterparties { get; set; }
    public bool IncludeBreakdown { get; set; } = true;
    public bool IncludePnL { get; set; } = true;
    public bool IncludeExposure { get; set; } = true;
}

public class GetPnLReportRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? AsOfDate { get; set; }
    public string[]? ProductTypes { get; set; }
    public string[]? Counterparties { get; set; }
    public string[]? ContractTypes { get; set; }
    public PnLReportGroupBy GroupBy { get; set; } = PnLReportGroupBy.Product;
    public bool IncludeRealized { get; set; } = true;
    public bool IncludeUnrealized { get; set; } = true;
    public bool IncludeDetailBreakdown { get; set; } = false;
}