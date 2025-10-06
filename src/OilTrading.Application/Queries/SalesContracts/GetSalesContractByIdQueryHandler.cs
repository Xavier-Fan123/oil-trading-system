using MediatR;
using AutoMapper;
using OilTrading.Core.Repositories;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Queries.SalesContracts;

public class GetSalesContractByIdQueryHandler : IRequestHandler<GetSalesContractByIdQuery, SalesContractDto>
{
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IMapper _mapper;

    public GetSalesContractByIdQueryHandler(
        ISalesContractRepository salesContractRepository,
        IMapper mapper)
    {
        _salesContractRepository = salesContractRepository;
        _mapper = mapper;
    }

    public async Task<SalesContractDto> Handle(GetSalesContractByIdQuery request, CancellationToken cancellationToken)
    {
        var contract = await _salesContractRepository.GetByIdWithIncludesAsync(
            request.Id,
            includeProperties: new[]
            {
                "TradingPartner",
                "Product", 
                "Trader",
                "LinkedPurchaseContract",
                "ShippingOperations",
                "PricingEvents"
            },
            cancellationToken);

        if (contract == null)
            throw new NotFoundException($"Sales contract with ID {request.Id} not found");

        return _mapper.Map<SalesContractDto>(contract);
    }
}