using MediatR;
using AutoMapper;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;
using OilTrading.Core.ValueObjects;
using System.Linq.Expressions;

namespace OilTrading.Application.Queries.ShippingOperations;

public class GetShippingOperationsQueryHandler : IRequestHandler<GetShippingOperationsQuery, PagedResult<ShippingOperationSummaryDto>>
{
    private readonly IShippingOperationRepository _shippingOperationRepository;
    private readonly IMapper _mapper;

    public GetShippingOperationsQueryHandler(
        IShippingOperationRepository shippingOperationRepository,
        IMapper mapper)
    {
        _shippingOperationRepository = shippingOperationRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<ShippingOperationSummaryDto>> Handle(GetShippingOperationsQuery request, CancellationToken cancellationToken)
    {
        // Build filter expression
        Expression<Func<ShippingOperation, bool>>? filter = null;

        if (!string.IsNullOrEmpty(request.ShippingNumber))
        {
            var shippingNumber = request.ShippingNumber.Trim();
            filter = CombineFilters(filter, x => x.ShippingNumber.Contains(shippingNumber));
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            if (Enum.TryParse<ShippingStatus>(request.Status, true, out var status))
            {
                filter = CombineFilters(filter, x => x.Status == status);
            }
        }

        if (request.ContractId.HasValue)
        {
            filter = CombineFilters(filter, x => x.ContractId == request.ContractId.Value);
        }

        if (!string.IsNullOrEmpty(request.VesselName))
        {
            var vesselName = request.VesselName.Trim();
            filter = CombineFilters(filter, x => x.VesselName.Contains(vesselName));
        }

        if (request.LoadPortETAFrom.HasValue)
        {
            filter = CombineFilters(filter, x => x.LoadPortETA >= request.LoadPortETAFrom.Value);
        }

        if (request.LoadPortETATo.HasValue)
        {
            filter = CombineFilters(filter, x => x.LoadPortETA <= request.LoadPortETATo.Value);
        }

        if (request.DischargePortETAFrom.HasValue)
        {
            filter = CombineFilters(filter, x => x.DischargePortETA >= request.DischargePortETAFrom.Value);
        }

        if (request.DischargePortETATo.HasValue)
        {
            filter = CombineFilters(filter, x => x.DischargePortETA <= request.DischargePortETATo.Value);
        }

        if (request.DateFrom.HasValue)
        {
            filter = CombineFilters(filter, x => x.CreatedAt >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            filter = CombineFilters(filter, x => x.CreatedAt <= request.DateTo.Value);
        }

        // Build sort expression
        Expression<Func<ShippingOperation, object>> sortExpression = request.SortBy?.ToLower() switch
        {
            "shippingnumber" => x => x.ShippingNumber,
            "status" => x => x.Status,
            "vesselname" => x => x.VesselName,
            "loadporteta" => x => x.LoadPortETA,
            "dischargeporteta" => x => x.DischargePortETA,
            "plannedquantity" => x => x.PlannedQuantity.Value,
            "createdat" => x => x.CreatedAt,
            _ => x => x.CreatedAt
        };

        // Get paged results with includes
        var shippingOperations = await _shippingOperationRepository.GetPagedAsync(
            filter: filter,
            orderBy: sortExpression,
            orderByDescending: request.SortDescending,
            page: request.Page,
            pageSize: request.PageSize,
            includeProperties: new[]
            {
                nameof(ShippingOperation.PurchaseContract),
                nameof(ShippingOperation.SalesContract)
            },
            cancellationToken: cancellationToken);

        // Map to DTOs
        var operationDtos = shippingOperations.Items.Select(operation => new ShippingOperationSummaryDto
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

        return new PagedResult<ShippingOperationSummaryDto>(
            operationDtos,
            shippingOperations.TotalCount,
            request.Page,
            request.PageSize);
    }

    private static Expression<Func<ShippingOperation, bool>>? CombineFilters(
        Expression<Func<ShippingOperation, bool>>? existing,
        Expression<Func<ShippingOperation, bool>> newFilter)
    {
        if (existing == null)
            return newFilter;

        // Combine using AND logic
        var parameter = Expression.Parameter(typeof(ShippingOperation));
        var combined = Expression.AndAlso(
            Expression.Invoke(existing, parameter),
            Expression.Invoke(newFilter, parameter));

        return Expression.Lambda<Func<ShippingOperation, bool>>(combined, parameter);
    }
}