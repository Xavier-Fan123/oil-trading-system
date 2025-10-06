using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Application.Common;
using OilTrading.Core.Entities;
using System.Linq.Expressions;

namespace OilTrading.Application.Queries.PurchaseContracts;

public class GetAvailablePurchaseContractsQueryHandler : IRequestHandler<GetAvailablePurchaseContractsQuery, PagedResult<AvailablePurchaseContractDto>>
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;

    public GetAvailablePurchaseContractsQueryHandler(IPurchaseContractRepository purchaseContractRepository)
    {
        _purchaseContractRepository = purchaseContractRepository;
    }

    public async Task<PagedResult<AvailablePurchaseContractDto>> Handle(GetAvailablePurchaseContractsQuery request, CancellationToken cancellationToken)
    {
        // Build filter expression for active contracts only
        Expression<Func<PurchaseContract, bool>>? filter = x => x.Status == ContractStatus.Active;

        if (request.ProductId.HasValue)
        {
            filter = CombineFilters(filter, x => x.ProductId == request.ProductId.Value);
        }

        if (request.TraderId.HasValue)
        {
            filter = CombineFilters(filter, x => x.TraderId == request.TraderId.Value);
        }

        // Build sort expression
        Expression<Func<PurchaseContract, object>> sortExpression = request.SortBy?.ToLower() switch
        {
            "contractnumber" => x => x.ContractNumber.Value,
            "supplier" => x => x.TradingPartner.Name,
            "product" => x => x.Product.ProductName,
            "contractvalue" => x => x.ContractValue != null ? x.ContractValue.Amount : 0,
            "laycanstart" => x => x.LaycanStart ?? DateTime.MinValue,
            _ => x => x.CreatedAt
        };

        // Get all matching contracts with related data
        var allContracts = await _purchaseContractRepository.GetPagedAsync(
            filter: filter,
            orderBy: sortExpression,
            orderByDescending: request.SortDescending,
            page: 1,
            pageSize: int.MaxValue, // Get all to calculate available quantities
            includeProperties: new[]
            {
                nameof(PurchaseContract.TradingPartner),
                nameof(PurchaseContract.Product),
                nameof(PurchaseContract.Trader),
                nameof(PurchaseContract.LinkedSalesContracts)
            },
            cancellationToken: cancellationToken);

        // Calculate available quantities and filter contracts with available quantity > 0
        var contractsWithAvailableQuantity = allContracts.Items
            .Select(contract => new
            {
                Contract = contract,
                AvailableQuantity = contract.GetAvailableQuantity().Value
            })
            .Where(x => x.AvailableQuantity > 0)
            .ToList();

        // Apply pagination manually since we filtered after retrieval
        var totalCount = contractsWithAvailableQuantity.Count;
        var paginatedContracts = contractsWithAvailableQuantity
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AvailablePurchaseContractDto
            {
                Id = x.Contract.Id,
                ContractNumber = x.Contract.ContractNumber.Value,
                SupplierName = x.Contract.TradingPartner?.Name ?? "Unknown",
                ProductName = x.Contract.Product?.ProductName ?? "Unknown",
                TraderName = x.Contract.Trader != null ? $"{x.Contract.Trader.FirstName} {x.Contract.Trader.LastName}" : "Unknown",
                TotalQuantity = x.Contract.ContractQuantity.Value,
                AvailableQuantity = x.AvailableQuantity,
                QuantityUnit = x.Contract.ContractQuantity.Unit.ToString(),
                ContractValue = x.Contract.ContractValue?.Amount,
                ContractValueCurrency = x.Contract.ContractValue?.Currency,
                LaycanStart = x.Contract.LaycanStart,
                LaycanEnd = x.Contract.LaycanEnd,
                CreatedAt = x.Contract.CreatedAt
            })
            .ToList();

        return new PagedResult<AvailablePurchaseContractDto>(
            paginatedContracts,
            totalCount,
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