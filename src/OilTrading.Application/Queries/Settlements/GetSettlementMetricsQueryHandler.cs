using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OilTrading.Application.Queries.Settlements;

/// <summary>
/// Handler for GetSettlementMetricsQuery
/// Calculates key performance metrics for dashboard KPIs
/// </summary>
public class GetSettlementMetricsQueryHandler : IRequestHandler<GetSettlementMetricsQuery, SettlementMetricsDto>
{
    private readonly IPurchaseSettlementRepository _purchaseSettlementRepository;
    private readonly ISalesSettlementRepository _salesSettlementRepository;
    private readonly ILogger<GetSettlementMetricsQueryHandler> _logger;

    public GetSettlementMetricsQueryHandler(
        IPurchaseSettlementRepository purchaseSettlementRepository,
        ISalesSettlementRepository salesSettlementRepository,
        ILogger<GetSettlementMetricsQueryHandler> logger)
    {
        _purchaseSettlementRepository = purchaseSettlementRepository;
        _salesSettlementRepository = salesSettlementRepository;
        _logger = logger;
    }

    public async Task<SettlementMetricsDto> Handle(GetSettlementMetricsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Calculating settlement metrics for {Days} days", request.DaysToAnalyze);

            var startDate = DateTime.UtcNow.AddDays(-request.DaysToAnalyze);
            var endDate = DateTime.UtcNow;

            var metrics = new SettlementMetricsDto();

            // Get current period settlements
            var purchaseSettlements = await GetSettlementsAsync(_purchaseSettlementRepository, startDate, endDate);
            var salesSettlements = await GetSettlementsAsync(_salesSettlementRepository, startDate, endDate);

            var allSettlements = purchaseSettlements.Concat(salesSettlements).ToList();

            if (allSettlements.Count == 0)
            {
                _logger.LogWarning("No settlements found for metrics calculation");
                return metrics;
            }

            // Calculate metrics
            metrics.TotalSettlementValue = allSettlements.Sum(s => s.TotalSettlementAmount);
            metrics.TotalSettlementCount = allSettlements.Count;
            metrics.AverageSettlementValue = allSettlements.Average(s => s.TotalSettlementAmount);

            // Calculate processing time
            var settlementsWithCreationDate = allSettlements.Where(s => s.CreatedDate != null).ToList();
            if (settlementsWithCreationDate.Count > 0)
            {
                var processingTimes = settlementsWithCreationDate
                    .Select(s => (DateTime.UtcNow - s.CreatedDate).TotalHours)
                    .ToList();

                metrics.AverageProcessingTimeHours = Math.Round(processingTimes.Average(), 2);
            }

            // Calculate on-time completion rate (SLA: 30 days)
            var completedOnTime = allSettlements.Count(s => (DateTime.UtcNow - s.CreatedDate).TotalDays <= 30);
            metrics.OnTimeCompletionRate = allSettlements.Count > 0
                ? Math.Round((decimal)completedOnTime / allSettlements.Count * 100, 2)
                : 0;

            // Count settlements with errors
            metrics.SettlementsWithErrors = 0;  // Placeholder - would check for error status/flags

            // Calculate success rate
            var successfulSettlements = allSettlements.Count(s => s.Status.ToString() == "Finalized");
            metrics.SuccessRate = allSettlements.Count > 0
                ? Math.Round((decimal)successfulSettlements / allSettlements.Count * 100, 2)
                : 0;

            // Pending and overdue
            metrics.PendingSettlements = allSettlements.Count(s => s.Status.ToString() != "Finalized");
            metrics.OverdueSettlements = allSettlements.Count(s =>
                s.Status.ToString() != "Finalized" && (DateTime.UtcNow - s.CreatedDate).TotalDays > 30);

            // Unique partners
            metrics.UniquePartners = allSettlements.Select(s => s.ContractId).Distinct().Count();

            // Most common currency
            var currencyGroups = allSettlements.GroupBy(s => s.SettlementCurrency).OrderByDescending(g => g.Count());
            if (currencyGroups.Any())
            {
                metrics.MostCommonCurrency = currencyGroups.First().Key;
            }

            // Calculate trends vs previous period
            var previousStartDate = startDate.AddDays(-request.DaysToAnalyze);
            var previousPurchaseSettlements = await GetSettlementsAsync(_purchaseSettlementRepository, previousStartDate, startDate);
            var previousSalesSettlements = await GetSettlementsAsync(_salesSettlementRepository, previousStartDate, startDate);
            var previousSettlements = previousPurchaseSettlements.Concat(previousSalesSettlements).ToList();

            if (previousSettlements.Count > 0)
            {
                // Settlement count trend
                metrics.SettlementCountTrend = ((decimal)(allSettlements.Count - previousSettlements.Count) / previousSettlements.Count) * 100;

                // Settlement value trend
                var previousValue = previousSettlements.Sum(s => s.TotalSettlementAmount);
                if (previousValue > 0)
                {
                    metrics.SettlementValueTrend = ((metrics.TotalSettlementValue - previousValue) / previousValue) * 100;
                }
            }

            _logger.LogInformation(
                "Metrics calculated: Total={Total}, Count={Count}, SuccessRate={Rate}%",
                metrics.TotalSettlementValue,
                metrics.TotalSettlementCount,
                metrics.SuccessRate);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating settlement metrics");
            throw;
        }
    }

    private async Task<List<Core.Entities.ContractSettlement>> GetSettlementsAsync(
        dynamic repository,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            // Placeholder implementation
            // Actual implementation would call repository method
            return new List<Core.Entities.ContractSettlement>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching settlements");
            return new List<Core.Entities.ContractSettlement>();
        }
    }
}

/// <summary>
/// Handler for GetSettlementVolumeTrendQuery
/// </summary>
public class GetSettlementVolumeTrendQueryHandler : IRequestHandler<GetSettlementVolumeTrendQuery, SettlementVolumeTrendDto>
{
    private readonly IPurchaseSettlementRepository _purchaseSettlementRepository;
    private readonly ISalesSettlementRepository _salesSettlementRepository;
    private readonly ILogger<GetSettlementVolumeTrendQueryHandler> _logger;

    public GetSettlementVolumeTrendQueryHandler(
        IPurchaseSettlementRepository purchaseSettlementRepository,
        ISalesSettlementRepository salesSettlementRepository,
        ILogger<GetSettlementVolumeTrendQueryHandler> logger)
    {
        _purchaseSettlementRepository = purchaseSettlementRepository;
        _salesSettlementRepository = salesSettlementRepository;
        _logger = logger;
    }

    public async Task<SettlementVolumeTrendDto> Handle(GetSettlementVolumeTrendQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Calculating settlement volume trend: {Days} days, {Granularity} granularity",
                request.DaysToAnalyze,
                request.Granularity);

            var trend = new SettlementVolumeTrendDto();

            // Placeholder implementation
            // Actual implementation would fetch data and calculate trends

            return trend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating settlement volume trend");
            throw;
        }
    }
}

/// <summary>
/// Handler for GetSettlementStatusBreakdownQuery
/// </summary>
public class GetSettlementStatusBreakdownQueryHandler : IRequestHandler<GetSettlementStatusBreakdownQuery, SettlementStatusBreakdownDto>
{
    private readonly IPurchaseSettlementRepository _purchaseSettlementRepository;
    private readonly ISalesSettlementRepository _salesSettlementRepository;
    private readonly ILogger<GetSettlementStatusBreakdownQueryHandler> _logger;

    public GetSettlementStatusBreakdownQueryHandler(
        IPurchaseSettlementRepository purchaseSettlementRepository,
        ISalesSettlementRepository salesSettlementRepository,
        ILogger<GetSettlementStatusBreakdownQueryHandler> logger)
    {
        _purchaseSettlementRepository = purchaseSettlementRepository;
        _salesSettlementRepository = salesSettlementRepository;
        _logger = logger;
    }

    public async Task<SettlementStatusBreakdownDto> Handle(GetSettlementStatusBreakdownQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Calculating settlement status breakdown for {Days} days", request.DaysToAnalyze);

            var breakdown = new SettlementStatusBreakdownDto();

            // Placeholder implementation
            // Actual implementation would fetch data and group by status

            return breakdown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating settlement status breakdown");
            throw;
        }
    }
}

/// <summary>
/// Handler for GetPartnerSettlementPerformanceQuery
/// </summary>
public class GetPartnerSettlementPerformanceQueryHandler : IRequestHandler<GetPartnerSettlementPerformanceQuery, PartnerPerformanceDto>
{
    private readonly IPurchaseSettlementRepository _purchaseSettlementRepository;
    private readonly ISalesSettlementRepository _salesSettlementRepository;
    private readonly ILogger<GetPartnerSettlementPerformanceQueryHandler> _logger;

    public GetPartnerSettlementPerformanceQueryHandler(
        IPurchaseSettlementRepository purchaseSettlementRepository,
        ISalesSettlementRepository salesSettlementRepository,
        ILogger<GetPartnerSettlementPerformanceQueryHandler> logger)
    {
        _purchaseSettlementRepository = purchaseSettlementRepository;
        _salesSettlementRepository = salesSettlementRepository;
        _logger = logger;
    }

    public async Task<PartnerPerformanceDto> Handle(GetPartnerSettlementPerformanceQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Calculating partner settlement performance: {Days} days, top {Count}",
                request.DaysToAnalyze,
                request.TopCount);

            var performance = new PartnerPerformanceDto();

            // Placeholder implementation
            // Actual implementation would fetch partner data and calculate metrics

            return performance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating partner settlement performance");
            throw;
        }
    }
}
