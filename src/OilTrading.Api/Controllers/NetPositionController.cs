using Microsoft.AspNetCore.Mvc;
using OilTrading.Application.Services;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/net-positions")]
public class NetPositionController : ControllerBase
{
    private readonly INetPositionService _netPositionService;
    private readonly ILogger<NetPositionController> _logger;

    public NetPositionController(INetPositionService netPositionService, ILogger<NetPositionController> logger)
    {
        _netPositionService = netPositionService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNetPositions()
    {
        var positions = await _netPositionService.CalculateNetPositionsAsync();
        return Ok(positions);
    }

    [HttpGet("monthly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonthlyPositions()
    {
        var positions = await _netPositionService.CalculateMonthlyNetPositionsAsync();
        return Ok(positions);
    }
}