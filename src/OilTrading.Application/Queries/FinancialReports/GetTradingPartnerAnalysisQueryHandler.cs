using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Queries.FinancialReports;

public class GetTradingPartnerAnalysisQueryHandler : IRequestHandler<GetTradingPartnerAnalysisQuery, TradingPartnerAnalysisDto?>
{
    private readonly IFinancialReportRepository _financialReportRepository;
    private readonly ITradingPartnerRepository _tradingPartnerRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly ILogger<GetTradingPartnerAnalysisQueryHandler> _logger;

    public GetTradingPartnerAnalysisQueryHandler(
        IFinancialReportRepository financialReportRepository,
        ITradingPartnerRepository tradingPartnerRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        ILogger<GetTradingPartnerAnalysisQueryHandler> logger)
    {
        _financialReportRepository = financialReportRepository;
        _tradingPartnerRepository = tradingPartnerRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _logger = logger;
    }

    public async Task<TradingPartnerAnalysisDto?> Handle(GetTradingPartnerAnalysisQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating trading partner analysis for {TradingPartnerId}", request.TradingPartnerId);

        // Get trading partner
        var tradingPartner = await _tradingPartnerRepository.GetByIdAsync(request.TradingPartnerId, cancellationToken);
        if (tradingPartner == null)
        {
            throw new NotFoundException("TradingPartner", request.TradingPartnerId);
        }

        _logger.LogInformation("Analyzing trading partner: {CompanyName} ({CompanyCode})", 
            tradingPartner.CompanyName, tradingPartner.CompanyCode);

        var analysis = new TradingPartnerAnalysisDto
        {
            TradingPartnerId = tradingPartner.Id,
            CompanyName = tradingPartner.CompanyName,
            CompanyCode = tradingPartner.CompanyCode,
            CreditLimit = tradingPartner.CreditLimit,
            CurrentExposure = tradingPartner.CurrentExposure,
            CreditUtilization = tradingPartner.CreditLimit > 0 ? (tradingPartner.CurrentExposure / tradingPartner.CreditLimit * 100) : 0
        };

        // Calculate cooperation volume if requested
        if (request.IncludeCooperationVolume)
        {
            var cooperationVolume = await CalculateCooperationVolumeAsync(request.TradingPartnerId, cancellationToken);
            analysis.TotalCooperationAmount = cooperationVolume.TotalCooperationAmount;
            analysis.TotalCooperationQuantity = cooperationVolume.TotalCooperationQuantity;
        }

        // Get financial reports if requested
        if (request.IncludeFinancialHistory)
        {
            var reports = await _financialReportRepository.GetByTradingPartnerIdAsync(request.TradingPartnerId, cancellationToken);
            var reportsList = reports.OrderByDescending(r => r.ReportStartDate).ToList();

            // Limit the number of reports if specified
            if (request.MaxReportsCount.HasValue && reportsList.Count > request.MaxReportsCount.Value)
            {
                reportsList = reportsList.Take(request.MaxReportsCount.Value).ToList();
            }

            // Map reports to DTOs with growth calculations
            analysis.FinancialReports = MapFinancialReportsWithGrowth(reportsList);

            // Set current financial indicators from the latest report
            if (reportsList.Any())
            {
                var latestReport = reportsList.First();
                analysis.CurrentRatio = latestReport.CurrentRatio;
                analysis.DebtToAssetRatio = latestReport.DebtToAssetRatio;
                analysis.ROE = latestReport.ROE;
                analysis.ROA = latestReport.ROA;
            }
        }

        // Perform risk assessment if requested
        if (request.IncludeRiskAssessment && analysis.FinancialReports.Any())
        {
            var riskAssessment = PerformFinancialRiskAssessment(analysis);
            analysis.FinancialHealthStatus = riskAssessment.HealthStatus;
            analysis.RiskIndicators = riskAssessment.RiskIndicators;
        }

        _logger.LogInformation("Trading partner analysis completed for {CompanyName}. Health status: {HealthStatus}, Reports count: {ReportsCount}",
            tradingPartner.CompanyName, analysis.FinancialHealthStatus, analysis.FinancialReports.Count);

        return analysis;
    }

    private async Task<CooperationVolumeDto> CalculateCooperationVolumeAsync(Guid tradingPartnerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calculating cooperation volume for trading partner {TradingPartnerId}", tradingPartnerId);

        // Get purchase contracts (where trading partner is supplier)
        var purchaseContracts = await _purchaseContractRepository.GetByTradingPartnerAsync(tradingPartnerId, cancellationToken);
        var activePurchases = purchaseContracts.Where(c => c.Status == ContractStatus.Active || c.Status == ContractStatus.Completed).ToList();

        // Get sales contracts (where trading partner is customer)
        var salesContracts = await _salesContractRepository.GetByTradingPartnerAsync(tradingPartnerId, cancellationToken);
        var activeSales = salesContracts.Where(c => c.Status == ContractStatus.Active || c.Status == ContractStatus.Completed).ToList();

        var cooperationVolume = new CooperationVolumeDto();

        // Calculate purchase totals
        foreach (var contract in activePurchases)
        {
            var valueInUsd = ConvertToUsd(contract.ContractValue);
            var quantityInMT = ConvertToMT(contract.ContractQuantity, contract.TonBarrelRatio);

            cooperationVolume.PurchaseAmount += valueInUsd;
            cooperationVolume.PurchaseQuantity += quantityInMT;
        }

        // Calculate sales totals
        foreach (var contract in activeSales)
        {
            var valueInUsd = ConvertToUsd(contract.ContractValue);
            var quantityInMT = ConvertToMT(contract.ContractQuantity, contract.TonBarrelRatio);

            cooperationVolume.SalesAmount += valueInUsd;
            cooperationVolume.SalesQuantity += quantityInMT;
        }

        // Calculate overall totals
        cooperationVolume.TotalCooperationAmount = cooperationVolume.PurchaseAmount + cooperationVolume.SalesAmount;
        cooperationVolume.TotalCooperationQuantity = cooperationVolume.PurchaseQuantity + cooperationVolume.SalesQuantity;
        cooperationVolume.TotalTransactions = activePurchases.Count + activeSales.Count;

        // Find last transaction date
        var allDates = new List<DateTime>();
        allDates.AddRange(activePurchases.Where(c => c.LaycanStart.HasValue).Select(c => c.LaycanStart!.Value));
        allDates.AddRange(activeSales.Where(c => c.LaycanStart.HasValue).Select(c => c.LaycanStart!.Value));
        
        if (allDates.Any())
        {
            cooperationVolume.LastTransactionDate = allDates.Max();
        }

        _logger.LogInformation("Cooperation volume calculated: Total Amount ${Amount:N2}, Total Quantity {Quantity:N2} MT, Transactions {Count}",
            cooperationVolume.TotalCooperationAmount, cooperationVolume.TotalCooperationQuantity, cooperationVolume.TotalTransactions);

        return cooperationVolume;
    }

    private static decimal ConvertToUsd(Money? money)
    {
        if (money == null) return 0;
        
        // For now, assume all values are in USD or use a simple conversion
        // In a real system, you would use a currency conversion service
        return money.Currency.ToUpper() switch
        {
            "USD" => money.Amount,
            "EUR" => money.Amount * 1.1m, // Simplified conversion
            "GBP" => money.Amount * 1.3m, // Simplified conversion
            _ => money.Amount // Default to direct value
        };
    }

    private static decimal ConvertToMT(Quantity quantity, decimal tonBarrelRatio)
    {
        return quantity.Unit switch
        {
            QuantityUnit.MT => quantity.Value,
            QuantityUnit.BBL => quantity.Value / tonBarrelRatio, // Convert barrels to metric tons
            _ => quantity.Value
        };
    }

    private List<FinancialReportDto> MapFinancialReportsWithGrowth(List<FinancialReport> reports)
    {
        var reportDtos = new List<FinancialReportDto>();

        for (int i = 0; i < reports.Count; i++)
        {
            var report = reports[i];
            var dto = MapToFinancialReportDto(report);

            // Calculate year-over-year growth
            if (i < reports.Count - 1)
            {
                var previousReport = FindPreviousYearReport(reports, report, i + 1);
                if (previousReport != null)
                {
                    dto.RevenueGrowth = CalculateGrowthPercentage(report.Revenue, previousReport.Revenue);
                    dto.NetProfitGrowth = CalculateGrowthPercentage(report.NetProfit, previousReport.NetProfit);
                    dto.TotalAssetsGrowth = CalculateGrowthPercentage(report.TotalAssets, previousReport.TotalAssets);
                }
            }

            reportDtos.Add(dto);
        }

        return reportDtos;
    }

    private static FinancialReport? FindPreviousYearReport(List<FinancialReport> reports, FinancialReport currentReport, int startIndex)
    {
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

    private static (string HealthStatus, List<string> RiskIndicators) PerformFinancialRiskAssessment(TradingPartnerAnalysisDto analysis)
    {
        var riskIndicators = new List<string>();
        int riskScore = 0;

        // Assess current ratio (ideal: > 1.5)
        if (analysis.CurrentRatio.HasValue)
        {
            if (analysis.CurrentRatio.Value < 1.0m)
            {
                riskIndicators.Add("Low Current Ratio (< 1.0)");
                riskScore += 3;
            }
            else if (analysis.CurrentRatio.Value < 1.5m)
            {
                riskIndicators.Add("Below Average Current Ratio (< 1.5)");
                riskScore += 1;
            }
        }

        // Assess debt-to-asset ratio (ideal: < 0.6)
        if (analysis.DebtToAssetRatio.HasValue)
        {
            if (analysis.DebtToAssetRatio.Value > 0.8m)
            {
                riskIndicators.Add("High Debt-to-Asset Ratio (> 80%)");
                riskScore += 3;
            }
            else if (analysis.DebtToAssetRatio.Value > 0.6m)
            {
                riskIndicators.Add("Elevated Debt-to-Asset Ratio (> 60%)");
                riskScore += 1;
            }
        }

        // Assess credit utilization
        if (analysis.CreditUtilization > 90)
        {
            riskIndicators.Add("Very High Credit Utilization (> 90%)");
            riskScore += 2;
        }
        else if (analysis.CreditUtilization > 75)
        {
            riskIndicators.Add("High Credit Utilization (> 75%)");
            riskScore += 1;
        }

        // Assess profitability trends
        if (analysis.FinancialReports.Any(r => r.NetProfitGrowth.HasValue && r.NetProfitGrowth.Value < -20))
        {
            riskIndicators.Add("Declining Profitability Trend");
            riskScore += 2;
        }

        // Assess revenue trends
        if (analysis.FinancialReports.Any(r => r.RevenueGrowth.HasValue && r.RevenueGrowth.Value < -15))
        {
            riskIndicators.Add("Revenue Decline Trend");
            riskScore += 2;
        }

        // Determine overall health status based on risk score
        var healthStatus = riskScore switch
        {
            0 => "Excellent",
            1 => "Good",
            2 or 3 => "Fair",
            4 or 5 or 6 => "Poor",
            _ => "Critical"
        };

        return (healthStatus, riskIndicators);
    }

    private static FinancialReportDto MapToFinancialReportDto(FinancialReport report)
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