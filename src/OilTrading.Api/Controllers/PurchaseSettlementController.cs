using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.Settlements;
using OilTrading.Application.Queries.Settlements;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Api.Controllers;

/// <summary>
/// API controller for managing purchase contract settlements.
/// Handles settlement creation, calculation, approval, and finalization workflow.
/// </summary>
[ApiController]
[Route("api/purchase-settlements")]
[Produces("application/json")]
public class PurchaseSettlementController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PurchaseSettlementController> _logger;

    public PurchaseSettlementController(
        IMediator mediator,
        ILogger<PurchaseSettlementController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets current user name from authentication context
    /// </summary>
    private string GetCurrentUserName()
    {
        try
        {
            return User?.Identity?.Name ?? "System";
        }
        catch
        {
            return "System";
        }
    }

    #region Query Endpoints

    /// <summary>
    /// Gets a purchase settlement by ID
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <returns>Settlement details if found</returns>
    [HttpGet("{settlementId:guid}")]
    [ProducesResponseType(typeof(SettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSettlement(Guid settlementId)
    {
        try
        {
            var query = new GetSettlementByIdQuery
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = true
            };

            var settlement = await _mediator.Send(query);

            if (settlement == null)
            {
                _logger.LogInformation("Purchase settlement not found: {SettlementId}", settlementId);
                return NotFound();
            }

            return Ok(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase settlement: {SettlementId}", settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the settlement" });
        }
    }

    /// <summary>
    /// Gets all settlements for a purchase contract
    /// </summary>
    /// <param name="contractId">Purchase contract ID</param>
    /// <returns>List of settlements for the contract</returns>
    [HttpGet("contract/{contractId:guid}")]
    [ProducesResponseType(typeof(List<SettlementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContractSettlements(Guid contractId)
    {
        try
        {
            var query = new GetContractSettlementsQuery
            {
                ContractId = contractId,
                IsPurchaseSettlement = true
            };

            var settlements = await _mediator.Send(query);

            if (settlements == null || settlements.Count == 0)
            {
                _logger.LogInformation("No settlements found for purchase contract: {ContractId}", contractId);
                return NotFound();
            }

            return Ok(settlements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlements for contract: {ContractId}", contractId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving settlements" });
        }
    }

    #endregion

    #region Command Endpoints

    /// <summary>
    /// Creates a new purchase settlement
    /// </summary>
    /// <param name="request">Settlement creation request</param>
    /// <returns>Created settlement ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePurchaseSettlementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSettlement(
        [FromBody] CreatePurchaseSettlementRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new CreatePurchaseSettlementCommand
            {
                PurchaseContractId = request.PurchaseContractId,
                ExternalContractNumber = request.ExternalContractNumber ?? string.Empty,
                DocumentNumber = request.DocumentNumber ?? string.Empty,
                DocumentType = request.DocumentType,
                DocumentDate = request.DocumentDate,
                CreatedBy = GetCurrentUserName()
            };

            var settlementId = await _mediator.Send(command);

            _logger.LogInformation("Created purchase settlement {SettlementId} for contract {ContractId}",
                settlementId, request.PurchaseContractId);

            return CreatedAtAction(nameof(GetSettlement), new { settlementId },
                new CreatePurchaseSettlementResponse { SettlementId = settlementId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase settlement");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while creating the settlement" });
        }
    }

    /// <summary>
    /// Calculates settlement amounts (benchmark, adjustment, cargo value)
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <param name="request">Calculation request with amounts</param>
    /// <returns>Updated settlement with calculation results</returns>
    [HttpPost("{settlementId:guid}/calculate")]
    [ProducesResponseType(typeof(SettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateSettlement(
        Guid settlementId,
        [FromBody] CalculateSettlementRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new CalculateSettlementCommand
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = true,
                CalculationQuantityMT = request.CalculationQuantityMT,
                CalculationQuantityBBL = request.CalculationQuantityBBL,
                BenchmarkAmount = request.BenchmarkAmount,
                AdjustmentAmount = request.AdjustmentAmount,
                CalculationNote = request.CalculationNote ?? string.Empty,
                UpdatedBy = GetCurrentUserName()
            };

            await _mediator.Send(command);

            _logger.LogInformation("Calculated purchase settlement {SettlementId}", settlementId);

            // Return updated settlement
            var query = new GetSettlementByIdQuery
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = true
            };
            var settlement = await _mediator.Send(query);
            return Ok(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating purchase settlement: {SettlementId}", settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while calculating the settlement" });
        }
    }

    /// <summary>
    /// Approves a settlement for finalization
    /// Transitions from Calculated to Approved status
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <returns>Updated settlement with approved status</returns>
    [HttpPost("{settlementId:guid}/approve")]
    [ProducesResponseType(typeof(SettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveSettlement(Guid settlementId)
    {
        try
        {
            var command = new ApproveSettlementCommand
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = true,
                ApprovedBy = GetCurrentUserName()
            };

            await _mediator.Send(command);

            _logger.LogInformation("Approved purchase settlement {SettlementId}", settlementId);

            // Return updated settlement
            var query = new GetSettlementByIdQuery
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = true
            };
            var settlement = await _mediator.Send(query);
            return Ok(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving purchase settlement: {SettlementId}", settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while approving the settlement" });
        }
    }

    /// <summary>
    /// Finalizes a settlement (locks it for editing)
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <returns>Updated settlement with finalized status</returns>
    [HttpPost("{settlementId:guid}/finalize")]
    [ProducesResponseType(typeof(SettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FinalizeSettlement(Guid settlementId)
    {
        try
        {
            var command = new FinalizeSettlementCommand
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = true,
                FinalizedBy = GetCurrentUserName()
            };

            await _mediator.Send(command);

            _logger.LogInformation("Finalized purchase settlement {SettlementId}", settlementId);

            // Return updated settlement
            var query = new GetSettlementByIdQuery
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = true
            };
            var settlement = await _mediator.Send(query);
            return Ok(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing purchase settlement: {SettlementId}", settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while finalizing the settlement" });
        }
    }

    #endregion
}
