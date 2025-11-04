using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.Settlements;
using OilTrading.Application.Queries.Settlements;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Api.Controllers;

/// <summary>
/// API controller for managing sales contract settlements.
/// Handles settlement creation, calculation, approval, and finalization workflow.
/// </summary>
[ApiController]
[Route("api/sales-settlements")]
[Produces("application/json")]
public class SalesSettlementController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SalesSettlementController> _logger;

    public SalesSettlementController(
        IMediator mediator,
        ILogger<SalesSettlementController> logger)
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
    /// Gets a sales settlement by ID
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
                IsPurchaseSettlement = false
            };

            var settlement = await _mediator.Send(query);

            if (settlement == null)
            {
                _logger.LogInformation("Sales settlement not found: {SettlementId}", settlementId);
                return NotFound();
            }

            return Ok(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales settlement: {SettlementId}", settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the settlement" });
        }
    }

    /// <summary>
    /// Gets all settlements for a sales contract
    /// </summary>
    /// <param name="contractId">Sales contract ID</param>
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
                IsPurchaseSettlement = false
            };

            var settlements = await _mediator.Send(query);

            if (settlements == null || settlements.Count == 0)
            {
                _logger.LogInformation("No settlements found for sales contract: {ContractId}", contractId);
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
    /// Creates a new sales settlement
    /// </summary>
    /// <param name="request">Settlement creation request</param>
    /// <returns>Created settlement ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateSalesSettlementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSettlement(
        [FromBody] CreateSalesSettlementRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new CreateSalesSettlementCommand
            {
                SalesContractId = request.SalesContractId,
                ExternalContractNumber = request.ExternalContractNumber ?? string.Empty,
                DocumentNumber = request.DocumentNumber ?? string.Empty,
                DocumentType = request.DocumentType,
                DocumentDate = request.DocumentDate,
                CreatedBy = GetCurrentUserName()
            };

            var settlementId = await _mediator.Send(command);

            _logger.LogInformation("Created sales settlement {SettlementId} for contract {ContractId}",
                settlementId, request.SalesContractId);

            return CreatedAtAction(nameof(GetSettlement), new { settlementId },
                new CreateSalesSettlementResponse { SettlementId = settlementId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sales settlement");
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
                IsPurchaseSettlement = false,
                CalculationQuantityMT = request.CalculationQuantityMT,
                CalculationQuantityBBL = request.CalculationQuantityBBL,
                BenchmarkAmount = request.BenchmarkAmount,
                AdjustmentAmount = request.AdjustmentAmount,
                CalculationNote = request.CalculationNote ?? string.Empty,
                UpdatedBy = GetCurrentUserName()
            };

            await _mediator.Send(command);

            _logger.LogInformation("Calculated sales settlement {SettlementId}", settlementId);

            // Return updated settlement
            var query = new GetSettlementByIdQuery
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = false
            };
            var settlement = await _mediator.Send(query);
            return Ok(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating sales settlement: {SettlementId}", settlementId);
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
                IsPurchaseSettlement = false,
                ApprovedBy = GetCurrentUserName()
            };

            await _mediator.Send(command);

            _logger.LogInformation("Approved sales settlement {SettlementId}", settlementId);

            // Return updated settlement
            var query = new GetSettlementByIdQuery
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = false
            };
            var settlement = await _mediator.Send(query);
            return Ok(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving sales settlement: {SettlementId}", settlementId);
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
                IsPurchaseSettlement = false,
                FinalizedBy = GetCurrentUserName()
            };

            await _mediator.Send(command);

            _logger.LogInformation("Finalized sales settlement {SettlementId}", settlementId);

            // Return updated settlement
            var query = new GetSettlementByIdQuery
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = false
            };
            var settlement = await _mediator.Send(query);
            return Ok(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing sales settlement: {SettlementId}", settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while finalizing the settlement" });
        }
    }

    #endregion
}
