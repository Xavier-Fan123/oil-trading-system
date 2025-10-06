using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.ShippingOperations;

public class RecordLiftingOperationCommandHandler : IRequestHandler<RecordLiftingOperationCommand>
{
    private readonly IShippingOperationRepository _shippingOperationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecordLiftingOperationCommandHandler(
        IShippingOperationRepository shippingOperationRepository,
        IUnitOfWork unitOfWork)
    {
        _shippingOperationRepository = shippingOperationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RecordLiftingOperationCommand request, CancellationToken cancellationToken)
    {
        var shippingOperation = await _shippingOperationRepository.GetByIdAsync(request.ShippingOperationId, cancellationToken);
        
        if (shippingOperation == null)
        {
            throw new NotFoundException($"Shipping operation with ID {request.ShippingOperationId} not found");
        }

        // Update lifting operation details based on shipping status
        if (request.NorDate.HasValue && shippingOperation.Status == Core.ValueObjects.ShippingStatus.Planned)
        {
            // Start loading when NOR is provided
            shippingOperation.StartLoading(request.NorDate.Value, request.NorDate.Value, request.UpdatedBy);
        }

        if (request.BillOfLadingDate.HasValue && request.ActualQuantity.HasValue && !string.IsNullOrEmpty(request.ActualQuantityUnit))
        {
            // Complete loading when Bill of Lading date and actual quantity are provided
            if (shippingOperation.Status == Core.ValueObjects.ShippingStatus.Loading)
            {
                var unit = Enum.Parse<QuantityUnit>(request.ActualQuantityUnit);
                var quantity = new Quantity(request.ActualQuantity.Value, unit);
                shippingOperation.CompletedLoading(request.BillOfLadingDate.Value, quantity, request.UpdatedBy);
            }
        }

        if (request.DischargeDate.HasValue && shippingOperation.Status == Core.ValueObjects.ShippingStatus.InTransit)
        {
            // Complete discharge when discharge date is provided
            shippingOperation.CompleteDischarge(request.DischargeDate.Value, request.DischargeDate.Value, request.UpdatedBy);
        }

        // Notes are part of the entity but there's no update method, we'll need to add one or skip this
        // For now, we'll skip updating notes as it requires modifying the entity
        
        // SetUpdatedBy is handled within the methods above

        await _shippingOperationRepository.UpdateAsync(shippingOperation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}