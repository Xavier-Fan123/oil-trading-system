using MediatR;
using System;

namespace OilTrading.Application.Queries.Settlements;

/// <summary>
/// Query to retrieve key settlement performance metrics
/// Used for dashboard KPI display
/// </summary>
public class GetSettlementMetricsQuery : IRequest<SettlementMetricsDto>
{
    /// <summary>
    /// Time period to analyze in days (default: 7)
    /// </summary>
    public int DaysToAnalyze { get; set; } = 7;
}

/// <summary>
/// DTO for settlement metrics/KPIs
/// </summary>
public class SettlementMetricsDto
{
    /// <summary>
    /// Total settlement value processed in period
    /// </summary>
    public decimal TotalSettlementValue { get; set; }

    /// <summary>
    /// Number of settlements processed
    /// </summary>
    public int TotalSettlementCount { get; set; }

    /// <summary>
    /// Average settlement processing time in hours
    /// </summary>
    public double AverageProcessingTimeHours { get; set; }

    /// <summary>
    /// Percentage of settlements completed on time (within SLA)
    /// </summary>
    public decimal OnTimeCompletionRate { get; set; }

    /// <summary>
    /// Number of settlements with errors or issues
    /// </summary>
    public int SettlementsWithErrors { get; set; }

    /// <summary>
    /// Percentage of successful settlements (no errors)
    /// </summary>
    public decimal SuccessRate { get; set; }

    /// <summary>
    /// Average settlement value
    /// </summary>
    public decimal AverageSettlementValue { get; set; }

    /// <summary>
    /// Pending settlements requiring action
    /// </summary>
    public int PendingSettlements { get; set; }

    /// <summary>
    /// Overdue settlements past SLA
    /// </summary>
    public int OverdueSettlements { get; set; }

    /// <summary>
    /// Total unique trading partners
    /// </summary>
    public int UniquePartners { get; set; }

    /// <summary>
    /// Most commonly used currency
    /// </summary>
    public string MostCommonCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Settlement count trend vs previous period (percentage change)
    /// </summary>
    public decimal SettlementCountTrend { get; set; }

    /// <summary>
    /// Settlement value trend vs previous period (percentage change)
    /// </summary>
    public decimal SettlementValueTrend { get; set; }

    /// <summary>
    /// Timestamp of metric calculation
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Query to retrieve settlement volume trends for charting
/// </summary>
public class GetSettlementVolumeTrendQuery : IRequest<SettlementVolumeTrendDto>
{
    /// <summary>
    /// Number of days of historical data (default: 30)
    /// </summary>
    public int DaysToAnalyze { get; set; } = 30;

    /// <summary>
    /// Granularity: "daily", "weekly", "monthly"
    /// </summary>
    public string Granularity { get; set; } = "daily";
}

/// <summary>
/// DTO for settlement volume trend data
/// </summary>
public class SettlementVolumeTrendDto
{
    /// <summary>
    /// Data points for trend line
    /// </summary>
    public List<VolumeDataPointDto> DataPoints { get; set; } = new();

    /// <summary>
    /// Summary statistics
    /// </summary>
    public TrendSummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Single data point for volume trend
/// </summary>
public class VolumeDataPointDto
{
    /// <summary>
    /// Time period (date for daily, week start for weekly, month start for monthly)
    /// </summary>
    public DateTime Period { get; set; }

    /// <summary>
    /// Settlement count in this period
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total value of settlements in this period
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Number of completed settlements
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// Number of pending settlements
    /// </summary>
    public int PendingCount { get; set; }
}

/// <summary>
/// Summary statistics for trends
/// </summary>
public class TrendSummaryDto
{
    /// <summary>
    /// Highest count in any period
    /// </summary>
    public int MaxCount { get; set; }

    /// <summary>
    /// Lowest count in any period
    /// </summary>
    public int MinCount { get; set; }

    /// <summary>
    /// Average count across all periods
    /// </summary>
    public double AverageCount { get; set; }

    /// <summary>
    /// Highest value in any period
    /// </summary>
    public decimal MaxValue { get; set; }

    /// <summary>
    /// Lowest value in any period
    /// </summary>
    public decimal MinValue { get; set; }

    /// <summary>
    /// Average value across all periods
    /// </summary>
    public decimal AverageValue { get; set; }

    /// <summary>
    /// Overall trend direction: "Up", "Down", or "Stable"
    /// </summary>
    public string TrendDirection { get; set; } = "Stable";

    /// <summary>
    /// Percentage change from first to last period
    /// </summary>
    public decimal PercentageChange { get; set; }
}

/// <summary>
/// Query to retrieve settlement status breakdown
/// </summary>
public class GetSettlementStatusBreakdownQuery : IRequest<SettlementStatusBreakdownDto>
{
    /// <summary>
    /// Days to analyze (default: 7)
    /// </summary>
    public int DaysToAnalyze { get; set; } = 7;
}

/// <summary>
/// Settlement status breakdown DTO
/// </summary>
public class SettlementStatusBreakdownDto
{
    /// <summary>
    /// Breakdown by status
    /// </summary>
    public List<StatusCountDto> StatusBreakdown { get; set; } = new();

    /// <summary>
    /// Total settlements analyzed
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Percentage in each status for pie chart
    /// </summary>
    public List<StatusPercentageDto> StatusPercentages { get; set; } = new();
}

/// <summary>
/// Status count data
/// </summary>
public class StatusCountDto
{
    /// <summary>
    /// Settlement status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Count of settlements in this status
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total value of settlements in this status
    /// </summary>
    public decimal TotalValue { get; set; }
}

/// <summary>
/// Status percentage for visualization
/// </summary>
public class StatusPercentageDto
{
    public string Status { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Query for settlement partners performance
/// </summary>
public class GetPartnerSettlementPerformanceQuery : IRequest<PartnerPerformanceDto>
{
    /// <summary>
    /// Days to analyze (default: 30)
    /// </summary>
    public int DaysToAnalyze { get; set; } = 30;

    /// <summary>
    /// Number of top partners to return (default: 10)
    /// </summary>
    public int TopCount { get; set; } = 10;
}

/// <summary>
/// Partner settlement performance DTO
/// </summary>
public class PartnerPerformanceDto
{
    /// <summary>
    /// List of partner performance metrics
    /// </summary>
    public List<PartnerMetricsDto> Partners { get; set; } = new();

    /// <summary>
    /// Total unique partners analyzed
    /// </summary>
    public int TotalPartners { get; set; }

    /// <summary>
    /// Top performer (by volume)
    /// </summary>
    public PartnerMetricsDto? TopPerformer { get; set; }

    /// <summary>
    /// Average metrics across all partners
    /// </summary>
    public PartnerMetricsDto? AverageMetrics { get; set; }
}

/// <summary>
/// Individual partner metrics
/// </summary>
public class PartnerMetricsDto
{
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string PartnerType { get; set; } = string.Empty;  // "Supplier" or "Customer"

    /// <summary>
    /// Settlement count
    /// </summary>
    public int SettlementCount { get; set; }

    /// <summary>
    /// Total settlement value
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Average settlement value
    /// </summary>
    public decimal AverageValue { get; set; }

    /// <summary>
    /// On-time completion rate
    /// </summary>
    public decimal OnTimeRate { get; set; }

    /// <summary>
    /// Settlement success rate (no errors)
    /// </summary>
    public decimal SuccessRate { get; set; }

    /// <summary>
    /// Average processing time in hours
    /// </summary>
    public double AverageProcessingHours { get; set; }

    /// <summary>
    /// Current outstanding balance
    /// </summary>
    public decimal OutstandingBalance { get; set; }

    /// <summary>
    /// Overdue amounts (past due date)
    /// </summary>
    public decimal OverdueAmount { get; set; }

    /// <summary>
    /// Credit limit set for partner
    /// </summary>
    public decimal CreditLimit { get; set; }

    /// <summary>
    /// Percentage of credit limit used
    /// </summary>
    public decimal CreditUtilizationRate { get; set; }
}
