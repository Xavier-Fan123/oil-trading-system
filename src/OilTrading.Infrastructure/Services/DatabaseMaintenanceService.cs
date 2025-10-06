using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OilTrading.Infrastructure.Data;
using OilTrading.Infrastructure.Data.Extensions;

namespace OilTrading.Infrastructure.Services;

/// <summary>
/// 数据库维护后台服务 - 定期执行数据库优化任务
/// </summary>
public class DatabaseMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMaintenanceService> _logger;
    private readonly DatabaseMaintenanceOptions _options;
    private readonly Timer _maintenanceTimer;

    public DatabaseMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMaintenanceService> logger,
        IOptions<DatabaseMaintenanceOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;

        // 设置定时维护任务
        _maintenanceTimer = new Timer(async _ => await ExecuteMaintenanceAsync(), 
            null, 
            _options.InitialDelay, 
            _options.MaintenanceInterval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database maintenance service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // 检查是否到了维护时间
                if (IsMaintenanceTime())
                {
                    await ExecuteMaintenanceAsync();
                }

                // 每小时检查一次
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Database maintenance service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database maintenance service encountered an error");
        }
    }

    private async Task ExecuteMaintenanceAsync()
    {
        if (!_options.EnableAutomaticMaintenance)
        {
            _logger.LogDebug("Automatic database maintenance is disabled");
            return;
        }

        _logger.LogInformation("Starting scheduled database maintenance");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            var maintenanceResult = new DatabaseMaintenanceResult
            {
                StartTime = DateTime.UtcNow
            };

            // 1. 索引维护
            if (_options.EnableIndexMaintenance)
            {
                maintenanceResult.IndexMaintenanceResult = await PerformIndexMaintenanceAsync(context);
            }

            // 2. 统计信息更新
            if (_options.EnableStatisticsUpdate)
            {
                maintenanceResult.StatisticsUpdateResult = await UpdateStatisticsAsync(context);
            }

            // 3. 数据清理
            if (_options.EnableDataCleanup)
            {
                maintenanceResult.DataCleanupResult = await PerformDataCleanupAsync(context);
            }

            // 4. 数据归档
            if (_options.EnableDataArchival)
            {
                maintenanceResult.DataArchivalResult = await PerformDataArchivalAsync(context);
            }

            // 5. 性能检查
            if (_options.EnablePerformanceCheck)
            {
                maintenanceResult.PerformanceCheckResult = await PerformPerformanceCheckAsync(context);
            }

            maintenanceResult.EndTime = DateTime.UtcNow;
            maintenanceResult.IsSuccessful = true;

            await LogMaintenanceResultAsync(maintenanceResult);

            _logger.LogInformation("Database maintenance completed successfully in {Duration}", 
                maintenanceResult.Duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database maintenance failed");
        }
    }

    private async Task<IndexMaintenanceResult> PerformIndexMaintenanceAsync(ApplicationDbContext context)
    {
        _logger.LogInformation("Starting index maintenance");

        var result = new IndexMaintenanceResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 获取需要维护的索引
            var fragmentedIndexes = await context.Database.SqlQuery<FragmentedIndex>($@"
                SELECT 
                    SCHEMA_NAME(t.schema_id) as SchemaName,
                    t.name as TableName,
                    i.name as IndexName,
                    ips.avg_fragmentation_in_percent as FragmentationPercent,
                    ips.page_count as PageCount
                FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
                INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                WHERE ips.avg_fragmentation_in_percent > {_options.IndexFragmentationThreshold}
                  AND ips.page_count > 100
                  AND i.name IS NOT NULL
                ORDER BY ips.avg_fragmentation_in_percent DESC
            ").ToListAsync();

            foreach (var index in fragmentedIndexes)
            {
                try
                {
                    if (index.FragmentationPercent > _options.IndexRebuildThreshold)
                    {
                        // 重建索引
                        await context.Database.ExecuteSqlRawAsync($@"
                            ALTER INDEX [{index.IndexName}] 
                            ON [{index.SchemaName}].[{index.TableName}] 
                            REBUILD ONLINE = ON");

                        result.RebuiltIndexes.Add(index.IndexName);
                        _logger.LogInformation("Rebuilt index {IndexName} with fragmentation {Fragmentation}%", 
                            index.IndexName, index.FragmentationPercent);
                    }
                    else
                    {
                        // 重新组织索引
                        await context.Database.ExecuteSqlRawAsync($@"
                            ALTER INDEX [{index.IndexName}] 
                            ON [{index.SchemaName}].[{index.TableName}] 
                            REORGANIZE");

                        result.ReorganizedIndexes.Add(index.IndexName);
                        _logger.LogInformation("Reorganized index {IndexName} with fragmentation {Fragmentation}%", 
                            index.IndexName, index.FragmentationPercent);
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to maintain index {index.IndexName}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to maintain index {IndexName}", index.IndexName);
                }
            }

            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = result.Errors.Count == 0;

            _logger.LogInformation("Index maintenance completed. Rebuilt: {RebuiltCount}, Reorganized: {ReorganizedCount}", 
                result.RebuiltIndexes.Count, result.ReorganizedIndexes.Count);

            return result;
        }
        catch (Exception ex)
        {
            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = false;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Index maintenance failed");
            return result;
        }
    }

    private async Task<StatisticsUpdateResult> UpdateStatisticsAsync(ApplicationDbContext context)
    {
        _logger.LogInformation("Starting statistics update");

        var result = new StatisticsUpdateResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 更新所有表的统计信息
            await context.Database.ExecuteSqlRawAsync("EXEC sp_updatestats");

            // 获取统计信息更新详情
            var statisticsInfo = await context.Database.SqlQuery<StatisticsInfo>($@"
                SELECT 
                    SCHEMA_NAME(t.schema_id) as SchemaName,
                    t.name as TableName,
                    s.name as StatisticsName,
                    sp.last_updated as LastUpdated,
                    sp.rows as RowCount,
                    sp.modification_counter as ModificationCount
                FROM sys.tables t
                INNER JOIN sys.stats s ON t.object_id = s.object_id
                CROSS APPLY sys.dm_db_stats_properties(s.object_id, s.stats_id) sp
                WHERE t.name IN ('PurchaseContracts', 'PricingEvents', 'TradingPartners', 'Products')
                ORDER BY sp.last_updated DESC
            ").ToListAsync();

            result.UpdatedStatistics = statisticsInfo.ToList();
            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = true;

            _logger.LogInformation("Statistics update completed for {Count} statistics", 
                result.UpdatedStatistics.Count);

            return result;
        }
        catch (Exception ex)
        {
            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Statistics update failed");
            return result;
        }
    }

    private async Task<DataCleanupResult> PerformDataCleanupAsync(ApplicationDbContext context)
    {
        _logger.LogInformation("Starting data cleanup");

        var result = new DataCleanupResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 清理旧的价格事件数据
            var pricingEventsCutoff = DateTime.UtcNow.AddDays(-_options.PricingEventsRetentionDays);
            var deletedPricingEvents = await context.Database.ExecuteSqlRawAsync($@"
                DELETE FROM PricingEvents 
                WHERE EventDate < {pricingEventsCutoff}
                  AND Id NOT IN (
                      SELECT DISTINCT pe.Id 
                      FROM PricingEvents pe 
                      INNER JOIN PurchaseContracts pc ON pe.ProductId = pc.ProductId 
                      WHERE pc.Status IN (1, 2)
                  )");

            result.DeletedPricingEvents = deletedPricingEvents;

            // 清理旧的审计日志
            var auditLogsCutoff = DateTime.UtcNow.AddDays(-_options.AuditLogsRetentionDays);
            var deletedAuditLogs = await context.Database.ExecuteSqlRawAsync($@"
                DELETE FROM OperationAuditLogs 
                WHERE Timestamp < {auditLogsCutoff}");

            result.DeletedAuditLogs = deletedAuditLogs;

            // 清理临时数据
            result.CleanedTempData = await CleanupTemporaryDataAsync(context);

            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = true;

            _logger.LogInformation("Data cleanup completed. Deleted: {PricingEvents} pricing events, {AuditLogs} audit logs", 
                result.DeletedPricingEvents, result.DeletedAuditLogs);

            return result;
        }
        catch (Exception ex)
        {
            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Data cleanup failed");
            return result;
        }
    }

    private async Task<DataArchivalResult> PerformDataArchivalAsync(ApplicationDbContext context)
    {
        _logger.LogInformation("Starting data archival");

        var result = new DataArchivalResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 这里集成数据归档服务
            // 由于DataArchivalService需要依赖注入，我们在这里做简化处理
            
            // 获取需要归档的数据统计
            var archivalCutoff = DateTime.UtcNow.AddMonths(-_options.DataArchivalAgeMonths);
            
            var contractsToArchive = await context.PurchaseContracts
                .Where(c => c.CreatedAt < archivalCutoff && c.Status == Core.Entities.ContractStatus.Completed)
                .CountAsync();

            var pricingEventsToArchive = await context.PricingEvents
                .Where(pe => pe.EventDate < archivalCutoff)
                .CountAsync();

            result.ContractsToArchive = contractsToArchive;
            result.PricingEventsToArchive = pricingEventsToArchive;

            // 实际归档操作应该在这里调用DataArchivalService
            // await _dataArchivalService.ArchiveOldContractsAsync(options);

            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = true;

            _logger.LogInformation("Data archival assessment completed. Found {Contracts} contracts and {PricingEvents} pricing events for archival", 
                contractsToArchive, pricingEventsToArchive);

            return result;
        }
        catch (Exception ex)
        {
            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Data archival failed");
            return result;
        }
    }

    private async Task<PerformanceCheckResult> PerformPerformanceCheckAsync(ApplicationDbContext context)
    {
        _logger.LogInformation("Starting performance check");

        var result = new PerformanceCheckResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 执行关键查询性能测试
            // Test Active Contracts
            var activeContractsResult = await TestActiveContractsQueryAsync(context);
            result.QueryPerformanceResults.Add("Active Contracts", activeContractsResult);
            
            // Test Recent Pricing Events
            var pricingEventsResult = await TestRecentPricingEventsQueryAsync(context);
            result.QueryPerformanceResults.Add("Recent Pricing Events", pricingEventsResult);
            
            // Test Trading Partners
            var tradingPartnersResult = await TestTradingPartnersQueryAsync(context);
            result.QueryPerformanceResults.Add("Trading Partners", tradingPartnersResult);
            
            // Check for slow queries
            foreach (var (testName, testResult) in result.QueryPerformanceResults)
            {
                if (testResult.Duration.TotalMilliseconds > _options.SlowQueryThresholdMs)
                {
                    _logger.LogWarning("Slow query detected: {TestName} took {Duration}ms", 
                        testName, testResult.Duration.TotalMilliseconds);
                }

            }

            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = true;

            _logger.LogInformation("Performance check completed");

            return result;
        }
        catch (Exception ex)
        {
            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Performance check failed");
            return result;
        }
    }

    private async Task<int> CleanupTemporaryDataAsync(ApplicationDbContext context)
    {
        // 清理临时数据，例如过期的缓存条目、临时文件引用等
        // 这里是示例实现
        await Task.CompletedTask;
        return 0;
    }

    private async Task<QueryPerformanceResult> TestActiveContractsQueryAsync(ApplicationDbContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var count = await context.PurchaseContracts
            .Where(c => c.Status == Core.Entities.ContractStatus.Active)
            .AsNoTracking()
            .CountAsync();

        stopwatch.Stop();

        return new QueryPerformanceResult
        {
            Duration = stopwatch.Elapsed,
            RecordCount = count,
            IsSuccessful = true
        };
    }

    private async Task<QueryPerformanceResult> TestRecentPricingEventsQueryAsync(ApplicationDbContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var count = await context.PricingEvents
            .Where(pe => pe.EventDate >= DateTime.UtcNow.AddDays(-7))
            .AsNoTracking()
            .CountAsync();

        stopwatch.Stop();

        return new QueryPerformanceResult
        {
            Duration = stopwatch.Elapsed,
            RecordCount = count,
            IsSuccessful = true
        };
    }

    private async Task<QueryPerformanceResult> TestTradingPartnersQueryAsync(ApplicationDbContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var count = await context.TradingPartners
            .Where(tp => tp.IsActive)
            .AsNoTracking()
            .CountAsync();

        stopwatch.Stop();

        return new QueryPerformanceResult
        {
            Duration = stopwatch.Elapsed,
            RecordCount = count,
            IsSuccessful = true
        };
    }

    private bool IsMaintenanceTime()
    {
        var now = DateTime.Now;
        return now.Hour == _options.MaintenanceHour && now.Minute < 30;
    }

    private async Task LogMaintenanceResultAsync(DatabaseMaintenanceResult result)
    {
        // 记录维护结果到日志和可能的监控系统
        _logger.LogInformation("Database maintenance summary: Duration={Duration}, Success={Success}", 
            result.Duration, result.IsSuccessful);

        // 这里可以发送到监控系统、邮件通知等
        await Task.CompletedTask;
    }

    public override void Dispose()
    {
        _maintenanceTimer?.Dispose();
        base.Dispose();
    }
}

// 配置和结果类定义
public class DatabaseMaintenanceOptions
{
    public bool EnableAutomaticMaintenance { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan MaintenanceInterval { get; set; } = TimeSpan.FromHours(24);
    public int MaintenanceHour { get; set; } = 2; // 凌晨2点执行维护
    
    public bool EnableIndexMaintenance { get; set; } = true;
    public double IndexFragmentationThreshold { get; set; } = 10.0;
    public double IndexRebuildThreshold { get; set; } = 30.0;
    
    public bool EnableStatisticsUpdate { get; set; } = true;
    public bool EnableDataCleanup { get; set; } = true;
    public bool EnableDataArchival { get; set; } = false;
    public bool EnablePerformanceCheck { get; set; } = true;
    
    public int PricingEventsRetentionDays { get; set; } = 730; // 2 years
    public int AuditLogsRetentionDays { get; set; } = 2555; // 7 years
    public int DataArchivalAgeMonths { get; set; } = 12;
    public int SlowQueryThresholdMs { get; set; } = 1000;
}

public class DatabaseMaintenanceResult
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsSuccessful { get; set; }
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
    
    public IndexMaintenanceResult? IndexMaintenanceResult { get; set; }
    public StatisticsUpdateResult? StatisticsUpdateResult { get; set; }
    public DataCleanupResult? DataCleanupResult { get; set; }
    public DataArchivalResult? DataArchivalResult { get; set; }
    public PerformanceCheckResult? PerformanceCheckResult { get; set; }
}

public class IndexMaintenanceResult
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsSuccessful { get; set; }
    public List<string> RebuiltIndexes { get; set; } = new();
    public List<string> ReorganizedIndexes { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class StatisticsUpdateResult
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public List<StatisticsInfo> UpdatedStatistics { get; set; } = new();
}

public class DataCleanupResult
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public int DeletedPricingEvents { get; set; }
    public int DeletedAuditLogs { get; set; }
    public int CleanedTempData { get; set; }
}

public class DataArchivalResult
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public int ContractsToArchive { get; set; }
    public int PricingEventsToArchive { get; set; }
}

public class PerformanceCheckResult
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, QueryPerformanceResult> QueryPerformanceResults { get; set; } = new();
}

public class QueryPerformanceResult
{
    public TimeSpan Duration { get; set; }
    public int RecordCount { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}

public class FragmentedIndex
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public double FragmentationPercent { get; set; }
    public long PageCount { get; set; }
}

public class StatisticsInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string StatisticsName { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }
    public long RowCount { get; set; }
    public long ModificationCount { get; set; }
}