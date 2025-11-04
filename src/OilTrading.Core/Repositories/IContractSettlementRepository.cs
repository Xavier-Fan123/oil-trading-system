using OilTrading.Core.Entities;
using System.Linq.Expressions;

namespace OilTrading.Core.Repositories;

/// <summary>
/// Repository interface for ContractSettlement entity operations.
/// Provides data access methods for contract settlements with specialized queries
/// for settlement lookups and financial calculations.
/// </summary>
public interface IContractSettlementRepository : IRepository<ContractSettlement>
{
    /// <summary>
    /// Gets all settlements for a contract ID (Purchase or Sales contract).
    /// A contract can have multiple settlements (one-to-many relationship).
    /// </summary>
    /// <param name="contractId">Contract ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of settlements for the contract</returns>
    Task<IReadOnlyList<ContractSettlement>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlement by external contract number for easy lookup from trading systems
    /// </summary>
    /// <param name="externalContractNumber">External contract number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement if found</returns>
    Task<ContractSettlement?> GetByExternalContractNumberAsync(string externalContractNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlement by document number (B/L or CQ number)
    /// </summary>
    /// <param name="documentNumber">Document number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement if found</returns>
    Task<ContractSettlement?> GetByDocumentNumberAsync(string documentNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements by status for workflow management
    /// </summary>
    /// <param name="status">Settlement status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of settlements with specified status</returns>
    Task<IReadOnlyList<ContractSettlement>> GetByStatusAsync(ContractSettlementStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements by document date range for reporting
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of settlements within date range</returns>
    Task<IReadOnlyList<ContractSettlement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements that require recalculation (have zero amounts or are in draft status)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of settlements requiring recalculation</returns>
    Task<IReadOnlyList<ContractSettlement>> GetRequiringRecalculationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements by multiple contract IDs for batch processing
    /// </summary>
    /// <param name="contractIds">List of contract IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of settlements for specified contracts</returns>
    Task<IReadOnlyList<ContractSettlement>> GetByContractIdsAsync(IEnumerable<Guid> contractIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlement with charges included for complete settlement information
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement with charges loaded</returns>
    Task<ContractSettlement?> GetWithChargesAsync(Guid settlementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlement with related contract information (Purchase or Sales contract)
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement with contract relationship loaded</returns>
    Task<ContractSettlement?> GetWithContractAsync(Guid settlementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements with total amounts exceeding a threshold for large transaction monitoring
    /// </summary>
    /// <param name="minimumAmount">Minimum settlement amount</param>
    /// <param name="currency">Currency for amount comparison</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of high-value settlements</returns>
    Task<IReadOnlyList<ContractSettlement>> GetByMinimumAmountAsync(decimal minimumAmount, string currency = "USD", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements by creation date range for audit and reporting
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of settlements created within date range</returns>
    Task<IReadOnlyList<ContractSettlement>> GetByCreationDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets finalized settlements for final settlement processing
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of finalized settlements</returns>
    Task<IReadOnlyList<ContractSettlement>> GetFinalizedSettlementsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements by multiple statuses for workflow queries
    /// </summary>
    /// <param name="statuses">List of settlement statuses</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of settlements with any of the specified statuses</returns>
    Task<IReadOnlyList<ContractSettlement>> GetByStatusesAsync(IEnumerable<ContractSettlementStatus> statuses, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a settlement exists for a given contract
    /// </summary>
    /// <param name="contractId">Contract ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if settlement exists</returns>
    Task<bool> ExistsForContractAsync(Guid contractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a settlement exists for a given document number
    /// </summary>
    /// <param name="documentNumber">Document number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if settlement exists</returns>
    Task<bool> ExistsForDocumentAsync(string documentNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets summary statistics for settlements within a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement summary statistics</returns>
    Task<ContractSettlementSummary> GetSummaryStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Summary statistics for contract settlements
/// </summary>
public class ContractSettlementSummary
{
    public int TotalCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal AverageAmount { get; set; }
    public Dictionary<ContractSettlementStatus, int> StatusBreakdown { get; set; } = new();
    public Dictionary<DocumentType, int> DocumentTypeBreakdown { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}