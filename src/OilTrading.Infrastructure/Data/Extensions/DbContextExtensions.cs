using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using OilTrading.Infrastructure.Data.Configurations;
using OilTrading.Infrastructure.Data.QueryOptimization;

namespace OilTrading.Infrastructure.Data.Extensions;

/// <summary>
/// Extension methods for DbContext optimization
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Apply database optimizations to the context
    /// </summary>
    public static void ApplyDatabaseOptimizations(this ModelBuilder modelBuilder)
    {
        DatabaseOptimizationConfiguration.ApplyOptimizations(modelBuilder);
    }

    /// <summary>
    /// Configure performance monitoring for the context
    /// </summary>
    public static DbContextOptionsBuilder AddPerformanceMonitoring(
        this DbContextOptionsBuilder optionsBuilder,
        ILoggerFactory loggerFactory,
        QueryOptimizationSettings? settings = null)
    {
        settings ??= new QueryOptimizationSettings();
        
        var logger = loggerFactory.CreateLogger<QueryInterceptor>();
        var interceptor = new QueryInterceptor(logger, settings);
        
        return optionsBuilder.AddInterceptors(interceptor);
    }

    /// <summary>
    /// Execute database maintenance operations
    /// </summary>
    public static async Task ExecuteMaintenanceAsync(this ApplicationDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting database maintenance operations");

            // Update statistics
            await context.Database.ExecuteSqlRawAsync("EXEC sp_updatestats");
            logger.LogInformation("Database statistics updated");

            // Rebuild fragmented indexes (simplified version)
            await context.Database.ExecuteSqlRawAsync(@"
                DECLARE @SQL NVARCHAR(MAX) = '';
                SELECT @SQL = @SQL + 
                    CASE 
                        WHEN avg_fragmentation_in_percent > 30 THEN
                            'ALTER INDEX ' + i.name + ' ON ' + SCHEMA_NAME(t.schema_id) + '.' + t.name + ' REBUILD;'
                        WHEN avg_fragmentation_in_percent > 10 THEN
                            'ALTER INDEX ' + i.name + ' ON ' + SCHEMA_NAME(t.schema_id) + '.' + t.name + ' REORGANIZE;'
                    END
                FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
                INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                WHERE avg_fragmentation_in_percent > 10 AND i.name IS NOT NULL;
                
                IF LEN(@SQL) > 0 EXEC sp_executesql @SQL;
            ");
            logger.LogInformation("Index maintenance completed");

            // Clean up old data (if configured)
            await CleanupOldDataAsync(context, logger);

            logger.LogInformation("Database maintenance completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database maintenance failed");
            throw;
        }
    }

    /// <summary>
    /// Optimize query execution for large datasets
    /// </summary>
    public static IQueryable<T> OptimizeForLargeDataset<T>(this IQueryable<T> query) where T : class
    {
        // Disable change tracking for read-only operations
        return query.AsNoTracking();
    }

    /// <summary>
    /// Apply caching hints for frequently accessed data
    /// </summary>
    public static IQueryable<T> WithCacheHint<T>(this IQueryable<T> query, TimeSpan? cacheExpiry = null) where T : class
    {
        // In a real implementation, this would integrate with a query result cache
        // For now, we'll add a comment hint that could be processed by query interceptors
        
        var expiry = cacheExpiry ?? TimeSpan.FromMinutes(5);
        return query.TagWith($"CACHE_HINT:ExpireAfter={expiry.TotalMinutes}min");
    }

    /// <summary>
    /// Execute bulk operations efficiently
    /// </summary>
    public static async Task BulkInsertAsync<T>(this DbContext context, IEnumerable<T> entities, int batchSize = 1000) where T : class
    {
        var entityList = entities.ToList();
        
        for (int i = 0; i < entityList.Count; i += batchSize)
        {
            var batch = entityList.Skip(i).Take(batchSize);
            context.Set<T>().AddRange(batch);
            
            await context.SaveChangesAsync();
            
            // Clear change tracker to free memory
            context.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Execute bulk updates efficiently
    /// </summary>
    public static async Task BulkUpdateAsync<T>(this DbContext context, 
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, T>> updateExpression) where T : class
    {
        // In EF Core 7+, you can use ExecuteUpdateAsync
        // This is a simplified version for demonstration
        
        var entities = await context.Set<T>().Where(predicate).ToListAsync();
        
        var updateFunc = updateExpression.Compile();
        
        foreach (var entity in entities)
        {
            var updatedEntity = updateFunc(entity);
            context.Entry(entity).CurrentValues.SetValues(updatedEntity);
        }
        
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Get database performance statistics
    /// </summary>
    public static async Task<DatabasePerformanceStats> GetPerformanceStatsAsync(this ApplicationDbContext context)
    {
        var stats = new DatabasePerformanceStats();

        try
        {
            // Get index usage statistics
            var indexStats = await context.Database.SqlQuery<IndexUsageStats>($@"
                SELECT 
                    OBJECT_SCHEMA_NAME(i.object_id) as SchemaName,
                    OBJECT_NAME(i.object_id) as TableName,
                    i.name as IndexName,
                    s.user_seeks,
                    s.user_scans,
                    s.user_lookups,
                    s.user_updates
                FROM sys.indexes i
                LEFT JOIN sys.dm_db_index_usage_stats s ON i.object_id = s.object_id AND i.index_id = s.index_id
                WHERE i.object_id IN (SELECT object_id FROM sys.tables)
                ORDER BY s.user_seeks + s.user_scans + s.user_lookups DESC
            ").ToListAsync();

            stats.IndexUsageStats = indexStats;

            // Get table sizes
            var tableSizes = await context.Database.SqlQuery<TableSizeStats>($@"
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
                GROUP BY t.schema_id, t.name
                ORDER BY SUM(a.total_pages) DESC
            ").ToListAsync();

            stats.TableSizes = tableSizes;

            // Get query performance from plan cache
            var queryStats = await context.Database.SqlQuery<QueryPerformanceStats>($@"
                SELECT TOP 20
                    qs.execution_count,
                    qs.total_elapsed_time / 1000000.0 as total_elapsed_time_sec,
                    qs.total_elapsed_time / qs.execution_count / 1000000.0 as avg_elapsed_time_sec,
                    qs.total_logical_reads,
                    qs.total_logical_reads / qs.execution_count as avg_logical_reads,
                    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1, 100) as query_text
                FROM sys.dm_exec_query_stats qs
                CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
                WHERE qt.text NOT LIKE '%sys.%'
                ORDER BY qs.total_elapsed_time DESC
            ").ToListAsync();

            stats.TopQueries = queryStats;

            stats.GeneratedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            stats.ErrorMessage = ex.Message;
        }

        return stats;
    }

    private static async Task CleanupOldDataAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Clean up old pricing events (older than 2 years)
            var cutoffDate = DateTime.UtcNow.AddYears(-2);
            
            var deletedCount = await context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM PricingEvents 
                WHERE EventDate < {0} 
                AND Id NOT IN (
                    SELECT DISTINCT pe.Id 
                    FROM PricingEvents pe 
                    INNER JOIN PurchaseContracts pc ON pe.ProductId = pc.ProductId 
                    WHERE pc.Status IN (1, 2)
                )", cutoffDate);

            if (deletedCount > 0)
            {
                logger.LogInformation("Cleaned up {Count} old pricing events", deletedCount);
            }

            // Clean up old audit logs (if they exist and are older than retention period)
            // This would be implemented based on your audit log retention policy
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cleanup old data");
        }
    }
}

/// <summary>
/// Database performance statistics
/// </summary>
public class DatabasePerformanceStats
{
    public DateTime GeneratedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public List<IndexUsageStats> IndexUsageStats { get; set; } = new();
    public List<TableSizeStats> TableSizes { get; set; } = new();
    public List<QueryPerformanceStats> TopQueries { get; set; } = new();
}

public class IndexUsageStats
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public long UserSeeks { get; set; }
    public long UserScans { get; set; }
    public long UserLookups { get; set; }
    public long UserUpdates { get; set; }
    
    public long TotalReads => UserSeeks + UserScans + UserLookups;
    public double ReadWriteRatio => UserUpdates > 0 ? (double)TotalReads / UserUpdates : TotalReads;
}

public class TableSizeStats
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public long TotalSpaceKB { get; set; }
    public long UsedSpaceKB { get; set; }
    
    public double SpaceUtilizationPercent => TotalSpaceKB > 0 ? (double)UsedSpaceKB / TotalSpaceKB * 100 : 0;
}

public class QueryPerformanceStats
{
    public long ExecutionCount { get; set; }
    public double TotalElapsedTimeSec { get; set; }
    public double AvgElapsedTimeSec { get; set; }
    public long TotalLogicalReads { get; set; }
    public long AvgLogicalReads { get; set; }
    public string QueryText { get; set; } = string.Empty;
}