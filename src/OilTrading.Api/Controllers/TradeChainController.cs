using Microsoft.AspNetCore.Mvc;
using OilTrading.Application.Services;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Api.Attributes;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/trade-chains")]
[Produces("application/json")]
public class TradeChainController : ControllerBase
{
    private readonly ITradeChainService _tradeChainService;
    private readonly ILogger<TradeChainController> _logger;

    public TradeChainController(
        ITradeChainService tradeChainService,
        ILogger<TradeChainController> logger)
    {
        _tradeChainService = tradeChainService;
        _logger = logger;
    }

    private string GetCurrentUserName()
    {
        return User?.Identity?.Name ?? HttpContext?.User?.Identity?.Name ?? "System";
    }

    /// <summary>
    /// Creates a new trade chain
    /// </summary>
    /// <param name="request">Trade chain creation details</param>
    /// <returns>The created trade chain</returns>
    [HttpPost]
    [RiskCheck(RiskCheckLevel.Standard)]
    [ProducesResponseType(typeof(TradeChain), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTradeChain([FromBody] CreateTradeChainRequest request)
    {
        request.CreatedBy = GetCurrentUserName();
        var tradeChain = await _tradeChainService.CreateTradeChainAsync(request);
        
        _logger.LogInformation("Trade chain {ChainId} created successfully", tradeChain.ChainId);
        
        return CreatedAtAction(nameof(GetTradeChain), new { chainId = tradeChain.ChainId }, tradeChain);
    }

    /// <summary>
    /// Gets a trade chain by ID
    /// </summary>
    /// <param name="chainId">The trade chain ID</param>
    /// <returns>The trade chain details</returns>
    [HttpGet("{chainId}")]
    [ProducesResponseType(typeof(TradeChain), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTradeChain(string chainId)
    {
        var tradeChain = await _tradeChainService.GetTradeChainAsync(chainId);
        
        if (tradeChain == null)
            return NotFound($"Trade chain {chainId} not found");
        
        return Ok(tradeChain);
    }

    /// <summary>
    /// Links a purchase contract to a trade chain
    /// </summary>
    /// <param name="chainId">The trade chain ID</param>
    /// <param name="request">Purchase contract linking details</param>
    /// <returns>The updated trade chain</returns>
    [HttpPost("{chainId}/link-purchase")]
    [RiskCheck(RiskCheckLevel.Enhanced)]
    [ProducesResponseType(typeof(TradeChain), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LinkPurchaseContract(string chainId, [FromBody] LinkPurchaseContractRequest request)
    {
        request.ChainId = chainId;
        request.LinkedBy = GetCurrentUserName();
        
        try
        {
            var tradeChain = await _tradeChainService.LinkPurchaseContractAsync(request);
            
            _logger.LogInformation("Purchase contract {ContractId} linked to trade chain {ChainId}", 
                request.PurchaseContractId, chainId);
            
            return Ok(tradeChain);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Links a sales contract to a trade chain
    /// </summary>
    /// <param name="chainId">The trade chain ID</param>
    /// <param name="request">Sales contract linking details</param>
    /// <returns>The updated trade chain</returns>
    [HttpPost("{chainId}/link-sales")]
    [RiskCheck(RiskCheckLevel.Enhanced)]
    [ProducesResponseType(typeof(TradeChain), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LinkSalesContract(string chainId, [FromBody] LinkSalesContractRequest request)
    {
        request.ChainId = chainId;
        request.LinkedBy = GetCurrentUserName();
        
        try
        {
            var tradeChain = await _tradeChainService.LinkSalesContractAsync(request);
            
            _logger.LogInformation("Sales contract {ContractId} linked to trade chain {ChainId}", 
                request.SalesContractId, chainId);
            
            return Ok(tradeChain);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Adds an operation to a trade chain
    /// </summary>
    /// <param name="chainId">The trade chain ID</param>
    /// <param name="request">Operation details</param>
    /// <returns>The updated trade chain</returns>
    [HttpPost("{chainId}/operations")]
    [RiskCheck(RiskCheckLevel.Basic)]
    [ProducesResponseType(typeof(TradeChain), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddOperation(string chainId, [FromBody] AddOperationRequest request)
    {
        request.ChainId = chainId;
        request.PerformedBy = GetCurrentUserName();
        
        try
        {
            var tradeChain = await _tradeChainService.AddOperationAsync(request);
            
            _logger.LogInformation("Operation {OperationType} added to trade chain {ChainId}", 
                request.OperationType, chainId);
            
            return Ok(tradeChain);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Updates delivery actuals for a trade chain
    /// </summary>
    /// <param name="chainId">The trade chain ID</param>
    /// <param name="request">Delivery actuals details</param>
    /// <returns>The updated trade chain</returns>
    [HttpPut("{chainId}/delivery-actuals")]
    [RiskCheck(RiskCheckLevel.Standard)]
    [ProducesResponseType(typeof(TradeChain), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDeliveryActuals(string chainId, [FromBody] UpdateDeliveryActualsRequest request)
    {
        request.ChainId = chainId;
        request.UpdatedBy = GetCurrentUserName();
        
        try
        {
            var tradeChain = await _tradeChainService.UpdateDeliveryActualsAsync(request);
            
            _logger.LogInformation("Delivery actuals updated for trade chain {ChainId}", chainId);
            
            return Ok(tradeChain);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Completes a trade chain
    /// </summary>
    /// <param name="chainId">The trade chain ID</param>
    /// <param name="request">Completion details</param>
    /// <returns>The completed trade chain</returns>
    [HttpPost("{chainId}/complete")]
    [RiskCheck(RiskCheckLevel.Standard)]
    [ProducesResponseType(typeof(TradeChain), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteTradeChain(string chainId, [FromBody] CompleteTradeChainRequest request)
    {
        request.ChainId = chainId;
        request.CompletedBy = GetCurrentUserName();
        
        try
        {
            var tradeChain = await _tradeChainService.CompleteTradeChainAsync(request);
            
            _logger.LogInformation("Trade chain {ChainId} completed", chainId);
            
            return Ok(tradeChain);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Cancels a trade chain
    /// </summary>
    /// <param name="chainId">The trade chain ID</param>
    /// <param name="request">Cancellation details</param>
    /// <returns>The cancelled trade chain</returns>
    [HttpPost("{chainId}/cancel")]
    [RiskCheck(RiskCheckLevel.Enhanced)]
    [ProducesResponseType(typeof(TradeChain), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelTradeChain(string chainId, [FromBody] CancelTradeChainRequest request)
    {
        request.ChainId = chainId;
        request.CancelledBy = GetCurrentUserName();
        
        try
        {
            var tradeChain = await _tradeChainService.CancelTradeChainAsync(request);
            
            _logger.LogInformation("Trade chain {ChainId} cancelled", chainId);
            
            return Ok(tradeChain);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Searches trade chains with various criteria
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <returns>Paginated list of trade chain summaries</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTradeChains([FromQuery] TradeChainSearchCriteria criteria)
    {
        var (items, totalCount) = await _tradeChainService.SearchTradeChainsAsync(criteria);
        
        var result = new
        {
            Items = items,
            TotalCount = totalCount,
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize)
        };
        
        return Ok(result);
    }

    /// <summary>
    /// Gets trade chains by contract
    /// </summary>
    /// <param name="contractId">The contract ID</param>
    /// <param name="isPurchase">Whether it's a purchase contract</param>
    /// <returns>List of trade chains</returns>
    [HttpGet("by-contract/{contractId}")]
    [ProducesResponseType(typeof(List<TradeChain>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTradeChainsByContract(Guid contractId, [FromQuery] bool isPurchase = true)
    {
        var tradeChains = await _tradeChainService.GetTradeChainsByContractAsync(contractId, isPurchase);
        return Ok(tradeChains);
    }

    /// <summary>
    /// Gets active trade chains
    /// </summary>
    /// <returns>List of active trade chains</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<TradeChain>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveTradeChains()
    {
        var tradeChains = await _tradeChainService.GetActiveTradeChainsAsync();
        return Ok(tradeChains);
    }

    /// <summary>
    /// Gets trade chains requiring attention
    /// </summary>
    /// <returns>List of trade chains requiring attention</returns>
    [HttpGet("requiring-attention")]
    [ProducesResponseType(typeof(List<TradeChain>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTradeChainsRequiringAttention()
    {
        var tradeChains = await _tradeChainService.GetTradeChainsRequiringAttentionAsync();
        return Ok(tradeChains);
    }

    /// <summary>
    /// Gets trade chain analytics
    /// </summary>
    /// <param name="startDate">Start date for analysis</param>
    /// <param name="endDate">End date for analysis</param>
    /// <returns>Trade chain analytics</returns>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(TradeChainAnalytics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalytics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-12);
        var end = endDate ?? DateTime.UtcNow;
        
        var analytics = await _tradeChainService.GetAnalyticsAsync(start, end);
        return Ok(analytics);
    }

    /// <summary>
    /// Generates a performance report
    /// </summary>
    /// <param name="startDate">Start date for report</param>
    /// <param name="endDate">End date for report</param>
    /// <returns>Performance report</returns>
    [HttpGet("performance-report")]
    [ProducesResponseType(typeof(TradeChainPerformanceReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> GeneratePerformanceReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-12);
        var end = endDate ?? DateTime.UtcNow;
        
        var report = await _tradeChainService.GeneratePerformanceReportAsync(start, end);
        return Ok(report);
    }

    /// <summary>
    /// Gets trade chain visualization data
    /// </summary>
    /// <param name="chainId">The trade chain ID</param>
    /// <returns>Visualization data</returns>
    [HttpGet("{chainId}/visualization")]
    [ProducesResponseType(typeof(TradeChainVisualization), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTradeChainVisualization(string chainId)
    {
        try
        {
            var visualization = await _tradeChainService.GetTradeChainVisualizationAsync(chainId);
            return Ok(visualization);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Gets trade chain summary
    /// </summary>
    /// <param name="chainId">The trade chain ID</param>
    /// <returns>Trade chain summary</returns>
    [HttpGet("{chainId}/summary")]
    [ProducesResponseType(typeof(TradeChainSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTradeChainSummary(string chainId)
    {
        var tradeChain = await _tradeChainService.GetTradeChainAsync(chainId);
        
        if (tradeChain == null)
            return NotFound($"Trade chain {chainId} not found");
        
        var summary = tradeChain.GetSummary();
        return Ok(summary);
    }

    /// <summary>
    /// Gets trade chain performance metrics
    /// </summary>
    /// <param name="chainId">The trade chain ID</param>
    /// <returns>Performance metrics</returns>
    [HttpGet("{chainId}/performance")]
    [ProducesResponseType(typeof(TradeChainPerformanceMetrics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTradeChainPerformance(string chainId)
    {
        var tradeChain = await _tradeChainService.GetTradeChainAsync(chainId);
        
        if (tradeChain == null)
            return NotFound($"Trade chain {chainId} not found");
        
        var metrics = tradeChain.CalculatePerformanceMetrics();
        return Ok(metrics);
    }

    /// <summary>
    /// Gets operations for a trade chain
    /// </summary>
    /// <param name="chainId">The trade chain ID</param>
    /// <param name="operationType">Optional operation type filter</param>
    /// <returns>List of operations</returns>
    [HttpGet("{chainId}/operations")]
    [ProducesResponseType(typeof(List<TradeChainOperation>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTradeChainOperations(string chainId, [FromQuery] TradeChainOperationType? operationType)
    {
        var tradeChain = await _tradeChainService.GetTradeChainAsync(chainId);
        
        if (tradeChain == null)
            return NotFound($"Trade chain {chainId} not found");
        
        var operations = operationType.HasValue 
            ? tradeChain.GetOperationsByType(operationType.Value)
            : tradeChain.Operations;
        
        return Ok(operations);
    }

    /// <summary>
    /// Gets events for a trade chain
    /// </summary>
    /// <param name="chainId">The trade chain ID</param>
    /// <param name="eventType">Optional event type filter</param>
    /// <returns>List of events</returns>
    [HttpGet("{chainId}/events")]
    [ProducesResponseType(typeof(List<TradeChainEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTradeChainEvents(string chainId, [FromQuery] TradeChainEventType? eventType)
    {
        var tradeChain = await _tradeChainService.GetTradeChainAsync(chainId);
        
        if (tradeChain == null)
            return NotFound($"Trade chain {chainId} not found");
        
        var events = eventType.HasValue 
            ? tradeChain.GetEventsByType(eventType.Value)
            : tradeChain.Events;
        
        return Ok(events.OrderByDescending(e => e.PerformedAt));
    }
}