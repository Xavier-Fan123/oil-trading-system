using MediatR;

namespace OilTrading.Application.Commands.TradeGroups;

/// <summary>
/// 更新交易组风险参数命令 - Update Trade Group Risk Parameters Command
/// </summary>
public class UpdateTradeGroupRiskParametersCommand : IRequest<bool>
{
    public Guid TradeGroupId { get; set; }
    public int? ExpectedRiskLevel { get; set; }
    public decimal? MaxAllowedLoss { get; set; }
    public decimal? TargetProfit { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public string? UpdateReason { get; set; }
}