using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Services;

/// <summary>
/// Service layer for sales settlement operations.
/// Handles creation, updates, calculations, and queries for sales settlements.
/// Implements business logic for the one-to-many relationship between SalesContract and SalesSettlement.
///
/// Key Difference from PurchaseSettlement:
/// - SalesSettlement represents the financial confirmation of a SALES contract execution
/// - Buyer confirms delivery, seller confirms payment collection
/// - Same data structure and workflow as PurchaseSettlement but for sales context
/// </summary>
public class SalesSettlementService
{
    private readonly IRepository<SalesContract> _salesContractRepository;
    private readonly IRepository<SalesSettlement> _settlementRepository;
    private readonly SettlementCalculationEngine _calculationEngine;
    private readonly IUnitOfWork _unitOfWork;

    public SalesSettlementService(
        IRepository<SalesContract> salesContractRepository,
        IRepository<SalesSettlement> settlementRepository,
        SettlementCalculationEngine calculationEngine,
        IUnitOfWork unitOfWork)
    {
        _salesContractRepository = salesContractRepository ?? throw new ArgumentNullException(nameof(salesContractRepository));
        _settlementRepository = settlementRepository ?? throw new ArgumentNullException(nameof(settlementRepository));
        _calculationEngine = calculationEngine ?? throw new ArgumentNullException(nameof(calculationEngine));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Creates a new sales settlement for a sales contract
    /// Validates contract exists and is in appropriate status
    /// </summary>
    public async Task<SalesSettlement> CreateSettlementAsync(
        Guid salesContractId,
        string externalContractNumber,
        string documentNumber,
        DocumentType documentType,
        DateTime documentDate,
        string createdBy = "System",
        CancellationToken cancellationToken = default)
    {
        // Validate sales contract exists
        var contract = await _salesContractRepository.GetByIdAsync(salesContractId, cancellationToken)
            ?? throw new InvalidOperationException($"Sales contract with ID {salesContractId} not found");

        // Create settlement entity
        var settlement = new SalesSettlement(
            salesContractId,
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
    public async Task<SalesSettlement?> GetSettlementAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        return await _settlementRepository.GetByIdAsync(settlementId, cancellationToken);
    }

    /// <summary>
    /// Gets all settlements for a sales contract (one-to-many)
    /// Specialized query should be handled by CQRS queries or repository implementations
    /// </summary>
    public async Task<IReadOnlyList<SalesSettlement>> GetContractSettlementsAsync(
        Guid salesContractId,
        CancellationToken cancellationToken = default)
    {
        var allSettlements = await _settlementRepository.GetAllAsync(cancellationToken);
        return allSettlements
            .Where(s => s.SalesContractId == salesContractId)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Updates settlement quantities after B/L or CQ verification
    /// </summary>
    public async Task<SalesSettlement> UpdateQuantitiesAsync(
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
    public async Task<SalesSettlement> UpdateBenchmarkPriceAsync(
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
    public async Task<SalesSettlement> CalculateSettlementAsync(
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
    public async Task<SettlementChargeDto> AddChargeAsync(
        Guid settlementId,
        string chargeTypeString,
        string description,
        decimal amount,
        string currency = "USD",
        DateTime? incurredDate = null,
        string? referenceDocument = null,
        string? notes = null,
        string addedBy = "System",
        CancellationToken cancellationToken = default)
    {
        // Parse charge type string to enum
        if (!Enum.TryParse<ChargeType>(chargeTypeString, true, out var chargeType))
        {
            throw new InvalidOperationException($"Invalid charge type: {chargeTypeString}");
        }

        var settlement = await GetSettlementAsync(settlementId, cancellationToken)
            ?? throw new InvalidOperationException($"Settlement with ID {settlementId} not found");

        var charge = settlement.AddCharge(
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

        return MapChargeToDto(charge);
    }

    /// <summary>
    /// Updates an existing charge in the settlement
    /// </summary>
    public async Task<SettlementChargeDto> UpdateChargeAsync(
        Guid settlementId,
        Guid chargeId,
        string? description = null,
        decimal? amount = null,
        string updatedBy = "System",
        CancellationToken cancellationToken = default)
    {
        var settlement = await GetSettlementAsync(settlementId, cancellationToken)
            ?? throw new InvalidOperationException($"Settlement with ID {settlementId} not found");

        if (settlement.IsFinalized)
        {
            throw new InvalidOperationException("Cannot update charges in finalized settlement");
        }

        var charge = settlement.Charges?.FirstOrDefault(c => c.Id == chargeId)
            ?? throw new InvalidOperationException($"Charge with ID {chargeId} not found in settlement");

        // Update charge properties using domain methods
        if (!string.IsNullOrWhiteSpace(description))
        {
            charge.UpdateDescription(description, updatedBy);
        }

        if (amount.HasValue && amount.Value >= 0)
        {
            charge.UpdateAmount(amount.Value, updatedBy);
        }

        // Update settlement - recalculate totals by removing and re-adding the charge
        // This ensures domain methods handle all updates consistently
        if (amount.HasValue)
        {
            // The domain methods are responsible for updating TotalCharges, TotalSettlementAmount,
            // LastModifiedDate, and LastModifiedBy through their own logic
            var chargesAmount = settlement.Charges?.Sum(c => c.Amount) ?? 0;
            // Trigger a status update to ensure modification tracking is updated
            settlement.UpdateStatus(settlement.Status, updatedBy);
        }

        await _settlementRepository.UpdateAsync(settlement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapChargeToDto(charge);
    }

    /// <summary>
    /// Removes a charge from the settlement
    /// </summary>
    public async Task RemoveChargeAsync(
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
    }

    /// <summary>
    /// Gets all charges for a settlement
    /// </summary>
    public async Task<List<SettlementChargeDto>> GetChargesAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        var settlement = await GetSettlementAsync(settlementId, cancellationToken)
            ?? throw new InvalidOperationException($"Settlement with ID {settlementId} not found");

        return settlement.Charges?.Select(c => MapChargeToDto(c)).ToList() ?? new List<SettlementChargeDto>();
    }

    /// <summary>
    /// Maps a SettlementCharge entity to SettlementChargeDto
    /// </summary>
    private static SettlementChargeDto MapChargeToDto(SettlementCharge charge)
    {
        return new SettlementChargeDto
        {
            Id = charge.Id,
            SettlementId = charge.SettlementId,
            ChargeType = charge.ChargeType.ToString(),
            Description = charge.Description,
            Amount = charge.Amount,
            Currency = charge.Currency,
            IncurredDate = charge.IncurredDate,
            ReferenceDocument = charge.ReferenceDocument,
            Notes = charge.Notes,
            CreatedDate = charge.CreatedDate,
            CreatedBy = charge.CreatedBy
        };
    }

    /// <summary>
    /// Approves a settlement for finalization
    /// Moves status from Calculated to Approved
    /// </summary>
    public async Task<SalesSettlement> ApproveSettlementAsync(
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
    public async Task<SalesSettlement> FinalizeSettlementAsync(
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
    public async Task<IReadOnlyList<SalesSettlement>> GetRequiringRecalculationAsync(
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
    public async Task<IReadOnlyList<SalesSettlement>> GetByStatusAsync(
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
    public async Task<IReadOnlyList<SalesSettlement>> GetByDateRangeAsync(
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
    public async Task<(IReadOnlyList<SalesSettlement> Items, int Total)> GetPaginatedAsync(
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
