using Microsoft.Extensions.Logging;
using OilTrading.Application.Common;

namespace OilTrading.Application.Services;

public interface ICacheInvalidationService
{
    Task InvalidatePurchaseContractCacheAsync(Guid? contractId = null);
    Task InvalidateSalesContractCacheAsync(Guid? contractId = null);
    Task InvalidateRiskCacheAsync();
    Task InvalidateMarketDataCacheAsync();
    Task InvalidateDashboardCacheAsync();
    Task InvalidateReferenceDataCacheAsync();
}

public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(ICacheService cacheService, ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task InvalidatePurchaseContractCacheAsync(Guid? contractId = null)
    {
        try
        {
            // Remove all purchase contract lists
            await _cacheService.RemovePatternAsync($"{CacheKeys.PURCHASE_CONTRACTS}:*");
            
            if (contractId.HasValue)
            {
                // Remove specific contract cache
                var contractKey = _cacheService.GenerateKey(CacheKeys.PURCHASE_CONTRACT, contractId);
                await _cacheService.RemoveAsync(contractKey);
                
                // Remove available quantity cache
                var quantityKey = _cacheService.GenerateKey(CacheKeys.PURCHASE_CONTRACT_AVAILABLE_QUANTITY, contractId);
                await _cacheService.RemoveAsync(quantityKey);
            }

            // Invalidate related caches
            await InvalidateRiskCacheAsync();
            await InvalidateDashboardCacheAsync();

            _logger.LogInformation("Purchase contract cache invalidated for contract: {ContractId}", contractId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating purchase contract cache");
        }
    }

    public async Task InvalidateSalesContractCacheAsync(Guid? contractId = null)
    {
        try
        {
            // Remove all sales contract lists
            await _cacheService.RemovePatternAsync($"{CacheKeys.SALES_CONTRACTS}:*");
            
            if (contractId.HasValue)
            {
                // Remove specific contract cache
                var contractKey = _cacheService.GenerateKey(CacheKeys.SALES_CONTRACT, contractId);
                await _cacheService.RemoveAsync(contractKey);
            }

            // Invalidate related caches
            await InvalidateRiskCacheAsync();
            await InvalidateDashboardCacheAsync();

            _logger.LogInformation("Sales contract cache invalidated for contract: {ContractId}", contractId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating sales contract cache");
        }
    }

    public async Task InvalidateRiskCacheAsync()
    {
        try
        {
            // Remove all risk-related caches
            await _cacheService.RemoveAsync(CacheKeys.RISK_CALCULATION);
            await _cacheService.RemoveAsync(CacheKeys.PORTFOLIO_SUMMARY);
            await _cacheService.RemovePatternAsync($"{CacheKeys.PRODUCT_RISK}:*");
            await _cacheService.RemovePatternAsync($"{CacheKeys.RISK_BACKTEST}:*");

            _logger.LogInformation("Risk calculation cache invalidated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating risk cache");
        }
    }

    public async Task InvalidateMarketDataCacheAsync()
    {
        try
        {
            // Remove market data caches
            await _cacheService.RemovePatternAsync($"{CacheKeys.MARKET_DATA}:*");
            await _cacheService.RemovePatternAsync($"{CacheKeys.PRICE_HISTORY}:*");
            await _cacheService.RemoveAsync(CacheKeys.LATEST_PRICES);

            // Market data changes affect risk calculations
            await InvalidateRiskCacheAsync();

            _logger.LogInformation("Market data cache invalidated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating market data cache");
        }
    }

    public async Task InvalidateDashboardCacheAsync()
    {
        try
        {
            // Remove dashboard caches
            await _cacheService.RemoveAsync(CacheKeys.DASHBOARD_OVERVIEW);
            await _cacheService.RemovePatternAsync($"{CacheKeys.DASHBOARD_METRICS}:*");
            await _cacheService.RemoveAsync(CacheKeys.PERFORMANCE_ANALYTICS);

            _logger.LogInformation("Dashboard cache invalidated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating dashboard cache");
        }
    }

    public async Task InvalidateReferenceDataCacheAsync()
    {
        try
        {
            // Remove reference data caches (products, trading partners)
            await _cacheService.RemoveAsync(CacheKeys.PRODUCTS);
            await _cacheService.RemoveAsync(CacheKeys.TRADING_PARTNERS);
            await _cacheService.RemovePatternAsync($"{CacheKeys.PRODUCT}:*");
            await _cacheService.RemovePatternAsync($"{CacheKeys.TRADING_PARTNER}:*");

            _logger.LogInformation("Reference data cache invalidated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating reference data cache");
        }
    }
}