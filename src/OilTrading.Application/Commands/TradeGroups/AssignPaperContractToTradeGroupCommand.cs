using MediatR;

namespace OilTrading.Application.Commands.TradeGroups;

/// <summary>
/// 将纸质合同分配给交易组命令 - Assign Paper Contract to Trade Group Command
/// </summary>
public class AssignPaperContractToTradeGroupCommand : IRequest<bool>
{
    public Guid TradeGroupId { get; set; }
    public Guid PaperContractId { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}