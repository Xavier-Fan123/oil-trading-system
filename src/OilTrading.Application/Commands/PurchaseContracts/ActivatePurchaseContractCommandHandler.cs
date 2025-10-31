using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.Common;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Application.Services;

namespace OilTrading.Application.Commands.PurchaseContracts;

public class ActivatePurchaseContractCommandHandler : IRequestHandler<ActivatePurchaseContractCommand, Unit>
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ICacheInvalidationService _cacheInvalidationService;
    private readonly IUnitOfWork _unitOfWork;

    public ActivatePurchaseContractCommandHandler(
        IPurchaseContractRepository purchaseContractRepository,
        ICacheInvalidationService cacheInvalidationService,
        IUnitOfWork unitOfWork)
    {
        _purchaseContractRepository = purchaseContractRepository;
        _cacheInvalidationService = cacheInvalidationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ActivatePurchaseContractCommand request, CancellationToken cancellationToken)
    {
        var contract = await _purchaseContractRepository.GetByIdAsync(request.Id, cancellationToken);
        if (contract == null)
            throw new NotFoundException($"Purchase contract with ID {request.Id} not found");

        // Set updated by before activation (for audit trail)
        contract.SetUpdatedBy(request.ActivatedBy);

        // Activate the contract (this will validate all required fields)
        contract.Activate();

        // Update in repository
        await _purchaseContractRepository.UpdateAsync(contract, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate position-related caches since contract is now Active
        // This ensures new contract immediately shows in position calculations
        await _cacheInvalidationService.InvalidatePurchaseContractCacheAsync();
        await _cacheInvalidationService.InvalidatePositionCacheAsync();

        return Unit.Value;
    }
}