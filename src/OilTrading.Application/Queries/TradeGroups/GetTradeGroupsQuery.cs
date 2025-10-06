using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;

namespace OilTrading.Application.Queries.TradeGroups;

/// <summary>
/// 获取交易组查询 - Get Trade Groups Query
/// </summary>
public class GetTradeGroupsQuery : IRequest<PagedResult<TradeGroupSummaryDto>>
{
    /// <summary>
    /// 页码 - Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页大小 - Page size
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// 搜索关键词 - Search keyword for group name
    /// </summary>
    public string? SearchKeyword { get; set; }

    /// <summary>
    /// 策略类型过滤 - Strategy type filter
    /// </summary>
    public int? StrategyType { get; set; }

    /// <summary>
    /// 状态过滤 - Status filter
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 风险等级过滤 - Risk level filter
    /// </summary>
    public int? RiskLevel { get; set; }

    /// <summary>
    /// 创建日期开始 - Created date from
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// 创建日期结束 - Created date to
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// 排序字段 - Sort field
    /// </summary>
    public string? SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// 排序方向 - Sort direction
    /// </summary>
    public string? SortDirection { get; set; } = "desc";
}