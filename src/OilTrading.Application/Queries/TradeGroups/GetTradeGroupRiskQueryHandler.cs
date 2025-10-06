using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.TradeGroups;

/// <summary>
/// 获取交易组风险查询处理器 - Get Trade Group Risk Query Handler
/// </summary>
public class GetTradeGroupRiskQueryHandler : IRequestHandler<GetTradeGroupRiskQuery, TradeGroupRiskDto?>
{
    private readonly ITradeGroupRepository _tradeGroupRepository;
    private readonly ITradeGroupRiskCalculationService _riskCalculationService;
    private readonly ILogger<GetTradeGroupRiskQueryHandler> _logger;

    public GetTradeGroupRiskQueryHandler(
        ITradeGroupRepository tradeGroupRepository,
        ITradeGroupRiskCalculationService riskCalculationService,
        ILogger<GetTradeGroupRiskQueryHandler> logger)
    {
        _tradeGroupRepository = tradeGroupRepository;
        _riskCalculationService = riskCalculationService;
        _logger = logger;
    }

    public async Task<TradeGroupRiskDto?> Handle(GetTradeGroupRiskQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calculating risk for trade group ID: {TradeGroupId}", request.TradeGroupId);

        var tradeGroup = await _tradeGroupRepository.GetWithContractsAsync(request.TradeGroupId, cancellationToken);

        if (tradeGroup == null)
        {
            _logger.LogWarning("Trade group not found with ID: {TradeGroupId}", request.TradeGroupId);
            return null;
        }

        var riskMetrics = await _riskCalculationService.CalculateTradeGroupRiskAsync(tradeGroup, cancellationToken);

        var result = new TradeGroupRiskDto
        {
            TradeGroupId = tradeGroup.Id,
            GroupName = tradeGroup.GroupName,
            StrategyType = tradeGroup.StrategyType.ToString(),
            Status = tradeGroup.Status.ToString(),
            NetExposure = riskMetrics.NetExposure,
            GrossExposure = riskMetrics.GrossExposure,
            VaR95 = riskMetrics.VaR95,
            VaR99 = riskMetrics.VaR99,
            ExpectedShortfall = riskMetrics.ExpectedShortfall,
            PortfolioVolatility = riskMetrics.PortfolioVolatility,
            NetPnL = riskMetrics.NetPnL,
            DailyVolatility = riskMetrics.DailyVolatility,
            CorrelationAdjustment = riskMetrics.CorrelationAdjustment,
            IsSpreadStrategy = tradeGroup.IsSpreadStrategy(),
            IsHedgeStrategy = tradeGroup.IsHedgeStrategy(),
            CalculatedAt = riskMetrics.CalculatedAt
        };

        _logger.LogInformation("Risk calculation completed for trade group {GroupName}. VaR95: {VaR95}", 
            tradeGroup.GroupName, riskMetrics.VaR95);

        return result;
    }
}