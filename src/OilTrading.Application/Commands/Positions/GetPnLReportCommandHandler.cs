using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Services;
using OilTrading.Application.DTOs;
using System.Diagnostics;

namespace OilTrading.Application.Commands.Positions;

public class GetPnLReportCommandHandler : IRequestHandler<GetPnLReportCommand, PnLReportDto>
{
    private readonly INetPositionService _netPositionService;
    private readonly ILogger<GetPnLReportCommandHandler> _logger;

    public GetPnLReportCommandHandler(
        INetPositionService netPositionService,
        ILogger<GetPnLReportCommandHandler> logger)
    {
        _netPositionService = netPositionService;
        _logger = logger;
    }

    public async Task<PnLReportDto> Handle(GetPnLReportCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Generating P&L report grouped by {GroupBy}", request.GroupBy);

            var report = new PnLReportDto
            {
                ReportDate = DateTime.UtcNow,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                GroupedBy = request.GroupBy,
                GeneratedBy = "System"
            };

            // Get P&L details
            var pnlDetails = await _netPositionService.CalculatePnLAsync(request.AsOfDate, cancellationToken);

            // Apply filters
            if (request.ProductTypes?.Length > 0)
            {
                pnlDetails = pnlDetails.Where(p => request.ProductTypes.Contains(p.ProductType, StringComparer.OrdinalIgnoreCase));
            }

            if (request.ContractTypes?.Length > 0)
            {
                pnlDetails = pnlDetails.Where(p => request.ContractTypes.Contains(p.ContractType, StringComparer.OrdinalIgnoreCase));
            }

            var pnlList = pnlDetails.ToList();

            // Calculate summary
            report.Summary = CalculateSummary(pnlList);

            // Group data according to request
            report.Groups = GroupPnLData(pnlList, request.GroupBy);

            // Include details if requested
            if (request.IncludeDetailBreakdown)
            {
                report.Details = pnlList;
            }

            stopwatch.Stop();
            report.GenerationTime = stopwatch.Elapsed;

            _logger.LogInformation("P&L report generated successfully in {ElapsedMs}ms. Total P&L: {TotalPnL:C}", 
                stopwatch.ElapsedMilliseconds, report.Summary.TotalPnL);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate P&L report");
            throw;
        }
    }

    private PnLReportSummaryDto CalculateSummary(List<PnLDto> pnlDetails)
    {
        return new PnLReportSummaryDto
        {
            TotalUnrealizedPnL = pnlDetails.Sum(p => p.UnrealizedPnL),
            TotalRealizedPnL = pnlDetails.Sum(p => p.RealizedPnL),
            TotalPnL = pnlDetails.Sum(p => p.UnrealizedPnL + p.RealizedPnL),
            LargestGain = pnlDetails.Any() ? pnlDetails.Max(p => p.UnrealizedPnL + p.RealizedPnL) : 0,
            LargestLoss = pnlDetails.Any() ? pnlDetails.Min(p => p.UnrealizedPnL + p.RealizedPnL) : 0,
            TotalContracts = pnlDetails.Count,
            ProfitableContracts = pnlDetails.Count(p => (p.UnrealizedPnL + p.RealizedPnL) > 0),
            LossContracts = pnlDetails.Count(p => (p.UnrealizedPnL + p.RealizedPnL) < 0),
            AveragePnL = pnlDetails.Any() ? pnlDetails.Average(p => p.UnrealizedPnL + p.RealizedPnL) : 0
        };
    }

    private IEnumerable<PnLGroupDto> GroupPnLData(List<PnLDto> pnlDetails, PnLReportGroupBy groupBy)
    {
        return groupBy switch
        {
            PnLReportGroupBy.Product => GroupByProduct(pnlDetails),
            PnLReportGroupBy.Counterparty => GroupByCounterparty(pnlDetails),
            PnLReportGroupBy.ContractType => GroupByContractType(pnlDetails),
            PnLReportGroupBy.Month => GroupByMonth(pnlDetails),
            PnLReportGroupBy.Trader => GroupByTrader(pnlDetails),
            _ => GroupByProduct(pnlDetails)
        };
    }

    private IEnumerable<PnLGroupDto> GroupByProduct(List<PnLDto> pnlDetails)
    {
        return pnlDetails
            .GroupBy(p => p.ProductType)
            .Select(g => new PnLGroupDto
            {
                GroupName = g.Key,
                GroupType = "Product",
                TotalPnL = g.Sum(p => p.UnrealizedPnL + p.RealizedPnL),
                UnrealizedPnL = g.Sum(p => p.UnrealizedPnL),
                RealizedPnL = g.Sum(p => p.RealizedPnL),
                ContractCount = g.Count(),
                TotalQuantity = g.Sum(p => p.Quantity),
                AveragePrice = g.Any() ? g.Average(p => p.ContractPrice) : 0,
                CurrentMarketPrice = g.Any() ? g.Average(p => p.MarketPrice) : 0
            })
            .OrderByDescending(g => Math.Abs(g.TotalPnL));
    }

    private IEnumerable<PnLGroupDto> GroupByCounterparty(List<PnLDto> pnlDetails)
    {
        // For now, we'll group by contract type since we don't have counterparty info in PnLDto
        // In a real implementation, we'd need to join with contract data to get counterparty info
        return GroupByContractType(pnlDetails);
    }

    private IEnumerable<PnLGroupDto> GroupByContractType(List<PnLDto> pnlDetails)
    {
        return pnlDetails
            .GroupBy(p => p.ContractType)
            .Select(g => new PnLGroupDto
            {
                GroupName = g.Key,
                GroupType = "ContractType",
                TotalPnL = g.Sum(p => p.UnrealizedPnL + p.RealizedPnL),
                UnrealizedPnL = g.Sum(p => p.UnrealizedPnL),
                RealizedPnL = g.Sum(p => p.RealizedPnL),
                ContractCount = g.Count(),
                TotalQuantity = g.Sum(p => p.Quantity),
                AveragePrice = g.Any() ? g.Average(p => p.ContractPrice) : 0,
                CurrentMarketPrice = g.Any() ? g.Average(p => p.MarketPrice) : 0
            })
            .OrderByDescending(g => Math.Abs(g.TotalPnL));
    }

    private IEnumerable<PnLGroupDto> GroupByMonth(List<PnLDto> pnlDetails)
    {
        return pnlDetails
            .GroupBy(p => p.AsOfDate.ToString("yyyy-MM"))
            .Select(g => new PnLGroupDto
            {
                GroupName = g.Key,
                GroupType = "Month",
                TotalPnL = g.Sum(p => p.UnrealizedPnL + p.RealizedPnL),
                UnrealizedPnL = g.Sum(p => p.UnrealizedPnL),
                RealizedPnL = g.Sum(p => p.RealizedPnL),
                ContractCount = g.Count(),
                TotalQuantity = g.Sum(p => p.Quantity),
                AveragePrice = g.Any() ? g.Average(p => p.ContractPrice) : 0,
                CurrentMarketPrice = g.Any() ? g.Average(p => p.MarketPrice) : 0
            })
            .OrderBy(g => g.GroupName);
    }

    private IEnumerable<PnLGroupDto> GroupByTrader(List<PnLDto> pnlDetails)
    {
        // Placeholder - would need trader information in PnLDto
        return new List<PnLGroupDto>
        {
            new PnLGroupDto
            {
                GroupName = "All Traders",
                GroupType = "Trader",
                TotalPnL = pnlDetails.Sum(p => p.UnrealizedPnL + p.RealizedPnL),
                UnrealizedPnL = pnlDetails.Sum(p => p.UnrealizedPnL),
                RealizedPnL = pnlDetails.Sum(p => p.RealizedPnL),
                ContractCount = pnlDetails.Count,
                TotalQuantity = pnlDetails.Sum(p => p.Quantity),
                AveragePrice = pnlDetails.Any() ? pnlDetails.Average(p => p.ContractPrice) : 0,
                CurrentMarketPrice = pnlDetails.Any() ? pnlDetails.Average(p => p.MarketPrice) : 0
            }
        };
    }
}