using Microsoft.AspNetCore.Mvc;
using OilTrading.Application.Services;
using OilTrading.Core.Enums;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Api.Controllers;

/// <summary>
/// Controller for data lineage operations
/// Provides endpoints for Deal Reference ID management, shipping splits, and lineage queries
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DataLineageController : ControllerBase
{
    private readonly IDealReferenceIdService _dealReferenceIdService;
    private readonly IShippingOperationSplitService _shippingOperationSplitService;
    private readonly ILogger<DataLineageController> _logger;

    public DataLineageController(
        IDealReferenceIdService dealReferenceIdService,
        IShippingOperationSplitService shippingOperationSplitService,
        ILogger<DataLineageController> logger)
    {
        _dealReferenceIdService = dealReferenceIdService;
        _shippingOperationSplitService = shippingOperationSplitService;
        _logger = logger;
    }

    // ========================================================================
    // DEAL REFERENCE ID ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Validate a Deal Reference ID format
    /// </summary>
    [HttpGet("deal-reference-id/validate")]
    [ProducesResponseType(typeof(DealIdValidationResponse), StatusCodes.Status200OK)]
    public IActionResult ValidateDealReferenceId([FromQuery] string dealReferenceId)
    {
        var isValid = _dealReferenceIdService.ValidateDealReferenceIdFormat(dealReferenceId);
        var parsed = _dealReferenceIdService.ParseDealReferenceId(dealReferenceId);

        return Ok(new DealIdValidationResponse
        {
            DealReferenceId = dealReferenceId,
            IsValid = isValid,
            Year = parsed?.Year,
            Sequence = parsed?.Sequence
        });
    }

    /// <summary>
    /// Propagate Deal Reference ID from contract to settlement
    /// </summary>
    [HttpPost("deal-reference-id/propagate-to-settlement")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PropagateToSettlement(
        [FromBody] PropagateToSettlementRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dealReferenceIdService.PropagateToSettlementAsync(
                request.ContractId,
                request.IsPurchaseContract,
                request.SettlementId,
                cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to propagate Deal Reference ID to settlement {SettlementId}", request.SettlementId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Propagate Deal Reference ID from contract to shipping operation
    /// </summary>
    [HttpPost("deal-reference-id/propagate-to-shipping")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PropagateToShipping(
        [FromBody] PropagateToShippingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dealReferenceIdService.PropagateToShippingOperationAsync(
                request.ContractId,
                request.IsPurchaseContract,
                request.ShippingOperationId,
                cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to propagate Deal Reference ID to shipping operation {ShippingId}", request.ShippingOperationId);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ========================================================================
    // SHIPPING OPERATION SPLIT ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Check if a shipping operation can be split
    /// </summary>
    [HttpGet("shipping-splits/{shippingOperationId}/can-split")]
    [ProducesResponseType(typeof(CanSplitResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CanSplitShippingOperation(
        Guid shippingOperationId,
        CancellationToken cancellationToken)
    {
        var (canSplit, message) = await _shippingOperationSplitService.CanSplitAsync(
            shippingOperationId, cancellationToken);

        return Ok(new CanSplitResponse
        {
            ShippingOperationId = shippingOperationId,
            CanSplit = canSplit,
            ValidationMessage = message
        });
    }

    /// <summary>
    /// Split a shipping operation into multiple child operations
    /// </summary>
    [HttpPost("shipping-splits/{shippingOperationId}/split")]
    [ProducesResponseType(typeof(SplitShippingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SplitShippingOperation(
        Guid shippingOperationId,
        [FromBody] SplitShippingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var quantities = request.SplitQuantities
                .Select(sq => new Quantity(sq.Value, sq.Unit))
                .ToList();

            var children = await _shippingOperationSplitService.SplitShippingOperationAsync(
                shippingOperationId,
                quantities,
                request.SplitReason,
                request.SplitReasonNotes,
                request.CreatedBy ?? "System",
                cancellationToken);

            return Ok(new SplitShippingResponse
            {
                ParentShippingOperationId = shippingOperationId,
                ChildOperations = children.Select(c => new SplitChildDto
                {
                    Id = c.Id,
                    Sequence = c.SplitSequence,
                    Quantity = c.PlannedQuantity?.Value ?? 0,
                    Unit = c.PlannedQuantity?.Unit ?? QuantityUnit.MT
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to split shipping operation {ShippingId}", shippingOperationId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get children of a split shipping operation
    /// </summary>
    [HttpGet("shipping-splits/{shippingOperationId}/children")]
    [ProducesResponseType(typeof(List<SplitChildDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChildShippingOperations(
        Guid shippingOperationId,
        CancellationToken cancellationToken)
    {
        var children = await _shippingOperationSplitService.GetChildShippingOperationsAsync(
            shippingOperationId, cancellationToken);

        return Ok(children.Select(c => new SplitChildDto
        {
            Id = c.Id,
            Sequence = c.SplitSequence,
            Quantity = c.PlannedQuantity?.Value ?? 0,
            Unit = c.PlannedQuantity?.Unit ?? QuantityUnit.MT
        }).ToList());
    }

    /// <summary>
    /// Get the complete split tree for a shipping operation
    /// </summary>
    [HttpGet("shipping-splits/{shippingOperationId}/tree")]
    [ProducesResponseType(typeof(SplitTreeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSplitTree(
        Guid shippingOperationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var tree = await _shippingOperationSplitService.GetSplitTreeAsync(
                shippingOperationId, cancellationToken);

            return Ok(new SplitTreeResponse
            {
                RootOperationId = tree.RootOperation.Id,
                TotalLeafQuantity = tree.TotalLeafQuantity,
                MaxDepth = tree.MaxDepth,
                Nodes = tree.Nodes.Select(n => new SplitTreeNodeDto
                {
                    OperationId = n.Operation.Id,
                    ParentOperationId = n.Parent?.Operation.Id,
                    Depth = n.Depth,
                    IsLeaf = n.IsLeaf,
                    Quantity = n.Operation.PlannedQuantity?.Value ?? 0,
                    Sequence = n.Operation.SplitSequence
                }).ToList()
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

// ========================================================================
// REQUEST/RESPONSE DTOS
// ========================================================================

public class DealIdValidationResponse
{
    public required string DealReferenceId { get; set; }
    public bool IsValid { get; set; }
    public int? Year { get; set; }
    public int? Sequence { get; set; }
}

public class PropagateToSettlementRequest
{
    public Guid ContractId { get; set; }
    public bool IsPurchaseContract { get; set; }
    public Guid SettlementId { get; set; }
}

public class PropagateToShippingRequest
{
    public Guid ContractId { get; set; }
    public bool IsPurchaseContract { get; set; }
    public Guid ShippingOperationId { get; set; }
}

public class CanSplitResponse
{
    public Guid ShippingOperationId { get; set; }
    public bool CanSplit { get; set; }
    public string? ValidationMessage { get; set; }
}

public class SplitShippingRequest
{
    public required List<QuantityDto> SplitQuantities { get; set; }
    public SplitReason SplitReason { get; set; }
    public string? SplitReasonNotes { get; set; }
    public string? CreatedBy { get; set; }
}

public class QuantityDto
{
    public decimal Value { get; set; }
    public QuantityUnit Unit { get; set; }
}

public class SplitShippingResponse
{
    public Guid ParentShippingOperationId { get; set; }
    public required List<SplitChildDto> ChildOperations { get; set; }
}

public class SplitChildDto
{
    public Guid Id { get; set; }
    public int Sequence { get; set; }
    public decimal Quantity { get; set; }
    public QuantityUnit Unit { get; set; }
}

public class SplitTreeResponse
{
    public Guid RootOperationId { get; set; }
    public decimal TotalLeafQuantity { get; set; }
    public int MaxDepth { get; set; }
    public required List<SplitTreeNodeDto> Nodes { get; set; }
}

public class SplitTreeNodeDto
{
    public Guid OperationId { get; set; }
    public Guid? ParentOperationId { get; set; }
    public int Depth { get; set; }
    public bool IsLeaf { get; set; }
    public decimal Quantity { get; set; }
    public int Sequence { get; set; }
}
