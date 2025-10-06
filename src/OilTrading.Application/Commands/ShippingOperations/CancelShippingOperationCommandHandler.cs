using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.Common;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.ShippingOperations;

public class CancelShippingOperationCommandHandler : IRequestHandler<CancelShippingOperationCommand, Unit>
{
    private readonly IShippingOperationRepository _shippingOperationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelShippingOperationCommandHandler(
        IShippingOperationRepository shippingOperationRepository,
        IUnitOfWork unitOfWork)
    {
        _shippingOperationRepository = shippingOperationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CancelShippingOperationCommand request, CancellationToken cancellationToken)
    {
        var shippingOperation = await _shippingOperationRepository.GetByIdAsync(request.ShippingOperationId, cancellationToken);
        if (shippingOperation == null)
            throw new NotFoundException($"Shipping operation with ID {request.ShippingOperationId} not found");

        shippingOperation.Cancel(request.Reason, request.UpdatedBy);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}