using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Services;

/// <summary>
/// 交易组风险计算服务实现 - Trade Group Risk Calculation Service Implementation
/// </summary>
public class TradeGroupRiskCalculationService : ITradeGroupRiskCalculationService
{
    private readonly ILogger<TradeGroupRiskCalculationService> _logger;

    public TradeGroupRiskCalculationService(ILogger<TradeGroupRiskCalculationService> logger)
    {
        _logger = logger;
    }

    public async Task<TradeGroupRiskMetrics> CalculateTradeGroupRiskAsync(TradeGroup tradeGroup, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating risk metrics for trade group {GroupName} ({GroupId})", 
            tradeGroup.GroupName, tradeGroup.Id);

        var metrics = new TradeGroupRiskMetrics
        {
            TradeGroupId = tradeGroup.Id,
            GroupName = tradeGroup.GroupName,
            StrategyType = tradeGroup.StrategyType,
            NetPnL = tradeGroup.GetNetPnL(),
            CalculatedAt = DateTime.UtcNow
        };

        // Calculate based on strategy type
        if (tradeGroup.IsSpreadStrategy())
        {
            metrics = await CalculateSpreadStrategyRisk(tradeGroup, metrics, cancellationToken);
        }
        else if (tradeGroup.IsHedgeStrategy())
        {
            metrics = await CalculateHedgeStrategyRisk(tradeGroup, metrics, cancellationToken);
        }
        else
        {
            // Directional or other strategies - use gross exposure
            metrics = await CalculateDirectionalStrategyRisk(tradeGroup, metrics, cancellationToken);
        }

        _logger.LogInformation("Risk calculation completed for trade group {GroupName}. VaR95: {VaR95}, NetExposure: {NetExposure}", 
            tradeGroup.GroupName, metrics.VaR95, metrics.NetExposure);

        return metrics;
    }

    public async Task<decimal> CalculateSpreadRiskAsync(IEnumerable<PaperContract> spreadContracts, CancellationToken cancellationToken = default)
    {
        // For spread contracts, the risk is primarily the spread risk, not the individual legs
        var contracts = spreadContracts.ToList();
        if (!contracts.Any()) return 0;

        // Calculate net exposure across the spread
        decimal netExposure = 0;
        foreach (var contract in contracts)
        {
            var positionMultiplier = contract.Position == PositionType.Long ? 1 : -1;
            var contractValue = contract.Quantity * contract.LotSize * (contract.CurrentPrice ?? contract.EntryPrice);
            netExposure += contractValue * positionMultiplier;
        }

        // For spreads, risk is typically 5-15% of net exposure (much lower than gross)
        // This reflects the fact that spread positions have natural hedging
        var spreadRiskFactor = 0.10m; // 10% risk factor for spreads
        return Math.Abs(netExposure) * spreadRiskFactor;
    }

    public async Task<decimal> CalculateHedgeRiskAsync(IEnumerable<object> hedgeContracts, CancellationToken cancellationToken = default)
    {
        // For hedge strategies, the risk is even lower than spreads
        // Hedges are designed to offset risk in the underlying portfolio
        var hedgeRiskFactor = 0.05m; // 5% risk factor for hedges
        
        // For now, return a simplified calculation
        // In a real implementation, this would calculate the hedge effectiveness
        return 1000m * hedgeRiskFactor; // Placeholder
    }

    public async Task<decimal> CalculateStandaloneRiskAsync(object contract, CancellationToken cancellationToken = default)
    {
        // For standalone positions, use full gross exposure with standard volatility
        var standardVolatility = 0.25m; // 25% annual volatility assumption
        var dailyVolatility = standardVolatility / (decimal)Math.Sqrt(252); // Convert to daily
        
        decimal contractValue = 0;
        
        if (contract is PaperContract paperContract)
        {
            contractValue = paperContract.GetTotalValue();
        }
        else if (contract is PurchaseContract purchaseContract)
        {
            contractValue = purchaseContract.ContractQuantity.Value * (purchaseContract.ContractValue?.Amount ?? 0);
        }
        else if (contract is SalesContract salesContract)
        {
            contractValue = salesContract.ContractQuantity.Value * (salesContract.ContractValue?.Amount ?? 0);
        }

        // Standard VaR calculation at 95% confidence (1.65 standard deviations)
        return contractValue * dailyVolatility * 1.65m;
    }

    private async Task<TradeGroupRiskMetrics> CalculateSpreadStrategyRisk(TradeGroup tradeGroup, TradeGroupRiskMetrics metrics, CancellationToken cancellationToken)
    {
        // For spread strategies, calculate net exposure and correlation-adjusted risk
        var paperContracts = tradeGroup.PaperContracts.Where(pc => pc.Status == PaperContractStatus.Open).ToList();
        
        decimal netExposure = 0;
        decimal grossExposure = 0;

        foreach (var contract in paperContracts)
        {
            var positionMultiplier = contract.Position == PositionType.Long ? 1 : -1;
            var contractValue = contract.GetTotalValue();
            
            netExposure += contractValue * positionMultiplier;
            grossExposure += contractValue;
        }

        metrics.NetExposure = Math.Abs(netExposure);
        metrics.GrossExposure = grossExposure;

        // For spread strategies, VaR is based on spread volatility (much lower than individual legs)
        var spreadVolatility = 0.08m; // 8% annual volatility for spreads
        var dailyVolatility = spreadVolatility / (decimal)Math.Sqrt(252);
        
        metrics.VaR95 = Math.Abs(netExposure) * dailyVolatility * 1.65m;
        metrics.VaR99 = Math.Abs(netExposure) * dailyVolatility * 2.33m;
        metrics.ExpectedShortfall = metrics.VaR95 * 1.3m; // Approximation
        metrics.PortfolioVolatility = spreadVolatility;
        metrics.DailyVolatility = dailyVolatility;
        
        // Correlation benefit: difference between gross and net exposure
        metrics.CorrelationAdjustment = grossExposure - Math.Abs(netExposure);

        return metrics;
    }

    private async Task<TradeGroupRiskMetrics> CalculateHedgeStrategyRisk(TradeGroup tradeGroup, TradeGroupRiskMetrics metrics, CancellationToken cancellationToken)
    {
        // For hedge strategies, risk is even lower than spreads
        var hedgeVolatility = 0.03m; // 3% annual volatility for hedged positions
        var dailyVolatility = hedgeVolatility / (decimal)Math.Sqrt(252);

        // Calculate net exposure across all contracts in the hedge group
        decimal netExposure = 0;
        decimal grossExposure = 0;

        // Paper contracts
        foreach (var contract in tradeGroup.PaperContracts.Where(pc => pc.Status == PaperContractStatus.Open))
        {
            var positionMultiplier = contract.Position == PositionType.Long ? 1 : -1;
            var contractValue = contract.GetTotalValue();
            
            netExposure += contractValue * positionMultiplier;
            grossExposure += contractValue;
        }

        // Physical contracts (purchase and sales)
        foreach (var contract in tradeGroup.PurchaseContracts.Where(pc => pc.Status == ContractStatus.Active))
        {
            var contractValue = contract.ContractQuantity.Value * (contract.ContractValue?.Amount ?? 0);
            netExposure += contractValue; // Purchases are long positions
            grossExposure += contractValue;
        }

        foreach (var contract in tradeGroup.SalesContracts.Where(sc => sc.Status == ContractStatus.Active))
        {
            var contractValue = contract.ContractQuantity.Value * (contract.ContractValue?.Amount ?? 0);
            netExposure -= contractValue; // Sales are short positions
            grossExposure += contractValue;
        }

        metrics.NetExposure = Math.Abs(netExposure);
        metrics.GrossExposure = grossExposure;
        
        metrics.VaR95 = Math.Abs(netExposure) * dailyVolatility * 1.65m;
        metrics.VaR99 = Math.Abs(netExposure) * dailyVolatility * 2.33m;
        metrics.ExpectedShortfall = metrics.VaR95 * 1.2m;
        metrics.PortfolioVolatility = hedgeVolatility;
        metrics.DailyVolatility = dailyVolatility;
        
        // Significant correlation benefit for hedged positions
        metrics.CorrelationAdjustment = grossExposure - Math.Abs(netExposure);

        return metrics;
    }

    private async Task<TradeGroupRiskMetrics> CalculateDirectionalStrategyRisk(TradeGroup tradeGroup, TradeGroupRiskMetrics metrics, CancellationToken cancellationToken)
    {
        // For directional strategies, use standard gross exposure calculation
        var standardVolatility = 0.25m; // 25% annual volatility
        var dailyVolatility = standardVolatility / (decimal)Math.Sqrt(252);

        decimal totalValue = tradeGroup.GetTotalValue();
        
        metrics.NetExposure = totalValue;
        metrics.GrossExposure = totalValue;
        
        metrics.VaR95 = totalValue * dailyVolatility * 1.65m;
        metrics.VaR99 = totalValue * dailyVolatility * 2.33m;
        metrics.ExpectedShortfall = metrics.VaR95 * 1.4m;
        metrics.PortfolioVolatility = standardVolatility;
        metrics.DailyVolatility = dailyVolatility;
        
        // No correlation benefit for directional positions
        metrics.CorrelationAdjustment = 0;

        return metrics;
    }
}