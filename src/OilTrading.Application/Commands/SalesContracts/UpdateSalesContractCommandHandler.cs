using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.SalesContracts;

public class UpdateSalesContractCommandHandler : IRequestHandler<UpdateSalesContractCommand, Unit>
{
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSalesContractCommandHandler(
        ISalesContractRepository salesContractRepository,
        IUnitOfWork unitOfWork)
    {
        _salesContractRepository = salesContractRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateSalesContractCommand request, CancellationToken cancellationToken)
    {
        var contract = await _salesContractRepository.GetByIdAsync(request.Id, cancellationToken);
        if (contract == null)
            throw new NotFoundException($"Sales contract with ID {request.Id} not found");

        // Check if contract can be updated
        if (contract.Status == Core.Entities.ContractStatus.Completed || 
            contract.Status == Core.Entities.ContractStatus.Cancelled)
            throw new DomainException($"Cannot update contract in {contract.Status} status");

        // Update external contract number if provided
        if (!string.IsNullOrWhiteSpace(request.ExternalContractNumber))
        {
            contract.SetExternalContractNumber(request.ExternalContractNumber, request.UpdatedBy);
        }

        // Update price benchmark if provided
        if (request.PriceBenchmarkId.HasValue)
        {
            contract.SetPriceBenchmark(request.PriceBenchmarkId, request.UpdatedBy);
        }

        // Update laycan dates if provided
        if (request.LaycanStart.HasValue && request.LaycanEnd.HasValue)
        {
            contract.UpdateLaycan(request.LaycanStart.Value, request.LaycanEnd.Value);
        }

        // Update ports if provided
        if (!string.IsNullOrWhiteSpace(request.LoadPort) && !string.IsNullOrWhiteSpace(request.DischargePort))
        {
            contract.UpdatePorts(request.LoadPort, request.DischargePort);
        }

        // Update pricing information if provided
        if (!string.IsNullOrEmpty(request.PricingType))
        {
            var priceFormula = CreatePriceFormula(request);
            var contractValue = CalculateContractValue(request, contract);
            
            // Calculate profit margin if linked to purchase contract
            Money? profitMargin = null;
            if (contract.LinkedPurchaseContract?.ContractValue != null && contractValue != null)
            {
                profitMargin = contractValue.Subtract(contract.LinkedPurchaseContract.ContractValue);
            }

            contract.UpdatePricing(priceFormula, contractValue, profitMargin);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private static PriceFormula CreatePriceFormula(UpdateSalesContractCommand request)
    {
        if (request.PricingType == "Fixed" && request.FixedPrice.HasValue)
        {
            return PriceFormula.Fixed(request.FixedPrice.Value, "USD");
        }

        if (!string.IsNullOrWhiteSpace(request.PricingFormula))
        {
            return PriceFormula.Index(
                request.PricingFormula,
                PricingMethod.AVG,
                null // No adjustment for now
            );
        }

        throw new DomainException("Either fixed price or pricing formula must be provided");
    }

    private static Money CalculateContractValue(UpdateSalesContractCommand request, Core.Entities.SalesContract contract)
    {
        if (request.PricingType == "Fixed" && request.FixedPrice.HasValue)
        {
            var quantity = request.Quantity ?? contract.ContractQuantity.Value;
            var totalValue = request.FixedPrice.Value * quantity;
            return new Money(totalValue, "USD");
        }

        // For formula-based pricing, we'll set a placeholder value
        // In a real system, this would be calculated based on current market prices
        return new Money(0, "USD");
    }
}