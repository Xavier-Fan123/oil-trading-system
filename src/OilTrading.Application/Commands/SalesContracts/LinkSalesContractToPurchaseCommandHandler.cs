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
}