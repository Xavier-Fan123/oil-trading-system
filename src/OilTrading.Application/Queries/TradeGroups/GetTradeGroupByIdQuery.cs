using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.TradeGroups;

/// <summary>
/// 根据ID获取交易组查询 - Get Trade Group By ID Query
/// </summary>
public class GetTradeGroupByIdQuery : IRequest<TradeGroupDto>
{
    /// <summary>
    /// 交易组ID - Trade Group ID
    /// </summary>
    public Guid Id { get; set; }

    public GetTradeGroupByIdQuery(Guid id)
    {
        Id = id;
    }
}