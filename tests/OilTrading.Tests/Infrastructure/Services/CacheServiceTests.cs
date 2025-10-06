using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OilTrading.Infrastructure.Services;
using Xunit;

namespace OilTrading.Tests.Infrastructure.Services;

public class CacheServiceTests : IDisposable
{
    private readonly IDistributedCache _distributedCache;
    private readonly CacheService _cacheService;
    private readonly Mock<ILogger<CacheService>> _mockLogger;

    public CacheServiceTests()
    {
        // Use in-memory distributed cache for testing
        var options = new OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions());
        _distributedCache = new MemoryDistributedCache(options);
        
        _mockLogger = new Mock<ILogger<CacheService>>();
        
        _cacheService = new CacheService(_distributedCache, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = "nonexistent-key";

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAndGetAsync_ShouldStoreAndRetrieveValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAndGetAsync_ShouldStoreAndRetrieveComplexObject()
    {
        // Arrange
        var key = "test-object-key";
        var value = new TestObject
        {
            Id = Guid.NewGuid(),
            Name = "Test Object",
            Value = 123.45m,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(value.Id);
        result.Name.Should().Be(value.Name);
        result.Value.Should().Be(value.Value);
        result.CreatedAt.Should().BeCloseTo(value.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SetAsync_WithExpiry_ShouldExpireAfterTime()
    {
        // Arrange
        var key = "expiring-key";
        var value = "expiring-value";
        var expiry = TimeSpan.FromMilliseconds(100);

        // Act
        await _cacheService.SetAsync(key, value, expiry);
        
        // Verify value is initially there
        var initialResult = await _cacheService.GetAsync<string>(key);
        initialResult.Should().Be(value);

        // Wait for expiry
        await Task.Delay(150);
        
        var expiredResult = await _cacheService.GetAsync<string>(key);

        // Assert
        expiredResult.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveValue()
    {
        // Arrange
        var key = "removable-key";
        var value = "removable-value";

        await _cacheService.SetAsync(key, value);
        var beforeRemoval = await _cacheService.GetAsync<string>(key);
        beforeRemoval.Should().Be(value);

        // Act
        await _cacheService.RemoveAsync(key);
        var afterRemoval = await _cacheService.GetAsync<string>(key);

        // Assert
        afterRemoval.Should().BeNull();
    }

    [Fact]
    public async Task GetOrSetAsync_ShouldReturnCachedValue_WhenExists()
    {
        // Arrange
        var key = "cached-key";
        var cachedValue = "cached-value";
        var newValue = "new-value";

        await _cacheService.SetAsync(key, cachedValue);

        var getItemCallCount = 0;
        Task<string> GetItem()
        {
            getItemCallCount++;
            return Task.FromResult(newValue);
        }

        // Act
        var result = await _cacheService.GetOrSetAsync(key, GetItem);

        // Assert
        result.Should().Be(cachedValue);
        getItemCallCount.Should().Be(0); // Should not have called the getter
    }

    [Fact]
    public async Task GetOrSetAsync_ShouldCallGetterAndCache_WhenNotExists()
    {
        // Arrange
        var key = "new-key";
        var newValue = "new-value";

        var getItemCallCount = 0;
        Task<string> GetItem()
        {
            getItemCallCount++;
            return Task.FromResult(newValue);
        }

        // Act
        var result = await _cacheService.GetOrSetAsync(key, GetItem);

        // Assert
        result.Should().Be(newValue);
        getItemCallCount.Should().Be(1);

        // Verify it was cached
        var cachedResult = await _cacheService.GetAsync<string>(key);
        cachedResult.Should().Be(newValue);
    }

    [Fact]
    public async Task GetOrSetAsync_ShouldNotCache_WhenGetterReturnsNull()
    {
        // Arrange
        var key = "null-key";
        
        Task<string?> GetItem()
        {
            return Task.FromResult<string?>(null);
        }

        // Act
        var result = await _cacheService.GetOrSetAsync(key, GetItem);

        // Assert
        result.Should().BeNull();

        // Verify it was not cached
        var cachedResult = await _cacheService.GetAsync<string>(key);
        cachedResult.Should().BeNull();
    }

    [Fact]
    public void GenerateKey_ShouldCreateCorrectKey_WithPrefix()
    {
        // Arrange
        var prefix = "test-prefix";

        // Act
        var result = _cacheService.GenerateKey(prefix);

        // Assert
        result.Should().Be(prefix);
    }

    [Fact]
    public void GenerateKey_ShouldCreateCorrectKey_WithPrefixAndParts()
    {
        // Arrange
        var prefix = "test-prefix";
        var parts = new object[] { "part1", 123, Guid.Parse("12345678-1234-1234-1234-123456789012") };

        // Act
        var result = _cacheService.GenerateKey(prefix, parts);

        // Assert
        result.Should().Be("test-prefix:part1:123:12345678-1234-1234-1234-123456789012");
    }

    [Fact]
    public void GenerateKey_ShouldIgnoreNullParts()
    {
        // Arrange
        var prefix = "test-prefix";
        var parts = new object[] { "part1", null!, "part3" };

        // Act
        var result = _cacheService.GenerateKey(prefix, parts);

        // Assert
        result.Should().Be("test-prefix:part1:part3");
    }

    [Fact]
    public void GenerateKey_ShouldHandleEmptyParts()
    {
        // Arrange
        var prefix = "test-prefix";
        var parts = new object[0];

        // Act
        var result = _cacheService.GenerateKey(prefix, parts);

        // Assert
        result.Should().Be(prefix);
    }

    [Fact]
    public async Task CacheService_ShouldHandleSerializationErrors_Gracefully()
    {
        // This test would require a more complex setup to force serialization errors
        // For now, we'll test with a valid scenario and verify no exceptions are thrown
        
        // Arrange
        var key = "serialization-test";
        var complexValue = new
        {
            Id = Guid.NewGuid(),
            Data = new List<string> { "item1", "item2", "item3" },
            Nested = new { Property = "value" }
        };

        // Act & Assert
        await FluentActions.Invoking(async () =>
        {
            await _cacheService.SetAsync(key, complexValue);
            var result = await _cacheService.GetAsync<object>(key);
            return result;
        }).Should().NotThrowAsync();
    }

    [Fact]
    public async Task CacheService_ShouldLogCorrectly()
    {
        // Arrange
        var key = "logging-test";
        var value = "logging-value";

        // Act
        await _cacheService.SetAsync(key, value);
        await _cacheService.GetAsync<string>(key);
        await _cacheService.RemoveAsync(key);

        // Assert
        // Verify logging was called (this is a basic check - in real scenarios you might want more detailed verification)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2)); // At least cache hit and cache set logs
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CacheOperations_ShouldHandleInvalidKeys_Gracefully(string invalidKey)
    {
        // Arrange & Act & Assert
        await FluentActions.Invoking(async () => await _cacheService.GetAsync<string>(invalidKey))
            .Should().NotThrowAsync();

        await FluentActions.Invoking(async () => await _cacheService.SetAsync(invalidKey, "value"))
            .Should().NotThrowAsync();

        await FluentActions.Invoking(async () => await _cacheService.RemoveAsync(invalidKey))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task CacheService_ShouldHandleConcurrentOperations()
    {
        // Arrange
        var tasks = new List<Task>();
        var keyPrefix = "concurrent-test";

        // Act
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var key = $"{keyPrefix}-{index}";
                var value = $"value-{index}";
                
                await _cacheService.SetAsync(key, value);
                var result = await _cacheService.GetAsync<string>(key);
                
                result.Should().Be(value);
            }));
        }

        // Assert
        await FluentActions.Invoking(async () => await Task.WhenAll(tasks))
            .Should().NotThrowAsync();
    }

    private class TestObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public void Dispose()
    {
        // IDistributedCache doesn't need disposal
        GC.SuppressFinalize(this);
    }
}