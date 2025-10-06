using MediatR;

namespace OilTrading.Application.Commands.TradeGroups;

/// <summary>
/// 分配合约到交易组命令 - Assign Contract to Trade Group Command
/// </summary>
public class AssignContractToTradeGroupCommand : IRequest<bool>
{
    /// <summary>
    /// 交易组ID - Trade Group ID
    /// </summary>
    public Guid TradeGroupId { get; set; }

    /// <summary>
    /// 合约ID - Contract ID
    /// </summary>
    public Guid ContractId { get; set; }

    /// <summary>
    /// 合约类型 - Contract type (PaperContract, PurchaseContract, SalesContract)
    /// </summary>
    public string ContractType { get; set; } = string.Empty;

    /// <summary>
    /// 更新者 - Updated by
    /// </summary>
    public string UpdatedBy { get; set; } = "System";
}