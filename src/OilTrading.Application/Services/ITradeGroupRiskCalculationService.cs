using OilTrading.Core.Entities;

namespace OilTrading.Application.Services;

/// <summary>
/// 交易组风险计算服务接口 - Trade Group Risk Calculation Service Interface
/// </summary>
public interface ITradeGroupRiskCalculationService
{
    /// <summary>
    /// 计算交易组的风险指标 - Calculate risk metrics for a trade group
    /// </summary>
    Task<TradeGroupRiskMetrics> CalculateTradeGroupRiskAsync(TradeGroup tradeGroup, CancellationToken cancellationToken = default);

    /// <summary>
    /// 计算价差策略的风险 - Calculate risk for spread strategies
    /// </summary>
    Task<decimal> CalculateSpreadRiskAsync(IEnumerable<PaperContract> spreadContracts, CancellationToken cancellationToken = default);

    /// <summary>
    /// 计算对冲策略的风险 - Calculate risk for hedge strategies
    /// </summary>
    Task<decimal> CalculateHedgeRiskAsync(IEnumerable<object> hedgeContracts, CancellationToken cancellationToken = default);

    /// <summary>
    /// 计算单个合约的独立风险 - Calculate standalone contract risk
    /// </summary>
    Task<decimal> CalculateStandaloneRiskAsync(object contract, CancellationToken cancellationToken = default);
}

/// <summary>
/// 交易组风险指标 - Trade Group Risk Metrics
/// </summary>
public class TradeGroupRiskMetrics
{
    public Guid TradeGroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public StrategyType StrategyType { get; set; }

    /// <summary>
    /// 净敞口 - Net exposure value
    /// </summary>
    public decimal NetExposure { get; set; }

    /// <summary>
    /// 总敞口 - Gross exposure (for standalone positions)
    /// </summary>
    public decimal GrossExposure { get; set; }

    /// <summary>
    /// 95% 置信度VaR - 95% confidence VaR
    /// </summary>
    public decimal VaR95 { get; set; }

    /// <summary>
    /// 99% 置信度VaR - 99% confidence VaR
    /// </summary>
    public decimal VaR99 { get; set; }

    /// <summary>
    /// 预期尾部损失 - Expected Shortfall (CVaR)
    /// </summary>
    public decimal ExpectedShortfall { get; set; }

    /// <summary>
    /// 组合波动率 - Portfolio volatility
    /// </summary>
    public decimal PortfolioVolatility { get; set; }

    /// <summary>
    /// 净盈亏 - Net P&L
    /// </summary>
    public decimal NetPnL { get; set; }

    /// <summary>
    /// 日收益波动率 - Daily return volatility
    /// </summary>
    public decimal DailyVolatility { get; set; }

    /// <summary>
    /// 相关性调整因子 - Correlation adjustment factor
    /// </summary>
    public decimal CorrelationAdjustment { get; set; }

    /// <summary>
    /// 计算时间 - Calculation timestamp
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}