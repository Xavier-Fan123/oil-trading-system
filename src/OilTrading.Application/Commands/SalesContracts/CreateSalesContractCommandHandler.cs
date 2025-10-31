using MediatR;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.SalesContracts;

public class CreateSalesContractCommandHandler : IRequestHandler<CreateSalesContractCommand, Guid>
{
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ITradingPartnerRepository _tradingPartnerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IContractNumberGenerator _contractNumberGenerator;
    private readonly ICacheInvalidationService _cacheInvalidationService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSalesContractCommandHandler(
        ISalesContractRepository salesContractRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ITradingPartnerRepository tradingPartnerRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IContractNumberGenerator contractNumberGenerator,
        ICacheInvalidationService cacheInvalidationService,
        IUnitOfWork unitOfWork)
    {
        _salesContractRepository = salesContractRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _tradingPartnerRepository = tradingPartnerRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _contractNumberGenerator = contractNumberGenerator;
        _cacheInvalidationService = cacheInvalidationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateSalesContractCommand request, CancellationToken cancellationToken)
    {
        // DEBUG: Log the external contract number received
        Console.WriteLine($"[DEBUG] CreateSalesContractCommandHandler - ExternalContractNumber: '{request.ExternalContractNumber ?? "NULL"}'");

        // Validate entities exist
        var customer = await _tradingPartnerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
            throw new NotFoundException($"Customer with ID {request.CustomerId} not found");

        // A partner can be a sales customer if they are:
        // - Customer (2): Pure customer
        // - Both (3): Both supplier and customer (common for traders/resellers)
        // - EndUser (5): End user/consumer
        // Cannot be Supplier (1) or just Trader (4) - these are supply-side roles
        if (customer.Type == TradingPartnerType.Supplier)
            throw new DomainException($"Trading partner {customer.Name} is a supplier and cannot be a sales customer");

        if (customer.Type == TradingPartnerType.Trader && customer.Type != TradingPartnerType.Both)
            throw new DomainException($"Trading partner {customer.Name} is a trader and cannot be a direct sales customer");

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
            throw new NotFoundException($"Product with ID {request.ProductId} not found");

        var trader = await _userRepository.GetByIdAsync(request.TraderId, cancellationToken);
        if (trader == null)
            throw new NotFoundException($"Trader with ID {request.TraderId} not found");

        // Validate linked purchase contract if provided
        PurchaseContract? linkedPurchaseContract = null;
        if (request.LinkedPurchaseContractId.HasValue)
        {
            linkedPurchaseContract = await _purchaseContractRepository.GetByIdAsync(request.LinkedPurchaseContractId.Value, cancellationToken);
            if (linkedPurchaseContract == null)
                throw new NotFoundException($"Linked purchase contract with ID {request.LinkedPurchaseContractId} not found");

            // Validate that the purchase contract is for the same product
            if (linkedPurchaseContract.ProductId != request.ProductId)
                throw new DomainException("Sales contract product must match linked purchase contract product");

            // Validate that the purchase contract is active
            if (linkedPurchaseContract.Status != ContractStatus.Active)
                throw new DomainException("Can only link to active purchase contracts");
        }

        // Generate contract number
        var currentYear = DateTime.UtcNow.Year;
        var contractType = Enum.Parse<ContractType>(request.ContractType, true);
        var contractNumber = await _salesContractRepository.GetNextContractNumberAsync(currentYear, contractType, cancellationToken);

        // Create quantity value object
        var quantityUnit = Enum.Parse<QuantityUnit>(request.QuantityUnit, true);
        var quantity = new Quantity(request.Quantity, quantityUnit);

        // Create the sales contract
        var salesContract = new SalesContract(
            contractNumber,
            contractType,
            request.CustomerId,
            request.ProductId,
            request.TraderId,
            quantity,
            request.TonBarrelRatio,
            request.LinkedPurchaseContractId,
            request.PriceBenchmarkId,
            request.ExternalContractNumber);

        // DEBUG: Log the created entity's external contract number
        Console.WriteLine($"[DEBUG] SalesContract created - ExternalContractNumber: '{salesContract.ExternalContractNumber ?? "NULL"}'");

        // Set delivery information if provided
        if (request.LaycanStart != default && request.LaycanEnd != default)
        {
            salesContract.UpdateLaycan(request.LaycanStart, request.LaycanEnd);
        }

        if (!string.IsNullOrWhiteSpace(request.LoadPort) && !string.IsNullOrWhiteSpace(request.DischargePort))
        {
            salesContract.UpdatePorts(request.LoadPort, request.DischargePort);
        }

        // Set pricing information if provided
        if (!string.IsNullOrWhiteSpace(request.PricingFormula) || request.FixedPrice.HasValue)
        {
            var priceFormula = CreatePriceFormula(request);
            var contractValue = CalculateContractValue(request, quantity);
            
            // Calculate profit margin if linked to purchase contract
            Money? profitMargin = null;
            if (linkedPurchaseContract?.ContractValue != null && contractValue != null)
            {
                profitMargin = contractValue.Subtract(linkedPurchaseContract.ContractValue);
            }

            salesContract.UpdatePricing(priceFormula, contractValue, profitMargin);
        }

        // Add to repository
        await _salesContractRepository.AddAsync(salesContract, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate related caches
        await _cacheInvalidationService.InvalidateSalesContractCacheAsync();

        return salesContract.Id;
    }

    private static PriceFormula CreatePriceFormula(CreateSalesContractCommand request)
    {
        if (request.FixedPrice.HasValue)
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

    private static Money CalculateContractValue(CreateSalesContractCommand request, Quantity quantity)
    {
        if (request.FixedPrice.HasValue)
        {
            var totalValue = request.FixedPrice.Value * quantity.Value;
            return new Money(totalValue, "USD");
        }

        // For formula-based pricing, we'll set a placeholder value
        // In a real system, this would be calculated based on current market prices
        return new Money(0, "USD");
    }
}