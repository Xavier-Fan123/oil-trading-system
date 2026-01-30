using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.TradingPartners;

public class CreateTradingPartnerCommandHandler : IRequestHandler<CreateTradingPartnerCommand, TradingPartnerDto>
{
    private readonly ITradingPartnerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTradingPartnerCommandHandler> _logger;

    public CreateTradingPartnerCommandHandler(
        ITradingPartnerRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateTradingPartnerCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TradingPartnerDto> Handle(CreateTradingPartnerCommand request, CancellationToken cancellationToken)
    {
        // Parse partner type
        TradingPartnerType partnerType = request.PartnerType.ToLower() switch
        {
            "trader" => TradingPartnerType.Trader,
            "enduser" => TradingPartnerType.EndUser,
            "both" => TradingPartnerType.Both,
            "supplier" => TradingPartnerType.Supplier,
            "customer" => TradingPartnerType.Customer,
            _ => TradingPartnerType.Both
        };

        // Generate unique company code
        var companyCode = await _repository.GenerateCompanyCodeAsync(cancellationToken);

        // Create trading partner
        var partner = new TradingPartner
        {
            CompanyName = request.CompanyName,
            CompanyCode = companyCode,
            Name = request.CompanyName,
            Code = companyCode,
            Type = partnerType,
            PartnerType = partnerType,
            ContactPerson = request.ContactPerson,
            ContactEmail = request.ContactEmail ?? string.Empty,
            ContactPhone = request.ContactPhone ?? string.Empty,
            Address = request.Address ?? string.Empty,
            TaxNumber = request.TaxNumber,
            CreditLimit = request.CreditLimit,
            CreditLimitValidUntil = request.CreditLimitValidUntil,
            PaymentTermDays = request.PaymentTermDays,
            CurrentExposure = 0,
            IsActive = true
        };

        partner.SetRowVersion(new byte[] { 0 });
        await _repository.AddAsync(partner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Trading partner created: {CompanyName} ({CompanyCode})", 
            partner.CompanyName, partner.CompanyCode);

        // Return DTO
        return new TradingPartnerDto
        {
            Id = partner.Id,
            CompanyName = partner.CompanyName,
            CompanyCode = partner.CompanyCode,
            PartnerType = partner.PartnerType,
            ContactPerson = partner.ContactPerson,
            ContactEmail = string.IsNullOrEmpty(partner.ContactEmail) ? null : partner.ContactEmail,
            ContactPhone = string.IsNullOrEmpty(partner.ContactPhone) ? null : partner.ContactPhone,
            Address = string.IsNullOrEmpty(partner.Address) ? null : partner.Address,
            TaxNumber = partner.TaxNumber,
            CreditLimit = partner.CreditLimit,
            CreditLimitValidUntil = partner.CreditLimitValidUntil,
            PaymentTermDays = partner.PaymentTermDays,
            CurrentExposure = partner.CurrentExposure,
            CreditUtilization = partner.CreditLimit > 0 ? (partner.CurrentExposure / partner.CreditLimit * 100) : 0,
            IsActive = partner.IsActive,
            IsBlocked = partner.IsBlocked,
            BlockReason = partner.BlockReason
        };
    }
}