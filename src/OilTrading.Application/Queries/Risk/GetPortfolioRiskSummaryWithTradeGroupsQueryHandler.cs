using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.Risk;

/// <summary>
/// 基于交易组的投资组合风险计算查询处理器
/// Portfolio Risk Calculation Query Handler with Trade Groups
/// </summary>
public class GetPortfolioRiskSummaryWithTradeGroupsQueryHandler : IRequestHandler<GetPortfolioRiskSummaryWithTradeGroupsQuery, PortfolioRiskWithTradeGroupsDto>
{
    private readonly IPaperContractRepository _paperContractRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly ITradeGroupRepository _tradeGroupRepository;
    private readonly ITradeGroupRiskCalculationService _tradeGroupRiskService;
    private readonly IRiskCalculationService _riskService;
    private readonly ILogger<GetPortfolioRiskSummaryWithTradeGroupsQueryHandler> _logger;

    public GetPortfolioRiskSummaryWithTradeGroupsQueryHandler(
        IPaperContractRepository paperContractRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        ITradeGroupRepository tradeGroupRepository,
        ITradeGroupRiskCalculationService tradeGroupRiskService,
        IRiskCalculationService riskService,
        ILogger<GetPortfolioRiskSummaryWithTradeGroupsQueryHandler> logger)
    {
        _paperContractRepository = paperContractRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _tradeGroupRepository = tradeGroupRepository;
        _tradeGroupRiskService = tradeGroupRiskService;
        _riskService = riskService;
        _logger = logger;
    }

    public async Task<PortfolioRiskWithTradeGroupsDto> Handle(GetPortfolioRiskSummaryWithTradeGroupsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calculating portfolio risk with trade groups consideration");

        var today = DateTime.UtcNow.Date;

        // 1. Get all open positions
        var openPaperContracts = (await _paperContractRepository.GetOpenPositionsAsync(cancellationToken)).ToList();
        var activePurchaseContracts = (await _purchaseContractRepository.GetActiveContractsAsync(cancellationToken)).ToList();
        var activeSalesContracts = (await _salesContractRepository.GetActiveContractsAsync(cancellationToken)).ToList();

        // 2. Separate positions into trade groups and standalone positions
        var standalonePositions = new StandalonePositions
        {
            PaperContracts = openPaperContracts.Where(p => p.TradeGroupId == null).ToList(),
            PurchaseContracts = activePurchaseContracts.Where(p => p.TradeGroupId == null).ToList(),
            SalesContracts = activeSalesContracts.Where(p => p.TradeGroupId == null).ToList()
        };

        var groupedPositions = new GroupedPositions
        {
            PaperContracts = openPaperContracts.Where(p => p.TradeGroupId != null).ToList(),
            PurchaseContracts = activePurchaseContracts.Where(p => p.TradeGroupId != null).ToList(),
            SalesContracts = activeSalesContracts.Where(p => p.TradeGroupId != null).ToList()
        };

        // 3. Calculate risk for standalone positions
        var standaloneRisk = await CalculateStandaloneRiskAsync(standalonePositions, cancellationToken);

        // 4. Calculate risk for trade groups
        var tradeGroupRisks = await CalculateTradeGroupRisksAsync(groupedPositions, cancellationToken);

        // 5. Aggregate total portfolio risk
        var totalPortfolioRisk = AggregatePortfolioRisk(standaloneRisk, tradeGroupRisks);

        return new PortfolioRiskWithTradeGroupsDto
        {
            AsOfDate = today,
            StandaloneRisk = standaloneRisk,
            TradeGroupRisks = tradeGroupRisks,
            TotalPortfolioRisk = totalPortfolioRisk,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private async Task<StandaloneRiskDto> CalculateStandaloneRiskAsync(StandalonePositions positions, CancellationToken cancellationToken)
    {
        if (!positions.HasPositions())
        {
            return new StandaloneRiskDto
            {
                TotalPositions = 0,
                NetExposure = 0,
                GrossExposure = 0,
                VaR95 = 0,
                VaR99 = 0
            };
        }

        // Calculate exposure for standalone paper contracts (original logic)
        var longExposure = positions.PaperContracts
            .Where(p => p.Position == OilTrading.Core.Entities.PositionType.Long)
            .Sum(p => p.Quantity * p.LotSize * (p.CurrentPrice ?? p.EntryPrice));

        var shortExposure = positions.PaperContracts
            .Where(p => p.Position == OilTrading.Core.Entities.PositionType.Short)
            .Sum(p => p.Quantity * p.LotSize * (p.CurrentPrice ?? p.EntryPrice));

        // Add physical contract values
        var purchaseExposure = positions.PurchaseContracts
            .Where(p => p.ContractValue != null)
            .Sum(p => p.ContractValue!.Amount);

        var salesExposure = positions.SalesContracts
            .Where(p => p.ContractValue != null)
            .Sum(p => p.ContractValue!.Amount);

        var netExposure = longExposure - shortExposure + purchaseExposure - salesExposure;
        var grossExposure = longExposure + shortExposure + purchaseExposure + salesExposure;

        // Calculate VaR using traditional method for standalone positions
        var productTypes = positions.PaperContracts.Select(p => p.ProductType).Distinct().ToList();
        var historicalReturns = await _riskService.GetHistoricalReturnsAsync(productTypes, DateTime.UtcNow.Date, 252);
        var portfolioVolatility = _riskService.CalculatePortfolioVolatility(positions.PaperContracts, historicalReturns);

        var vaR95 = Math.Abs(netExposure) * portfolioVolatility * 1.645m / (decimal)Math.Sqrt(252);
        var vaR99 = Math.Abs(netExposure) * portfolioVolatility * 2.326m / (decimal)Math.Sqrt(252);

        return new StandaloneRiskDto
        {
            TotalPositions = positions.GetTotalPositionCount(),
            NetExposure = Math.Round(netExposure),
            GrossExposure = Math.Round(grossExposure),
            VaR95 = Math.Round(vaR95),
            VaR99 = Math.Round(vaR99),
            DailyVolatility = Math.Round(portfolioVolatility, 6)
        };
    }

    private async Task<List<TradeGroupRiskDto>> CalculateTradeGroupRisksAsync(GroupedPositions positions, CancellationToken cancellationToken)
    {
        var tradeGroupRisks = new List<TradeGroupRiskDto>();

        // Get all trade groups with positions
        var tradeGroupIds = positions.GetUniqueTradeGroupIds();
        var tradeGroups = new List<TradeGroup>();

        foreach (var groupId in tradeGroupIds)
        {
            var tradeGroup = await _tradeGroupRepository.GetWithContractsAsync(groupId, cancellationToken);
            if (tradeGroup != null)
            {
                tradeGroups.Add(tradeGroup);
            }
        }

        foreach (var tradeGroup in tradeGroups)
        {
            try
            {
                var riskMetrics = await _tradeGroupRiskService.CalculateTradeGroupRiskAsync(tradeGroup, cancellationToken);

                var tradeGroupRisk = new TradeGroupRiskDto
                {
                    TradeGroupId = tradeGroup.Id,
                    GroupName = tradeGroup.GroupName,
                    StrategyType = tradeGroup.StrategyType.ToString(),
                    NetExposure = Math.Round(riskMetrics.NetExposure),
                    GrossExposure = Math.Round(riskMetrics.GrossExposure),
                    VaR95 = Math.Round(riskMetrics.VaR95),
                    VaR99 = Math.Round(riskMetrics.VaR99),
                    NetPnL = Math.Round(riskMetrics.NetPnL),
                    ContractCount = tradeGroup.PaperContracts.Count + 
                                   tradeGroup.PurchaseContracts.Count + 
                                   tradeGroup.SalesContracts.Count,
                    PortfolioVolatility = Math.Round(riskMetrics.PortfolioVolatility, 6),
                    CorrelationBenefit = CalculateCorrelationBenefit(riskMetrics)
                };

                tradeGroupRisks.Add(tradeGroupRisk);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating risk for trade group {GroupId}", tradeGroup.Id);
                
                // Add a fallback risk calculation
                tradeGroupRisks.Add(new TradeGroupRiskDto
                {
                    TradeGroupId = tradeGroup.Id,
                    GroupName = tradeGroup.GroupName,
                    StrategyType = tradeGroup.StrategyType.ToString(),
                    Error = ex.Message
                });
            }
        }

        return tradeGroupRisks;
    }

    private decimal CalculateCorrelationBenefit(TradeGroupRiskMetrics metrics)
    {
        // Correlation benefit = GrossVaR - NetVaR
        // This shows how much VaR is reduced due to correlations in the strategy
        if (metrics.GrossExposure > 0 && metrics.NetExposure > 0)
        {
            var grossVaR = metrics.GrossExposure * metrics.DailyVolatility * 1.645m / (decimal)Math.Sqrt(252);
            var correlationBenefit = grossVaR - metrics.VaR95;
            return Math.Round(correlationBenefit);
        }
        return 0;
    }

    private TotalPortfolioRiskDto AggregatePortfolioRisk(StandaloneRiskDto standaloneRisk, List<TradeGroupRiskDto> tradeGroupRisks)
    {
        var totalVaR95 = standaloneRisk.VaR95 + tradeGroupRisks.Sum(g => g.VaR95);
        var totalVaR99 = standaloneRisk.VaR99 + tradeGroupRisks.Sum(g => g.VaR99);
        var totalNetExposure = standaloneRisk.NetExposure + tradeGroupRisks.Sum(g => g.NetExposure);
        var totalGrossExposure = standaloneRisk.GrossExposure + tradeGroupRisks.Sum(g => g.GrossExposure);

        return new TotalPortfolioRiskDto
        {
            TotalVaR95 = Math.Round(totalVaR95),
            TotalVaR99 = Math.Round(totalVaR99),
            TotalNetExposure = Math.Round(totalNetExposure),
            TotalGrossExposure = Math.Round(totalGrossExposure),
            TotalPositions = standaloneRisk.TotalPositions + tradeGroupRisks.Sum(g => g.ContractCount),
            TradeGroupCount = tradeGroupRisks.Count,
            CorrelationBenefit = tradeGroupRisks.Sum(g => g.CorrelationBenefit ?? 0),
            DiversificationRatio = totalGrossExposure > 0 ? Math.Round(totalNetExposure / totalGrossExposure, 4) : 0
        };
    }
}

// Helper classes for organizing positions
public class StandalonePositions
{
    public List<PaperContract> PaperContracts { get; set; } = new();
    public List<PurchaseContract> PurchaseContracts { get; set; } = new();
    public List<SalesContract> SalesContracts { get; set; } = new();

    public bool HasPositions() => PaperContracts.Any() || PurchaseContracts.Any() || SalesContracts.Any();
    public int GetTotalPositionCount() => PaperContracts.Count + PurchaseContracts.Count + SalesContracts.Count;
}

public class GroupedPositions
{
    public List<PaperContract> PaperContracts { get; set; } = new();
    public List<PurchaseContract> PurchaseContracts { get; set; } = new();
    public List<SalesContract> SalesContracts { get; set; } = new();

    public List<Guid> GetUniqueTradeGroupIds()
    {
        var groupIds = new List<Guid>();
        
        groupIds.AddRange(PaperContracts.Where(p => p.TradeGroupId.HasValue).Select(p => p.TradeGroupId!.Value));
        groupIds.AddRange(PurchaseContracts.Where(p => p.TradeGroupId.HasValue).Select(p => p.TradeGroupId!.Value));
        groupIds.AddRange(SalesContracts.Where(p => p.TradeGroupId.HasValue).Select(p => p.TradeGroupId!.Value));
        
        return groupIds.Distinct().ToList();
    }
}