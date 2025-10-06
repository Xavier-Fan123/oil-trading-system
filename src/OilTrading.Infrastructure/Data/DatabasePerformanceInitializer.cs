using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OilTrading.Infrastructure.Data.Extensions;
using OilTrading.Infrastructure.Data.QueryOptimization;

namespace OilTrading.Infrastructure.Data;

/// <summary>
/// 数据库性能初始化器 - 在应用启动时优化数据库配置
/// </summary>
public class DatabasePerformanceInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabasePerformanceInitializer> _logger;
    private readonly DatabaseOptimizationOptions _options;

    public DatabasePerformanceInitializer(
        ApplicationDbContext context,
        ILogger<DatabasePerformanceInitializer> logger,
        IOptions<DatabaseOptimizationOptions> options)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 初始化数据库性能优化配置
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Starting database performance initialization");

        try
        {
            // 1. 确保数据库存在
            await EnsureDatabaseCreatedAsync();

            // 2. 检查并创建关键索引
            if (_options.CreatePerformanceIndexes)
            {
                await CreatePerformanceIndexesAsync();
            }

            // 3. 更新数据库统计信息
            if (_options.UpdateStatisticsOnStartup)
            {
                await UpdateDatabaseStatisticsAsync();
            }

            // 4. 预热缓存
            if (_options.WarmupCacheOnStartup)
            {
                await WarmupCacheAsync();
            }

            // 5. 验证查询性能
            await ValidateQueryPerformanceAsync();

            _logger.LogInformation("Database performance initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database performance initialization failed");
            throw;
        }
    }

    private async Task EnsureDatabaseCreatedAsync()
    {
        try
        {
            var created = await _context.Database.EnsureCreatedAsync();
            if (created)
            {
                _logger.LogInformation("Database created successfully");
            }
            else
            {
                _logger.LogDebug("Database already exists");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure database creation - continuing with existing database");
        }
    }

    private async Task CreatePerformanceIndexesAsync()
    {
        _logger.LogInformation("Creating performance-critical indexes");

        var indexScripts = new[]
        {
            // 合同查询核心索引
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PurchaseContracts_Performance_Core')
              CREATE NONCLUSTERED INDEX IX_PurchaseContracts_Performance_Core
              ON PurchaseContracts (Status, CreatedAt DESC)
              INCLUDE (TradingPartnerId, ProductId, TraderId)
              WHERE Status IN (1, 2, 3);",

            // 价格事件时序索引
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PricingEvents_TimeSeries_Core')
              CREATE NONCLUSTERED INDEX IX_PricingEvents_TimeSeries_Core
              ON PricingEvents (ProductId, EventDate DESC)
              INCLUDE (Price_Amount, EventType)
              WHERE EventDate >= DATEADD(month, -12, GETDATE());",

            // 库存查询索引
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryPositions_Core')
              CREATE NONCLUSTERED INDEX IX_InventoryPositions_Core
              ON InventoryPositions (ProductId, LocationId, LastUpdated DESC)
              INCLUDE (AvailableQuantity_Value, ReservedQuantity_Value);",

            // 交易合作伙伴查询索引
            @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TradingPartners_Performance')
              CREATE NONCLUSTERED INDEX IX_TradingPartners_Performance
              ON TradingPartners (IsActive, PartnerType)
              INCLUDE (Name, Country, CreditRating);"
        };

        foreach (var script in indexScripts)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(script);
                _logger.LogDebug("Created performance index successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create performance index: {Script}", script);
            }
        }
    }

    private async Task UpdateDatabaseStatisticsAsync()
    {
        _logger.LogInformation("Updating database statistics");

        try
        {
            await _context.Database.ExecuteSqlRawAsync("EXEC sp_updatestats");
            _logger.LogInformation("Database statistics updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update database statistics");
        }
    }

    private async Task WarmupCacheAsync()
    {
        _logger.LogInformation("Starting cache warmup");

        try
        {
            // 预热关键查询
            var warmupQueries = new[]
            {
                // 预热活动合同查询
                async () => await _context.PurchaseContracts
                    .Where(c => c.Status == Core.Entities.ContractStatus.Active)
                    .Take(100)
                    .AsNoTracking()
                    .CountAsync(),

                // 预热产品数据
                async () => await _context.Products
                    .Where(p => p.IsActive)
                    .AsNoTracking()
                    .CountAsync(),

                // 预热交易伙伴数据
                async () => await _context.TradingPartners
                    .Where(tp => tp.IsActive)
                    .AsNoTracking()
                    .CountAsync(),

                // 预热近期价格数据
                async () => await _context.PricingEvents
                    .Where(pe => pe.EventDate >= DateTime.UtcNow.AddDays(-30))
                    .AsNoTracking()
                    .CountAsync()
            };

            var warmupTasks = warmupQueries.Select(async query =>
            {
                try
                {
                    await query();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cache warmup query failed");
                }
            });

            await Task.WhenAll(warmupTasks);

            _logger.LogInformation("Cache warmup completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache warmup failed");
        }
    }

    private async Task ValidateQueryPerformanceAsync()
    {
        _logger.LogInformation("Validating query performance");

        var performanceTests = new[]
        {
            new PerformanceTest
            {
                Name = "Active Contracts Query",
                Query = async () => await _context.PurchaseContracts
                    .Where(c => c.Status == Core.Entities.ContractStatus.Active)
                    .Include(c => c.TradingPartner)
                    .Include(c => c.Product)
                    .AsNoTracking()
                    .Take(50)
                    .ToListAsync(),
                ExpectedMaxDurationMs = 500
            },
            new PerformanceTest
            {
                Name = "Recent Price Events Query",
                Query = async () => await _context.PricingEvents
                    .Where(pe => pe.EventDate >= DateTime.UtcNow.AddDays(-7))
                    .OrderByDescending(pe => pe.EventDate)
                    .AsNoTracking()
                    .Take(100)
                    .ToListAsync(),
                ExpectedMaxDurationMs = 300
            },
            new PerformanceTest
            {
                Name = "Trading Partners Query",
                Query = async () => await _context.TradingPartners
                    .Where(tp => tp.IsActive)
                    .AsNoTracking()
                    .ToListAsync(),
                ExpectedMaxDurationMs = 200
            }
        };

        foreach (var test in performanceTests)
        {
            await ValidateQueryPerformance(test);
        }
    }

    private async Task ValidateQueryPerformance(PerformanceTest test)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await test.Query();
            stopwatch.Stop();

            var duration = stopwatch.ElapsedMilliseconds;

            if (duration <= test.ExpectedMaxDurationMs)
            {
                _logger.LogInformation("✅ {TestName}: {Duration}ms (Expected: <{Expected}ms)", 
                    test.Name, duration, test.ExpectedMaxDurationMs);
            }
            else
            {
                _logger.LogWarning("⚠️ {TestName}: {Duration}ms (Expected: <{Expected}ms) - Performance degraded", 
                    test.Name, duration, test.ExpectedMaxDurationMs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ {TestName}: Query failed", test.Name);
        }
    }

    /// <summary>
    /// 获取数据库性能诊断信息
    /// </summary>
    public async Task<DatabaseDiagnostics> GetDatabaseDiagnosticsAsync()
    {
        var diagnostics = new DatabaseDiagnostics
        {
            GeneratedAt = DateTime.UtcNow
        };

        try
        {
            // 获取表大小信息
            var tableSizes = await _context.Database.SqlQuery<TableSizeInfo>($@"
                SELECT 
                    SCHEMA_NAME(t.schema_id) as SchemaName,
                    t.name as TableName,
                    SUM(p.rows) as RowCount,
                    SUM(a.total_pages) * 8 as TotalSpaceKB,
                    SUM(a.used_pages) * 8 as UsedSpaceKB
                FROM sys.tables t
                INNER JOIN sys.indexes i ON t.object_id = i.object_id
                INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
                INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
                WHERE t.name IN ('PurchaseContracts', 'PricingEvents', 'TradingPartners', 'Products', 'InventoryPositions')
                GROUP BY t.schema_id, t.name
                ORDER BY SUM(a.total_pages) DESC
            ").ToListAsync();

            diagnostics.TableSizes = tableSizes.ToList();

            // 获取索引碎片信息
            var indexFragmentation = await _context.Database.SqlQuery<IndexFragmentationInfo>($@"
                SELECT TOP 10
                    OBJECT_SCHEMA_NAME(ips.object_id) as SchemaName,
                    OBJECT_NAME(ips.object_id) as TableName,
                    i.name as IndexName,
                    ips.avg_fragmentation_in_percent as Fragmentation,
                    ips.page_count as PageCount
                FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
                INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
                WHERE ips.avg_fragmentation_in_percent > 10
                  AND ips.page_count > 100
                  AND i.name IS NOT NULL
                ORDER BY ips.avg_fragmentation_in_percent DESC
            ").ToListAsync();

            diagnostics.IndexFragmentation = indexFragmentation.ToList();

            // 获取数据库配置信息
            diagnostics.DatabaseVersion = "Database Version Info"; // Simplified for compatibility
            diagnostics.DatabaseName = _context.Database.GetDbConnection().Database;

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate complete database diagnostics");
            diagnostics.ErrorMessage = ex.Message;
        }

        return diagnostics;
    }
}

/// <summary>
/// 数据库优化配置选项
/// </summary>
public class DatabaseOptimizationOptions
{
    public bool CreatePerformanceIndexes { get; set; } = true;
    public bool UpdateStatisticsOnStartup { get; set; } = true;
    public bool WarmupCacheOnStartup { get; set; } = true;
    public bool EnableQueryLogging { get; set; } = false;
    public int SlowQueryThresholdMs { get; set; } = 1000;
    public bool EnableAutomaticMaintenance { get; set; } = false;
}

/// <summary>
/// 性能测试配置
/// </summary>
public class PerformanceTest
{
    public string Name { get; set; } = string.Empty;
    public Func<Task<object>> Query { get; set; } = null!;
    public int ExpectedMaxDurationMs { get; set; }
}

/// <summary>
/// 数据库诊断信息
/// </summary>
public class DatabaseDiagnostics
{
    public DateTime GeneratedAt { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string DatabaseVersion { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public List<TableSizeInfo> TableSizes { get; set; } = new();
    public List<IndexFragmentationInfo> IndexFragmentation { get; set; } = new();
}

/// <summary>
/// 表大小信息
/// </summary>
public class TableSizeInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public long TotalSpaceKB { get; set; }
    public long UsedSpaceKB { get; set; }
    public double SpaceUtilizationPercent => TotalSpaceKB > 0 ? (double)UsedSpaceKB / TotalSpaceKB * 100 : 0;
}

/// <summary>
/// 索引碎片信息
/// </summary>
public class IndexFragmentationInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public double Fragmentation { get; set; }
    public long PageCount { get; set; }
    public bool RequiresRebuild => Fragmentation > 30;
    public bool RequiresReorganize => Fragmentation > 10 && Fragmentation <= 30;
}