using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.TradeGroups;

/// <summary>
/// 获取所有交易组查询 - Get All Trade Groups Query
/// </summary>
public class GetAllTradeGroupsQuery : IRequest<List<TradeGroupDetailsDto>>
{
}