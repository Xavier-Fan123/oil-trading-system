using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.PaperContracts;

public class GetOpenPositionsQuery : IRequest<IEnumerable<PaperContractListDto>>
{
}