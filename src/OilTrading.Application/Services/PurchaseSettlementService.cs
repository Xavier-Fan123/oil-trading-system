using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Services;

/// <summary>
/// Service layer for purchase settlement operations.
/// Handles creation, updates, calculations, and queries for purchase settlements.
/// Implements business logic for the one-to-many relationship between PurchaseContract and PurchaseSettlement.
/// </summary>
public class PurchaseSettlementService
{
    private readonly IRepository<PurchaseContract> _purchaseContractRepository;
    private readonly IRepository<PurchaseSettlement> _settlementRepository;
    private readonly SettlementCalculationEngine _calculationEngine;
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseSettlementService(
        IRepository<PurchaseContract> purchaseContractRepository,
        IRepository<PurchaseSettlement> settlementRepository,
        SettlementCalculationEngine calculationEngine,
        IUnitOfWork unitOfWork)
    {
        _purchaseContractRepository = purchaseContractRepository ?? throw new ArgumentNullException(nameof(purchaseContractRepository));
        _settlementRepository = settlementRepository ?? throw new ArgumentNullException(nameof(settlementRepository));
        _calculationEngine = calculationEngine ?? throw new ArgumentNullException(nameof(calculationEngine));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Creates a new purchase settlement for a purchase contract
    /// Validates contract exists and is in appropriate status
    /// </summary>
    public async Task<PurchaseSettlement> CreateSettlementAsync(
        Guid purchaseContractId,
        string externalContractNumber,
        string documentNumber,
        DocumentType documentType,
        DateTime documentDate,
        string createdBy = "System",
        CancellationToken cancellationToken = default)
    {
        // Validate purchase contract exists
        var contract = await _purchaseContractRepository.GetByIdAsync(purchaseContractId, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase contract with ID {purchaseContractId} not found");

        // Create settlement entity
        var settlement = new PurchaseSettlement(
            purchaseContractId,
            contract.ContractNumber.Value,
            externalContractNumber,
            documentNumber,
            documentType,
            documentDate,
            createdBy);

        // Add to repository
        await _settlementRepository.AddAsync(settlement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settlement;
    }

    /// <summary>
    /// Retrieves a specific settlement with all charges
    /// </summary>
    public async Task<PurchaseSettlement?> GetSettlementAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        return await _settlementRepository.GetByIdAsync(settlementId, cancellationToken);
    }

    /// <summary>
    /// Gets all settlements for a purchase contract (one-to-many)
    /// Specialized query should be handled by CQRS queries or repository implementations
    /// </summary>
    public async Task<IReadOnlyList<PurchaseSettlement>> GetContractSettlementsAsync(
        Guid purchaseContractId,
        CancellationToken cancellationToken = default)
    {
        var allSettlements = await _settlementRepository.GetAllAsync(cancellationToken);
        return allSettlements
            .Where(s => s.PurchaseContractId == purchaseContractId)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Updates settlement quantities after B/L or CQ verification
    /// </summary>
    public async Task<PurchaseSettlement> UpdateQuantitiesAsync(
        Guid settlementId,
        decimal actualQuantityMT,
        decimal actualQuantityBBL,
        string updatedBy = "System",
        CancellationToken cancellationToken = default)
    {
        var settlement = await GetSettlementAsync(settlementId, cancellationToken)
            ?? throw new InvalidOperationException($"Settlement with ID {settlementId} not found");

        // Validate quantities
        if (actualQuantityMT < 0 || actualQuantityBBL < 0)
        {
            throw new ArgumentException("Quantities cannot be negative");
        }

        // Update settlement
        settlement.UpdateActualQuantities(actualQuantityMT, actualQuantityBBL, updatedBy);

        await _settlementRepository.UpdateAsync(settlement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settlement;
    }

    /// <summary>
    /// Updates benchmark price and triggers recalculation
    /// </summary>
    public async Task<PurchaseSettlement> UpdateBenchmarkPriceAsync(
        Guid settlementId,
        decimal benchmarkPrice,
        string priceFormula,
        DateTime pricingStartDate,
        DateTime pricingEndDate,
        string currency = "USD",
        string updatedBy = "System",
        CancellationToken cancellationToken = default)
    {
        var settlement = await GetSettlementAsync(settlementId, cancellationToken)
            ?? throw new InvalidOperationException($"Settlement with ID {settlementId} not found");

        settlement.UpdateBenchmarkPrice(
            benchmarkPrice,
            priceFormula,
            pricingStartDate,
            pricingEndDate,
            currency,
            updatedBy);

        await _settlementRepository.UpdateAsync(settlement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settlement;
    }

    /// <summary>
    /// Calculates settlement amounts (benchmark, adjustment, cargo value, total)
    /// </summary>
    public async Task<PurchaseSettlement> CalculateSettlementAsync(
        Guid settlementId,
        decimal calculationQuantityMT,
        decimal calculationQuantityBBL,
        decimal benchmarkAmount,
        decimal adjustmentAmount,
        string calculationNote = "",
        string updatedBy = "System",
        CancellationToken cancellationToken = default)
    {
        var settlement = await GetSettlementAsync(settlementId, cancellationToken)
            ?? throw new InvalidOperationException($"Settlement with ID {settlementId} not found");

        // Validate calculations
        var chargesList = settlement.Charges?.ToList() ?? new List<SettlementCharge>();
        var validationErrors = _calculationEngine.ValidateCalculationCompletion(
            settlement.ActualQuantityMT,
            settlement.ActualQuantityBBL,
            settlement.BenchmarkPrice,
            benchmarkAmount,
            chargesList);

        if (validationErrors.Any())
        {
            throw new InvalidOperationException(
                $"Settlement calculation validation failed: {string.Join(", ", validationErrors)}");
        }

        // Set calculation quantities
        settlement.SetCalculationQuantities(calculationQuantityMT, calculationQuantityBBL, calculationNote, updatedBy);

        // Calculate and update amounts
        var cargoValue = _calculationEngine.CalculateCargoValue(benchmarkAmount, adjustmentAmount);
        settlement.UpdateCalculationResults(benchmarkAmount, adjustmentAmount, cargoValue, updatedBy);

        // Update status to indicate calculation is complete
        settlement.UpdateStatus(ContractSettlementStatus.Calculated, updatedBy);

        await _settlementRepository.UpdateAsync(settlement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settlement;
    }

    /// <summary>
    /// Adds a charge to the settlement
    /// </summary>
    public async Task<PurchaseSettlement> AddChargeAsync(
        Guid settlementId,
        ChargeType chargeType,
        string description,
        decimal amount,
        string currency = "USD",
        DateTime? incurredDate = null,
        string? referenceDocument = null,
        string? notes = null,
        string addedBy = "System",
        CancellationToken cancellationToken = default)
    {
        var settlement = await GetSettlementAsync(settlementId, cancellationToken)
            ?? throw new InvalidOperationException($"Settlement with ID {settlementId} not found");

        settlement.AddCharge(
            chargeType,
            description,
            amount,
            currency,
            incurredDate,
            referenceDocument,
            notes,
            addedBy);

        await _settlementRepository.UpdateAsync(settlement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settlement;
    }

    /// <summary>
    /// Removes a charge from the settlement
    /// </summary>
    public async Task<PurchaseSettlement> RemoveChargeAsync(
        Guid settlementId,
        Guid chargeId,
        string removedBy = "System",
        CancellationToken cancellationToken = default)
    {
        var settlement = await GetSettlementAsync(settlementId, cancellationToken)
            ?? throw new InvalidOperationException($"Settlement with ID {settlementId} not found");

        settlement.RemoveCharge(chargeId, removedBy);

        await _settlementRepository.UpdateAsync(settlement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settlement;
    }

    /// <summary>
    /// Approves a settlement for finalization
    /// Moves status from Calculated to Approved
    /// </summary>
    public async Task<PurchaseSettlement> ApproveSettlementAsync(
        Guid settlementId,
        string approvedBy = "System",
        CancellationToken cancellationToken = default)
    {
        var settlement = await GetSettlementAsync(settlementId, cancellationToken)
            ?? throw new InvalidOperationException($"Settlement with ID {settlementId} not found");

        if (settlement.Status != ContractSettlementStatus.Calculated)
        {
            throw new InvalidOperationException(
                $"Settlement must be in Calculated status to approve. Current status: {settlement.Status}");
        }

        settlement.UpdateStatus(ContractSettlementStatus.Approved, approvedBy);

        await _settlementRepository.UpdateAsync(settlement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settlement;
    }

    /// <summary>
    /// Finalizes a settlement (locks it for editing)
    /// </summary>
    public async Task<PurchaseSettlement> FinalizeSettlementAsync(
        Guid settlementId,
        string finalizedBy = "System",
        CancellationToken cancellationToken = default)
    {
        var settlement = await GetSettlementAsync(settlementId, cancellationToken)
            ?? throw new InvalidOperationException($"Settlement with ID {settlementId} not found");

        settlement.Finalize(finalizedBy);

        await _settlementRepository.UpdateAsync(settlement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settlement;
    }

    /// <summary>
    /// Gets settlements requiring recalculation
    /// Note: Specialized queries should be implemented via CQRS Query Handlers
    /// </summary>
    public async Task<IReadOnlyList<PurchaseSettlement>> GetRequiringRecalculationAsync(
        CancellationToken cancellationToken = default)
    {
        var allSettlements = await _settlementRepository.GetAllAsync(cancellationToken);
        return allSettlements
            .Where(s => s.Status == ContractSettlementStatus.Draft &&
                       (s.BenchmarkAmount == 0 || s.CalculationQuantityMT == 0))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets settlements by status
    /// Note: Specialized queries should be implemented via CQRS Query Handlers
    /// </summary>
    public async Task<IReadOnlyList<PurchaseSettlement>> GetByStatusAsync(
        ContractSettlementStatus status,
        CancellationToken cancellationToken = default)
    {
        var allSettlements = await _settlementRepository.GetAllAsync(cancellationToken);
        return allSettlements
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedDate)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets settlements within a date range
    /// Note: Specialized queries should be implemented via CQRS Query Handlers
    /// </summary>
    public async Task<IReadOnlyList<PurchaseSettlement>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var allSettlements = await _settlementRepository.GetAllAsync(cancellationToken);
        return allSettlements
            .Where(s => s.DocumentDate >= startDate && s.DocumentDate <= endDate)
            .OrderByDescending(s => s.DocumentDate)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets paginated list of settlements
    /// Note: Proper pagination should be implemented via CQRS Query Handlers
    /// </summary>
    public async Task<(IReadOnlyList<PurchaseSettlement> Items, int Total)> GetPaginatedAsync(
        int pageNumber,
        int pageSize,
        ContractSettlementStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var allSettlements = await _settlementRepository.GetAllAsync(cancellationToken);
        var query = allSettlements.AsEnumerable();

        if (statusFilter.HasValue)
        {
            query = query.Where(s => s.Status == statusFilter.Value);
        }

        var total = query.Count();
        var items = query
            .OrderByDescending(s => s.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList()
            .AsReadOnly();

        return (items, total);
    }

    /// <summary>
    /// Deletes a settlement (soft delete via IsDeleted flag)
    /// </summary>
    public async Task DeleteSettlementAsync(
        Guid settlementId,
        string deletedBy = "System",
        CancellationToken cancellationToken = default)
    {
        var settlement = await GetSettlementAsync(settlementId, cancellationToken)
            ?? throw new InvalidOperationException($"Settlement with ID {settlementId} not found");

        if (settlement.IsFinalized)
        {
            throw new InvalidOperationException("Cannot delete finalized settlement");
        }

        settlement.SoftDelete(deletedBy);

        await _settlementRepository.UpdateAsync(settlement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
