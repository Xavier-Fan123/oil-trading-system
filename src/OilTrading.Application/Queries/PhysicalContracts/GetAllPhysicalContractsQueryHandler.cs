using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.PhysicalContracts;

public class GetAllPhysicalContractsQueryHandler : IRequestHandler<GetAllPhysicalContractsQuery, IEnumerable<PhysicalContractListDto>>
{
    private readonly IPhysicalContractRepository _repository;

    public GetAllPhysicalContractsQueryHandler(IPhysicalContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PhysicalContractListDto>> Handle(GetAllPhysicalContractsQuery request, CancellationToken cancellationToken)
    {
        var contracts = await _repository.GetAllAsync(cancellationToken);
        
        return contracts.Select(c => new PhysicalContractListDto
        {
            Id = c.Id,
            ContractNumber = c.ContractNumber,
            ContractType = c.ContractType.ToString(),
            ContractDate = c.ContractDate,
            TradingPartnerName = c.TradingPartner?.CompanyName ?? string.Empty,
            ProductType = c.ProductType,
            Quantity = c.Quantity,
            QuantityUnit = c.QuantityUnit.ToString(),
            ContractValue = c.ContractValue,
            Status = c.Status.ToString(),
            LaycanStart = c.LaycanStart,
            LaycanEnd = c.LaycanEnd,
            OutstandingAmount = c.OutstandingAmount
        });
    }
}