using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.FinancialReports;

public class GetFinancialReportByIdQueryHandler : IRequestHandler<GetFinancialReportByIdQuery, FinancialReportDto?>
{
    private readonly IFinancialReportRepository _financialReportRepository;
    private readonly ILogger<GetFinancialReportByIdQueryHandler> _logger;

    public GetFinancialReportByIdQueryHandler(
        IFinancialReportRepository financialReportRepository,
        ILogger<GetFinancialReportByIdQueryHandler> logger)
    {
        _financialReportRepository = financialReportRepository;
        _logger = logger;
    }

    public async Task<FinancialReportDto?> Handle(GetFinancialReportByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving financial report with ID {ReportId}", request.Id);

        var financialReport = await _financialReportRepository.GetByIdAsync(request.Id, cancellationToken);
        if (financialReport == null)
        {
            _logger.LogWarning("Financial report with ID {ReportId} not found", request.Id);
            return null;
        }

        _logger.LogInformation("Financial report {ReportId} retrieved successfully for trading partner {TradingPartnerId}",
            request.Id, financialReport.TradingPartnerId);

        return MapToDto(financialReport);
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