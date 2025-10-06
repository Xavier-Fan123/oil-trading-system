using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.PhysicalContracts;

public class CreatePhysicalContractCommandHandler : IRequestHandler<CreatePhysicalContractCommand, PhysicalContractDto>
{
    private readonly IPhysicalContractRepository _contractRepository;
    private readonly ITradingPartnerRepository _partnerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatePhysicalContractCommandHandler> _logger;

    public CreatePhysicalContractCommandHandler(
        IPhysicalContractRepository contractRepository,
        ITradingPartnerRepository partnerRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreatePhysicalContractCommandHandler> logger)
    {
        _contractRepository = contractRepository;
        _partnerRepository = partnerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PhysicalContractDto> Handle(CreatePhysicalContractCommand request, CancellationToken cancellationToken)
    {
        // Parse enums
        var contractType = request.ContractType.ToLower() == "purchase" 
            ? PhysicalContractType.Purchase 
            : PhysicalContractType.Sales;

        var pricingType = request.PricingType.ToLower() switch
        {
            "fixed" => PricingType.Fixed,
            "floating" => PricingType.Floating,
            "formula" => PricingType.Formula,
            _ => PricingType.Fixed
        };

        // Generate contract number
        var contractNumber = await _contractRepository.GenerateContractNumberAsync(contractType, cancellationToken);

        // Get trading partner
        var partner = await _partnerRepository.GetByIdAsync(request.TradingPartnerId, cancellationToken);
        if (partner == null)
        {
            throw new ArgumentException($"Trading partner with ID {request.TradingPartnerId} not found");
        }

        // Calculate contract value if fixed price
        decimal? contractValue = null;
        if (pricingType == PricingType.Fixed && request.FixedPrice.HasValue)
        {
            contractValue = request.Quantity * request.FixedPrice.Value;
            if (request.Premium.HasValue)
            {
                contractValue += request.Premium.Value;
            }
        }

        // Create physical contract
        var contract = new PhysicalContract
        {
            ContractNumber = contractNumber,
            ContractType = contractType,
            ContractDate = request.ContractDate,
            TradingPartnerId = request.TradingPartnerId,
            ProductType = request.ProductType,
            Quantity = request.Quantity,
            QuantityUnit = request.QuantityUnit,
            ProductSpec = request.ProductSpec ?? string.Empty,
            PricingType = pricingType,
            FixedPrice = request.FixedPrice,
            PricingFormula = request.PricingFormula,
            PricingBasis = request.PricingBasis,
            Premium = request.Premium,
            Currency = "USD",
            ContractValue = contractValue,
            DeliveryTerms = request.DeliveryTerms,
            LoadPort = request.LoadPort,
            DischargePort = request.DischargePort,
            LaycanStart = request.LaycanStart,
            LaycanEnd = request.LaycanEnd,
            PaymentTerms = request.PaymentTerms,
            PrepaymentPercentage = request.PrepaymentPercentage,
            CreditDays = request.CreditDays,
            PaymentDueDate = request.ContractDate.AddDays(request.CreditDays),
            IsAgencyTrade = request.IsAgencyTrade,
            PrincipalName = request.PrincipalName,
            AgencyFee = request.AgencyFee,
            Status = PhysicalContractStatus.Active,
            OutstandingAmount = contractValue,
            Notes = request.Notes
        };

        await _contractRepository.AddAsync(contract, cancellationToken);

        // Update partner exposure
        var newExposure = await _contractRepository.CalculatePartnerExposureAsync(request.TradingPartnerId, cancellationToken);
        await _partnerRepository.UpdateExposureAsync(request.TradingPartnerId, newExposure, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Physical contract created: {ContractNumber} for {Partner}", 
            contract.ContractNumber, partner.CompanyName);

        // Return DTO
        return new PhysicalContractDto
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber,
            ContractType = contract.ContractType.ToString(),
            ContractDate = contract.ContractDate,
            TradingPartnerId = contract.TradingPartnerId,
            TradingPartnerName = partner.CompanyName,
            TradingPartnerCode = partner.CompanyCode,
            ProductType = contract.ProductType,
            Quantity = contract.Quantity,
            QuantityUnit = contract.QuantityUnit.ToString(),
            ProductSpec = contract.ProductSpec,
            PricingType = contract.PricingType.ToString(),
            FixedPrice = contract.FixedPrice,
            PricingFormula = contract.PricingFormula,
            PricingBasis = contract.PricingBasis,
            Premium = contract.Premium,
            Currency = contract.Currency,
            ContractValue = contract.ContractValue,
            DeliveryTerms = contract.DeliveryTerms,
            LoadPort = contract.LoadPort,
            DischargePort = contract.DischargePort,
            LaycanStart = contract.LaycanStart,
            LaycanEnd = contract.LaycanEnd,
            PaymentTerms = contract.PaymentTerms,
            PrepaymentPercentage = contract.PrepaymentPercentage,
            CreditDays = contract.CreditDays,
            PaymentDueDate = contract.PaymentDueDate,
            IsAgencyTrade = contract.IsAgencyTrade,
            PrincipalName = contract.PrincipalName,
            AgencyFee = contract.AgencyFee,
            Status = contract.Status.ToString(),
            OutstandingAmount = contract.OutstandingAmount,
            IsFullySettled = contract.IsFullySettled,
            Notes = contract.Notes,
            CreatedAt = contract.CreatedAt,
            CreatedBy = contract.CreatedBy
        };
    }
}