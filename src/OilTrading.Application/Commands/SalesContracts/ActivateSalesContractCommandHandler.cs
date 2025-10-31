using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.Common;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Application.Services;

namespace OilTrading.Application.Commands.SalesContracts;

public class ActivateSalesContractCommandHandler : IRequestHandler<ActivateSalesContractCommand, Unit>
{
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly ICacheInvalidationService _cacheInvalidationService;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateSalesContractCommandHandler(
        ISalesContractRepository salesContractRepository,
        ICacheInvalidationService cacheInvalidationService,
        IUnitOfWork unitOfWork)
    {
        _salesContractRepository = salesContractRepository;
        _cacheInvalidationService = cacheInvalidationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ActivateSalesContractCommand request, CancellationToken cancellationToken)
    {
        var contract = await _salesContractRepository.GetByIdAsync(request.Id, cancellationToken);
        if (contract == null)
            throw new NotFoundException($"Sales contract with ID {request.Id} not found");

        // Activate the contract (this will validate the contract is ready for activation)
        contract.Activate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate position-related caches since contract is now Active
        // This ensures new contract immediately shows in position calculations
        await _cacheInvalidationService.InvalidateSalesContractCacheAsync();
        await _cacheInvalidationService.InvalidatePositionCacheAsync();

        return Unit.Value;
    }
}