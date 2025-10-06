using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Services;

/// <summary>
/// Service interface for contract settlement calculations and management.
/// Handles mixed-unit pricing (MT and BBL), benchmark price calculations,
/// quantity calculations based on actual B/L data or contractual ratios,
/// and charge management for oil trading contracts.
/// </summary>
public interface ISettlementCalculationService
{
    /// <summary>
    /// Creates or updates a contract settlement with actual quantities from Bill of Lading or Certificate of Quantity.
    /// Supports mixed-unit calculations where pricing can use different units than quantities.
    /// </summary>
    /// <param name="contractId">The contract ID (Purchase or Sales contract)</param>
    /// <param name="documentNumber">B/L or CQ document number</param>
    /// <param name="documentType">Type of document (BillOfLading, QuantityCertificate, etc.)</param>
    /// <param name="actualMT">Actual quantity in metric tons from document</param>
    /// <param name="actualBBL">Actual quantity in barrels from document</param>
    /// <param name="documentDate">Date of the document</param>
    /// <param name="createdBy">User creating/updating the settlement</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created or updated settlement DTO</returns>
    Task<ContractSettlementDto> CreateOrUpdateSettlementAsync(
        Guid contractId,
        string documentNumber,
        DocumentType documentType,
        decimal actualMT,
        decimal actualBBL,
        DateTime documentDate,
        string createdBy = "System",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates benchmark price using various pricing methods (AVG, MIN, MAX, FIRST, LAST, etc.)
    /// from market data for a specific index over a date range.
    /// </summary>
    /// <param name="indexName">Market index name (e.g., "Brent", "WTI", "MGO")</param>
    /// <param name="method">Pricing calculation method</param>
    /// <param name="startDate">Start date of pricing period</param>
    /// <param name="endDate">End date of pricing period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Calculated benchmark price</returns>
    Task<decimal> CalculateBenchmarkPriceAsync(
        string indexName,
        PricingMethod method,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a charge on a settlement (demurrage, despatch, inspection fees, etc.)
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <param name="chargeType">Type of charge</param>
    /// <param name="amount">Charge amount</param>
    /// <param name="description">Charge description</param>
    /// <param name="referenceDocument">Optional reference document number</param>
    /// <param name="addedBy">User adding the charge</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Added or updated charge DTO</returns>
    Task<SettlementChargeDto> AddOrUpdateChargeAsync(
        Guid settlementId,
        ChargeType chargeType,
        decimal amount,
        string description,
        string? referenceDocument = null,
        string addedBy = "System",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates all amounts for a settlement based on current market prices,
    /// quantities, and charges. Updates benchmark amounts, adjustment amounts,
    /// cargo value, and total settlement amount.
    /// </summary>
    /// <param name="settlementId">Settlement ID to recalculate</param>
    /// <param name="updatedBy">User triggering the recalculation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated settlement DTO</returns>
    Task<ContractSettlementDto> RecalculateSettlementAsync(
        Guid settlementId,
        string updatedBy = "System",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlement by external contract number for easy lookup
    /// </summary>
    /// <param name="externalContractNumber">External contract number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement DTO if found</returns>
    Task<ContractSettlementDto?> GetSettlementByExternalContractNumberAsync(
        string externalContractNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlement by contract ID
    /// </summary>
    /// <param name="contractId">Contract ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement DTO if found</returns>
    Task<ContractSettlementDto?> GetSettlementByContractIdAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlement by settlement ID
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement DTO if found</returns>
    Task<ContractSettlementDto?> GetSettlementByIdAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a charge from a settlement
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <param name="chargeId">Charge ID to remove</param>
    /// <param name="removedBy">User removing the charge</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if charge was removed successfully</returns>
    Task<bool> RemoveChargeAsync(
        Guid settlementId,
        Guid chargeId,
        string removedBy = "System",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates settlement status (Draft, DataEntered, Calculated, Reviewed, Approved, Finalized)
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <param name="newStatus">New settlement status</param>
    /// <param name="updatedBy">User updating the status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated settlement DTO</returns>
    Task<ContractSettlementDto> UpdateSettlementStatusAsync(
        Guid settlementId,
        ContractSettlementStatus newStatus,
        string updatedBy = "System",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes a settlement, preventing further modifications
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <param name="finalizedBy">User finalizing the settlement</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Finalized settlement DTO</returns>
    Task<ContractSettlementDto> FinalizeSettlementAsync(
        Guid settlementId,
        string finalizedBy = "System",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settlements for a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of settlement DTOs</returns>
    Task<IEnumerable<ContractSettlementDto>> GetSettlementsByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets settlements by status
    /// </summary>
    /// <param name="status">Settlement status to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of settlement DTOs</returns>
    Task<IEnumerable<ContractSettlementDto>> GetSettlementsByStatusAsync(
        ContractSettlementStatus status,
        CancellationToken cancellationToken = default);
}