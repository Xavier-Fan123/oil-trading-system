using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.FinancialReports;

public class GetFinancialReportsByTradingPartnerQueryHandler : IRequestHandler<GetFinancialReportsByTradingPartnerQuery, IReadOnlyList<FinancialReportDto>>
{
    private readonly IFinancialReportRepository _financialReportRepository;
    private readonly ITradingPartnerRepository _tradingPartnerRepository;
    private readonly ILogger<GetFinancialReportsByTradingPartnerQueryHandler> _logger;

    public GetFinancialReportsByTradingPartnerQueryHandler(
        IFinancialReportRepository financialReportRepository,
        ITradingPartnerRepository tradingPartnerRepository,
        ILogger<GetFinancialReportsByTradingPartnerQueryHandler> logger)
    {
        _financialReportRepository = financialReportRepository;
        _tradingPartnerRepository = tradingPartnerRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FinancialReportDto>> Handle(GetFinancialReportsByTradingPartnerQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving financial reports for trading partner {TradingPartnerId}, year filter: {Year}",
            request.TradingPartnerId, request.Year?.ToString() ?? "All years");

        // Validate trading partner exists
        var tradingPartner = await _tradingPartnerRepository.GetByIdAsync(request.TradingPartnerId, cancellationToken);
        if (tradingPartner == null)
        {
            throw new NotFoundException("TradingPartner", request.TradingPartnerId);
        }

        // Get financial reports
        IReadOnlyList<Core.Entities.FinancialReport> reports;
        
        if (request.Year.HasValue)
        {
            reports = await _financialReportRepository.GetByTradingPartnerAndYearAsync(
                request.TradingPartnerId, 
                request.Year.Value, 
                cancellationToken);
        }
        else
        {
            reports = await _financialReportRepository.GetByTradingPartnerIdAsync(
                request.TradingPartnerId, 
                cancellationToken);
        }

        _logger.LogInformation("Found {ReportCount} financial reports for trading partner {CompanyName}",
            reports.Count, tradingPartner.CompanyName);

        // Map to DTOs and calculate growth metrics if requested
        var reportDtos = new List<FinancialReportDto>();
        var reportsList = reports.OrderByDescending(r => r.ReportStartDate).ToList();

        for (int i = 0; i < reportsList.Count; i++)
        {
            var report = reportsList[i];
            var dto = MapToDto(report);

            // Calculate year-over-year growth if requested and previous year data exists
            if (request.IncludeGrowthMetrics && i < reportsList.Count - 1)
            {
                var previousReport = FindPreviousYearReport(reportsList, report, i + 1);
                if (previousReport != null)
                {
                    dto.RevenueGrowth = CalculateGrowthPercentage(report.Revenue, previousReport.Revenue);
                    dto.NetProfitGrowth = CalculateGrowthPercentage(report.NetProfit, previousReport.NetProfit);
                    dto.TotalAssetsGrowth = CalculateGrowthPercentage(report.TotalAssets, previousReport.TotalAssets);
                }
            }

            reportDtos.Add(dto);
        }

        return reportDtos.AsReadOnly();
    }

    private static Core.Entities.FinancialReport? FindPreviousYearReport(
        List<Core.Entities.FinancialReport> reports, 
        Core.Entities.FinancialReport currentReport, 
        int startIndex)
    {
        // Look for a report from the previous year (approximately)
        var targetYear = currentReport.ReportYear - 1;
        
        for (int i = startIndex; i < reports.Count; i++)
        {
            var report = reports[i];
            if (report.ReportYear == targetYear)
            {
                return report;
            }
        }
        
        return null;
    }

    private static decimal? CalculateGrowthPercentage(decimal? current, decimal? previous)
    {
        if (!current.HasValue || !previous.HasValue || previous.Value == 0)
            return null;

        return Math.Round(((current.Value - previous.Value) / Math.Abs(previous.Value)) * 100, 2);
    }

    private static FinancialReportDto MapToDto(Core.Entities.FinancialReport report)
    {
        return new FinancialReportDto
        {
            Id = report.Id,
            TradingPartnerId = report.TradingPartnerId,
            ReportStartDate = report.ReportStartDate,
            ReportEndDate = report.ReportEndDate,
            TotalAssets = report.TotalAssets,
            TotalLiabilities = report.TotalLiabilities,
            NetAssets = report.NetAssets,
            CurrentAssets = report.CurrentAssets,
            CurrentLiabilities = report.CurrentLiabilities,
            Revenue = report.Revenue,
            NetProfit = report.NetProfit,
            OperatingCashFlow = report.OperatingCashFlow,
            CurrentRatio = report.CurrentRatio,
            DebtToAssetRatio = report.DebtToAssetRatio,
            ROE = report.ROE,
            ROA = report.ROA,
            ReportYear = report.ReportYear,
            IsAnnualReport = report.IsAnnualReport,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt,
            CreatedBy = report.CreatedBy,
            UpdatedBy = report.UpdatedBy
        };
    }
}