namespace OilTrading.Application.DTOs;

/// <summary>
/// 基于交易组的投资组合风险DTO - Portfolio Risk with Trade Groups DTO
/// </summary>
public class PortfolioRiskWithTradeGroupsDto
{
    /// <summary>
    /// 计算日期 - Calculation date
    /// </summary>
    public DateTime AsOfDate { get; set; }

    /// <summary>
    /// 独立头寸风险 - Standalone positions risk
    /// </summary>
    public StandaloneRiskDto StandaloneRisk { get; set; } = new();

    /// <summary>
    /// 交易组风险列表 - Trade group risks list
    /// </summary>
    public List<TradeGroupRiskDto> TradeGroupRisks { get; set; } = new();

    /// <summary>
    /// 总投资组合风险 - Total portfolio risk
    /// </summary>
    public TotalPortfolioRiskDto TotalPortfolioRisk { get; set; } = new();

    /// <summary>
    /// 计算时间 - Calculation timestamp
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// 独立头寸风险DTO - Standalone Risk DTO
/// </summary>
public class StandaloneRiskDto
{
    /// <summary>
    /// 总头寸数 - Total positions count
    /// </summary>
    public int TotalPositions { get; set; }

    /// <summary>
    /// 净敞口 - Net exposure
    /// </summary>
    public decimal NetExposure { get; set; }

    /// <summary>
    /// 总敞口 - Gross exposure
    /// </summary>
    public decimal GrossExposure { get; set; }

    /// <summary>
    /// 95%置信度VaR - 95% confidence VaR
    /// </summary>
    public decimal VaR95 { get; set; }

    /// <summary>
    /// 99%置信度VaR - 99% confidence VaR
    /// </summary>
    public decimal VaR99 { get; set; }

    /// <summary>
    /// 日波动率 - Daily volatility
    /// </summary>
    public decimal DailyVolatility { get; set; }
}

/// <summary>
/// 交易组风险DTO - Trade Group Risk DTO
/// </summary>
public class TradeGroupRiskDto
{
    /// <summary>
    /// 交易组ID - Trade Group ID
    /// </summary>
    public Guid TradeGroupId { get; set; }

    /// <summary>
    /// 组名 - Group name
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// 策略类型 - Strategy type
    /// </summary>
    public string StrategyType { get; set; } = string.Empty;

    /// <summary>
    /// 净敞口 - Net exposure (the actual risk for spread strategies)
    /// </summary>
    public decimal NetExposure { get; set; }

    /// <summary>
    /// 总敞口 - Gross exposure (sum of all legs)
    /// </summary>
    public decimal GrossExposure { get; set; }

    /// <summary>
    /// 95%置信度VaR - 95% confidence VaR (based on net exposure for spreads)
    /// </summary>
    public decimal VaR95 { get; set; }

    /// <summary>
    /// 99%置信度VaR - 99% confidence VaR
    /// </summary>
    public decimal VaR99 { get; set; }

    /// <summary>
    /// 净盈亏 - Net P&L
    /// </summary>
    public decimal NetPnL { get; set; }

    /// <summary>
    /// 合约数量 - Contract count
    /// </summary>
    public int ContractCount { get; set; }

    /// <summary>
    /// 组合波动率 - Portfolio volatility
    /// </summary>
    public decimal PortfolioVolatility { get; set; }

    /// <summary>
    /// 相关性收益 - Correlation benefit (VaR reduction due to correlations)
    /// </summary>
    public decimal? CorrelationBenefit { get; set; }

    /// <summary>
    /// 错误信息 - Error message (if calculation failed)
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 状态 - Trade group status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 预期尾部损失 - Expected Shortfall (CVaR)
    /// </summary>
    public decimal ExpectedShortfall { get; set; }

    /// <summary>
    /// 日收益波动率 - Daily return volatility
    /// </summary>
    public decimal DailyVolatility { get; set; }

    /// <summary>
    /// 相关性调整因子 - Correlation adjustment factor
    /// </summary>
    public decimal CorrelationAdjustment { get; set; }

    /// <summary>
    /// 是否为价差策略 - Is spread strategy
    /// </summary>
    public bool IsSpreadStrategy { get; set; }

    /// <summary>
    /// 是否为对冲策略 - Is hedge strategy
    /// </summary>
    public bool IsHedgeStrategy { get; set; }

    /// <summary>
    /// 计算时间 - Calculation timestamp
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// 总投资组合风险DTO - Total Portfolio Risk DTO
/// </summary>
public class TotalPortfolioRiskDto
{
    /// <summary>
    /// 总VaR 95% - Total VaR 95%
    /// </summary>
    public decimal TotalVaR95 { get; set; }

    /// <summary>
    /// 总VaR 99% - Total VaR 99%
    /// </summary>
    public decimal TotalVaR99 { get; set; }

    /// <summary>
    /// 总净敞口 - Total net exposure
    /// </summary>
    public decimal TotalNetExposure { get; set; }

    /// <summary>
    /// 总敞口 - Total gross exposure
    /// </summary>
    public decimal TotalGrossExposure { get; set; }

    /// <summary>
    /// 总头寸数 - Total positions
    /// </summary>
    public int TotalPositions { get; set; }

    /// <summary>
    /// 交易组数量 - Trade group count
    /// </summary>
    public int TradeGroupCount { get; set; }

    /// <summary>
    /// 相关性收益总计 - Total correlation benefit
    /// </summary>
    public decimal CorrelationBenefit { get; set; }

    /// <summary>
    /// 多样化比率 - Diversification ratio (Net/Gross)
    /// </summary>
    public decimal DiversificationRatio { get; set; }
}

/// <summary>
/// 交易组创建DTO - Trade Group Creation DTO
/// </summary>
public class CreateTradeGroupDto
{
    /// <summary>
    /// 组名 - Group name
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// 策略类型 - Strategy type
    /// </summary>
    public string StrategyType { get; set; } = string.Empty;

    /// <summary>
    /// 描述 - Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 预期风险水平 - Expected risk level
    /// </summary>
    public string? ExpectedRiskLevel { get; set; }

    /// <summary>
    /// 最大允许损失 - Maximum allowed loss
    /// </summary>
    public decimal? MaxAllowedLoss { get; set; }

    /// <summary>
    /// 目标利润 - Target profit
    /// </summary>
    public decimal? TargetProfit { get; set; }
}

/// <summary>
/// 交易组详情DTO - Trade Group Details DTO
/// </summary>
public class TradeGroupDetailsDto
{
    /// <summary>
    /// 交易组ID - Trade Group ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 组名 - Group name
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// 策略类型 - Strategy type
    /// </summary>
    public string StrategyType { get; set; } = string.Empty;

    /// <summary>
    /// 状态 - Status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 描述 - Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 创建时间 - Created at
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建者 - Created by
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 纸面合约 - Paper contracts
    /// </summary>
    public List<PaperContractSummaryDto> PaperContracts { get; set; } = new();

    /// <summary>
    /// 采购合约 - Purchase contracts
    /// </summary>
    public List<PurchaseContractSummaryDto> PurchaseContracts { get; set; } = new();

    /// <summary>
    /// 销售合约 - Sales contracts
    /// </summary>
    public List<SalesContractSummaryDto> SalesContracts { get; set; } = new();

    /// <summary>
    /// 风险指标 - Risk metrics
    /// </summary>
    public TradeGroupRiskDto? RiskMetrics { get; set; }

    /// <summary>
    /// 预期风险水平 - Expected risk level
    /// </summary>
    public string? ExpectedRiskLevel { get; set; }

    /// <summary>
    /// 最大允许损失 - Maximum allowed loss
    /// </summary>
    public decimal? MaxAllowedLoss { get; set; }

    /// <summary>
    /// 目标利润 - Target profit
    /// </summary>
    public decimal? TargetProfit { get; set; }

    /// <summary>
    /// 合约总数 - Total contract count
    /// </summary>
    public int ContractCount { get; set; }

    /// <summary>
    /// 纸面合约数量 - Paper contract count
    /// </summary>
    public int PaperContractCount { get; set; }

    /// <summary>
    /// 采购合约数量 - Purchase contract count
    /// </summary>
    public int PurchaseContractCount { get; set; }

    /// <summary>
    /// 销售合约数量 - Sales contract count
    /// </summary>
    public int SalesContractCount { get; set; }

    /// <summary>
    /// 净盈亏 - Net P&L
    /// </summary>
    public decimal NetPnL { get; set; }

    /// <summary>
    /// 总价值 - Total value
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// 更新时间 - Updated at
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 更新者 - Updated by
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// 纸面合约摘要DTO - Paper Contract Summary DTO
/// </summary>
public class PaperContractSummaryDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? UnrealizedPnL { get; set; }
    public DateTime TradeDate { get; set; }
}

