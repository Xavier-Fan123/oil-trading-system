using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.Common;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.SalesContracts;

public class ActivateSalesContractCommandHandler : IRequestHandler<ActivateSalesContractCommand, Unit>
{
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateSalesContractCommandHandler(
        ISalesContractRepository salesContractRepository,
        IUnitOfWork unitOfWork)
    {
        _salesContractRepository = salesContractRepository;
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
        return Unit.Value;
    }
}