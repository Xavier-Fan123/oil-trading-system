using OilTrading.Application.DTOs;

namespace OilTrading.Application.Services;

public interface IPositionCacheService
{
    Task<IEnumerable<NetPositionDto>?> GetCachedPositionsAsync(CancellationToken cancellationToken = default);
    Task SetCachedPositionsAsync(IEnumerable<NetPositionDto> positions, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<PositionSummaryDto?> GetCachedSummaryAsync(CancellationToken cancellationToken = default);
    Task SetCachedSummaryAsync(PositionSummaryDto summary, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<PnLDto>?> GetCachedPnLAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default);
    Task SetCachedPnLAsync(IEnumerable<PnLDto> pnl, DateTime? asOfDate = null, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task InvalidatePositionCacheAsync(CancellationToken cancellationToken = default);
    Task InvalidateAllCacheAsync(CancellationToken cancellationToken = default);
    Task<bool> IsCacheValidAsync(CancellationToken cancellationToken = default);
    Task SetLastUpdateTimeAsync(DateTime updateTime, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastUpdateTimeAsync(CancellationToken cancellationToken = default);
    Task<T?> GetCachedDataAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetCachedDataAsync<T>(string key, T data, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    Task<decimal> GetCacheHitRatioAsync(CancellationToken cancellationToken = default);
}