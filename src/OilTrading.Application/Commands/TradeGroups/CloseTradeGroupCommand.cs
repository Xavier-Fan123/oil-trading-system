using MediatR;

namespace OilTrading.Application.Commands.TradeGroups;

/// <summary>
/// 关闭交易组命令 - Close Trade Group Command
/// </summary>
public class CloseTradeGroupCommand : IRequest<bool>
{
    public Guid TradeGroupId { get; set; }
    public string ClosedBy { get; set; } = string.Empty;
}