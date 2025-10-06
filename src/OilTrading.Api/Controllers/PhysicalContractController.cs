using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.PhysicalContracts;
using OilTrading.Application.Queries.PhysicalContracts;
using OilTrading.Application.DTOs;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/physical-contracts")]
public class PhysicalContractController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PhysicalContractController> _logger;

    public PhysicalContractController(IMediator mediator, ILogger<PhysicalContractController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PhysicalContractListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var query = new GetAllPhysicalContractsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PhysicalContractDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePhysicalContractCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }
}