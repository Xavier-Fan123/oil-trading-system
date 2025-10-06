using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.TradeGroups;

/// <summary>
/// 获取交易组详情查询 - Get Trade Group Details Query
/// </summary>
public class GetTradeGroupDetailsQuery : IRequest<TradeGroupDetailsDto?>
{
    public Guid TradeGroupId { get; set; }
}