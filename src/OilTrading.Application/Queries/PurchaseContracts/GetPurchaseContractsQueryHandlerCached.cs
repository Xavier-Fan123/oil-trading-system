using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Common;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;

namespace OilTrading.Application.Queries.PurchaseContracts;

public class GetPurchaseContractsQueryHandlerCached : IRequestHandler<GetPurchaseContractsQuery, PagedResult<PurchaseContractListDto>>
{
    private readonly IRequestHandler<GetPurchaseContractsQuery, PagedResult<PurchaseContractListDto>> _innerHandler;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetPurchaseContractsQueryHandlerCached> _logger;

    public GetPurchaseContractsQueryHandlerCached(
        GetPurchaseContractsQueryHandler innerHandler,
        ICacheService cacheService,
        ILogger<GetPurchaseContractsQueryHandlerCached> logger)
    {
        _innerHandler = innerHandler;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<PagedResult<PurchaseContractListDto>> Handle(GetPurchaseContractsQuery request, CancellationToken cancellationToken)
    {
        // Generate cache key based on query parameters
        var cacheKey = _cacheService.GenerateKey(
            CacheKeys.PURCHASE_CONTRACTS,
            request.Status,
            request.SupplierId,
            request.ProductId,
            request.LaycanFrom?.ToString("yyyy-MM-dd"),
            request.LaycanTo?.ToString("yyyy-MM-dd"),
            request.Page,
            request.PageSize);

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Cache miss for purchase contracts query, executing database query");
                return await _innerHandler.Handle(request, cancellationToken);
            },
            CacheKeys.Expiry.Contracts,
            cancellationToken);
    }
}