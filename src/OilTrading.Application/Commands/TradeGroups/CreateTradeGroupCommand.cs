using MediatR;

namespace OilTrading.Application.Commands.TradeGroups;

/// <summary>
/// 创建交易组命令 - Create Trade Group Command
/// </summary>
public class CreateTradeGroupCommand : IRequest<Guid>
{
    /// <summary>
    /// 交易组名称 - Group name
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// 策略类型 - Strategy type
    /// </summary>
    public int StrategyType { get; set; }

    /// <summary>
    /// 描述 - Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 预期风险等级 - Expected risk level
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
    /// 标签ID列表 - Tag IDs to assign
    /// </summary>
    public List<Guid> TagIds { get; set; } = new List<Guid>();

    /// <summary>
    /// 创建人 - Created by
    /// </summary>
    public string CreatedBy { get; set; } = "System";
}