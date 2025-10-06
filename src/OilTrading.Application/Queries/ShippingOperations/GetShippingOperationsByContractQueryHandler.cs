using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.ShippingOperations;

public class GetShippingOperationsByContractQueryHandler : IRequestHandler<GetShippingOperationsByContractQuery, IReadOnlyList<ShippingOperationSummaryDto>>
{
    private readonly IShippingOperationRepository _shippingOperationRepository;

    public GetShippingOperationsByContractQueryHandler(IShippingOperationRepository shippingOperationRepository)
    {
        _shippingOperationRepository = shippingOperationRepository;
    }

    public async Task<IReadOnlyList<ShippingOperationSummaryDto>> Handle(GetShippingOperationsByContractQuery request, CancellationToken cancellationToken)
    {
        var shippingOperations = await _shippingOperationRepository.GetByContractAsync(request.ContractId, cancellationToken);

        return shippingOperations.Select(operation => new ShippingOperationSummaryDto
        {
            Id = operation.Id,
            VesselName = operation.VesselName,
            Status = operation.Status.ToString(),
            PlannedQuantity = operation.PlannedQuantity.Value,
            PlannedQuantityUnit = operation.PlannedQuantity.Unit.ToString(),
            ActualQuantity = operation.ActualQuantity?.Value,
            LaycanStart = operation.LoadPortETA,
            LaycanEnd = operation.DischargePortETA,
            BillOfLadingDate = operation.BillOfLadingDate,
            CreatedAt = operation.CreatedAt
        }).ToList();
    }
}