using MediatR;
using AutoMapper;
using OilTrading.Core.Repositories;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Queries.ShippingOperations;

public class GetShippingOperationByIdQueryHandler : IRequestHandler<GetShippingOperationByIdQuery, ShippingOperationDto>
{
    private readonly IShippingOperationRepository _shippingOperationRepository;
    private readonly IMapper _mapper;

    public GetShippingOperationByIdQueryHandler(
        IShippingOperationRepository shippingOperationRepository,
        IMapper mapper)
    {
        _shippingOperationRepository = shippingOperationRepository;
        _mapper = mapper;
    }

    public async Task<ShippingOperationDto> Handle(GetShippingOperationByIdQuery request, CancellationToken cancellationToken)
    {
        var shippingOperation = await _shippingOperationRepository.GetByIdWithIncludesAsync(
            request.Id,
            includeProperties: new[]
            {
                "PurchaseContract",
                "SalesContract",
                "PricingEvents"
            },
            cancellationToken);

        if (shippingOperation == null)
            throw new NotFoundException($"Shipping operation with ID {request.Id} not found");

        return _mapper.Map<ShippingOperationDto>(shippingOperation);
    }
}