using MediatR;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Enums;

namespace OilTrading.Application.Commands.PurchaseContracts;

public class CreatePurchaseContractCommandHandler : IRequestHandler<CreatePurchaseContractCommand, Guid>
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ITradingPartnerRepository _tradingPartnerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IContractNumberGenerator _contractNumberGenerator;
    private readonly ICacheInvalidationService _cacheInvalidationService;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePurchaseContractCommandHandler(
        IPurchaseContractRepository purchaseContractRepository,
        ITradingPartnerRepository tradingPartnerRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IContractNumberGenerator contractNumberGenerator,
        ICacheInvalidationService cacheInvalidationService,
        IUnitOfWork unitOfWork)
    {
        _purchaseContractRepository = purchaseContractRepository;
        _tradingPartnerRepository = tradingPartnerRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _contractNumberGenerator = contractNumberGenerator;
        _cacheInvalidationService = cacheInvalidationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreatePurchaseContractCommand request, CancellationToken cancellationToken)
    {
        // Validate entities exist
        var supplier = await _tradingPartnerRepository.GetByIdAsync(request.SupplierId, cancellationToken);
        if (supplier == null)
            throw new NotFoundException($"Supplier with ID {request.SupplierId} not found");

        // A partner can be a purchase supplier if they are:
        // - Supplier (1): Pure supplier
        // - Both (3): Both supplier and customer (common for traders/resellers)
        // - Trader (4): Professional trader
        // Cannot be Customer (2) or EndUser (5) - these are demand-side roles
        if (supplier.Type == TradingPartnerType.Customer || supplier.Type == TradingPartnerType.EndUser)
            throw new DomainException($"Trading partner {supplier.Name} cannot be a supplier (only customers and end users)");

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
            throw new NotFoundException($"Product with ID {request.ProductId} not found");

        var trader = await _userRepository.GetByIdAsync(request.TraderId, cancellationToken);
        if (trader == null)
            throw new NotFoundException($"Trader with ID {request.TraderId} not found");

        // Generate contract number
        var contractType = MapContractType(request.ContractType);
        var contractNumberString = await _contractNumberGenerator.GenerateAsync(contractType, DateTime.Now.Year);
        var contractNumber = ContractNumber.Parse(contractNumberString);

        // Create quantity value object
        var quantityUnit = MapQuantityUnit(request.QuantityUnit);
        var quantity = new Quantity(request.Quantity, quantityUnit);

        // Create contract with price benchmark and external contract number
        // Purpose: 创建合同并设置价格基准物和外部合同编号，用于后续的价格计算和结算
        var contract = new PurchaseContract(
            contractNumber: contractNumber,
            contractType: contractType,
            tradingPartnerId: request.SupplierId,
            productId: request.ProductId,
            traderId: request.TraderId,
            contractQuantity: quantity,
            tonBarrelRatio: request.TonBarrelRatio,
            priceBenchmarkId: request.PriceBenchmarkId,
            externalContractNumber: request.ExternalContractNumber);

        // Set pricing information
        if (request.PricingType == PricingType.Fixed && request.FixedPrice.HasValue)
        {
            var priceFormula = PriceFormula.Fixed(request.FixedPrice.Value);
            var contractValue = Money.Dollar(request.FixedPrice.Value * request.Quantity);
            contract.UpdatePricing(priceFormula, contractValue);
        }
        else if (!string.IsNullOrEmpty(request.PricingFormula))
        {
            var priceFormula = PriceFormula.Parse(request.PricingFormula);
            contract.UpdatePricing(priceFormula, Money.Dollar(0)); // Will be calculated later
            
            if (request.PricingPeriodStart.HasValue && request.PricingPeriodEnd.HasValue)
            {
                contract.SetPricingPeriod(request.PricingPeriodStart.Value, request.PricingPeriodEnd.Value);
            }
        }

        // Set laycan dates
        contract.UpdateLaycan(request.LaycanStart, request.LaycanEnd);

        // Set ports
        contract.UpdatePorts(request.LoadPort, request.DischargePort);

        // Set delivery terms
        var deliveryTerms = MapDeliveryTerms(request.DeliveryTerms);
        contract.UpdateDeliveryTerms(deliveryTerms);

        // Set settlement type
        var settlementType = MapSettlementType(request.SettlementType);
        contract.UpdateSettlementType(settlementType);

        // Set payment terms
        if (!string.IsNullOrEmpty(request.PaymentTerms))
        {
            contract.UpdatePaymentTerms(request.PaymentTerms, request.CreditPeriodDays);
        }

        // Set prepayment percentage
        if (request.PrepaymentPercentage.HasValue)
        {
            contract.SetPrepaymentPercentage(request.PrepaymentPercentage.Value);
        }

        // Set additional details
        if (!string.IsNullOrEmpty(request.QualitySpecifications))
        {
            contract.UpdateQualitySpecifications(request.QualitySpecifications);
        }

        if (!string.IsNullOrEmpty(request.InspectionAgency))
        {
            contract.UpdateInspectionAgency(request.InspectionAgency);
        }

        if (!string.IsNullOrEmpty(request.Notes))
        {
            contract.AddNotes(request.Notes);
        }

        // Set created by
        contract.SetCreatedBy(request.CreatedBy);

        // Add to repository
        await _purchaseContractRepository.AddAsync(contract, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate related caches
        await _cacheInvalidationService.InvalidatePurchaseContractCacheAsync();

        return contract.Id;
    }

    private static OilTrading.Core.ValueObjects.ContractType MapContractType(OilTrading.Core.ValueObjects.ContractType contractType)
    {
        return contractType;
    }

    private static QuantityUnit MapQuantityUnit(QuantityUnit unit)
    {
        return unit;
    }

    private static DeliveryTerms MapDeliveryTerms(DeliveryTerms terms)
    {
        return terms;
    }

    private static OilTrading.Core.Enums.SettlementType MapSettlementType(ContractPaymentMethod type)
    {
        return type switch
        {
            ContractPaymentMethod.TT => OilTrading.Core.Enums.SettlementType.ContractPayment,
            ContractPaymentMethod.LC => OilTrading.Core.Enums.SettlementType.ContractPayment,
            ContractPaymentMethod.CAD => OilTrading.Core.Enums.SettlementType.ContractPayment,
            ContractPaymentMethod.SBLC => OilTrading.Core.Enums.SettlementType.ContractPayment,
            ContractPaymentMethod.DP => OilTrading.Core.Enums.SettlementType.ContractPayment,
            _ => OilTrading.Core.Enums.SettlementType.ContractPayment // Default to contract payment
        };
    }
}