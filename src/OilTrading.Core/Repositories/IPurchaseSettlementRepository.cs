using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

/// <summary>
/// Specialized repository interface for Purchase Settlement operations.
///
/// Design Principle:
/// - Handles ALL purchase contract settlement queries
/// - Type-safe: only returns PurchaseSettlement entities
/// - Business-specific methods for supplier payment management
/// - No generic settlement queries - purchase-specific only
///
/// This interface separates purchase settlement logic from sales settlement logic,
/// enabling clean, focused query methods without ambiguity about payment direction.
/// </summary>
public interface IPurchaseSettlementRepository : IRepository<PurchaseSettlement>
{
    /// <summary>
    /// Gets a settlement by its purchase contract ID.
    /// Optimal when you know the contract and want its settlement.
    /// </summary>
    /// <param name="purchaseContractId">The purchase contract ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement or null if not found</returns>
    Task<PurchaseSettlement?> GetByPurchaseContractIdAsync(
        Guid purchaseContractId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settlements for a purchase contract (one-to-many relationship).
    /// Handles term contracts with multiple delivery periods.
    /// </summary>
    /// <param name="purchaseContractId">The purchase contract ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of settlements, ordered by creation date descending</returns>
    Task<IReadOnlyList<PurchaseSettlement>> GetByPurchaseContractAsync(
        Guid purchaseContractId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a settlement by external contract number.
    /// Critical for integration with external trading systems (e.g., Bloomberg, Reuters).
    /// </summary>
    /// <param name="externalContractNumber">The external contract number from source system</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement or null if not found</returns>
    Task<PurchaseSettlement?> GetByExternalContractNumberAsync(
        string externalContractNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a settlement by invoice number (supplier invoice).
    /// Essential for supplier payment reconciliation.
    /// </summary>
    /// <param name="invoiceNumber">The supplier invoice number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement or null if not found</returns>
    Task<PurchaseSettlement?> GetByInvoiceNumberAsync(
        string invoiceNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a settlement by document number (Bill of Lading or similar).
    /// Used for logistics and shipping reconciliation.
    /// </summary>
    /// <param name="documentNumber">The B/L or shipping document number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement or null if not found</returns>
    Task<PurchaseSettlement?> GetByDocumentNumberAsync(
        string documentNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settlements with a specific status.
    /// Critical for workflow management (Draft → DataEntered → Calculated → Reviewed → Approved → Finalized).
    /// </summary>
    /// <param name="status">The settlement status to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements with the specified status, ordered by creation date descending</returns>
    Task<IReadOnlyList<PurchaseSettlement>> GetByStatusAsync(
        ContractSettlementStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements within a date range.
    /// Essential for financial reporting and period-based reconciliation.
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements created within the date range, ordered by date descending</returns>
    Task<IReadOnlyList<PurchaseSettlement>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements that require recalculation.
    /// Identifies settlements stuck in Draft status with zero amounts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements needing recalculation, ordered by creation date ascending</returns>
    Task<IReadOnlyList<PurchaseSettlement>> GetRequiringRecalculationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements for a batch of purchase contracts.
    /// Optimized for bulk operations and batch reporting.
    /// </summary>
    /// <param name="purchaseContractIds">Collection of purchase contract IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements for the specified contracts</returns>
    Task<IReadOnlyList<PurchaseSettlement>> GetByPurchaseContractsAsync(
        IEnumerable<Guid> purchaseContractIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending payments from suppliers.
    /// Critical for accounts payable (AP) management.
    /// </summary>
    /// <param name="dueDate">Filter for settlements due on or before this date (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements pending supplier payment, ordered by due date</returns>
    Task<IReadOnlyList<PurchaseSettlement>> GetPendingSupplierPaymentAsync(
        DateTime? dueDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overdue payments to suppliers.
    /// Critical for compliance and supplier relationship management.
    /// </summary>
    /// <param name="asOfDate">Check overdue status as of this date (defaults to today)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements with overdue supplier payments</returns>
    Task<IReadOnlyList<PurchaseSettlement>> GetOverdueSupplierPaymentAsync(
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates total outstanding payment exposure to a specific supplier.
    /// Critical for supplier credit limit management.
    /// </summary>
    /// <param name="supplierId">The supplier (trading partner) ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total USD amount owed to supplier</returns>
    Task<decimal> CalculateSupplierPaymentExposureAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements for a specific supplier.
    /// Essential for supplier-level financial analysis and relationship management.
    /// </summary>
    /// <param name="supplierId">The supplier (trading partner) ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All settlements with the specified supplier</returns>
    Task<IReadOnlyList<PurchaseSettlement>> GetBySupplierAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements within a settlement currency.
    /// Important for multi-currency accounting and foreign exchange exposure analysis.
    /// </summary>
    /// <param name="currency">The currency code (e.g., "USD", "EUR")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements in the specified currency</returns>
    Task<IReadOnlyList<PurchaseSettlement>> GetByCurrencyAsync(
        string currency,
        CancellationToken cancellationToken = default);
}
