using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.Common;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.SalesContracts;

public class LinkSalesContractToPurchaseCommandHandler : IRequestHandler<LinkSalesContractToPurchaseCommand, Unit>
{
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LinkSalesContractToPurchaseCommandHandler(
        ISalesContractRepository salesContractRepository,
        IPurchaseContractRepository purchaseContractRepository,
        IUnitOfWork unitOfWork)
    {
        _salesContractRepository = salesContractRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(LinkSalesContractToPurchaseCommand request, CancellationToken cancellationToken)
    {
        // Get sales contract
        var salesContract = await _salesContractRepository.GetByIdAsync(request.SalesContractId, cancellationToken);
        if (salesContract == null)
            throw new NotFoundException($"Sales contract with ID {request.SalesContractId} not found");

        // Get purchase contract with linked sales contracts to check available quantity
        var purchaseContract = await _purchaseContractRepository.GetByIdWithIncludesAsync(
            request.PurchaseContractId,
            new[] { "LinkedSalesContracts" },
            cancellationToken);
        
        if (purchaseContract == null)
            throw new NotFoundException($"Purchase contract with ID {request.PurchaseContractId} not found");

        // Validate business rules
        ValidateLinkingRules(salesContract, purchaseContract);

        // Link the contracts
        salesContract.LinkToPurchaseContract(request.PurchaseContractId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private static void ValidateLinkingRules(
        OilTrading.Core.Entities.SalesContract salesContract,
        OilTrading.Core.Entities.PurchaseContract purchaseContract)
    {
        var errors = new List<string>();

        // Check if purchase contract can be linked
        if (!purchaseContract.CanBeLinkedToSalesContract())
        {
            errors.Add("Purchase contract is not available for linking (must be active with available quantity)");
        }

        // Check if products match
        if (salesContract.ProductId != purchaseContract.ProductId)
        {
            errors.Add("Sales and purchase contracts must have the same product");
        }

        // ✅ VALIDATION: Check if laycan dates overlap for matching
        // This ensures that purchase and sales contracts for the same product have compatible delivery periods
        if (salesContract.LaycanStart.HasValue && purchaseContract.LaycanEnd.HasValue &&
            salesContract.LaycanStart > purchaseContract.LaycanEnd)
        {
            errors.Add($"Sales contract laycan start ({salesContract.LaycanStart:yyyy-MM-dd}) cannot be after " +
                      $"purchase contract laycan end ({purchaseContract.LaycanEnd:yyyy-MM-dd}). " +
                      $"Contracts must have overlapping delivery periods.");
        }

        // Check if sales contract quantity doesn't exceed available purchase quantity
        var availableQuantity = purchaseContract.GetAvailableQuantity();
        if (salesContract.ContractQuantity.Value > availableQuantity.Value)
        {
            errors.Add($"Sales contract quantity ({salesContract.ContractQuantity.Value} {salesContract.ContractQuantity.Unit}) " +
                      $"exceeds available purchase quantity ({availableQuantity.Value} {availableQuantity.Unit})");
        }

        // Check if quantity units are compatible
        if (salesContract.ContractQuantity.Unit != purchaseContract.ContractQuantity.Unit)
        {
            errors.Add("Sales and purchase contracts must have compatible quantity units");
        }

        // Check if sales contract is already linked to another purchase contract
        if (salesContract.LinkedPurchaseContractId.HasValue &&
            salesContract.LinkedPurchaseContractId.Value != purchaseContract.Id)
        {
            errors.Add("Sales contract is already linked to another purchase contract");
        }

        if (errors.Any())
            throw new DomainException($"Cannot link contracts: {string.Join(", ", errors)}");
    }

    /// <summary>
    /// Validates that contract months match for contract linking.
    /// Both months must be either set to the same value, or both can be null (edge case for spot contracts).
    /// Contract months are in YYMM format (e.g., "2511" for Nov 2025, "2512" for Dec 2025)
    ///
    /// Matching Rules:
    /// - "2511" matches "2511" ✅ (same month)
    /// - "2511" does NOT match "2512" ❌ (different months)
    /// - null matches null ✅ (both spot/unspecified - allowed edge case)
    /// - null does NOT match "2511" ❌ (one is spot, one is futures - incompatible)
    ///
    /// Business Context:
    /// For futures/derivatives contracts, the contract month is critical for risk matching.
    /// A purchase of August Brent cannot be hedged with September Brent - these are different products.
    /// </summary>
    private static bool ContractMonthsMatch(string? salesMonth, string? purchaseMonth)
    {
        // Both null = allowed (edge case for spot contracts without fixed delivery month)
        if (string.IsNullOrEmpty(salesMonth) && string.IsNullOrEmpty(purchaseMonth))
            return true;

        // One null, one set = not allowed (cannot match spot with futures)
        if (string.IsNullOrEmpty(salesMonth) || string.IsNullOrEmpty(purchaseMonth))
            return false;

        // Both set = must be identical (exact month match required)
        return salesMonth.Equals(purchaseMonth, StringComparison.OrdinalIgnoreCase);
    }
}