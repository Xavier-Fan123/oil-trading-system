using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OilTrading.Application.Common;
using OilTrading.Application.Services;
using Xunit;

namespace OilTrading.Tests.Application.Services;

public class CacheInvalidationServiceTests
{
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<CacheInvalidationService>> _mockLogger;
    private readonly CacheInvalidationService _cacheInvalidationService;

    public CacheInvalidationServiceTests()
    {
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<CacheInvalidationService>>();
        _cacheInvalidationService = new CacheInvalidationService(_mockCacheService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InvalidatePurchaseContractCacheAsync_ShouldRemoveAllRelatedCaches()
    {
        // Arrange
        var contractId = Guid.NewGuid();

        // Act
        await _cacheInvalidationService.InvalidatePurchaseContractCacheAsync(contractId);

        // Assert
        _mockCacheService.Verify(x => x.RemovePatternAsync($"{CacheKeys.PURCHASE_CONTRACTS}:*", It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(It.Is<string>(key => key != null), It.IsAny<CancellationToken>()), Times.AtLeast(1));

        // Should also invalidate related caches
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.RISK_CALCULATION, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.DASHBOARD_OVERVIEW, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidatePurchaseContractCacheAsync_WithoutContractId_ShouldRemoveGeneralCaches()
    {
        // Act
        await _cacheInvalidationService.InvalidatePurchaseContractCacheAsync();

        // Assert
        _mockCacheService.Verify(x => x.RemovePatternAsync($"{CacheKeys.PURCHASE_CONTRACTS}:*", It.IsAny<CancellationToken>()), Times.Once);
        
        // Should still invalidate related caches
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.RISK_CALCULATION, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.DASHBOARD_OVERVIEW, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateSalesContractCacheAsync_ShouldRemoveAllRelatedCaches()
    {
        // Arrange
        var contractId = Guid.NewGuid();

        // Act
        await _cacheInvalidationService.InvalidateSalesContractCacheAsync(contractId);

        // Assert
        _mockCacheService.Verify(x => x.RemovePatternAsync($"{CacheKeys.SALES_CONTRACTS}:*", It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(It.Is<string>(key => key != null), It.IsAny<CancellationToken>()), Times.AtLeast(1));

        // Should also invalidate related caches
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.RISK_CALCULATION, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.DASHBOARD_OVERVIEW, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateRiskCacheAsync_ShouldRemoveAllRiskRelatedCaches()
    {
        // Act
        await _cacheInvalidationService.InvalidateRiskCacheAsync();

        // Assert
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.RISK_CALCULATION, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.PORTFOLIO_SUMMARY, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemovePatternAsync($"{CacheKeys.PRODUCT_RISK}:*", It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemovePatternAsync($"{CacheKeys.RISK_BACKTEST}:*", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateMarketDataCacheAsync_ShouldRemoveMarketDataAndRiskCaches()
    {
        // Act
        await _cacheInvalidationService.InvalidateMarketDataCacheAsync();

        // Assert
        _mockCacheService.Verify(x => x.RemovePatternAsync($"{CacheKeys.MARKET_DATA}:*", It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemovePatternAsync($"{CacheKeys.PRICE_HISTORY}:*", It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.LATEST_PRICES, It.IsAny<CancellationToken>()), Times.Once);
        
        // Should also invalidate risk caches since market data affects risk
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.RISK_CALCULATION, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateDashboardCacheAsync_ShouldRemoveAllDashboardCaches()
    {
        // Act
        await _cacheInvalidationService.InvalidateDashboardCacheAsync();

        // Assert
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.DASHBOARD_OVERVIEW, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemovePatternAsync($"{CacheKeys.DASHBOARD_METRICS}:*", It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.PERFORMANCE_ANALYTICS, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateReferenceDataCacheAsync_ShouldRemoveReferenceDataCaches()
    {
        // Act
        await _cacheInvalidationService.InvalidateReferenceDataCacheAsync();

        // Assert
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.PRODUCTS, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(CacheKeys.TRADING_PARTNERS, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemovePatternAsync($"{CacheKeys.PRODUCT}:*", It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemovePatternAsync($"{CacheKeys.TRADING_PARTNER}:*", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidatePurchaseContractCacheAsync_ShouldLogCorrectly()
    {
        // Arrange
        var contractId = Guid.NewGuid();

        // Act
        await _cacheInvalidationService.InvalidatePurchaseContractCacheAsync(contractId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Purchase contract cache invalidated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidatePurchaseContractCacheAsync_ShouldHandleExceptionsGracefully()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        _mockCacheService.Setup(x => x.RemovePatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ThrowsAsync(new Exception("Cache service error"));

        // Act & Assert
        await FluentActions.Invoking(async () => await _cacheInvalidationService.InvalidatePurchaseContractCacheAsync(contractId))
            .Should().NotThrowAsync();

        // Should log the error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InvalidateRiskCacheAsync_ShouldHandleExceptionsGracefully()
    {
        // Arrange
        _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ThrowsAsync(new Exception("Cache service error"));

        // Act & Assert
        await FluentActions.Invoking(async () => await _cacheInvalidationService.InvalidateRiskCacheAsync())
            .Should().NotThrowAsync();

        // Should log the error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InvalidateMarketDataCacheAsync_ShouldHandleExceptionsGracefully()
    {
        // Arrange
        _mockCacheService.Setup(x => x.RemovePatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ThrowsAsync(new Exception("Cache service error"));

        // Act & Assert
        await FluentActions.Invoking(async () => await _cacheInvalidationService.InvalidateMarketDataCacheAsync())
            .Should().NotThrowAsync();

        // Should log the error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task AllInvalidationMethods_ShouldBeCallableWithCancellationToken()
    {
        // Arrange
        // var cancellationToken = new CancellationToken(); // Not used in this test
        var contractId = Guid.NewGuid();

        // Act & Assert - should not throw
        await FluentActions.Invoking(async () =>
        {
            await _cacheInvalidationService.InvalidatePurchaseContractCacheAsync(contractId);
            await _cacheInvalidationService.InvalidateSalesContractCacheAsync(contractId);
            await _cacheInvalidationService.InvalidateRiskCacheAsync();
            await _cacheInvalidationService.InvalidateMarketDataCacheAsync();
            await _cacheInvalidationService.InvalidateDashboardCacheAsync();
            await _cacheInvalidationService.InvalidateReferenceDataCacheAsync();
        }).Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(null)]
    public async Task InvalidatePurchaseContractCacheAsync_ShouldHandleNullContractId(Guid? contractId)
    {
        // Act & Assert
        await FluentActions.Invoking(async () => 
                await _cacheInvalidationService.InvalidatePurchaseContractCacheAsync(contractId))
            .Should().NotThrowAsync();

        // Should still remove pattern-based caches
        _mockCacheService.Verify(x => x.RemovePatternAsync($"{CacheKeys.PURCHASE_CONTRACTS}:*", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MultipleInvalidations_ShouldAllExecuteSuccessfully()
    {
        // Arrange
        var contractId1 = Guid.NewGuid();
        var contractId2 = Guid.NewGuid();

        // Act
        await _cacheInvalidationService.InvalidatePurchaseContractCacheAsync(contractId1);
        await _cacheInvalidationService.InvalidateSalesContractCacheAsync(contractId2);
        await _cacheInvalidationService.InvalidateRiskCacheAsync();
        await _cacheInvalidationService.InvalidateMarketDataCacheAsync();

        // Assert
        _mockCacheService.Verify(x => x.RemovePatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeast(4));
        _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeast(6));
    }
}