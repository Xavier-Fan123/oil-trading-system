using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for PurchaseSettlement entity operations.
/// Handles data access for purchase contract settlements with specialized queries
/// for settlement lookups and financial calculations.
///
/// This implementation is purchase-settlement-specific, ensuring type safety and
/// clean separation from sales settlement logic.
/// </summary>
public class PurchaseSettlementRepository : Repository<PurchaseSettlement>, IPurchaseSettlementRepository
{
    public PurchaseSettlementRepository(ApplicationDbContext context) : base(context) { }

    /// <summary>
    /// Gets settlement by purchase contract ID
    /// </summary>
    public async Task<PurchaseSettlement?> GetByPurchaseContractIdAsync(Guid purchaseContractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.PurchaseContractId == purchaseContractId, cancellationToken);
    }

    /// <summary>
    /// Gets all settlements for a purchase contract (one-to-many relationship)
    /// </summary>
    public async Task<IReadOnlyList<PurchaseSettlement>> GetByPurchaseContractAsync(Guid purchaseContractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => s.PurchaseContractId == purchaseContractId)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlement by external contract number for easy lookup from trading systems
    /// </summary>
    public async Task<PurchaseSettlement?> GetByExternalContractNumberAsync(string externalContractNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.ExternalContractNumber == externalContractNumber, cancellationToken);
    }

    /// <summary>
    /// Gets settlement by document number (B/L or CQ number)
    /// </summary>
    public async Task<PurchaseSettlement?> GetByDocumentNumberAsync(string documentNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.DocumentNumber == documentNumber, cancellationToken);
    }

    /// <summary>
    /// Gets settlements by status for workflow management
    /// </summary>
    public async Task<IReadOnlyList<PurchaseSettlement>> GetByStatusAsync(ContractSettlementStatus status, CancellationToken cancellationToken = default)
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
    public async Task<IReadOnlyList<PurchaseSettlement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
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
    public async Task<IReadOnlyList<PurchaseSettlement>> GetRequiringRecalculationAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => s.Status == ContractSettlementStatus.Draft &&
                       (s.BenchmarkAmount == 0 || s.CalculationQuantityMT == 0))
            .OrderBy(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlements by multiple purchase contract IDs for batch processing
    /// </summary>
    public async Task<IReadOnlyList<PurchaseSettlement>> GetByPurchaseContractsAsync(IEnumerable<Guid> purchaseContractIds, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Where(s => purchaseContractIds.Contains(s.PurchaseContractId))
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlement with charges included for complete settlement information
    /// </summary>
    public async Task<PurchaseSettlement?> GetWithChargesAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.Id == settlementId, cancellationToken);
    }

    /// <summary>
    /// Gets settlement with related purchase contract information
    /// </summary>
    public async Task<PurchaseSettlement?> GetWithPurchaseContractAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.PurchaseContract)
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.Id == settlementId, cancellationToken);
    }

    /// <summary>
    /// Gets settlements with total amounts exceeding a threshold for large transaction monitoring
    /// </summary>
    public async Task<IReadOnlyList<PurchaseSettlement>> GetByMinimumAmountAsync(decimal minimumAmount, string currency = "USD", CancellationToken cancellationToken = default)
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
    public async Task<IReadOnlyList<PurchaseSettlement>> GetByCreationDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
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
    public async Task<IReadOnlyList<PurchaseSettlement>> GetFinalizedSettlementsAsync(CancellationToken cancellationToken = default)
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
    public async Task<(IReadOnlyList<PurchaseSettlement> Items, int Total)> GetPaginatedAsync(
        int pageNumber,
        int pageSize,
        ContractSettlementStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PurchaseSettlement> query = _dbSet.Include(s => s.Charges);

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
    /// Gets pending payments to suppliers (unpaid settlements).
    /// Critical for Accounts Payable (AP) management.
    /// </summary>
    public async Task<IReadOnlyList<PurchaseSettlement>> GetPendingSupplierPaymentAsync(
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
    /// Gets overdue payments to suppliers.
    /// Critical for compliance and supplier relationship management.
    /// </summary>
    public async Task<IReadOnlyList<PurchaseSettlement>> GetOverdueSupplierPaymentAsync(
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
    /// Calculates total payment exposure to a specific supplier.
    /// Critical for supplier credit limit management and risk assessment.
    /// </summary>
    public async Task<decimal> CalculateSupplierPaymentExposureAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.PurchaseContract != null &&
                       s.PurchaseContract.TradingPartnerId == supplierId &&
                       !s.IsFinalized)
            .SumAsync(s => s.TotalSettlementAmount, cancellationToken);
    }

    /// <summary>
    /// Gets all settlements for a specific supplier.
    /// Essential for supplier-level financial analysis.
    /// </summary>
    public async Task<IReadOnlyList<PurchaseSettlement>> GetBySupplierAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Charges)
            .Include(s => s.PurchaseContract)
            .Where(s => s.PurchaseContract != null &&
                       s.PurchaseContract.TradingPartnerId == supplierId)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets settlements in a specific currency.
    /// Important for multi-currency accounting and FX exposure analysis.
    /// </summary>
    public async Task<IReadOnlyList<PurchaseSettlement>> GetByCurrencyAsync(
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
    /// Gets settlement by invoice number for supplier invoice reconciliation.
    /// </summary>
    public async Task<PurchaseSettlement?> GetByInvoiceNumberAsync(
        string invoiceNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("Invoice number cannot be null or empty", nameof(invoiceNumber));

        return await _dbSet
            .Include(s => s.Charges)
            .FirstOrDefaultAsync(s => s.DocumentNumber == invoiceNumber, cancellationToken);
    }
}
