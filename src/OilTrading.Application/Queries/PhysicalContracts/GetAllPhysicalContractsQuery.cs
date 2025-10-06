using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.PhysicalContracts;

public class GetAllPhysicalContractsQuery : IRequest<IEnumerable<PhysicalContractListDto>>
{
}