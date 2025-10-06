using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.Common;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.SalesContracts;

public class UnlinkSalesContractFromPurchaseCommandHandler : IRequestHandler<UnlinkSalesContractFromPurchaseCommand, Unit>
{
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnlinkSalesContractFromPurchaseCommandHandler(
        ISalesContractRepository salesContractRepository,
        IUnitOfWork unitOfWork)
    {
        _salesContractRepository = salesContractRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UnlinkSalesContractFromPurchaseCommand request, CancellationToken cancellationToken)
    {
        var salesContract = await _salesContractRepository.GetByIdAsync(request.SalesContractId, cancellationToken);
        if (salesContract == null)
            throw new NotFoundException($"Sales contract with ID {request.SalesContractId} not found");

        // Unlink the contract only if it's currently linked (idempotent operation)
        if (salesContract.LinkedPurchaseContractId.HasValue)
        {
            salesContract.UnlinkFromPurchaseContract();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}