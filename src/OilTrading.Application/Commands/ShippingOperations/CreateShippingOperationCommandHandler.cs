using MediatR;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Application.Services;

namespace OilTrading.Application.Commands.ShippingOperations;

public class CreateShippingOperationCommandHandler : IRequestHandler<CreateShippingOperationCommand, Guid>
{
    private readonly IShippingOperationRepository _shippingOperationRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateShippingOperationCommandHandler(
        IShippingOperationRepository shippingOperationRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IUnitOfWork unitOfWork)
    {
        _shippingOperationRepository = shippingOperationRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateShippingOperationCommand request, CancellationToken cancellationToken)
    {
        // Validate contract exists and is active
        var contract = await ValidateContract(request.ContractId, cancellationToken);

        // Generate shipping number
        var shippingNumber = await GenerateShippingNumber(cancellationToken);

        // Create quantity value object
        var quantityUnit = Enum.Parse<QuantityUnit>(request.PlannedQuantityUnit, true);
        var plannedQuantity = new Quantity(request.PlannedQuantity, quantityUnit);

        // Use contract ports if not specified
        var loadPort = !string.IsNullOrEmpty(request.LoadPort) ? request.LoadPort : contract.LoadPort;
        var dischargePort = !string.IsNullOrEmpty(request.DischargePort) ? request.DischargePort : contract.DischargePort;

        // Create shipping operation
        var shippingOperation = new ShippingOperation(
            shippingNumber,
            request.ContractId,
            request.VesselName,
            plannedQuantity,
            request.LoadPortETA,
            request.DischargePortETA,
            loadPort,
            dischargePort);

        // Set additional vessel details if provided
        if (!string.IsNullOrEmpty(request.IMONumber) || 
            request.VesselCapacity.HasValue ||
            !string.IsNullOrEmpty(request.ChartererName) ||
            !string.IsNullOrEmpty(request.ShippingAgent))
        {
            shippingOperation.UpdateVesselDetails(
                request.VesselName, 
                request.IMONumber, 
                request.VesselCapacity, 
                request.CreatedBy);

            // Update additional properties using reflection or direct access
            // For now, we'll set them through the constructor or update methods
        }

        // Set audit information
        shippingOperation.SetCreatedBy(request.CreatedBy);

        // Add to repository
        await _shippingOperationRepository.AddAsync(shippingOperation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return shippingOperation.Id;
    }

    private async Task<dynamic> ValidateContract(Guid contractId, CancellationToken cancellationToken)
    {
        // Try to find purchase contract first
        var purchaseContract = await _purchaseContractRepository.GetByIdAsync(contractId, cancellationToken);
        if (purchaseContract != null)
        {
            // Allow shipping operations to be created for Draft contracts too
            // This supports planning purposes where shipping operations can be pre-arranged before contract activation
            if (purchaseContract.Status != ContractStatus.Active &&
                purchaseContract.Status != ContractStatus.Draft &&
                purchaseContract.Status != ContractStatus.PendingApproval)
                throw new DomainException($"Purchase contract {purchaseContract.ContractNumber.Value} cannot have shipping operations in {purchaseContract.Status} status");
            return purchaseContract;
        }

        // Try to find sales contract
        var salesContract = await _salesContractRepository.GetByIdAsync(contractId, cancellationToken);
        if (salesContract != null)
        {
            // Allow shipping operations to be created for Draft contracts too
            // This supports planning purposes where shipping operations can be pre-arranged before contract activation
            if (salesContract.Status != ContractStatus.Active &&
                salesContract.Status != ContractStatus.Draft &&
                salesContract.Status != ContractStatus.PendingApproval)
                throw new DomainException($"Sales contract {salesContract.ContractNumber.Value} cannot have shipping operations in {salesContract.Status} status");
            return salesContract;
        }

        throw new NotFoundException($"Contract with ID {contractId} not found");
    }

    private async Task<string> GenerateShippingNumber(CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;
        var prefix = $"SHIP-{currentYear}";
        
        var lastShipping = await _shippingOperationRepository.GetLastShippingByYearAsync(currentYear, cancellationToken);
        
        var nextSerial = 1;
        if (lastShipping != null)
        {
            // Extract serial number from shipping number (format: SHIP-YYYY-NNNN)
            var parts = lastShipping.ShippingNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastSerial))
            {
                nextSerial = lastSerial + 1;
            }
        }

        return $"{prefix}-{nextSerial:D4}";
    }
}