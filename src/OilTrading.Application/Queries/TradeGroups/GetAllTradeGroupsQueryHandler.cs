using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.TradeGroups;

/// <summary>
/// 获取所有交易组查询处理器 - Get All Trade Groups Query Handler
/// </summary>
public class GetAllTradeGroupsQueryHandler : IRequestHandler<GetAllTradeGroupsQuery, List<TradeGroupDetailsDto>>
{
    private readonly ITradeGroupRepository _tradeGroupRepository;
    private readonly ILogger<GetAllTradeGroupsQueryHandler> _logger;

    public GetAllTradeGroupsQueryHandler(
        ITradeGroupRepository tradeGroupRepository,
        ILogger<GetAllTradeGroupsQueryHandler> logger)
    {
        _tradeGroupRepository = tradeGroupRepository;
        _logger = logger;
    }

    public async Task<List<TradeGroupDetailsDto>> Handle(GetAllTradeGroupsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all trade groups");

        var tradeGroups = await _tradeGroupRepository.GetAllAsync(cancellationToken);

        var result = tradeGroups.Select(tg => new TradeGroupDetailsDto
        {
            Id = tg.Id,
            GroupName = tg.GroupName,
            StrategyType = tg.StrategyType.ToString(),
            Status = tg.Status.ToString(),
            Description = tg.Description,
            ExpectedRiskLevel = tg.ExpectedRiskLevel?.ToString(),
            MaxAllowedLoss = tg.MaxAllowedLoss,
            TargetProfit = tg.TargetProfit,
            ContractCount = tg.PaperContracts.Count + tg.PurchaseContracts.Count + tg.SalesContracts.Count,
            NetPnL = tg.GetNetPnL(),
            TotalValue = tg.GetTotalValue(),
            CreatedAt = tg.CreatedAt,
            CreatedBy = tg.CreatedBy,
            UpdatedAt = tg.UpdatedAt,
            UpdatedBy = tg.UpdatedBy
        }).ToList();

        _logger.LogInformation("Retrieved {Count} trade groups", result.Count);

        return result;
    }
}