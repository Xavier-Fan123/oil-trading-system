using MediatR;
using AutoMapper;
using OilTrading.Core.Repositories;
using OilTrading.Core.Common;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Queries.PurchaseContracts;

public class GetPurchaseContractByIdQueryHandler : IRequestHandler<GetPurchaseContractByIdQuery, PurchaseContractDto>
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly IMapper _mapper;

    public GetPurchaseContractByIdQueryHandler(
        IPurchaseContractRepository purchaseContractRepository,
        IMapper mapper)
    {
        _purchaseContractRepository = purchaseContractRepository;
        _mapper = mapper;
    }

    public async Task<PurchaseContractDto> Handle(GetPurchaseContractByIdQuery request, CancellationToken cancellationToken)
    {
        var contract = await _purchaseContractRepository.GetByIdWithIncludesAsync(
            request.Id,
            includeProperties: new[]
            {
                "TradingPartner",
                "Product", 
                "Trader",
                "BenchmarkContract",
                "ShippingOperations",
                "PricingEvents",
                "LinkedSalesContracts",
                "LinkedSalesContracts.TradingPartner"
            },
            cancellationToken);

        if (contract == null)
            throw new NotFoundException($"Purchase contract with ID {request.Id} not found");

        return _mapper.Map<PurchaseContractDto>(contract);
    }
}