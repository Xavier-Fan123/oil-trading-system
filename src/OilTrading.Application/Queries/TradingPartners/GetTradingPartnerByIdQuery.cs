using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.TradingPartners;

public class GetTradingPartnerByIdQuery : IRequest<TradingPartnerDto?>
{
    public Guid Id { get; set; }

    public GetTradingPartnerByIdQuery(Guid id)
    {
        Id = id;
    }
}
