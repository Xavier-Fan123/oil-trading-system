using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.TradeGroups;

/// <summary>
/// 获取交易组风险查询 - Get Trade Group Risk Query
/// </summary>
public class GetTradeGroupRiskQuery : IRequest<TradeGroupRiskDto?>
{
    public Guid TradeGroupId { get; set; }
}