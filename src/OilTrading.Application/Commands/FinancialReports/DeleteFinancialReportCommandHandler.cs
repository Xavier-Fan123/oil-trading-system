using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.FinancialReports;

public class DeleteFinancialReportCommandHandler : IRequestHandler<DeleteFinancialReportCommand, bool>
{
    private readonly IFinancialReportRepository _financialReportRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteFinancialReportCommandHandler> _logger;

    public DeleteFinancialReportCommandHandler(
        IFinancialReportRepository financialReportRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteFinancialReportCommandHandler> logger)
    {
        _financialReportRepository = financialReportRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteFinancialReportCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting financial report {ReportId} by user {DeletedBy}", 
            request.Id, request.DeletedBy);

        // Get existing financial report with trading partner information
        var financialReport = await _financialReportRepository.GetByIdWithTradingPartnerAsync(request.Id, cancellationToken);
        if (financialReport == null)
        {
            throw new NotFoundException("FinancialReport", request.Id);
        }

        try
        {
            // Log the deletion details before removing
            _logger.LogInformation("Deleting financial report for trading partner {CompanyName} ({CompanyCode}), period {StartDate} - {EndDate}, reason: {Reason}",
                financialReport.TradingPartner.CompanyName,
                financialReport.TradingPartner.CompanyCode,
                financialReport.ReportStartDate,
                financialReport.ReportEndDate,
                request.DeletionReason ?? "No reason provided");

            // Delete the financial report
            await _financialReportRepository.DeleteAsync(financialReport, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Financial report {ReportId} deleted successfully for trading partner {CompanyName}",
                request.Id, financialReport.TradingPartner.CompanyName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting financial report {ReportId}", request.Id);
            throw;
        }
    }
}