using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.TradeGroups;
using OilTrading.Application.Queries.Risk;
using OilTrading.Application.Queries.TradeGroups;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Api.Controllers;

/// <summary>
/// 交易组管理控制器 - Trade Group Management Controller
/// </summary>
[ApiController]
[Route("api/trade-groups")]
[Produces("application/json")]
public class TradeGroupController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TradeGroupController> _logger;

    public TradeGroupController(IMediator mediator, ILogger<TradeGroupController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// 创建新的交易组 - Create a new trade group
    /// </summary>
    /// <param name="dto">交易组创建信息 - Trade group creation info</param>
    /// <returns>创建的交易组ID - Created trade group ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTradeGroup([FromBody] CreateTradeGroupDto dto)
    {
        try
        {
            if (!Enum.TryParse<StrategyType>(dto.StrategyType, out var strategyType))
            {
                return BadRequest($"Invalid strategy type: {dto.StrategyType}");
            }

            RiskLevel? riskLevel = null;
            if (!string.IsNullOrEmpty(dto.ExpectedRiskLevel) && 
                Enum.TryParse<RiskLevel>(dto.ExpectedRiskLevel, out var parsedRiskLevel))
            {
                riskLevel = parsedRiskLevel;
            }

            var command = new CreateTradeGroupCommand
            {
                GroupName = dto.GroupName,
                StrategyType = (int)strategyType,
                Description = dto.Description,
                ExpectedRiskLevel = riskLevel.HasValue ? (int)riskLevel.Value : null,
                MaxAllowedLoss = dto.MaxAllowedLoss,
                TargetProfit = dto.TargetProfit,
                CreatedBy = GetCurrentUserName()
            };

            var tradeGroupId = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetTradeGroup), new { id = tradeGroupId }, tradeGroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating trade group");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 分配合约到交易组 - Assign contract to trade group
    /// </summary>
    /// <param name="tradeGroupId">交易组ID - Trade group ID</param>
    /// <param name="contractId">合约ID - Contract ID</param>
    /// <param name="contractType">合约类型 - Contract type</param>
    /// <returns>分配结果 - Assignment result</returns>
    [HttpPost("{tradeGroupId:guid}/contracts")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignContractToTradeGroup(
        Guid tradeGroupId,
        [FromBody] AssignContractRequest request)
    {
        try
        {
            var command = new AssignContractToTradeGroupCommand
            {
                TradeGroupId = tradeGroupId,
                ContractId = request.ContractId,
                ContractType = request.ContractType,
                UpdatedBy = GetCurrentUserName()
            };

            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning contract {ContractId} to trade group {TradeGroupId}", 
                request.ContractId, tradeGroupId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 获取交易组详情 - Get trade group details
    /// </summary>
    /// <param name="id">交易组ID - Trade group ID</param>
    /// <returns>交易组详情 - Trade group details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TradeGroupDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTradeGroup(Guid id)
    {
        try
        {
            // This would need a corresponding query handler
            var query = new GetTradeGroupDetailsQuery { TradeGroupId = id };
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound($"Trade group with ID {id} not found");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trade group {TradeGroupId}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 获取所有交易组 - Get all trade groups
    /// </summary>
    /// <returns>交易组列表 - Trade groups list</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<TradeGroupDetailsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTradeGroups()
    {
        try
        {
            // This would need a corresponding query handler
            var query = new GetAllTradeGroupsQuery();
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all trade groups");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 获取投资组合风险摘要（包含交易组） - Get portfolio risk summary with trade groups
    /// </summary>
    /// <returns>投资组合风险摘要 - Portfolio risk summary</returns>
    [HttpGet("portfolio-risk")]
    [ProducesResponseType(typeof(PortfolioRiskWithTradeGroupsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPortfolioRiskWithTradeGroups()
    {
        try
        {
            var query = new GetPortfolioRiskSummaryWithTradeGroupsQuery();
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating portfolio risk with trade groups");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 获取特定交易组的风险指标 - Get specific trade group risk metrics
    /// </summary>
    /// <param name="id">交易组ID - Trade group ID</param>
    /// <returns>交易组风险指标 - Trade group risk metrics</returns>
    [HttpGet("{id:guid}/risk")]
    [ProducesResponseType(typeof(TradeGroupRiskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTradeGroupRisk(Guid id)
    {
        try
        {
            // This would need a corresponding query handler
            var query = new GetTradeGroupRiskQuery { TradeGroupId = id };
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound($"Trade group with ID {id} not found");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating risk for trade group {TradeGroupId}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 分配纸质合同到交易组 - Assign paper contract to trade group
    /// </summary>
    /// <param name="tradeGroupId">交易组ID - Trade group ID</param>
    /// <param name="request">分配请求 - Assignment request</param>
    /// <returns>分配结果 - Assignment result</returns>
    [HttpPost("{tradeGroupId:guid}/paper-contracts")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignPaperContractToTradeGroup(
        Guid tradeGroupId,
        [FromBody] AssignPaperContractRequest request)
    {
        try
        {
            var command = new AssignPaperContractToTradeGroupCommand
            {
                TradeGroupId = tradeGroupId,
                PaperContractId = request.PaperContractId,
                AssignedBy = GetCurrentUserName(),
                Notes = request.Notes
            };

            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning paper contract {PaperContractId} to trade group {TradeGroupId}", 
                request.PaperContractId, tradeGroupId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 从交易组移除纸质合同 - Remove paper contract from trade group
    /// </summary>
    /// <param name="paperContractId">纸质合同ID - Paper contract ID</param>
    /// <param name="request">移除请求 - Remove request</param>
    /// <returns>移除结果 - Remove result</returns>
    [HttpDelete("paper-contracts/{paperContractId:guid}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePaperContractFromTradeGroup(
        Guid paperContractId,
        [FromBody] RemovePaperContractRequest request)
    {
        try
        {
            var command = new RemovePaperContractFromTradeGroupCommand
            {
                PaperContractId = paperContractId,
                RemovedBy = GetCurrentUserName(),
                Reason = request.Reason
            };

            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing paper contract {PaperContractId} from trade group", 
                paperContractId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 更新交易组风险参数 - Update trade group risk parameters
    /// </summary>
    /// <param name="tradeGroupId">交易组ID - Trade group ID</param>
    /// <param name="request">更新请求 - Update request</param>
    /// <returns>更新结果 - Update result</returns>
    [HttpPut("{tradeGroupId:guid}/risk-parameters")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTradeGroupRiskParameters(
        Guid tradeGroupId,
        [FromBody] UpdateRiskParametersRequest request)
    {
        try
        {
            var command = new UpdateTradeGroupRiskParametersCommand
            {
                TradeGroupId = tradeGroupId,
                ExpectedRiskLevel = request.ExpectedRiskLevel,
                MaxAllowedLoss = request.MaxAllowedLoss,
                TargetProfit = request.TargetProfit,
                UpdatedBy = GetCurrentUserName(),
                UpdateReason = request.UpdateReason
            };

            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating risk parameters for trade group {TradeGroupId}", 
                tradeGroupId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 关闭交易组 - Close trade group
    /// </summary>
    /// <param name="id">交易组ID - Trade group ID</param>
    /// <returns>关闭结果 - Close result</returns>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseTradeGroup(Guid id)
    {
        try
        {
            // This would need a corresponding command handler
            var command = new CloseTradeGroupCommand 
            { 
                TradeGroupId = id,
                ClosedBy = GetCurrentUserName()
            };
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing trade group {TradeGroupId}", id);
            return BadRequest(ex.Message);
        }
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
}

/// <summary>
/// 分配合约请求 - Assign Contract Request
/// </summary>
public class AssignContractRequest
{
    /// <summary>
    /// 合约ID - Contract ID
    /// </summary>
    public Guid ContractId { get; set; }

    /// <summary>
    /// 合约类型 - Contract type (PaperContract, PurchaseContract, SalesContract)
    /// </summary>
    public string ContractType { get; set; } = string.Empty;
}

/// <summary>
/// 分配纸质合同请求 - Assign Paper Contract Request
/// </summary>
public class AssignPaperContractRequest
{
    /// <summary>
    /// 纸质合同ID - Paper contract ID
    /// </summary>
    public Guid PaperContractId { get; set; }

    /// <summary>
    /// 备注 - Notes
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// 移除纸质合同请求 - Remove Paper Contract Request
/// </summary>
public class RemovePaperContractRequest
{
    /// <summary>
    /// 移除原因 - Removal reason
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// 更新风险参数请求 - Update Risk Parameters Request
/// </summary>
public class UpdateRiskParametersRequest
{
    /// <summary>
    /// 预期风险级别 - Expected risk level
    /// </summary>
    public int? ExpectedRiskLevel { get; set; }

    /// <summary>
    /// 最大允许损失 - Maximum allowed loss
    /// </summary>
    public decimal? MaxAllowedLoss { get; set; }

    /// <summary>
    /// 目标利润 - Target profit
    /// </summary>
    public decimal? TargetProfit { get; set; }

    /// <summary>
    /// 更新原因 - Update reason
    /// </summary>
    public string? UpdateReason { get; set; }
}

