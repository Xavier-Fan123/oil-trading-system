using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ContractSettlement entity operations.
/// Provides data access methods for contract settlements with specialized queries
/// for settlement lookups and financial calculations.
/// </summary>
public class ContractSettlementRepository : Repository<ContractSettlement>, IContractSettlementRepository
{
    public ContractSettlementRepository(ApplicationDbContext context) : base(context) { }

    /// <summary>
    /// Gets settlement by contract ID (Purchase or Sales contract)
    /// </summary>
    public async Task<ContractSettlement?> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.ContractId == contractId, cancellationToken);
    }

    /// <summary>
    /// Gets settlement by external contract number for easy lookup from trading systems
    /// </summary>
    public async Task<ContractSettlement?> GetByExternalContractNumberAsync(string externalContractNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.ExternalContractNumber == externalContractNumber, cancellationToken);
    }

    /// <summary>
    /// Gets settlement by document number (B/L or CQ number)
    /// </summary>
    public async Task<ContractSettlement?> GetByDocumentNumberAsync(string documentNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.DocumentNumber == documentNumber, cancellationToken);
    }

    /// <summary>
    /// Gets settlements by status for workflow management
    /// </summary>
    public async Task<IReadOnlyList<ContractSettlement>> GetByStatusAsync(ContractSettlementStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlements by document date range for reporting
    /// </summary>
    public async Task<IReadOnlyList<ContractSettlement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => s.DocumentDate >= startDate && s.DocumentDate <= endDate)
            .OrderByDescending(s => s.DocumentDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlements that require recalculation (have zero amounts or are in draft status)
    /// </summary>
    public async Task<IReadOnlyList<ContractSettlement>> GetRequiringRecalculationAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => s.Status == ContractSettlementStatus.Draft && 
                       (s.BenchmarkAmount == 0 || s.CalculationQuantityMT == 0))
            .OrderBy(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlements by multiple contract IDs for batch processing
    /// </summary>
    public async Task<IReadOnlyList<ContractSettlement>> GetByContractIdsAsync(IEnumerable<Guid> contractIds, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => contractIds.Contains(s.ContractId))
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlement with charges included for complete settlement information
    /// </summary>
    public async Task<ContractSettlement?> GetWithChargesAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.Id == settlementId, cancellationToken);
    }

    /// <summary>
    /// Gets settlement with related contract information (Purchase or Sales contract)
    /// </summary>
    public async Task<ContractSettlement?> GetWithContractAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Include(s => s.PurchaseContract)
            .Include(s => s.SalesContract)
            .FirstOrDefaultAsync(s => s.Id == settlementId, cancellationToken);
    }

    /// <summary>
    /// Gets settlements with total amounts exceeding a threshold for large transaction monitoring
    /// </summary>
    public async Task<IReadOnlyList<ContractSettlement>> GetByMinimumAmountAsync(decimal minimumAmount, string currency = "USD", CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => s.TotalSettlementAmount >= minimumAmount && s.SettlementCurrency == currency)
            .OrderByDescending(s => s.TotalSettlementAmount)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlements by creation date range for audit and reporting
    /// </summary>
    public async Task<IReadOnlyList<ContractSettlement>> GetByCreationDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => s.CreatedDate >= startDate && s.CreatedDate <= endDate)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets finalized settlements for final settlement processing
    /// </summary>
    public async Task<IReadOnlyList<ContractSettlement>> GetFinalizedSettlementsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => s.IsFinalized)
            .OrderByDescending(s => s.FinalizedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlements by multiple statuses for workflow queries
    /// </summary>
    public async Task<IReadOnlyList<ContractSettlement>> GetByStatusesAsync(IEnumerable<ContractSettlementStatus> statuses, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => statuses.Contains(s.Status))
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a settlement exists for a given contract
    /// </summary>
    public async Task<bool> ExistsForContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(s => s.ContractId == contractId, cancellationToken);
    }

    /// <summary>
    /// Checks if a settlement exists for a given document number
    /// </summary>
    public async Task<bool> ExistsForDocumentAsync(string documentNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(s => s.DocumentNumber == documentNumber, cancellationToken);
    }

    /// <summary>
    /// Gets summary statistics for settlements within a date range
    /// </summary>
    public async Task<ContractSettlementSummary> GetSummaryStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var settlements = await _dbSet
            .Where(s => s.DocumentDate >= startDate && s.DocumentDate <= endDate)
            .ToListAsync(cancellationToken);

        var summary = new ContractSettlementSummary
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalCount = settlements.Count,
            TotalAmount = settlements.Sum(s => s.TotalSettlementAmount),
            AverageAmount = settlements.Any() ? settlements.Average(s => s.TotalSettlementAmount) : 0,
            Currency = "USD"
        };

        // Calculate status breakdown
        summary.StatusBreakdown = settlements
            .GroupBy(s => s.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // Calculate document type breakdown
        summary.DocumentTypeBreakdown = settlements
            .GroupBy(s => s.DocumentType)
            .ToDictionary(g => g.Key, g => g.Count());

        return summary;
    }
}