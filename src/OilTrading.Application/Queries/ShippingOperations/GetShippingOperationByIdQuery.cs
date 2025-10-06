using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.ShippingOperations;

public class GetShippingOperationByIdQuery : IRequest<ShippingOperationDto>
{
    public Guid Id { get; set; }

    public GetShippingOperationByIdQuery(Guid id)
    {
        Id = id;
    }
}