using Microsoft.EntityFrameworkCore;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;
using PartnerRiskLevel = OilTrading.Application.DTOs.RiskLevel;

namespace OilTrading.Api.Services;

/// <summary>
/// Service for calculating trading partner credit exposure and risk assessment
/// </summary>
public class TradingPartnerExposureService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TradingPartnerExposureService> _logger;

    public TradingPartnerExposureService(ApplicationDbContext dbContext, ILogger<TradingPartnerExposureService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get credit exposure and risk level for a specific trading partner
    /// </summary>
    public async Task<TradingPartnerExposureDto?> GetPartnerExposureAsync(Guid tradingPartnerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating exposure for trading partner {TradingPartnerId}", tradingPartnerId);

        var partner = await _dbContext.TradingPartners
            .FirstOrDefaultAsync(p => p.Id == tradingPartnerId, cancellationToken);

        if (partner == null)
        {
            _logger.LogWarning("Trading partner {TradingPartnerId} not found", tradingPartnerId);
            return null;
        }

        return await CalculateExposureForPartnerAsync(partner, cancellationToken);
    }

    /// <summary>
    /// Get exposure for all active trading partners
    /// </summary>
    public async Task<List<TradingPartnerExposureDto>> GetAllPartnersExposureAsync(
        string? sortBy = "riskLevel",
        bool sortDescending = true,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting exposure for all trading partners, sortBy={SortBy}, descending={SortDescending}", sortBy, sortDescending);

        var partners = await _dbContext.TradingPartners
            .Where(p => p.IsActive && !p.IsBlocked)
            .ToListAsync(cancellationToken);

        var exposures = new List<TradingPartnerExposureDto>();

        foreach (var partner in partners)
        {
            var exposure = await CalculateExposureForPartnerAsync(partner, cancellationToken);
            exposures.Add(exposure);
        }

        // Sort by specified field
        exposures = (sortBy?.ToLower()) switch
        {
            "riskLevel" => sortDescending
                ? exposures.OrderByDescending(x => x.RiskLevel).ToList()
                : exposures.OrderBy(x => x.RiskLevel).ToList(),
            "utilizationpercentage" => sortDescending
                ? exposures.OrderByDescending(x => x.CreditUtilizationPercentage).ToList()
                : exposures.OrderBy(x => x.CreditUtilizationPercentage).ToList(),
            "companyname" => sortDescending
                ? exposures.OrderByDescending(x => x.CompanyName).ToList()
                : exposures.OrderBy(x => x.CompanyName).ToList(),
            _ => exposures.OrderByDescending(x => x.RiskLevel).ToList()
        };

        // Apply pagination
        if (pageNumber.HasValue && pageSize.HasValue)
        {
            exposures = exposures
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();
        }

        return exposures;
    }

    /// <summary>
    /// Get trading partners with high or critical risk levels
    /// </summary>
    public async Task<List<TradingPartnerExposureDto>> GetAtRiskPartnersAsync(
        PartnerRiskLevel minimumRiskLevel = PartnerRiskLevel.High,
        CancellationToken cancellationToken = default)
    {
        var allExposures = await GetAllPartnersExposureAsync(cancellationToken: cancellationToken);

        return allExposures
            .Where(x => x.RiskLevel >= minimumRiskLevel)
            .OrderByDescending(x => x.RiskLevel)
            .ThenByDescending(x => x.CreditUtilizationPercentage)
            .ToList();
    }

    /// <summary>
    /// Get detailed settlement breakdown (AP vs AR) for a trading partner
    /// </summary>
    public async Task<PartnerSettlementSummaryDto?> GetPartnerSettlementDetailsAsync(Guid tradingPartnerId, CancellationToken cancellationToken = default)
    {
        var partner = await _dbContext.TradingPartners
            .FirstOrDefaultAsync(p => p.Id == tradingPartnerId, cancellationToken);

        if (partner == null)
        {
            return null;
        }

        // Get all settlements for contracts related to this trading partner
        // We need to find settlements by looking up contracts that involve this partner
        var purchaseContracts = await _dbContext.PurchaseContracts
            .Where(c => c.TradingPartnerId == tradingPartnerId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var salesContracts = await _dbContext.SalesContracts
            .Where(c => c.TradingPartnerId == tradingPartnerId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        // Get purchase settlements (AP - we owe)
        var purchaseSettlements = await _dbContext.Set<ContractSettlement>()
            .Where(s => purchaseContracts.Contains(s.ContractId))
            .ToListAsync(cancellationToken);

        // Get sales settlements (AR - they owe us)
        var salesSettlements = await _dbContext.Set<ContractSettlement>()
            .Where(s => salesContracts.Contains(s.ContractId))
            .ToListAsync(cancellationToken);

        // Calculate unpaid amounts
        var apUnpaid = purchaseSettlements
            .Where(s => s.Status != ContractSettlementStatus.Finalized && s.Status != ContractSettlementStatus.Cancelled)
            .Sum(s => s.TotalSettlementAmount);
        var arUnpaid = salesSettlements
            .Where(s => s.Status != ContractSettlementStatus.Finalized && s.Status != ContractSettlementStatus.Cancelled)
            .Sum(s => s.TotalSettlementAmount);

        var dto = new PartnerSettlementSummaryDto
        {
            TradingPartnerId = partner.Id,
            CompanyName = partner.CompanyName,

            TotalApAmount = purchaseSettlements.Sum(s => s.TotalSettlementAmount),
            UnpaidApAmount = apUnpaid,
            PaidApAmount = purchaseSettlements
                .Where(s => s.Status == ContractSettlementStatus.Finalized)
                .Sum(s => s.TotalSettlementAmount),
            ApSettlementCount = purchaseSettlements.Count,

            TotalArAmount = 0, // Will need separate sales settlements query
            UnpaidArAmount = arUnpaid,
            PaidArAmount = 0,
            ArSettlementCount = 0,

            NetAmount = apUnpaid - arUnpaid,
            NetDirection = apUnpaid > arUnpaid ? "We Owe" : (arUnpaid > apUnpaid ? "They Owe Us" : "Balanced")
        };

        return dto;
    }

    // Private helper method
    private async Task<TradingPartnerExposureDto> CalculateExposureForPartnerAsync(
        TradingPartner partner,
        CancellationToken cancellationToken)
    {
        // Get all contracts for this trading partner (both purchase and sales)
        var partnerContractIds = new HashSet<Guid>();

        // Add purchase contracts
        var purchaseContractIds = await _dbContext.PurchaseContracts
            .Where(c => c.TradingPartnerId == partner.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        foreach (var id in purchaseContractIds)
            partnerContractIds.Add(id);

        // Add sales contracts
        var salesContractIds = await _dbContext.SalesContracts
            .Where(c => c.TradingPartnerId == partner.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        foreach (var id in salesContractIds)
            partnerContractIds.Add(id);

        // Get all unpaid settlements for these contracts
        var unpaidSettlements = await _dbContext.Set<ContractSettlement>()
            .Where(s => s.ContractId != Guid.Empty
                && partnerContractIds.Contains(s.ContractId)
                && s.Status != ContractSettlementStatus.Cancelled
                && s.Status != ContractSettlementStatus.Finalized)
            .ToListAsync(cancellationToken);

        // Calculate totals
        var totalAmount = unpaidSettlements.Sum(s => s.TotalSettlementAmount);
        var currentExposure = totalAmount;
        var creditUtilizationPercentage = partner.CreditLimit > 0
            ? (currentExposure / partner.CreditLimit) * 100
            : 0;

        // Calculate overdue amounts
        var now = DateTime.UtcNow;
        var overdueAmount = unpaidSettlements
            .Where(s => s.ActualPayableDueDate.HasValue && s.ActualPayableDueDate < now)
            .Sum(s => s.TotalSettlementAmount);

        // Calculate settlements due in next 30 days
        var thirtyDaysFromNow = now.AddDays(30);
        var settlementsDueIn30Days = unpaidSettlements
            .Count(s => s.ActualPayableDueDate.HasValue
                && s.ActualPayableDueDate > now
                && s.ActualPayableDueDate <= thirtyDaysFromNow);

        // Determine risk level
        var riskLevel = DetermineRiskLevel(creditUtilizationPercentage, overdueAmount, partner);
        var riskLevelDescription = GetRiskLevelDescription(riskLevel);

        // Create DTO
        var exposureDto = new TradingPartnerExposureDto
        {
            TradingPartnerId = partner.Id,
            CompanyName = partner.CompanyName,
            CompanyCode = partner.CompanyCode,
            PartnerType = partner.PartnerType,

            // Credit
            CreditLimit = partner.CreditLimit,
            AvailableCredit = partner.CreditLimit - currentExposure,
            CurrentExposure = currentExposure,
            CreditUtilizationPercentage = Math.Round(creditUtilizationPercentage, 2),

            // Outstanding
            OutstandingApAmount = totalAmount,
            OutstandingArAmount = 0,
            NetExposure = currentExposure,

            // Overdue
            OverdueApAmount = overdueAmount,
            OverdueArAmount = 0,
            OverdueSettlementCount = unpaidSettlements
                .Count(s => s.ActualPayableDueDate.HasValue && s.ActualPayableDueDate < now),

            // Statistics
            TotalUnpaidSettlements = unpaidSettlements.Count,
            SettlementsDueIn30Days = settlementsDueIn30Days,

            // Risk
            RiskLevel = riskLevel,
            RiskLevelDescription = riskLevelDescription,
            IsOverLimit = currentExposure > partner.CreditLimit,
            IsCreditExpired = partner.CreditLimitValidUntil < DateTime.UtcNow,

            // Status
            IsActive = partner.IsActive,
            IsBlocked = partner.IsBlocked,
            BlockReason = partner.BlockReason,

            // Dates
            CreditLimitValidUntil = partner.CreditLimitValidUntil,
            LastTransactionDate = partner.LastTransactionDate,
            ExposureCalculatedDate = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Trading partner {CompanyName} exposure: Total={Total}, Risk={RiskLevel}",
            partner.CompanyName, totalAmount, riskLevel);

        return exposureDto;
    }

    private PartnerRiskLevel DetermineRiskLevel(decimal creditUtilization, decimal overdueAmount, TradingPartner partner)
    {
        // Credit expired = Critical
        if (partner.CreditLimitValidUntil < DateTime.UtcNow)
            return PartnerRiskLevel.Critical;

        // Over limit with overdue = Critical
        if (creditUtilization > 100 && overdueAmount > 0)
            return PartnerRiskLevel.Critical;

        // Over limit = High
        if (creditUtilization > 100)
            return PartnerRiskLevel.High;

        // 85-100% utilization = High
        if (creditUtilization >= 85)
            return PartnerRiskLevel.High;

        // 60-85% utilization = Medium
        if (creditUtilization >= 60)
            return PartnerRiskLevel.Medium;

        // <60% = Low
        return PartnerRiskLevel.Low;
    }

    private string GetRiskLevelDescription(PartnerRiskLevel riskLevel) => riskLevel switch
    {
        PartnerRiskLevel.Low => "Credit utilization < 60%, no overdue amounts",
        PartnerRiskLevel.Medium => "Credit utilization 60-85%, minimal overdue amounts",
        PartnerRiskLevel.High => "Credit utilization 85-100%, moderate overdue amounts",
        PartnerRiskLevel.Critical => "Credit limit exceeded, significant overdue amounts, or credit expired",
        _ => "Unknown risk level"
    };
}
