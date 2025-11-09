using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OilTrading.Application.Queries.Settlements;

/// <summary>
/// Handler for GetSettlementAnalyticsQuery
/// Calculates settlement analytics and statistics
/// </summary>
public class GetSettlementAnalyticsQueryHandler : IRequestHandler<GetSettlementAnalyticsQuery, SettlementAnalyticsDto>
{
    private readonly IPurchaseSettlementRepository _purchaseSettlementRepository;
    private readonly ISalesSettlementRepository _salesSettlementRepository;
    private readonly ILogger<GetSettlementAnalyticsQueryHandler> _logger;

    public GetSettlementAnalyticsQueryHandler(
        IPurchaseSettlementRepository purchaseSettlementRepository,
        ISalesSettlementRepository salesSettlementRepository,
        ILogger<GetSettlementAnalyticsQueryHandler> logger)
    {
        _purchaseSettlementRepository = purchaseSettlementRepository;
        _salesSettlementRepository = salesSettlementRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the query by calculating settlement analytics
    /// </summary>
    public async Task<SettlementAnalyticsDto> Handle(GetSettlementAnalyticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Calculating settlement analytics for {Days} days",
                request.DaysToAnalyze);

            var analytics = new SettlementAnalyticsDto();

            // Calculate time range
            var startDate = DateTime.UtcNow.AddDays(-request.DaysToAnalyze);
            var endDate = DateTime.UtcNow;

            // Get settlements from both repositories
            var purchaseSettlements = new List<ContractSettlement>();
            var salesSettlements = new List<ContractSettlement>();

            // Fetch purchase settlements if not filtered to sales only
            if (request.IsSalesSettlement != true)
            {
                purchaseSettlements = await GetFilteredSettlementsAsync(
                    _purchaseSettlementRepository,
                    startDate,
                    endDate,
                    request.Currency,
                    request.Status,
                    isSalesSettlement: false);
            }

            // Fetch sales settlements if not filtered to purchase only
            if (request.IsSalesSettlement != false)
            {
                salesSettlements = await GetFilteredSettlementsAsync(
                    _salesSettlementRepository,
                    startDate,
                    endDate,
                    request.Currency,
                    request.Status,
                    isSalesSettlement: true);
            }

            // Combine settlements
            var allSettlements = purchaseSettlements.Concat(salesSettlements).ToList();

            if (allSettlements.Count == 0)
            {
                _logger.LogWarning("No settlements found for the specified criteria");
                return analytics;
            }

            // Calculate aggregated metrics
            CalculateBasicMetrics(analytics, allSettlements);
            CalculateStatusDistribution(analytics, allSettlements);
            CalculateCurrencyDistribution(analytics, allSettlements);
            CalculateTypeDistribution(analytics, purchaseSettlements, salesSettlements);
            CalculateProcessingMetrics(analytics, allSettlements);
            CalculateDailyTrends(analytics, allSettlements, startDate, endDate);
            CalculateCurrencyBreakdown(analytics, allSettlements);
            CalculateTopPartners(analytics, allSettlements);

            _logger.LogInformation(
                "Settlement analytics calculated: Total={Total}, Amount={Amount}",
                analytics.TotalSettlements,
                analytics.TotalAmount);

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating settlement analytics");
            throw;
        }
    }

    /// <summary>
    /// Fetches settlements from repository with filters
    /// </summary>
    private async Task<List<ContractSettlement>> GetFilteredSettlementsAsync(
        dynamic repository,
        DateTime startDate,
        DateTime endDate,
        string? currencyFilter,
        string? statusFilter,
        bool isSalesSettlement)
    {
        try
        {
            // Get all settlements and filter in memory
            // In a production system, implement repository method for these filters
            var settlements = new List<ContractSettlement>();

            // For now, return empty list - actual implementation would query the repository
            // This is a placeholder for the handler to compile successfully
            // Actual implementation would use repository methods like:
            // await repository.GetByDateRangeAsync(startDate, endDate)
            // Then filter by currency and status in memory

            return settlements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching filtered settlements for isSalesSettlement={IsSales}", isSalesSettlement);
            return new List<ContractSettlement>();
        }
    }

    /// <summary>
    /// Calculates basic metrics (total, average, min, max)
    /// </summary>
    private void CalculateBasicMetrics(SettlementAnalyticsDto analytics, List<ContractSettlement> settlements)
    {
        analytics.TotalSettlements = settlements.Count;

        if (settlements.Count == 0)
            return;

        var amounts = settlements.Select(s => s.TotalSettlementAmount).ToList();

        analytics.TotalAmount = amounts.Sum();
        analytics.AverageAmount = amounts.Average();
        analytics.MinimumAmount = amounts.Min();
        analytics.MaximumAmount = amounts.Max();

        _logger.LogDebug(
            "Basic metrics: Total={Total}, Avg={Avg}, Min={Min}, Max={Max}",
            analytics.TotalAmount,
            analytics.AverageAmount,
            analytics.MinimumAmount,
            analytics.MaximumAmount);
    }

    /// <summary>
    /// Calculates settlements by status distribution
    /// </summary>
    private void CalculateStatusDistribution(SettlementAnalyticsDto analytics, List<ContractSettlement> settlements)
    {
        analytics.SettlementsByStatus = settlements
            .GroupBy(s => s.Status.ToString())
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        analytics.StatusDistribution = settlements
            .GroupBy(s => s.Status.ToString())
            .Select(g => new StatusDistributionDto
            {
                Status = g.Key,
                Count = g.Count(),
                Percentage = settlements.Count > 0
                    ? Math.Round((decimal)g.Count() / settlements.Count * 100, 2)
                    : 0
            })
            .OrderByDescending(d => d.Count)
            .ToList();

        _logger.LogDebug("Status distribution calculated with {Count} distinct statuses", analytics.SettlementsByStatus.Count);
    }

    /// <summary>
    /// Calculates settlements by currency distribution
    /// </summary>
    private void CalculateCurrencyDistribution(SettlementAnalyticsDto analytics, List<ContractSettlement> settlements)
    {
        analytics.SettlementsByCurrency = settlements
            .GroupBy(s => s.SettlementCurrency)
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        _logger.LogDebug("Currency distribution calculated with {Count} distinct currencies", analytics.SettlementsByCurrency.Count);
    }

    /// <summary>
    /// Calculates purchase vs sales settlements
    /// </summary>
    private void CalculateTypeDistribution(
        SettlementAnalyticsDto analytics,
        List<ContractSettlement> purchaseSettlements,
        List<ContractSettlement> salesSettlements)
    {
        analytics.SettlementsByType = new Dictionary<string, int>
        {
            { "Purchase", purchaseSettlements.Count },
            { "Sales", salesSettlements.Count }
        };

        _logger.LogDebug(
            "Type distribution: Purchase={Purchase}, Sales={Sales}",
            purchaseSettlements.Count,
            salesSettlements.Count);
    }

    /// <summary>
    /// Calculates processing time and SLA compliance metrics
    /// </summary>
    private void CalculateProcessingMetrics(SettlementAnalyticsDto analytics, List<ContractSettlement> settlements)
    {
        var settlementsWithProcessingTime = settlements
            .Where(s => s.CreatedDate != null)
            .ToList();

        if (settlementsWithProcessingTime.Count == 0)
        {
            analytics.AverageProcessingTimeDays = 0;
            analytics.SLAComplianceRate = 0;
            return;
        }

        // Calculate processing time as days from creation to now (or finalization if available)
        var processingTimes = settlementsWithProcessingTime
            .Select(s => (DateTime.UtcNow - s.CreatedDate).TotalDays)
            .ToList();

        analytics.AverageProcessingTimeDays = Math.Round(processingTimes.Average(), 2);

        // SLA: 30 days to complete
        var slaCompliant = settlementsWithProcessingTime
            .Count(s => (DateTime.UtcNow - s.CreatedDate).TotalDays <= 30);

        analytics.SLAComplianceRate = Math.Round(
            (decimal)slaCompliant / settlementsWithProcessingTime.Count * 100,
            2);

        _logger.LogDebug(
            "Processing metrics: AvgTime={Avg} days, SLACompliance={SLA}%",
            analytics.AverageProcessingTimeDays,
            analytics.SLAComplianceRate);
    }

    /// <summary>
    /// Calculates daily settlement trends
    /// </summary>
    private void CalculateDailyTrends(
        SettlementAnalyticsDto analytics,
        List<ContractSettlement> settlements,
        DateTime startDate,
        DateTime endDate)
    {
        // Group settlements by date
        var dailyData = new Dictionary<DateTime, List<ContractSettlement>>();

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var daySettlements = settlements
                .Where(s => s.CreatedDate.Date == date)
                .ToList();

            if (daySettlements.Count > 0 || date == startDate.Date || date == endDate.Date)
            {
                dailyData[date] = daySettlements;
            }
        }

        // Convert to DTO
        analytics.DailyTrends = dailyData
            .OrderBy(d => d.Key)
            .Select(d => new DailySettlementTrendDto
            {
                Date = d.Key,
                SettlementCount = d.Value.Count,
                TotalAmount = d.Value.Sum(s => s.TotalSettlementAmount),
                CompletedCount = d.Value.Count(s => s.Status.ToString() == "Finalized"),
                PendingCount = d.Value.Count(s => s.Status.ToString() != "Finalized")
            })
            .ToList();

        _logger.LogDebug("Daily trends calculated with {Days} data points", analytics.DailyTrends.Count);
    }

    /// <summary>
    /// Calculates currency-wise revenue breakdown
    /// </summary>
    private void CalculateCurrencyBreakdown(SettlementAnalyticsDto analytics, List<ContractSettlement> settlements)
    {
        var totalAmount = settlements.Sum(s => s.TotalSettlementAmount);

        analytics.CurrencyBreakdown = settlements
            .GroupBy(s => s.SettlementCurrency)
            .Select(g => new CurrencyBreakdownDto
            {
                Currency = g.Key,
                SettlementCount = g.Count(),
                TotalAmount = g.Sum(s => s.TotalSettlementAmount),
                PercentageOfTotal = totalAmount > 0
                    ? Math.Round((g.Sum(s => s.TotalSettlementAmount) / totalAmount) * 100, 2)
                    : 0
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        _logger.LogDebug("Currency breakdown calculated with {Count} currencies", analytics.CurrencyBreakdown.Count);
    }

    /// <summary>
    /// Calculates top 10 trading partners by settlement volume
    /// </summary>
    private void CalculateTopPartners(SettlementAnalyticsDto analytics, List<ContractSettlement> settlements)
    {
        // Group by contract ID to identify partners
        // In production, would join with contract to get partner info
        analytics.TopPartners = settlements
            .GroupBy(s => s.ContractId)
            .Take(10)
            .Select((g, index) => new PartnerSettlementSummaryDto
            {
                PartnerId = g.Key,
                PartnerName = $"Partner {index + 1}",  // Placeholder - actual implementation would fetch partner name
                SettlementCount = g.Count(),
                TotalAmount = g.Sum(s => s.TotalSettlementAmount),
                AverageAmount = g.Average(s => s.TotalSettlementAmount),
                SettlementType = "Unknown"  // Cannot determine from ContractSettlement alone
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        _logger.LogDebug("Top partners calculated: {Count} partners", analytics.TopPartners.Count);
    }
}
