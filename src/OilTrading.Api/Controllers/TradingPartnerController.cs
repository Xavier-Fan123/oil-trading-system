using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Api.Services;
using OilTrading.Application.Commands.TradingPartners;
using OilTrading.Application.Queries.TradingPartners;
using OilTrading.Application.Queries.FinancialReports;
using OilTrading.Application.DTOs;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/trading-partners")]
public class TradingPartnerController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly TradingPartnerExposureService _exposureService;
    private readonly ILogger<TradingPartnerController> _logger;

    public TradingPartnerController(
        IMediator mediator,
        TradingPartnerExposureService exposureService,
        ILogger<TradingPartnerController> logger)
    {
        _mediator = mediator;
        _exposureService = exposureService;
        _logger = logger;
    }

    [HttpGet]
    [ResponseCache(Duration = 240, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(IEnumerable<TradingPartnerListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var query = new GetAllTradingPartnersQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TradingPartnerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTradingPartnerCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpGet("{id}/analysis")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(TradingPartnerAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysis(Guid id)
    {
        var query = new GetTradingPartnerAnalysisQuery(id, true, true, true, 10);
        
        var result = await _mediator.Send(query);
        if (result == null)
        {
            return NotFound($"Trading partner with ID {id} not found");
        }
        
        return Ok(result);
    }

    [HttpGet("{id}/financial-reports")]
    [ResponseCache(Duration = 180, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(IEnumerable<FinancialReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFinancialReports(Guid id, [FromQuery] int? year = null)
    {
        var query = new GetFinancialReportsByTradingPartnerQuery(id, year, true);

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TradingPartnerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetTradingPartnerByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound($"Trading partner with ID {id} not found");
        }

        return Ok(result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TradingPartnerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTradingPartnerCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);

        if (result == null)
        {
            return NotFound($"Trading partner with ID {id} not found");
        }

        _logger.LogInformation("Trading partner updated: {Id}", id);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteTradingPartnerCommand { Id = id };
        await _mediator.Send(command);

        _logger.LogInformation("Trading partner deleted: {Id}", id);
        return NoContent();
    }

    [HttpPost("{id}/block")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Block(Guid id, [FromBody] BlockTradingPartnerCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);

        _logger.LogInformation("Trading partner blocked: {Id}", id);
        return NoContent();
    }

    [HttpPost("{id}/unblock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unblock(Guid id)
    {
        var command = new UnblockTradingPartnerCommand { Id = id };
        await _mediator.Send(command);

        _logger.LogInformation("Trading partner unblocked: {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Get credit exposure and risk level for a specific trading partner
    /// </summary>
    [HttpGet("{id}/exposure")]
    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(TradingPartnerExposureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExposure(Guid id)
    {
        _logger.LogInformation("Getting exposure for trading partner {Id}", id);
        var result = await _exposureService.GetPartnerExposureAsync(id);

        if (result == null)
        {
            return NotFound($"Trading partner with ID {id} not found");
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all trading partners sorted by risk level
    /// </summary>
    [HttpGet("exposure/all")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(List<TradingPartnerExposureDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllExposure(
        [FromQuery] string? sortBy = "riskLevel",
        [FromQuery] bool sortDescending = true,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        _logger.LogInformation("Getting exposure for all trading partners");
        var result = await _exposureService.GetAllPartnersExposureAsync(sortBy, sortDescending, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get trading partners with high or critical risk levels
    /// </summary>
    [HttpGet("exposure/at-risk")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(List<TradingPartnerExposureDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAtRisk([FromQuery] int minimumRiskLevel = 3)
    {
        _logger.LogInformation("Getting at-risk trading partners");

        // Parse the risk level from query parameter
        if (!Enum.IsDefined(typeof(RiskLevel), minimumRiskLevel))
        {
            return BadRequest("Invalid risk level. Valid values: 1=Low, 2=Medium, 3=High, 4=Critical");
        }

        var riskLevel = (RiskLevel)minimumRiskLevel;
        var result = await _exposureService.GetAtRiskPartnersAsync(riskLevel);
        return Ok(result);
    }

    /// <summary>
    /// Get detailed settlement summary for a trading partner (AP and AR breakdown)
    /// </summary>
    [HttpGet("{id}/settlement-details")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(PartnerSettlementSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSettlementDetails(Guid id)
    {
        _logger.LogInformation("Getting settlement details for trading partner {Id}", id);
        var result = await _exposureService.GetPartnerSettlementDetailsAsync(id);

        if (result == null)
        {
            return NotFound($"Trading partner with ID {id} not found");
        }

        return Ok(result);
    }
}