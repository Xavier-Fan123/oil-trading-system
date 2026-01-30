using OilTrading.Core.Entities;

namespace OilTrading.Application.Services;

/// <summary>
/// Service interface for generating and managing Deal Reference IDs
/// Deal Reference ID is a business-meaningful identifier that flows through the entire transaction lifecycle
/// Format: DEAL-{YYYY}-{NNNNNN} where YYYY is year and NNNNNN is sequential number
/// </summary>
public interface IDealReferenceIdService
{
    /// <summary>
    /// Generate a new Deal Reference ID for a purchase contract
    /// </summary>
    Task<string> GenerateForPurchaseContractAsync(PurchaseContract contract, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a new Deal Reference ID for a sales contract
    /// Optionally inherits from linked purchase contract if available
    /// </summary>
    Task<string> GenerateForSalesContractAsync(SalesContract contract, string? linkedPurchaseDealId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Propagate Deal Reference ID from contract to settlement
    /// </summary>
    Task PropagateToSettlementAsync(Guid contractId, bool isPurchaseContract, Guid settlementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Propagate Deal Reference ID from contract to shipping operation
    /// </summary>
    Task PropagateToShippingOperationAsync(Guid contractId, bool isPurchaseContract, Guid shippingOperationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the next sequence number for Deal Reference ID generation
    /// </summary>
    Task<int> GetNextSequenceNumberAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a Deal Reference ID format
    /// </summary>
    bool ValidateDealReferenceIdFormat(string dealReferenceId);

    /// <summary>
    /// Parse a Deal Reference ID into its components
    /// </summary>
    (int Year, int Sequence)? ParseDealReferenceId(string dealReferenceId);
}
