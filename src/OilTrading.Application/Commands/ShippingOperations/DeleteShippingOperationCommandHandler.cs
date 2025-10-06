using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.ShippingOperations;

public class DeleteShippingOperationCommandHandler : IRequestHandler<DeleteShippingOperationCommand>
{
    private readonly IShippingOperationRepository _shippingOperationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteShippingOperationCommandHandler(
        IShippingOperationRepository shippingOperationRepository,
        IUnitOfWork unitOfWork)
    {
        _shippingOperationRepository = shippingOperationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteShippingOperationCommand request, CancellationToken cancellationToken)
    {
        var shippingOperation = await _shippingOperationRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (shippingOperation == null)
        {
            throw new NotFoundException($"Shipping operation with ID {request.Id} not found");
        }

        // Business rule: Only planned operations can be deleted
        if (shippingOperation.Status != Core.ValueObjects.ShippingStatus.Planned)
        {
            throw new BusinessRuleException("Only planned shipping operations can be deleted");
        }

        await _shippingOperationRepository.DeleteAsync(shippingOperation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}