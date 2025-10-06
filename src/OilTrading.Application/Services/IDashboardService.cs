using OilTrading.Application.DTOs;

namespace OilTrading.Application.Services;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetDashboardOverviewAsync(CancellationToken cancellationToken = default);
    Task<TradingMetricsDto> GetTradingMetricsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<PerformanceAnalyticsDto> GetPerformanceAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<MarketInsightsDto> GetMarketInsightsAsync(CancellationToken cancellationToken = default);
    Task<OperationalStatusDto> GetOperationalStatusAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AlertDto>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);
    Task<KpiSummaryDto> GetKpiSummaryAsync(CancellationToken cancellationToken = default);
}