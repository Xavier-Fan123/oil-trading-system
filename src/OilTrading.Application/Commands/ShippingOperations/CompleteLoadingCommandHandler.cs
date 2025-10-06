using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.ShippingOperations;

public class CompleteLoadingCommandHandler : IRequestHandler<CompleteLoadingCommand, Unit>
{
    private readonly IShippingOperationRepository _shippingOperationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteLoadingCommandHandler(
        IShippingOperationRepository shippingOperationRepository,
        IUnitOfWork unitOfWork)
    {
        _shippingOperationRepository = shippingOperationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CompleteLoadingCommand request, CancellationToken cancellationToken)
    {
        var shippingOperation = await _shippingOperationRepository.GetByIdAsync(request.ShippingOperationId, cancellationToken);
        if (shippingOperation == null)
            throw new NotFoundException($"Shipping operation with ID {request.ShippingOperationId} not found");

        // Create actual quantity value object
        var quantityUnit = Enum.Parse<QuantityUnit>(request.ActualQuantityUnit, true);
        var actualQuantity = new Quantity(request.ActualQuantity, quantityUnit);

        shippingOperation.CompletedLoading(
            request.BillOfLadingDate,
            actualQuantity,
            request.UpdatedBy);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}