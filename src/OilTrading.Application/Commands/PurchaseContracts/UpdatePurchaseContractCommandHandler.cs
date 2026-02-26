using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.PurchaseContracts;

public class UpdatePurchaseContractCommandHandler : IRequestHandler<UpdatePurchaseContractCommand, Unit>
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ITradingPartnerRepository _tradingPartnerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePurchaseContractCommandHandler(
        IPurchaseContractRepository purchaseContractRepository,
        ITradingPartnerRepository tradingPartnerRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _purchaseContractRepository = purchaseContractRepository;
        _tradingPartnerRepository = tradingPartnerRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdatePurchaseContractCommand request, CancellationToken cancellationToken)
    {
        var contract = await _purchaseContractRepository.GetByIdAsync(request.Id, cancellationToken);
        if (contract == null)
            throw new NotFoundException($"Purchase contract with ID {request.Id} not found");

        if (request.SupplierId.HasValue || request.ProductId.HasValue || request.TraderId.HasValue)
        {
            var supplierId = request.SupplierId ?? contract.TradingPartnerId;
            var productId = request.ProductId ?? contract.ProductId;
            var traderId = request.TraderId ?? contract.TraderId;

            var supplier = await _tradingPartnerRepository.GetByIdAsync(supplierId, cancellationToken);
            if (supplier == null)
                throw new NotFoundException($"Supplier with ID {supplierId} not found");

            if (supplier.Type == TradingPartnerType.Customer || supplier.Type == TradingPartnerType.EndUser)
                throw new DomainException($"Trading partner {supplier.Name} cannot be a supplier");

            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product == null)
                throw new NotFoundException($"Product with ID {productId} not found");

            var trader = await _userRepository.GetByIdAsync(traderId, cancellationToken);
            if (trader == null)
                throw new NotFoundException($"Trader with ID {traderId} not found");

            contract.UpdateCoreReferences(supplierId, productId, traderId, request.UpdatedBy);
        }

        if (!string.IsNullOrEmpty(request.ExternalContractNumber))
        {
            contract.SetExternalContractNumber(request.ExternalContractNumber, request.UpdatedBy);
        }

        if (request.PriceBenchmarkId.HasValue)
        {
            contract.SetPriceBenchmark(request.PriceBenchmarkId, request.UpdatedBy);
        }

        if (request.Quantity.HasValue && !string.IsNullOrEmpty(request.QuantityUnit))
        {
            var quantityUnit = MapQuantityUnit(request.QuantityUnit);
            var quantity = new Quantity(request.Quantity.Value, quantityUnit);
            contract.UpdateQuantity(quantity, request.UpdatedBy);
        }

        if (request.TonBarrelRatio.HasValue)
        {
            contract.UpdateTonBarrelRatio(request.TonBarrelRatio.Value);
        }

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

        if (request.PricingPeriodStart.HasValue && request.PricingPeriodEnd.HasValue)
        {
            contract.SetPricingPeriod(request.PricingPeriodStart.Value, request.PricingPeriodEnd.Value);
        }

        if (request.LaycanStart.HasValue && request.LaycanEnd.HasValue)
        {
            contract.UpdateLaycan(request.LaycanStart.Value, request.LaycanEnd.Value);
        }

        if (!string.IsNullOrEmpty(request.LoadPort) && !string.IsNullOrEmpty(request.DischargePort))
        {
            contract.UpdatePorts(request.LoadPort, request.DischargePort);
        }

        if (!string.IsNullOrEmpty(request.DeliveryTerms))
        {
            var deliveryTerms = MapDeliveryTerms(request.DeliveryTerms);
            contract.UpdateDeliveryTerms(deliveryTerms);
        }

        if (!string.IsNullOrEmpty(request.SettlementType))
        {
            contract.UpdateSettlementType(MapSettlementType(request.SettlementType));
        }

        if (request.PaymentTerms != null || request.CreditPeriodDays.HasValue)
        {
            var paymentTerms = request.PaymentTerms ?? contract.PaymentTerms ?? "NET 30";
            var creditPeriodDays = request.CreditPeriodDays ?? contract.CreditPeriodDays;
            contract.UpdatePaymentTerms(paymentTerms, creditPeriodDays);
        }

        if (request.PrepaymentPercentage.HasValue)
        {
            contract.SetPrepaymentPercentage(request.PrepaymentPercentage.Value);
        }

        if (request.QualitySpecifications != null)
        {
            contract.UpdateQualitySpecifications(request.QualitySpecifications);
        }

        if (request.InspectionAgency != null)
        {
            contract.UpdateInspectionAgency(request.InspectionAgency);
        }

        if (request.Notes != null)
        {
            contract.SetNotes(request.Notes);
        }

        var hasProfessionalUpdates =
            request.QuantityTolerancePercent.HasValue ||
            request.QuantityToleranceOption != null ||
            request.BrokerName != null ||
            request.BrokerCommission.HasValue ||
            request.BrokerCommissionType != null ||
            request.LaytimeHours.HasValue ||
            request.DemurrageRate.HasValue ||
            request.DespatchRate.HasValue;

        if (hasProfessionalUpdates)
        {
            contract.UpdateProfessionalTerms(
                request.QuantityTolerancePercent ?? contract.QuantityTolerancePercent,
                request.QuantityToleranceOption ?? contract.QuantityToleranceOption,
                request.BrokerName ?? contract.BrokerName,
                request.BrokerCommission ?? contract.BrokerCommission,
                request.BrokerCommissionType ?? contract.BrokerCommissionType,
                request.LaytimeHours ?? contract.LaytimeHours,
                request.DemurrageRate ?? contract.DemurrageRate,
                request.DespatchRate ?? contract.DespatchRate,
                request.UpdatedBy);
        }

        contract.SetUpdatedBy(request.UpdatedBy);

        await _purchaseContractRepository.UpdateAsync(contract, cancellationToken);
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
            "DAP" => DeliveryTerms.DAP,
            "DDP" => DeliveryTerms.DDP,
            "DES" => DeliveryTerms.DES,
            "DDU" => DeliveryTerms.DDU,
            "STS" => DeliveryTerms.STS,
            "ITT" => DeliveryTerms.ITT,
            "EXW" => DeliveryTerms.EXW,
            _ => throw new ArgumentException($"Invalid delivery terms: {terms}")
        };
    }

    private static OilTrading.Core.Enums.SettlementType MapSettlementType(string settlementType)
    {
        if (Enum.TryParse<OilTrading.Core.Enums.SettlementType>(settlementType, true, out var parsed))
        {
            return parsed;
        }

        return settlementType.ToUpper() switch
        {
            "TT" => OilTrading.Core.Enums.SettlementType.ContractPayment,
            "LC" => OilTrading.Core.Enums.SettlementType.ContractPayment,
            "CAD" => OilTrading.Core.Enums.SettlementType.ContractPayment,
            "SBLC" => OilTrading.Core.Enums.SettlementType.ContractPayment,
            "DP" => OilTrading.Core.Enums.SettlementType.ContractPayment,
            _ => throw new ArgumentException($"Invalid settlement type: {settlementType}")
        };
    }
}



