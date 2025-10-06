using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.PurchaseContracts;

public class UpdatePurchaseContractCommandHandler : IRequestHandler<UpdatePurchaseContractCommand, Unit>
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePurchaseContractCommandHandler(
        IPurchaseContractRepository purchaseContractRepository,
        IUnitOfWork unitOfWork)
    {
        _purchaseContractRepository = purchaseContractRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdatePurchaseContractCommand request, CancellationToken cancellationToken)
    {
        var contract = await _purchaseContractRepository.GetByIdAsync(request.Id, cancellationToken);
        if (contract == null)
            throw new NotFoundException($"Purchase contract with ID {request.Id} not found");

        // Update external contract number if provided
        if (!string.IsNullOrEmpty(request.ExternalContractNumber))
        {
            contract.SetExternalContractNumber(request.ExternalContractNumber, request.UpdatedBy);
        }

        // Update price benchmark if provided
        if (request.PriceBenchmarkId.HasValue)
        {
            contract.SetPriceBenchmark(request.PriceBenchmarkId, request.UpdatedBy);
        }

        // Update quantity if provided
        if (request.Quantity.HasValue && !string.IsNullOrEmpty(request.QuantityUnit))
        {
            var quantityUnit = MapQuantityUnit(request.QuantityUnit);
            var quantity = new Quantity(request.Quantity.Value, quantityUnit);
            contract.UpdateQuantity(quantity, "System");
        }

        // Update ton/barrel ratio
        if (request.TonBarrelRatio.HasValue)
        {
            contract.UpdateTonBarrelRatio(request.TonBarrelRatio.Value);
        }

        // Update pricing information
        if (!string.IsNullOrEmpty(request.PricingType))
        {
            if (request.PricingType == "Fixed" && request.FixedPrice.HasValue)
            {
                var priceFormula = PriceFormula.Fixed(request.FixedPrice.Value);
                var contractValue = Money.Dollar(request.FixedPrice.Value * (request.Quantity ?? contract.ContractQuantity.Value));
                contract.UpdatePricing(priceFormula, contractValue);
            }
            else if (!string.IsNullOrEmpty(request.PricingFormula))
            {
                var priceFormula = PriceFormula.Parse(request.PricingFormula);
                contract.UpdatePricing(priceFormula, contract.ContractValue ?? Money.Dollar(0));
            }
        }

        // Update pricing period
        if (request.PricingPeriodStart.HasValue && request.PricingPeriodEnd.HasValue)
        {
            contract.SetPricingPeriod(request.PricingPeriodStart.Value, request.PricingPeriodEnd.Value);
        }

        // Update laycan dates
        if (request.LaycanStart.HasValue && request.LaycanEnd.HasValue)
        {
            contract.UpdateLaycan(request.LaycanStart.Value, request.LaycanEnd.Value);
        }

        // Update ports
        if (!string.IsNullOrEmpty(request.LoadPort) && !string.IsNullOrEmpty(request.DischargePort))
        {
            contract.UpdatePorts(request.LoadPort, request.DischargePort);
        }

        // Update delivery terms
        if (!string.IsNullOrEmpty(request.DeliveryTerms))
        {
            var deliveryTerms = MapDeliveryTerms(request.DeliveryTerms);
            contract.UpdateDeliveryTerms(deliveryTerms);
        }

        // Update settlement type
        if (!string.IsNullOrEmpty(request.SettlementType))
        {
            var settlementType = MapSettlementType(request.SettlementType);
            contract.UpdateSettlementType(settlementType);
        }

        // Update payment terms
        if (!string.IsNullOrEmpty(request.PaymentTerms) && request.CreditPeriodDays.HasValue)
        {
            contract.UpdatePaymentTerms(request.PaymentTerms, request.CreditPeriodDays.Value);
        }

        // Update prepayment percentage
        if (request.PrepaymentPercentage.HasValue)
        {
            contract.SetPrepaymentPercentage(request.PrepaymentPercentage.Value);
        }

        // Update quality specifications
        if (request.QualitySpecifications != null)
        {
            contract.UpdateQualitySpecifications(request.QualitySpecifications);
        }

        // Update inspection agency
        if (request.InspectionAgency != null)
        {
            contract.UpdateInspectionAgency(request.InspectionAgency);
        }

        // Update notes
        if (request.Notes != null)
        {
            contract.AddNotes(request.Notes);
        }

        // Set updated by
        contract.SetUpdatedBy(request.UpdatedBy);

        // Update in repository
        await _purchaseContractRepository.UpdateAsync(contract, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private static QuantityUnit MapQuantityUnit(string unit)
    {
        return unit.ToUpper() switch
        {
            "MT" => QuantityUnit.MT,
            "BBL" => QuantityUnit.BBL,
            "GAL" => QuantityUnit.GAL,
            _ => throw new ArgumentException($"Invalid quantity unit: {unit}")
        };
    }

    private static DeliveryTerms MapDeliveryTerms(string terms)
    {
        return terms.ToUpper() switch
        {
            "FOB" => DeliveryTerms.FOB,
            "CFR" => DeliveryTerms.CFR,
            "CIF" => DeliveryTerms.CIF,
            "EXW" => DeliveryTerms.EXW,
            "DDP" => DeliveryTerms.DDP,
            _ => throw new ArgumentException($"Invalid delivery terms: {terms}")
        };
    }

    private static SettlementType MapSettlementType(string type)
    {
        return type.ToUpper() switch
        {
            "CONTRACT" => SettlementType.ContractPayment,
            "PARTIAL" => SettlementType.PartialPayment,
            "FINAL" => SettlementType.FinalPayment,
            "ADJUSTMENT" => SettlementType.Adjustment,
            "REFUND" => SettlementType.Refund,
            "PENALTY" => SettlementType.Penalty,
            "INTEREST" => SettlementType.Interest,
            "ADVANCE" => SettlementType.Advance,
            _ => SettlementType.ContractPayment // Default
        };
    }
}