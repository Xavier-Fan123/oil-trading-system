using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.TradeGroups;

/// <summary>
/// 获取交易组详情查询处理器 - Get Trade Group Details Query Handler
/// </summary>
public class GetTradeGroupDetailsQueryHandler : IRequestHandler<GetTradeGroupDetailsQuery, TradeGroupDetailsDto?>
{
    private readonly ITradeGroupRepository _tradeGroupRepository;
    private readonly ILogger<GetTradeGroupDetailsQueryHandler> _logger;

    public GetTradeGroupDetailsQueryHandler(
        ITradeGroupRepository tradeGroupRepository,
        ILogger<GetTradeGroupDetailsQueryHandler> logger)
    {
        _tradeGroupRepository = tradeGroupRepository;
        _logger = logger;
    }

    public async Task<TradeGroupDetailsDto?> Handle(GetTradeGroupDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving trade group details for ID: {TradeGroupId}", request.TradeGroupId);

        var tradeGroup = await _tradeGroupRepository.GetWithContractsAsync(request.TradeGroupId, cancellationToken);

        if (tradeGroup == null)
        {
            _logger.LogWarning("Trade group not found with ID: {TradeGroupId}", request.TradeGroupId);
            return null;
        }

        var result = new TradeGroupDetailsDto
        {
            Id = tradeGroup.Id,
            GroupName = tradeGroup.GroupName,
            StrategyType = tradeGroup.StrategyType.ToString(),
            Status = tradeGroup.Status.ToString(),
            Description = tradeGroup.Description,
            ExpectedRiskLevel = tradeGroup.ExpectedRiskLevel?.ToString(),
            MaxAllowedLoss = tradeGroup.MaxAllowedLoss,
            TargetProfit = tradeGroup.TargetProfit,
            ContractCount = tradeGroup.PaperContracts.Count + tradeGroup.PurchaseContracts.Count + tradeGroup.SalesContracts.Count,
            NetPnL = tradeGroup.GetNetPnL(),
            TotalValue = tradeGroup.GetTotalValue(),
            
            // Contract details
            PaperContractCount = tradeGroup.PaperContracts.Count,
            PurchaseContractCount = tradeGroup.PurchaseContracts.Count,
            SalesContractCount = tradeGroup.SalesContracts.Count,
            
            CreatedAt = tradeGroup.CreatedAt,
            CreatedBy = tradeGroup.CreatedBy,
            UpdatedAt = tradeGroup.UpdatedAt,
            UpdatedBy = tradeGroup.UpdatedBy
        };

        _logger.LogInformation("Successfully retrieved trade group details for {GroupName}", tradeGroup.GroupName);

        return result;
    }
}