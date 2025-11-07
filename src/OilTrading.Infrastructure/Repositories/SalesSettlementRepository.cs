using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SalesSettlement entity operations.
/// Handles data access for sales contract settlements with specialized queries
/// for settlement lookups and financial calculations.
///
/// This implementation is sales-settlement-specific, ensuring type safety and
/// clean separation from purchase settlement logic.
/// </summary>
public class SalesSettlementRepository : Repository<SalesSettlement>, ISalesSettlementRepository
{
    public SalesSettlementRepository(ApplicationDbContext context) : base(context) { }

    /// <summary>
    /// Gets settlement by sales contract ID
    /// </summary>
    public async Task<SalesSettlement?> GetBySalesContractIdAsync(Guid salesContractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.SalesContractId == salesContractId, cancellationToken);
    }

    /// <summary>
    /// Gets all settlements for a sales contract (one-to-many relationship)
    /// </summary>
    public async Task<IReadOnlyList<SalesSettlement>> GetBySalesContractAsync(Guid salesContractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => s.SalesContractId == salesContractId)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlement by external contract number for easy lookup from trading systems
    /// </summary>
    public async Task<SalesSettlement?> GetByExternalContractNumberAsync(string externalContractNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.ExternalContractNumber == externalContractNumber, cancellationToken);
    }

    /// <summary>
    /// Gets settlement by document number (B/L or CQ number)
    /// </summary>
    public async Task<SalesSettlement?> GetByDocumentNumberAsync(string documentNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.DocumentNumber == documentNumber, cancellationToken);
    }

    /// <summary>
    /// Gets settlements by status for workflow management
    /// </summary>
    public async Task<IReadOnlyList<SalesSettlement>> GetByStatusAsync(ContractSettlementStatus status, CancellationToken cancellationToken = default)
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
    public async Task<IReadOnlyList<SalesSettlement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
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
    public async Task<IReadOnlyList<SalesSettlement>> GetRequiringRecalculationAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => s.Status == ContractSettlementStatus.Draft &&
                       (s.BenchmarkAmount == 0 || s.CalculationQuantityMT == 0))
            .OrderBy(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlements by multiple sales contract IDs for batch processing
    /// </summary>
    public async Task<IReadOnlyList<SalesSettlement>> GetBySalesContractsAsync(IEnumerable<Guid> salesContractIds, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => salesContractIds.Contains(s.SalesContractId))
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlement with charges included for complete settlement information
    /// </summary>
    public async Task<SalesSettlement?> GetWithChargesAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.Id == settlementId, cancellationToken);
    }

    /// <summary>
    /// Gets settlement with related sales contract information
    /// </summary>
    public async Task<SalesSettlement?> GetWithSalesContractAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.SalesContract)
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.Id == settlementId, cancellationToken);
    }

    /// <summary>
    /// Gets settlements with total amounts exceeding a threshold for large transaction monitoring
    /// </summary>
    public async Task<IReadOnlyList<SalesSettlement>> GetByMinimumAmountAsync(decimal minimumAmount, string currency = "USD", CancellationToken cancellationToken = default)
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
    public async Task<IReadOnlyList<SalesSettlement>> GetByCreationDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
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
    public async Task<IReadOnlyList<SalesSettlement>> GetFinalizedSettlementsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => s.IsFinalized)
            .OrderByDescending(s => s.FinalizedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets paginated settlements with optional filtering
    /// </summary>
    public async Task<(IReadOnlyList<SalesSettlement> Items, int Total)> GetPaginatedAsync(
        int pageNumber,
        int pageSize,
        ContractSettlementStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<SalesSettlement> query = _dbSet.Include(s => s.Charges);

        if (statusFilter.HasValue)
        {
            query = query.Where(s => s.Status == statusFilter.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <summary>
    /// Gets outstanding receivables from buyers (unpaid settlements).
    /// Critical for Accounts Receivable (AR) management.
    /// </summary>
    public async Task<IReadOnlyList<SalesSettlement>> GetOutstandingReceivablesAsync(
        DateTime? dueDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(s => s.Charges)
            .Where(s => !s.IsFinalized &&
                       (s.Status == ContractSettlementStatus.Calculated ||
                        s.Status == ContractSettlementStatus.Reviewed ||
                        s.Status == ContractSettlementStatus.Approved));

        if (dueDate.HasValue)
        {
            query = query.Where(s => s.CreatedDate <= dueDate);
        }

        return await query
            .OrderBy(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets overdue payments from buyers.
    /// Critical for compliance and buyer relationship management.
    /// </summary>
    public async Task<IReadOnlyList<SalesSettlement>> GetOverdueBuyerPaymentAsync(
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var checkDate = asOfDate ?? DateTime.UtcNow;

        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => !s.IsFinalized &&
                       s.CreatedDate < checkDate &&
                       (s.Status == ContractSettlementStatus.Calculated ||
                        s.Status == ContractSettlementStatus.Reviewed ||
                        s.Status == ContractSettlementStatus.Approved))
            .OrderBy(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Calculates total receivable exposure from a specific buyer.
    /// Critical for buyer credit limit management and risk assessment.
    /// </summary>
    public async Task<decimal> CalculateBuyerCreditExposureAsync(
        Guid buyerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.SalesContract != null &&
                       s.SalesContract.TradingPartnerId == buyerId &&
                       !s.IsFinalized)
            .SumAsync(s => s.TotalSettlementAmount, cancellationToken);
    }

    /// <summary>
    /// Gets all settlements for a specific buyer.
    /// Essential for buyer-level financial analysis.
    /// </summary>
    public async Task<IReadOnlyList<SalesSettlement>> GetByBuyerAsync(
        Guid buyerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Include(s => s.SalesContract)
            .Where(s => s.SalesContract != null &&
                       s.SalesContract.TradingPartnerId == buyerId)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlements in a specific currency.
    /// Important for multi-currency accounting and FX exposure analysis.
    /// </summary>
    public async Task<IReadOnlyList<SalesSettlement>> GetByCurrencyAsync(
        string currency,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency code cannot be null or empty", nameof(currency));

        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => s.SettlementCurrency == currency)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlement by Bill of Lading (B/L) number for shipping reconciliation.
    /// </summary>
    public async Task<SalesSettlement?> GetByBLNumberAsync(
        string blNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blNumber))
            throw new ArgumentException("B/L number cannot be null or empty", nameof(blNumber));

        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.DocumentNumber == blNumber, cancellationToken);
    }
}
