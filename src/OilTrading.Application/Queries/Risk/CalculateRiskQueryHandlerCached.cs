using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Common;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;

namespace OilTrading.Application.Queries.Risk;

public class CalculateRiskQueryHandlerCached : IRequestHandler<CalculateRiskQuery, RiskCalculationResultDto>
{
    private readonly IRequestHandler<CalculateRiskQuery, RiskCalculationResultDto> _innerHandler;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CalculateRiskQueryHandlerCached> _logger;

    public CalculateRiskQueryHandlerCached(
        CalculateRiskQueryHandler innerHandler,
        ICacheService cacheService,
        ILogger<CalculateRiskQueryHandlerCached> logger)
    {
        _innerHandler = innerHandler;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<RiskCalculationResultDto> Handle(CalculateRiskQuery request, CancellationToken cancellationToken)
    {
        // Risk calculations are expensive, cache for shorter time but still cache
        var cacheKey = _cacheService.GenerateKey(CacheKeys.RISK_CALCULATION);

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Cache miss for risk calculation, executing computation");
                return await _innerHandler.Handle(request, cancellationToken);
            },
            CacheKeys.Expiry.RiskCalculations,
            cancellationToken);
    }
}