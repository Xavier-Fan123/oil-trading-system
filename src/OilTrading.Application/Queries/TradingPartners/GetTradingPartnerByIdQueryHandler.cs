using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.TradingPartners;

public class GetTradingPartnerByIdQueryHandler : IRequestHandler<GetTradingPartnerByIdQuery, TradingPartnerDto?>
{
    private readonly ITradingPartnerRepository _repository;

    public GetTradingPartnerByIdQueryHandler(ITradingPartnerRepository repository)
    {
        _repository = repository;
    }

    public async Task<TradingPartnerDto?> Handle(GetTradingPartnerByIdQuery request, CancellationToken cancellationToken)
    {
        var partner = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (partner == null)
        {
            return null;
        }

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
