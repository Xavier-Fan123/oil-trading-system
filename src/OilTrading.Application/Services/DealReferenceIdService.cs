using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Services;

/// <summary>
/// Service for generating and managing Deal Reference IDs
/// Provides a unified identifier that flows through the entire transaction lifecycle:
/// Contract -> Settlement -> ShippingOperation
/// </summary>
public class DealReferenceIdService : IDealReferenceIdService
{
    private readonly IRepository<PurchaseContract> _purchaseContractRepository;
    private readonly IRepository<SalesContract> _salesContractRepository;
    private readonly IRepository<PurchaseSettlement> _purchaseSettlementRepository;
    private readonly IRepository<SalesSettlement> _salesSettlementRepository;
    private readonly IRepository<ShippingOperation> _shippingOperationRepository;
    private readonly ILogger<DealReferenceIdService> _logger;

    // Deal Reference ID format: DEAL-{YYYY}-{NNNNNN}
    private const string DealIdPrefix = "DEAL";
    private static readonly Regex DealIdPattern = new(@"^DEAL-(\d{4})-(\d{6})$", RegexOptions.Compiled);

    public DealReferenceIdService(
        IRepository<PurchaseContract> purchaseContractRepository,
        IRepository<SalesContract> salesContractRepository,
        IRepository<PurchaseSettlement> purchaseSettlementRepository,
        IRepository<SalesSettlement> salesSettlementRepository,
        IRepository<ShippingOperation> shippingOperationRepository,
        ILogger<DealReferenceIdService> logger)
    {
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _purchaseSettlementRepository = purchaseSettlementRepository;
        _salesSettlementRepository = salesSettlementRepository;
        _shippingOperationRepository = shippingOperationRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GenerateForPurchaseContractAsync(
        PurchaseContract contract,
        CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var sequence = await GetNextSequenceNumberAsync(year, cancellationToken);
        var dealId = FormatDealReferenceId(year, sequence);

        _logger.LogInformation(
            "Generated Deal Reference ID {DealId} for Purchase Contract {ContractId}",
            dealId, contract.Id);

        return dealId;
    }

    /// <inheritdoc />
    public async Task<string> GenerateForSalesContractAsync(
        SalesContract contract,
        string? linkedPurchaseDealId = null,
        CancellationToken cancellationToken = default)
    {
        // If linked to a purchase contract, inherit its Deal ID with suffix
        if (!string.IsNullOrEmpty(linkedPurchaseDealId) && ValidateDealReferenceIdFormat(linkedPurchaseDealId))
        {
            // For linked sales, append "-S" to indicate it's a sales contract linked to purchase
            var linkedDealId = $"{linkedPurchaseDealId}-S";

            _logger.LogInformation(
                "Generated linked Deal Reference ID {DealId} for Sales Contract {ContractId} (linked to {PurchaseDealId})",
                linkedDealId, contract.Id, linkedPurchaseDealId);

            return linkedDealId;
        }

        // Generate new standalone Deal ID for unlinked sales contract
        var year = DateTime.UtcNow.Year;
        var sequence = await GetNextSequenceNumberAsync(year, cancellationToken);
        var dealId = FormatDealReferenceId(year, sequence);

        _logger.LogInformation(
            "Generated standalone Deal Reference ID {DealId} for Sales Contract {ContractId}",
            dealId, contract.Id);

        return dealId;
    }

    /// <inheritdoc />
    public async Task PropagateToSettlementAsync(
        Guid contractId,
        bool isPurchaseContract,
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        string? dealReferenceId = null;

        if (isPurchaseContract)
        {
            var contract = await _purchaseContractRepository.GetByIdAsync(contractId, cancellationToken);
            if (contract != null)
            {
                dealReferenceId = contract.DealReferenceId;
            }

            if (!string.IsNullOrEmpty(dealReferenceId))
            {
                var settlement = await _purchaseSettlementRepository.GetByIdAsync(settlementId, cancellationToken);
                if (settlement != null)
                {
                    settlement.SetDealReferenceId(dealReferenceId, "System");
                    await _purchaseSettlementRepository.UpdateAsync(settlement, cancellationToken);

                    _logger.LogInformation(
                        "Propagated Deal Reference ID {DealId} to Purchase Settlement {SettlementId}",
                        dealReferenceId, settlementId);
                }
            }
        }
        else
        {
            var contract = await _salesContractRepository.GetByIdAsync(contractId, cancellationToken);
            if (contract != null)
            {
                dealReferenceId = contract.DealReferenceId;
            }

            if (!string.IsNullOrEmpty(dealReferenceId))
            {
                var settlement = await _salesSettlementRepository.GetByIdAsync(settlementId, cancellationToken);
                if (settlement != null)
                {
                    settlement.SetDealReferenceId(dealReferenceId, "System");
                    await _salesSettlementRepository.UpdateAsync(settlement, cancellationToken);

                    _logger.LogInformation(
                        "Propagated Deal Reference ID {DealId} to Sales Settlement {SettlementId}",
                        dealReferenceId, settlementId);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task PropagateToShippingOperationAsync(
        Guid contractId,
        bool isPurchaseContract,
        Guid shippingOperationId,
        CancellationToken cancellationToken = default)
    {
        string? dealReferenceId = null;

        if (isPurchaseContract)
        {
            var contract = await _purchaseContractRepository.GetByIdAsync(contractId, cancellationToken);
            dealReferenceId = contract?.DealReferenceId;
        }
        else
        {
            var contract = await _salesContractRepository.GetByIdAsync(contractId, cancellationToken);
            dealReferenceId = contract?.DealReferenceId;
        }

        if (!string.IsNullOrEmpty(dealReferenceId))
        {
            var shippingOp = await _shippingOperationRepository.GetByIdAsync(shippingOperationId, cancellationToken);
            if (shippingOp != null)
            {
                shippingOp.SetDealReferenceId(dealReferenceId, "System");
                await _shippingOperationRepository.UpdateAsync(shippingOp, cancellationToken);

                _logger.LogInformation(
                    "Propagated Deal Reference ID {DealId} to Shipping Operation {ShippingOperationId}",
                    dealReferenceId, shippingOperationId);
            }
        }
    }

    /// <inheritdoc />
    public async Task<int> GetNextSequenceNumberAsync(int year, CancellationToken cancellationToken = default)
    {
        // Get the max sequence number from all contracts for the given year
        var yearPrefix = $"{DealIdPrefix}-{year}-";

        // Query purchase contracts
        var purchaseContracts = await _purchaseContractRepository.GetAllAsync(cancellationToken);
        var maxPurchaseSeq = purchaseContracts
            .Where(c => !string.IsNullOrEmpty(c.DealReferenceId) && c.DealReferenceId.StartsWith(yearPrefix))
            .Select(c => ParseDealReferenceId(c.DealReferenceId))
            .Where(parsed => parsed.HasValue)
            .Select(parsed => parsed!.Value.Sequence)
            .DefaultIfEmpty(0)
            .Max();

        // Query sales contracts
        var salesContracts = await _salesContractRepository.GetAllAsync(cancellationToken);
        var maxSalesSeq = salesContracts
            .Where(c => !string.IsNullOrEmpty(c.DealReferenceId) && c.DealReferenceId.StartsWith(yearPrefix))
            .Select(c => ParseDealReferenceId(c.DealReferenceId.Replace("-S", "")))
            .Where(parsed => parsed.HasValue)
            .Select(parsed => parsed!.Value.Sequence)
            .DefaultIfEmpty(0)
            .Max();

        var maxSequence = Math.Max(maxPurchaseSeq, maxSalesSeq);
        return maxSequence + 1;
    }

    /// <inheritdoc />
    public bool ValidateDealReferenceIdFormat(string dealReferenceId)
    {
        if (string.IsNullOrWhiteSpace(dealReferenceId))
            return false;

        // Handle linked sales format (DEAL-YYYY-NNNNNN-S)
        var cleanId = dealReferenceId.EndsWith("-S")
            ? dealReferenceId[..^2]
            : dealReferenceId;

        return DealIdPattern.IsMatch(cleanId);
    }

    /// <inheritdoc />
    public (int Year, int Sequence)? ParseDealReferenceId(string dealReferenceId)
    {
        if (string.IsNullOrWhiteSpace(dealReferenceId))
            return null;

        // Handle linked sales format
        var cleanId = dealReferenceId.EndsWith("-S")
            ? dealReferenceId[..^2]
            : dealReferenceId;

        var match = DealIdPattern.Match(cleanId);
        if (!match.Success)
            return null;

        var year = int.Parse(match.Groups[1].Value);
        var sequence = int.Parse(match.Groups[2].Value);

        return (year, sequence);
    }

    private static string FormatDealReferenceId(int year, int sequence)
    {
        return $"{DealIdPrefix}-{year}-{sequence:D6}";
    }
}
