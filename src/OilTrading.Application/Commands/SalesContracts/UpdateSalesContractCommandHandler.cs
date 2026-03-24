using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.SalesContracts;

public class UpdateSalesContractCommandHandler : IRequestHandler<UpdateSalesContractCommand, Unit>
{
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly ITradingPartnerRepository _tradingPartnerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSalesContractCommandHandler(
        ISalesContractRepository salesContractRepository,
        ITradingPartnerRepository tradingPartnerRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _salesContractRepository = salesContractRepository;
        _tradingPartnerRepository = tradingPartnerRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateSalesContractCommand request, CancellationToken cancellationToken)
    {
        var contract = await _salesContractRepository.GetByIdAsync(request.Id, cancellationToken);
        if (contract == null)
            throw new NotFoundException($"Sales contract with ID {request.Id} not found");

        if (contract.Status == ContractStatus.Completed || contract.Status == ContractStatus.Cancelled)
            throw new DomainException($"Cannot update contract in {contract.Status} status");

        if (request.CustomerId.HasValue || request.ProductId.HasValue || request.TraderId.HasValue)
        {
            var customerId = request.CustomerId ?? contract.TradingPartnerId;
            var productId = request.ProductId ?? contract.ProductId;
            var traderId = request.TraderId ?? contract.TraderId;

            var customer = await _tradingPartnerRepository.GetByIdAsync(customerId, cancellationToken);
            if (customer == null)
                throw new NotFoundException($"Customer with ID {customerId} not found");

            if (customer.Type == TradingPartnerType.Supplier)
                throw new DomainException($"Trading partner {customer.Name} is a supplier and cannot be a sales customer");

            if (customer.Type != TradingPartnerType.Customer && customer.Type != TradingPartnerType.Both)
                throw new DomainException($"Trading partner {customer.Name} is not a valid sales customer");

            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product == null)
                throw new NotFoundException($"Product with ID {productId} not found");

            var trader = await _userRepository.GetByIdAsync(traderId, cancellationToken);
            if (trader == null)
                throw new NotFoundException($"Trader with ID {traderId} not found");

            contract.UpdateCoreReferences(customerId, productId, traderId, request.UpdatedBy);
        }

        if (!string.IsNullOrWhiteSpace(request.ExternalContractNumber))
        {
            contract.SetExternalContractNumber(request.ExternalContractNumber, request.UpdatedBy);
        }

        if (request.PriceBenchmarkId.HasValue)
        {
            contract.SetPriceBenchmark(request.PriceBenchmarkId, request.UpdatedBy);
        }

        if (request.Quantity.HasValue && !string.IsNullOrWhiteSpace(request.QuantityUnit))
        {
            var quantityUnit = MapQuantityUnit(request.QuantityUnit);
            var quantity = new Quantity(request.Quantity.Value, quantityUnit);
            contract.UpdateQuantity(quantity, request.UpdatedBy);
        }

        if (request.TonBarrelRatio.HasValue)
        {
            contract.UpdateTonBarrelRatio(request.TonBarrelRatio.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.PricingType) || request.FixedPrice.HasValue || !string.IsNullOrWhiteSpace(request.PricingFormula))
        {
            var pricingType = request.PricingType;
            if (string.IsNullOrWhiteSpace(pricingType))
            {
                pricingType = request.FixedPrice.HasValue ? "Fixed" : "Formula";
            }

            var priceFormula = CreatePriceFormula(pricingType, request.FixedPrice, request.PricingFormula);
            var contractValue = CalculateContractValue(request, contract);

            Money? profitMargin = null;
            if (contract.LinkedPurchaseContract?.ContractValue != null)
            {
                profitMargin = contractValue.Subtract(contract.LinkedPurchaseContract.ContractValue);
            }

            contract.UpdatePricing(priceFormula, contractValue, profitMargin);
        }

        if (request.PricingPeriodStart.HasValue && request.PricingPeriodEnd.HasValue)
        {
            contract.SetPricingPeriod(request.PricingPeriodStart.Value, request.PricingPeriodEnd.Value);
        }

        if (request.LaycanStart.HasValue && request.LaycanEnd.HasValue)
        {
            contract.UpdateLaycan(request.LaycanStart.Value, request.LaycanEnd.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.LoadPort) && !string.IsNullOrWhiteSpace(request.DischargePort))
        {
            contract.UpdatePorts(request.LoadPort, request.DischargePort);
        }

        if (!string.IsNullOrWhiteSpace(request.DeliveryTerms))
        {
            contract.UpdateDeliveryTerms(MapDeliveryTerms(request.DeliveryTerms));
        }

        if (!string.IsNullOrWhiteSpace(request.SettlementType))
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

        await _salesContractRepository.UpdateAsync(contract, cancellationToken);
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

    private static PriceFormula CreatePriceFormula(string pricingType, decimal? fixedPrice, string? pricingFormula)
    {
        if (string.Equals(pricingType, "Fixed", StringComparison.OrdinalIgnoreCase))
        {
            if (!fixedPrice.HasValue)
                throw new DomainException("Fixed price is required when pricing type is Fixed");

            return PriceFormula.Fixed(fixedPrice.Value, "USD");
        }

        if (fixedPrice.HasValue && string.IsNullOrWhiteSpace(pricingFormula))
        {
            return PriceFormula.Fixed(fixedPrice.Value, "USD");
        }

        if (!string.IsNullOrWhiteSpace(pricingFormula))
        {
            return PriceFormula.Parse(pricingFormula);
        }

        throw new DomainException("Either fixed price or pricing formula must be provided");
    }

    private static Money CalculateContractValue(UpdateSalesContractCommand request, SalesContract contract)
    {
        if (request.FixedPrice.HasValue)
        {
            var quantity = request.Quantity ?? contract.ContractQuantity.Value;
            return new Money(request.FixedPrice.Value * quantity, "USD");
        }

        return contract.ContractValue ?? new Money(0, "USD");
    }
}



