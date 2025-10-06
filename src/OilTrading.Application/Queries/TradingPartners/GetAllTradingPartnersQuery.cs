using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.TradingPartners;

public class GetAllTradingPartnersQuery : IRequest<IEnumerable<TradingPartnerListDto>>
{
}