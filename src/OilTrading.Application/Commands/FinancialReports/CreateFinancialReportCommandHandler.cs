using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.Common;

namespace OilTrading.Application.Commands.FinancialReports;

public class CreateFinancialReportCommandHandler : IRequestHandler<CreateFinancialReportCommand, FinancialReportDto>
{
    private readonly IFinancialReportRepository _financialReportRepository;
    private readonly ITradingPartnerRepository _tradingPartnerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateFinancialReportCommandHandler> _logger;

    public CreateFinancialReportCommandHandler(
        IFinancialReportRepository financialReportRepository,
        ITradingPartnerRepository tradingPartnerRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateFinancialReportCommandHandler> logger)
    {
        _financialReportRepository = financialReportRepository;
        _tradingPartnerRepository = tradingPartnerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FinancialReportDto> Handle(CreateFinancialReportCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating financial report for trading partner {TradingPartnerId} for period {StartDate} - {EndDate}",
            request.TradingPartnerId, request.ReportStartDate, request.ReportEndDate);

        // Validate trading partner exists and is active
        var tradingPartner = await _tradingPartnerRepository.GetByIdAsync(request.TradingPartnerId, cancellationToken);
        if (tradingPartner == null)
        {
            throw new NotFoundException("TradingPartner", request.TradingPartnerId);
        }

        if (!tradingPartner.IsActive)
        {
            throw new BusinessRuleException(
                "INACTIVE_TRADING_PARTNER",
                $"Cannot create financial report for inactive trading partner: {tradingPartner.CompanyName}",
                new { TradingPartnerId = request.TradingPartnerId, CompanyName = tradingPartner.CompanyName }
            );
        }

        // Check for overlapping periods
        var hasOverlapping = await _financialReportRepository.HasOverlappingReportAsync(
            request.TradingPartnerId,
            request.ReportStartDate,
            request.ReportEndDate,
            null,
            cancellationToken);

        if (hasOverlapping)
        {
            throw new BusinessRuleException(
                "OVERLAPPING_REPORT_PERIOD",
                $"A financial report already exists for trading partner {tradingPartner.CompanyName} that overlaps with the specified period",
                new
                {
                    TradingPartnerId = request.TradingPartnerId,
                    StartDate = request.ReportStartDate,
                    EndDate = request.ReportEndDate,
                    CompanyName = tradingPartner.CompanyName
                }
            );
        }

        try
        {
            // Create financial report entity
            var financialReport = new FinancialReport(
                request.TradingPartnerId,
                request.ReportStartDate,
                request.ReportEndDate);

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

            // Set audit fields
            financialReport.SetCreated(request.CreatedBy ?? "System");

            // Add to repository
            await _financialReportRepository.AddAsync(financialReport, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Financial report created successfully with ID {ReportId} for trading partner {CompanyName}",
                financialReport.Id, tradingPartner.CompanyName);

            // Return DTO
            return MapToDto(financialReport, tradingPartner);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed while creating financial report for trading partner {TradingPartnerId}", request.TradingPartnerId);
            throw new BusinessRuleException("DOMAIN_VALIDATION_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating financial report for trading partner {TradingPartnerId}", request.TradingPartnerId);
            throw;
        }
    }

    private static FinancialReportDto MapToDto(FinancialReport report, TradingPartner? tradingPartner = null)
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