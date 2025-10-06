using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.ShippingOperations;

public class GetShippingOperationsByContractQuery : IRequest<IReadOnlyList<ShippingOperationSummaryDto>>
{
    public Guid ContractId { get; set; }

    public GetShippingOperationsByContractQuery(Guid contractId)
    {
        ContractId = contractId;
    }
}