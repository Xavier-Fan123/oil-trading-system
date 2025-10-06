using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.SalesContracts;

public class GetSalesContractByIdQuery : IRequest<SalesContractDto>
{
    public Guid Id { get; set; }

    public GetSalesContractByIdQuery(Guid id)
    {
        Id = id;
    }
}