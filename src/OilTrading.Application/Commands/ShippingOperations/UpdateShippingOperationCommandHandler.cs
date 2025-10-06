using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.ShippingOperations;

public class UpdateShippingOperationCommandHandler : IRequestHandler<UpdateShippingOperationCommand, Unit>
{
    private readonly IShippingOperationRepository _shippingOperationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateShippingOperationCommandHandler(
        IShippingOperationRepository shippingOperationRepository,
        IUnitOfWork unitOfWork)
    {
        _shippingOperationRepository = shippingOperationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateShippingOperationCommand request, CancellationToken cancellationToken)
    {
        var shippingOperation = await _shippingOperationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (shippingOperation == null)
            throw new NotFoundException($"Shipping operation with ID {request.Id} not found");

        // Update vessel details if provided
        if (!string.IsNullOrEmpty(request.VesselName) || 
            !string.IsNullOrEmpty(request.IMONumber) || 
            request.VesselCapacity.HasValue)
        {
            var vesselName = !string.IsNullOrEmpty(request.VesselName) ? request.VesselName : shippingOperation.VesselName;
            shippingOperation.UpdateVesselDetails(
                vesselName,
                request.IMONumber,
                request.VesselCapacity,
                request.UpdatedBy);
        }

        // Update schedule if provided
        if (request.LoadPortETA.HasValue || request.DischargePortETA.HasValue)
        {
            var loadPortETA = request.LoadPortETA ?? shippingOperation.LoadPortETA;
            var dischargePortETA = request.DischargePortETA ?? shippingOperation.DischargePortETA;
            
            shippingOperation.UpdateSchedule(loadPortETA, dischargePortETA, request.UpdatedBy);
        }

        // Update planned quantity if provided
        if (request.PlannedQuantity.HasValue && !string.IsNullOrEmpty(request.PlannedQuantityUnit))
        {
            var quantityUnit = Enum.Parse<QuantityUnit>(request.PlannedQuantityUnit, true);
            var newQuantity = new Quantity(request.PlannedQuantity.Value, quantityUnit);
            shippingOperation.UpdateQuantity(newQuantity, request.UpdatedBy);
        }

        // Update audit information
        shippingOperation.SetUpdatedBy(request.UpdatedBy);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}