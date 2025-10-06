using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.PaperContracts;
using OilTrading.Application.Queries.PaperContracts;
using OilTrading.Application.DTOs;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/paper-contracts")]
public class PaperContractsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaperContractsController> _logger;

    public PaperContractsController(IMediator mediator, ILogger<PaperContractsController> logger)
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