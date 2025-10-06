using MediatR;
using AutoMapper;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;
using System.Linq.Expressions;

namespace OilTrading.Application.Queries.SalesContracts;

public class GetSalesContractsQueryHandler : IRequestHandler<GetSalesContractsQuery, PagedResult<SalesContractSummaryDto>>
{
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IMapper _mapper;

    public GetSalesContractsQueryHandler(
        ISalesContractRepository salesContractRepository,
        IMapper mapper)
    {
        _salesContractRepository = salesContractRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<SalesContractSummaryDto>> Handle(GetSalesContractsQuery request, CancellationToken cancellationToken)
    {
        // Build filter expression
        Expression<Func<SalesContract, bool>>? filter = null;

        if (!string.IsNullOrEmpty(request.ContractNumber))
        {
            var contractNumber = request.ContractNumber.Trim();
            filter = CombineFilters(filter, x => x.ContractNumber.Value.Contains(contractNumber));
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            if (Enum.TryParse<ContractStatus>(request.Status, true, out var status))
            {
                filter = CombineFilters(filter, x => x.Status == status);
            }
        }

        if (request.CustomerId.HasValue)
        {
            filter = CombineFilters(filter, x => x.TradingPartnerId == request.CustomerId.Value);
        }

        if (request.ProductId.HasValue)
        {
            filter = CombineFilters(filter, x => x.ProductId == request.ProductId.Value);
        }

        if (request.TraderId.HasValue)
        {
            filter = CombineFilters(filter, x => x.TraderId == request.TraderId.Value);
        }

        if (request.LinkedPurchaseContractId.HasValue)
        {
            filter = CombineFilters(filter, x => x.LinkedPurchaseContractId == request.LinkedPurchaseContractId.Value);
        }

        if (request.DateFrom.HasValue)
        {
            filter = CombineFilters(filter, x => x.CreatedAt >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            filter = CombineFilters(filter, x => x.CreatedAt <= request.DateTo.Value);
        }

        if (request.LaycanFrom.HasValue)
        {
            filter = CombineFilters(filter, x => x.LaycanStart.HasValue && x.LaycanStart >= request.LaycanFrom.Value);
        }

        if (request.LaycanTo.HasValue)
        {
            filter = CombineFilters(filter, x => x.LaycanEnd.HasValue && x.LaycanEnd <= request.LaycanTo.Value);
        }

        // Build sort expression
        Expression<Func<SalesContract, object>> sortExpression = request.SortBy?.ToLower() switch
        {
            "contractnumber" => x => x.ContractNumber.Value,
            "status" => x => x.Status,
            "customer" => x => x.TradingPartner.Name,
            "quantity" => x => x.ContractQuantity.Value,
            "contractvalue" => x => x.ContractValue != null ? x.ContractValue.Amount : 0,
            "laycanstart" => x => x.LaycanStart ?? DateTime.MinValue,
            "laycanend" => x => x.LaycanEnd ?? DateTime.MinValue,
            "createdat" => x => x.CreatedAt,
            _ => x => x.CreatedAt
        };

        // Get paged results with includes
        var contracts = await _salesContractRepository.GetPagedAsync(
            filter: filter,
            orderBy: sortExpression,
            orderByDescending: request.SortDescending,
            page: request.Page,
            pageSize: request.PageSize,
            includeProperties: new[]
            {
                nameof(SalesContract.TradingPartner),
                nameof(SalesContract.Product),
                nameof(SalesContract.Trader)
            },
            cancellationToken: cancellationToken);

        // Map to DTOs using AutoMapper
        var contractDtos = _mapper.Map<List<SalesContractSummaryDto>>(contracts.Items);

        return new PagedResult<SalesContractSummaryDto>(
            contractDtos,
            contracts.TotalCount,
            request.Page,
            request.PageSize);
    }

    private static Expression<Func<SalesContract, bool>>? CombineFilters(
        Expression<Func<SalesContract, bool>>? existing,
        Expression<Func<SalesContract, bool>> newFilter)
    {
        if (existing == null)
            return newFilter;

        // Combine using AND logic
        var parameter = Expression.Parameter(typeof(SalesContract));
        var combined = Expression.AndAlso(
            Expression.Invoke(existing, parameter),
            Expression.Invoke(newFilter, parameter));

        return Expression.Lambda<Func<SalesContract, bool>>(combined, parameter);
    }
}