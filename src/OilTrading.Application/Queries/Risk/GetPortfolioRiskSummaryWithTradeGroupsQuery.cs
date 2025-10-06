using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.Risk;

/// <summary>
/// 获取基于交易组的投资组合风险摘要查询
/// Get Portfolio Risk Summary with Trade Groups Query
/// </summary>
public class GetPortfolioRiskSummaryWithTradeGroupsQuery : IRequest<PortfolioRiskWithTradeGroupsDto>
{
    /// <summary>
    /// 计算日期 - Calculation date (optional, defaults to today)
    /// </summary>
    public DateTime? AsOfDate { get; set; }

    /// <summary>
    /// 是否包括压力测试 - Include stress test scenarios
    /// </summary>
    public bool IncludeStressTests { get; set; } = false;

    /// <summary>
    /// 历史数据天数 - Historical data days for calculations
    /// </summary>
    public int HistoricalDays { get; set; } = 252;
}