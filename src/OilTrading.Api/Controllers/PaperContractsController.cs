using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.PaperContracts;
using OilTrading.Application.Queries.PaperContracts;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;
using OilTrading.Core.Enums;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/paper-contracts")]
public partial class PaperContractsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaperContractsController> _logger;
    private readonly IPaperContractRepository _paperContractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PaperContractsController(
        IMediator mediator,
        ILogger<PaperContractsController> logger,
        IPaperContractRepository paperContractRepository,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _logger = logger;
        _paperContractRepository = paperContractRepository;
        _unitOfWork = unitOfWork;
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

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PaperContractListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllContracts()
    {
        var query = new GetPaperContractsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaperContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContract(Guid id)
    {
        var query = new GetPaperContractByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    [HttpGet("open-positions")]
    [ProducesResponseType(typeof(IEnumerable<PaperContractListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpenPositions()
    {
        var query = new GetOpenPositionsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaperContractDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContract([FromBody] CreatePaperContractDto dto)
    {
        var command = new CreatePaperContractCommand
        {
            ContractMonth = dto.ContractMonth,
            ProductType = dto.ProductType,
            Position = dto.Position,
            Quantity = dto.Quantity,
            LotSize = dto.LotSize,
            EntryPrice = dto.EntryPrice,
            TradeDate = dto.TradeDate,
            TradeReference = dto.TradeReference,
            CounterpartyName = dto.CounterpartyName,
            Notes = dto.Notes,
            CreatedBy = GetCurrentUserName()
        };

        var result = await _mediator.Send(command);
        
        _logger.LogInformation(
            "Paper contract created. ID: {ContractId}, Product: {ProductType}, Month: {ContractMonth}",
            result.Id, result.ProductType, result.ContractMonth);
            
        return CreatedAtAction(nameof(GetContract), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(PaperContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClosePosition(Guid id, [FromBody] ClosePositionDto dto)
    {
        var command = new ClosePositionCommand
        {
            ContractId = id,
            ClosingPrice = dto.ClosingPrice,
            CloseDate = dto.CloseDate,
            ClosedBy = GetCurrentUserName()
        };

        var result = await _mediator.Send(command);
        
        if (result == null)
            return NotFound();
            
        _logger.LogInformation(
            "Paper contract closed. ID: {ContractId}, Closing Price: {ClosingPrice}, P&L: {RealizedPnL}",
            id, dto.ClosingPrice, result.RealizedPnL);
            
        return Ok(result);
    }

    [HttpPost("update-mtm")]
    [ProducesResponseType(typeof(IEnumerable<MTMUpdateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMarkToMarket([FromBody] UpdateMTMCommand command)
    {
        command.UpdatedBy = GetCurrentUserName();
        var result = await _mediator.Send(command);
        
        _logger.LogInformation(
            "MTM update completed for {ContractCount} contracts on {MTMDate}",
            result.Count(), command.MTMDate);
            
        return Ok(result);
    }

    [HttpGet("pnl-summary")]
    [ProducesResponseType(typeof(PnLSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPnLSummary([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var query = new GetPnLSummaryQuery
        {
            FromDate = fromDate ?? DateTime.UtcNow.AddMonths(-1),
            ToDate = toDate ?? DateTime.UtcNow
        };
        
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

public class ClosePositionDto
{
    public decimal ClosingPrice { get; set; }
    public DateTime CloseDate { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
// DATA LINEAGE ENHANCEMENT - Hedge Mapping Endpoints (v2.18.0)
// Purpose: API endpoints for designating and managing paper contract hedges
// ═══════════════════════════════════════════════════════════════════════════

public partial class PaperContractsController
{
    /// <summary>
    /// Designate a paper contract as a hedge for a physical contract
    /// </summary>
    [HttpPost("{id:guid}/designate-hedge")]
    [ProducesResponseType(typeof(HedgeDesignationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DesignateHedge(Guid id, [FromBody] DesignateHedgeRequestDto request)
    {
        var paperContract = await _paperContractRepository.GetByIdAsync(id);
        if (paperContract == null)
            return NotFound($"Paper contract with ID {id} not found");

        if (!paperContract.CanBeDesignatedAsHedge())
            return BadRequest("This paper contract cannot be designated as a hedge. It may be closed or already designated.");

        // Parse the hedged contract type
        if (!Enum.TryParse<HedgedContractType>(request.HedgedContractType, true, out var hedgedContractType))
            return BadRequest($"Invalid hedged contract type: {request.HedgedContractType}. Use 'Purchase' or 'Sales'.");

        try
        {
            paperContract.DesignateAsHedge(
                request.HedgedContractId,
                hedgedContractType,
                request.HedgeRatio,
                GetCurrentUserName());

            await _paperContractRepository.UpdateAsync(paperContract);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Paper contract {PaperContractId} designated as hedge for {HedgedContractType} contract {HedgedContractId} with ratio {HedgeRatio}",
                id, hedgedContractType, request.HedgedContractId, request.HedgeRatio);

            return Ok(new HedgeDesignationResultDto
            {
                PaperContractId = id,
                HedgedContractId = paperContract.HedgedContractId,
                HedgedContractType = paperContract.HedgedContractType?.ToString(),
                HedgeRatio = paperContract.HedgeRatio,
                HedgeEffectiveness = paperContract.HedgeEffectiveness,
                HedgeDesignationDate = paperContract.HedgeDesignationDate,
                IsDesignatedHedge = paperContract.IsDesignatedHedge,
                HedgedQuantity = paperContract.GetHedgedQuantity(),
                Message = $"Paper contract successfully designated as hedge for {hedgedContractType} contract"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error designating paper contract {PaperContractId} as hedge", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Remove hedge designation from a paper contract
    /// </summary>
    [HttpPost("{id:guid}/remove-hedge-designation")]
    [ProducesResponseType(typeof(HedgeDesignationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveHedgeDesignation(Guid id, [FromBody] RemoveHedgeDesignationRequestDto request)
    {
        var paperContract = await _paperContractRepository.GetByIdAsync(id);
        if (paperContract == null)
            return NotFound($"Paper contract with ID {id} not found");

        if (!paperContract.IsDesignatedHedge)
            return BadRequest("This paper contract is not designated as a hedge");

        var previousHedgedContractId = paperContract.HedgedContractId;

        try
        {
            paperContract.RemoveHedgeDesignation(request.Reason, GetCurrentUserName());

            await _paperContractRepository.UpdateAsync(paperContract);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Hedge designation removed from paper contract {PaperContractId}. Previous hedged contract: {HedgedContractId}. Reason: {Reason}",
                id, previousHedgedContractId, request.Reason);

            return Ok(new HedgeDesignationResultDto
            {
                PaperContractId = id,
                HedgedContractId = null,
                HedgedContractType = null,
                HedgeRatio = 1.0m,
                HedgeEffectiveness = null,
                HedgeDesignationDate = null,
                IsDesignatedHedge = false,
                HedgedQuantity = 0,
                Message = $"Hedge designation removed. Reason: {request.Reason}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing hedge designation from paper contract {PaperContractId}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update hedge effectiveness for a paper contract
    /// </summary>
    [HttpPut("{id:guid}/hedge-effectiveness")]
    [ProducesResponseType(typeof(HedgeDesignationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateHedgeEffectiveness(Guid id, [FromBody] UpdateHedgeEffectivenessRequestDto request)
    {
        var paperContract = await _paperContractRepository.GetByIdAsync(id);
        if (paperContract == null)
            return NotFound($"Paper contract with ID {id} not found");

        try
        {
            paperContract.UpdateHedgeEffectiveness(request.Effectiveness, GetCurrentUserName());

            await _paperContractRepository.UpdateAsync(paperContract);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Hedge effectiveness updated for paper contract {PaperContractId} to {Effectiveness}%",
                id, request.Effectiveness);

            return Ok(new HedgeDesignationResultDto
            {
                PaperContractId = id,
                HedgedContractId = paperContract.HedgedContractId,
                HedgedContractType = paperContract.HedgedContractType?.ToString(),
                HedgeRatio = paperContract.HedgeRatio,
                HedgeEffectiveness = paperContract.HedgeEffectiveness,
                HedgeDesignationDate = paperContract.HedgeDesignationDate,
                IsDesignatedHedge = paperContract.IsDesignatedHedge,
                HedgedQuantity = paperContract.GetHedgedQuantity(),
                Message = $"Hedge effectiveness updated to {request.Effectiveness}%"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hedge effectiveness for paper contract {PaperContractId}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update hedge ratio for a paper contract
    /// </summary>
    [HttpPut("{id:guid}/hedge-ratio")]
    [ProducesResponseType(typeof(HedgeDesignationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateHedgeRatio(Guid id, [FromBody] UpdateHedgeRatioRequestDto request)
    {
        var paperContract = await _paperContractRepository.GetByIdAsync(id);
        if (paperContract == null)
            return NotFound($"Paper contract with ID {id} not found");

        try
        {
            paperContract.UpdateHedgeRatio(request.HedgeRatio, GetCurrentUserName());

            await _paperContractRepository.UpdateAsync(paperContract);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Hedge ratio updated for paper contract {PaperContractId} to {HedgeRatio}",
                id, request.HedgeRatio);

            return Ok(new HedgeDesignationResultDto
            {
                PaperContractId = id,
                HedgedContractId = paperContract.HedgedContractId,
                HedgedContractType = paperContract.HedgedContractType?.ToString(),
                HedgeRatio = paperContract.HedgeRatio,
                HedgeEffectiveness = paperContract.HedgeEffectiveness,
                HedgeDesignationDate = paperContract.HedgeDesignationDate,
                IsDesignatedHedge = paperContract.IsDesignatedHedge,
                HedgedQuantity = paperContract.GetHedgedQuantity(),
                Message = $"Hedge ratio updated to {request.HedgeRatio}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hedge ratio for paper contract {PaperContractId}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get all paper contracts designated as hedges
    /// </summary>
    [HttpGet("designated-hedges")]
    [ProducesResponseType(typeof(IEnumerable<HedgingPaperContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDesignatedHedges()
    {
        var hedges = await _paperContractRepository.GetDesignatedHedgesAsync();

        var result = hedges.Select(p => new HedgingPaperContractDto
        {
            PaperContractId = p.Id,
            ContractNumber = p.ContractNumber,
            ContractMonth = p.ContractMonth,
            ProductType = p.ProductType,
            Position = p.Position.ToString(),
            Quantity = p.Quantity,
            LotSize = p.LotSize,
            HedgeRatio = p.HedgeRatio,
            HedgedQuantity = p.GetHedgedQuantity(),
            HedgeEffectiveness = p.HedgeEffectiveness,
            HedgeDesignationDate = p.HedgeDesignationDate,
            Status = p.Status.ToString(),
            EntryPrice = p.EntryPrice,
            CurrentPrice = p.CurrentPrice,
            UnrealizedPnL = p.UnrealizedPnL
        });

        return Ok(result);
    }

    /// <summary>
    /// Get paper contracts that hedge a specific physical contract
    /// </summary>
    [HttpGet("by-hedged-contract/{hedgedContractId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<HedgingPaperContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByHedgedContract(Guid hedgedContractId)
    {
        var hedges = await _paperContractRepository.GetByHedgedContractIdAsync(hedgedContractId);

        var result = hedges.Select(p => new HedgingPaperContractDto
        {
            PaperContractId = p.Id,
            ContractNumber = p.ContractNumber,
            ContractMonth = p.ContractMonth,
            ProductType = p.ProductType,
            Position = p.Position.ToString(),
            Quantity = p.Quantity,
            LotSize = p.LotSize,
            HedgeRatio = p.HedgeRatio,
            HedgedQuantity = p.GetHedgedQuantity(),
            HedgeEffectiveness = p.HedgeEffectiveness,
            HedgeDesignationDate = p.HedgeDesignationDate,
            Status = p.Status.ToString(),
            EntryPrice = p.EntryPrice,
            CurrentPrice = p.CurrentPrice,
            UnrealizedPnL = p.UnrealizedPnL
        });

        return Ok(result);
    }

    /// <summary>
    /// Get paper contracts available for hedge designation
    /// </summary>
    [HttpGet("available-for-hedge")]
    [ProducesResponseType(typeof(IEnumerable<PaperContractListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableForHedge()
    {
        var contracts = await _paperContractRepository.GetAvailableForHedgeDesignationAsync();

        var result = contracts.Select(p => new PaperContractListDto
        {
            Id = p.Id,
            ContractMonth = p.ContractMonth,
            ProductType = p.ProductType,
            Position = p.Position.ToString(),
            Quantity = p.Quantity,
            EntryPrice = p.EntryPrice,
            CurrentPrice = p.CurrentPrice,
            UnrealizedPnL = p.UnrealizedPnL,
            Status = p.Status.ToString(),
            TradeDate = p.TradeDate,
            IsDesignatedHedge = false,
            HedgedContractId = null,
            HedgedContractType = null,
            HedgeRatio = 1.0m
        });

        return Ok(result);
    }

    /// <summary>
    /// Get paper contracts with low hedge effectiveness (below threshold)
    /// </summary>
    [HttpGet("low-effectiveness-hedges")]
    [ProducesResponseType(typeof(IEnumerable<HedgingPaperContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowEffectivenessHedges([FromQuery] decimal threshold = 80m)
    {
        var hedges = await _paperContractRepository.GetLowEffectivenessHedgesAsync(threshold);

        var result = hedges.Select(p => new HedgingPaperContractDto
        {
            PaperContractId = p.Id,
            ContractNumber = p.ContractNumber,
            ContractMonth = p.ContractMonth,
            ProductType = p.ProductType,
            Position = p.Position.ToString(),
            Quantity = p.Quantity,
            LotSize = p.LotSize,
            HedgeRatio = p.HedgeRatio,
            HedgedQuantity = p.GetHedgedQuantity(),
            HedgeEffectiveness = p.HedgeEffectiveness,
            HedgeDesignationDate = p.HedgeDesignationDate,
            Status = p.Status.ToString(),
            EntryPrice = p.EntryPrice,
            CurrentPrice = p.CurrentPrice,
            UnrealizedPnL = p.UnrealizedPnL
        });

        return Ok(result);
    }
}