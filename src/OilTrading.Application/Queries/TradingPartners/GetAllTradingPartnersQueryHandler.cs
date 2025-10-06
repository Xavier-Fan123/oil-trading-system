using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.TradingPartners;

public class GetAllTradingPartnersQueryHandler : IRequestHandler<GetAllTradingPartnersQuery, IEnumerable<TradingPartnerListDto>>
{
    private readonly ITradingPartnerRepository _repository;

    public GetAllTradingPartnersQueryHandler(ITradingPartnerRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TradingPartnerListDto>> Handle(GetAllTradingPartnersQuery request, CancellationToken cancellationToken)
    {
        var partners = await _repository.GetAllAsync(cancellationToken);
        
        return partners.Select(p => new TradingPartnerListDto
        {
            Id = p.Id,
            CompanyName = p.CompanyName,
            CompanyCode = p.CompanyCode,
            PartnerType = p.PartnerType,
            CreditLimit = p.CreditLimit,
            CurrentExposure = p.CurrentExposure,
            CreditUtilization = p.CreditLimit > 0 ? (p.CurrentExposure / p.CreditLimit * 100) : 0,
            IsActive = p.IsActive,
            IsCreditExceeded = p.CurrentExposure > p.CreditLimit
        });
    }
}