using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.Common;

namespace OilTrading.Application.Commands.FinancialReports;

public class UpdateFinancialReportCommandHandler : IRequestHandler<UpdateFinancialReportCommand, FinancialReportDto>
{
    private readonly IFinancialReportRepository _financialReportRepository;
    private readonly ITradingPartnerRepository _tradingPartnerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateFinancialReportCommandHandler> _logger;

    public UpdateFinancialReportCommandHandler(
        IFinancialReportRepository financialReportRepository,
        ITradingPartnerRepository tradingPartnerRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateFinancialReportCommandHandler> logger)
    {
        _financialReportRepository = financialReportRepository;
        _tradingPartnerRepository = tradingPartnerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FinancialReportDto> Handle(UpdateFinancialReportCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating financial report {ReportId} for period {StartDate} - {EndDate}",
            request.Id, request.ReportStartDate, request.ReportEndDate);

        // Get existing financial report
        var financialReport = await _financialReportRepository.GetByIdWithTradingPartnerAsync(request.Id, cancellationToken);
        if (financialReport == null)
        {
            throw new NotFoundException("FinancialReport", request.Id);
        }

        // Validate trading partner is still active
        if (!financialReport.TradingPartner.IsActive)
        {
            throw new BusinessRuleException(
                "INACTIVE_TRADING_PARTNER",
                $"Cannot update financial report for inactive trading partner: {financialReport.TradingPartner.CompanyName}",
                new { TradingPartnerId = financialReport.TradingPartnerId, CompanyName = financialReport.TradingPartner.CompanyName }
            );
        }

        // Check for overlapping periods (excluding current report)
        var hasOverlapping = await _financialReportRepository.HasOverlappingReportAsync(
            financialReport.TradingPartnerId,
            request.ReportStartDate,
            request.ReportEndDate,
            request.Id,
            cancellationToken);

        if (hasOverlapping)
        {
            throw new BusinessRuleException(
                "OVERLAPPING_REPORT_PERIOD",
                $"Another financial report exists for trading partner {financialReport.TradingPartner.CompanyName} that overlaps with the specified period",
                new
                {
                    TradingPartnerId = financialReport.TradingPartnerId,
                    StartDate = request.ReportStartDate,
                    EndDate = request.ReportEndDate,
                    CompanyName = financialReport.TradingPartner.CompanyName
                }
            );
        }

        try
        {
            // Update report period
            financialReport.UpdateReportPeriod(request.ReportStartDate, request.ReportEndDate);

            // Update financial position
            financialReport.UpdateFinancialPosition(
                request.TotalAssets,
                request.TotalLiabilities,
                request.NetAssets,
                request.CurrentAssets,
                request.CurrentLiabilities);

            // Update performance data
            financialReport.UpdatePerformanceData(
                request.Revenue,
                request.NetProfit,
                request.OperatingCashFlow);

            // Update audit fields
            financialReport.SetUpdatedBy(request.UpdatedBy ?? "System");

            // Update in repository
            await _financialReportRepository.UpdateAsync(financialReport, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Financial report {ReportId} updated successfully for trading partner {CompanyName}",
                financialReport.Id, financialReport.TradingPartner.CompanyName);

            // Return DTO
            return MapToDto(financialReport);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed while updating financial report {ReportId}", request.Id);
            throw new BusinessRuleException("DOMAIN_VALIDATION_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating financial report {ReportId}", request.Id);
            throw;
        }
    }

    private static FinancialReportDto MapToDto(FinancialReport report)
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