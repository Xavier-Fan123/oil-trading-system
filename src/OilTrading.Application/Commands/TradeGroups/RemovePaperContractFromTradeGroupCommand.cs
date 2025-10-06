using MediatR;

namespace OilTrading.Application.Commands.TradeGroups;

/// <summary>
/// 从交易组移除纸质合同命令 - Remove Paper Contract from Trade Group Command
/// </summary>
public class RemovePaperContractFromTradeGroupCommand : IRequest<bool>
{
    public Guid PaperContractId { get; set; }
    public string RemovedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
}