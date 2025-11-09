using MediatR;
using System;

namespace OilTrading.Application.Queries.Settlements;

/// <summary>
/// Query to retrieve settlement analytics and statistics
/// Returns: SettlementAnalyticsDto with aggregated metrics
/// </summary>
public class GetSettlementAnalyticsQuery : IRequest<SettlementAnalyticsDto>
{
    /// <summary>
    /// Number of days to analyze (default: 30)
    /// </summary>
    public int DaysToAnalyze { get; set; } = 30;

    /// <summary>
    /// Optional: Filter by settlement type (true = sales, false = purchase, null = all)
    /// </summary>
    public bool? IsSalesSettlement { get; set; }

    /// <summary>
    /// Optional: Filter by currency
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Optional: Filter by status
    /// </summary>
    public string? Status { get; set; }
}

/// <summary>
/// DTO for settlement analytics results
/// </summary>
public class SettlementAnalyticsDto
{
    /// <summary>
    /// Total number of settlements in period
    /// </summary>
    public int TotalSettlements { get; set; }

    /// <summary>
    /// Total settlement amount across all settlements
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Average settlement amount
    /// </summary>
    public decimal AverageAmount { get; set; }

    /// <summary>
    /// Minimum settlement amount
    /// </summary>
    public decimal MinimumAmount { get; set; }

    /// <summary>
    /// Maximum settlement amount
    /// </summary>
    public decimal MaximumAmount { get; set; }

    /// <summary>
    /// Count of settlements by status
    /// </summary>
    public Dictionary<string, int> SettlementsByStatus { get; set; } = new();

    /// <summary>
    /// Count of settlements by currency
    /// </summary>
    public Dictionary<string, int> SettlementsByCurrency { get; set; } = new();

    /// <summary>
    /// Count of purchase vs sales settlements
    /// </summary>
    public Dictionary<string, int> SettlementsByType { get; set; } = new();

    /// <summary>
    /// Average processing time from creation to finalization (days)
    /// </summary>
    public double AverageProcessingTimeDays { get; set; }

    /// <summary>
    /// Percentage of settlements completed within SLA (30 days)
    /// </summary>
    public decimal SLAComplianceRate { get; set; }

    /// <summary>
    /// Daily settlement trend data
    /// </summary>
    public List<DailySettlementTrendDto> DailyTrends { get; set; } = new();

    /// <summary>
    /// Currency-wise revenue breakdown
    /// </summary>
    public List<CurrencyBreakdownDto> CurrencyBreakdown { get; set; } = new();

    /// <summary>
    /// Partner-wise settlement summary (top 10)
    /// </summary>
    public List<PartnerSettlementSummaryDto> TopPartners { get; set; } = new();

    /// <summary>
    /// Settlement status distribution for pie chart
    /// </summary>
    public List<StatusDistributionDto> StatusDistribution { get; set; } = new();
}

/// <summary>
/// Daily settlement trend data
/// </summary>
public class DailySettlementTrendDto
{
    public DateTime Date { get; set; }
    public int SettlementCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int CompletedCount { get; set; }
    public int PendingCount { get; set; }
}

/// <summary>
/// Currency-wise breakdown
/// </summary>
public class CurrencyBreakdownDto
{
    public string Currency { get; set; } = string.Empty;
    public int SettlementCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

/// <summary>
/// Partner settlement summary
/// </summary>
public class PartnerSettlementSummaryDto
{
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public int SettlementCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
    public string SettlementType { get; set; } = string.Empty;  // "Purchase" or "Sales"
}

/// <summary>
/// Status distribution for visualization
/// </summary>
public class StatusDistributionDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}
