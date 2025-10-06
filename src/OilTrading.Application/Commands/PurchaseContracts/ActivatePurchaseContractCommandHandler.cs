using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.Common;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.PurchaseContracts;

public class ActivatePurchaseContractCommandHandler : IRequestHandler<ActivatePurchaseContractCommand, Unit>
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivatePurchaseContractCommandHandler(
        IPurchaseContractRepository purchaseContractRepository,
        IUnitOfWork unitOfWork)
    {
        _purchaseContractRepository = purchaseContractRepository;
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

        return Unit.Value;
    }
}