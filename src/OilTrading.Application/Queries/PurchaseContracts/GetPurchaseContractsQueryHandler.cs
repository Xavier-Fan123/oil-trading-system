using MediatR;
using AutoMapper;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;
using System.Linq.Expressions;

namespace OilTrading.Application.Queries.PurchaseContracts;

public class GetPurchaseContractsQueryHandler : IRequestHandler<GetPurchaseContractsQuery, PagedResult<PurchaseContractListDto>>
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly IMapper _mapper;

    public GetPurchaseContractsQueryHandler(
        IPurchaseContractRepository purchaseContractRepository,
        IMapper mapper)
    {
        _purchaseContractRepository = purchaseContractRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<PurchaseContractListDto>> Handle(GetPurchaseContractsQuery request, CancellationToken cancellationToken)
    {
        // Build filter expression
        Expression<Func<PurchaseContract, bool>>? filter = null;

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

        if (request.SupplierId.HasValue)
        {
            filter = CombineFilters(filter, x => x.TradingPartnerId == request.SupplierId.Value);
        }

        if (request.ProductId.HasValue)
        {
            filter = CombineFilters(filter, x => x.ProductId == request.ProductId.Value);
        }

        if (request.TraderId.HasValue)
        {
            filter = CombineFilters(filter, x => x.TraderId == request.TraderId.Value);
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
            filter = CombineFilters(filter, x => x.LaycanStart >= request.LaycanFrom.Value);
        }

        if (request.LaycanTo.HasValue)
        {
            filter = CombineFilters(filter, x => x.LaycanEnd <= request.LaycanTo.Value);
        }

        // Build sort expression
        Expression<Func<PurchaseContract, object>> sortExpression = request.SortBy?.ToLower() switch
        {
            "contractnumber" => x => x.ContractNumber.Value,
            "status" => x => x.Status,
            "quantity" => x => x.ContractQuantity.Value,
            "contractvalue" => x => x.ContractValue != null ? x.ContractValue.Amount : 0,
            "laycanstart" => x => x.LaycanStart ?? DateTime.MinValue,
            "laycanend" => x => x.LaycanEnd ?? DateTime.MinValue,
            "createdat" => x => x.CreatedAt,
            _ => x => x.CreatedAt
        };

        // Get paged results with includes
        var contracts = await _purchaseContractRepository.GetPagedAsync(
            filter: filter,
            orderBy: sortExpression,
            orderByDescending: request.SortDescending,
            page: request.Page,
            pageSize: request.PageSize,
            includeProperties: new[]
            {
                nameof(PurchaseContract.TradingPartner),
                nameof(PurchaseContract.Product),
                nameof(PurchaseContract.Trader),
                nameof(PurchaseContract.ShippingOperations),
                nameof(PurchaseContract.LinkedSalesContracts)
            },
            cancellationToken: cancellationToken);

        // Map to DTOs using AutoMapper
        var contractDtos = _mapper.Map<List<PurchaseContractListDto>>(contracts.Items);

        return new PagedResult<PurchaseContractListDto>(
            contractDtos,
            contracts.TotalCount,
            request.Page,
            request.PageSize);
    }

    private static Expression<Func<PurchaseContract, bool>>? CombineFilters(
        Expression<Func<PurchaseContract, bool>>? existing,
        Expression<Func<PurchaseContract, bool>> newFilter)
    {
        if (existing == null)
            return newFilter;

        // Combine using AND logic
        var parameter = Expression.Parameter(typeof(PurchaseContract));
        var combined = Expression.AndAlso(
            Expression.Invoke(existing, parameter),
            Expression.Invoke(newFilter, parameter));

        return Expression.Lambda<Func<PurchaseContract, bool>>(combined, parameter);
    }
}