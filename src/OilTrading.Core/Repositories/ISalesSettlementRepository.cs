using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

/// <summary>
/// Specialized repository interface for Sales Settlement operations.
///
/// Design Principle:
/// - Handles ALL sales contract settlement queries
/// - Type-safe: only returns SalesSettlement entities
/// - Business-specific methods for buyer payment collection
/// - No generic settlement queries - sales-specific only
///
/// This interface separates sales settlement logic from purchase settlement logic,
/// enabling clean, focused query methods without ambiguity about payment direction.
/// </summary>
public interface ISalesSettlementRepository : IRepository<SalesSettlement>
{
    /// <summary>
    /// Gets a settlement by its sales contract ID.
    /// Optimal when you know the contract and want its settlement.
    /// </summary>
    /// <param name="salesContractId">The sales contract ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement or null if not found</returns>
    Task<SalesSettlement?> GetBySalesContractIdAsync(
        Guid salesContractId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settlements for a sales contract (one-to-many relationship).
    /// Handles term contracts with multiple shipment periods.
    /// </summary>
    /// <param name="salesContractId">The sales contract ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of settlements, ordered by creation date descending</returns>
    Task<IReadOnlyList<SalesSettlement>> GetBySalesContractAsync(
        Guid salesContractId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a settlement by external contract number.
    /// Critical for integration with external trading systems (e.g., Bloomberg, Reuters).
    /// </summary>
    /// <param name="externalContractNumber">The external contract number from source system</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement or null if not found</returns>
    Task<SalesSettlement?> GetByExternalContractNumberAsync(
        string externalContractNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a settlement by Bill of Lading (B/L) number.
    /// Essential for buyer payment reconciliation and shipping documentation.
    /// </summary>
    /// <param name="blNumber">The Bill of Lading number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement or null if not found</returns>
    Task<SalesSettlement?> GetByBLNumberAsync(
        string blNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a settlement by document number (Invoice or similar).
    /// Used for invoice matching and accounts receivable reconciliation.
    /// </summary>
    /// <param name="documentNumber">The invoice or shipping document number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement or null if not found</returns>
    Task<SalesSettlement?> GetByDocumentNumberAsync(
        string documentNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settlements with a specific status.
    /// Critical for workflow management (Draft → DataEntered → Calculated → Reviewed → Approved → Finalized).
    /// </summary>
    /// <param name="status">The settlement status to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements with the specified status, ordered by creation date descending</returns>
    Task<IReadOnlyList<SalesSettlement>> GetByStatusAsync(
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
    Task<IReadOnlyList<SalesSettlement>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements that require recalculation.
    /// Identifies settlements stuck in Draft status with zero amounts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements needing recalculation, ordered by creation date ascending</returns>
    Task<IReadOnlyList<SalesSettlement>> GetRequiringRecalculationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements for a batch of sales contracts.
    /// Optimized for bulk operations and batch reporting.
    /// </summary>
    /// <param name="salesContractIds">Collection of sales contract IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements for the specified contracts</returns>
    Task<IReadOnlyList<SalesSettlement>> GetBySalesContractsAsync(
        IEnumerable<Guid> salesContractIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets outstanding receivables from buyers.
    /// Critical for accounts receivable (AR) management.
    /// </summary>
    /// <param name="dueDate">Filter for settlements due on or before this date (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements pending buyer payment, ordered by due date</returns>
    Task<IReadOnlyList<SalesSettlement>> GetOutstandingReceivablesAsync(
        DateTime? dueDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overdue payments from buyers.
    /// Critical for compliance and buyer relationship management.
    /// </summary>
    /// <param name="asOfDate">Check overdue status as of this date (defaults to today)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements with overdue buyer payments</returns>
    Task<IReadOnlyList<SalesSettlement>> GetOverdueBuyerPaymentAsync(
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates total outstanding receivable exposure from a specific buyer.
    /// Critical for buyer credit limit management.
    /// </summary>
    /// <param name="buyerId">The buyer (trading partner) ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total USD amount owed by buyer</returns>
    Task<decimal> CalculateBuyerCreditExposureAsync(
        Guid buyerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements for a specific buyer.
    /// Essential for buyer-level financial analysis and relationship management.
    /// </summary>
    /// <param name="buyerId">The buyer (trading partner) ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All settlements with the specified buyer</returns>
    Task<IReadOnlyList<SalesSettlement>> GetByBuyerAsync(
        Guid buyerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements within a settlement currency.
    /// Important for multi-currency accounting and foreign exchange exposure analysis.
    /// </summary>
    /// <param name="currency">The currency code (e.g., "USD", "EUR")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlements in the specified currency</returns>
    Task<IReadOnlyList<SalesSettlement>> GetByCurrencyAsync(
        string currency,
        CancellationToken cancellationToken = default);
}
