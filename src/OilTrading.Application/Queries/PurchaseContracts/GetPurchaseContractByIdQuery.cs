using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.PurchaseContracts;

public class GetPurchaseContractByIdQuery : IRequest<PurchaseContractDto>
{
    public Guid Id { get; set; }

    public GetPurchaseContractByIdQuery(Guid id)
    {
        Id = id;
    }
}