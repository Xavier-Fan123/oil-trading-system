using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data.Common;

namespace OilTrading.Infrastructure.Data.QueryOptimization;

/// <summary>
/// Query interceptor for performance monitoring and optimization
/// </summary>
public class QueryInterceptor : DbCommandInterceptor
{
    private readonly ILogger<QueryInterceptor> _logger;
    private readonly QueryOptimizationSettings _settings;

    public QueryInterceptor(ILogger<QueryInterceptor> logger, QueryOptimizationSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        LogSlowQuery(command, eventData);
        OptimizeQuery(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        LogSlowQuery(command, eventData);
        OptimizeQuery(command);
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        LogQueryPerformance(command, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogQueryPerformance(command, eventData);
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void LogSlowQuery(DbCommand command, CommandEventData eventData)
    {
        if (_settings.EnableSlowQueryLogging)
        {
            // Set command timeout based on query complexity
            var estimatedComplexity = EstimateQueryComplexity(command.CommandText);
            if (estimatedComplexity > _settings.ComplexQueryThreshold)
            {
                command.CommandTimeout = _settings.ComplexQueryTimeoutSeconds;
                _logger.LogInformation("Complex query detected, timeout set to {Timeout}s: {Query}", 
                    _settings.ComplexQueryTimeoutSeconds, TruncateQuery(command.CommandText));
            }
        }
    }

    private void LogQueryPerformance(DbCommand command, CommandExecutedEventData eventData)
    {
        var duration = eventData.Duration;
        
        if (duration.TotalMilliseconds > _settings.SlowQueryThresholdMs)
        {
            _logger.LogWarning("Slow query detected - Duration: {Duration}ms, Query: {Query}", 
                duration.TotalMilliseconds, TruncateQuery(command.CommandText));
            
            // Log query plan for analysis
            if (_settings.EnableQueryPlanLogging)
            {
                LogQueryPlan(command);
            }
        }
        else if (_settings.EnablePerformanceLogging)
        {
            _logger.LogDebug("Query executed - Duration: {Duration}ms", duration.TotalMilliseconds);
        }
    }

    private void OptimizeQuery(DbCommand command)
    {
        if (!_settings.EnableQueryOptimization)
            return;

        var originalQuery = command.CommandText;
        var optimizedQuery = ApplyQueryOptimizations(originalQuery);
        
        if (optimizedQuery != originalQuery)
        {
            command.CommandText = optimizedQuery;
            _logger.LogDebug("Query optimized: {OriginalLength} -> {OptimizedLength} characters", 
                originalQuery.Length, optimizedQuery.Length);
        }
    }

    private int EstimateQueryComplexity(string query)
    {
        var complexity = 0;
        var queryUpper = query.ToUpperInvariant();
        
        // Count joins
        complexity += CountOccurrences(queryUpper, "JOIN") * 2;
        
        // Count subqueries
        complexity += CountOccurrences(queryUpper, "SELECT") - 1; // Subtract main SELECT
        
        // Count aggregations
        complexity += CountOccurrences(queryUpper, "GROUP BY");
        complexity += CountOccurrences(queryUpper, "ORDER BY");
        complexity += CountOccurrences(queryUpper, "HAVING");
        
        // Count window functions
        complexity += CountOccurrences(queryUpper, "OVER(") * 2;
        
        // Count CTEs
        complexity += CountOccurrences(queryUpper, "WITH") * 2;
        
        return complexity;
    }

    private string ApplyQueryOptimizations(string query)
    {
        var optimized = query;
        
        // Add query hints for known patterns
        if (_settings.EnableQueryHints)
        {
            optimized = AddQueryHints(optimized);
        }
        
        // Optimize common patterns
        optimized = OptimizeCommonPatterns(optimized);
        
        return optimized;
    }

    private string AddQueryHints(string query)
    {
        var queryUpper = query.ToUpperInvariant();
        
        // Add NOLOCK hints for read-only queries (if configured)
        if (_settings.UseReadUncommittedForReports && 
            queryUpper.Contains("SELECT") && 
            !queryUpper.Contains("UPDATE") && 
            !queryUpper.Contains("INSERT") && 
            !queryUpper.Contains("DELETE"))
        {
            // Add WITH (NOLOCK) hints - be careful with this in production
            // This is just an example - consider your isolation requirements
        }
        
        // Add index hints for specific tables
        foreach (var hint in _settings.IndexHints)
        {
            if (queryUpper.Contains(hint.Key.ToUpperInvariant()))
            {
                query = query.Replace(hint.Key, $"{hint.Key} WITH (INDEX({hint.Value}))");
            }
        }
        
        return query;
    }

    private string OptimizeCommonPatterns(string query)
    {
        // Optimize EXISTS patterns
        if (query.ToUpperInvariant().Contains("WHERE EXISTS"))
        {
            // Consider converting to JOIN where appropriate
            // This is a simplified example
        }
        
        // Optimize IN patterns with large lists
        if (query.ToUpperInvariant().Contains("IN (") && CountOccurrences(query, ",") > 50)
        {
            _logger.LogInformation("Large IN clause detected - consider using temporary table or table-valued parameter");
        }
        
        return query;
    }

    private void LogQueryPlan(DbCommand command)
    {
        try
        {
            // In a real implementation, you would execute EXPLAIN PLAN or similar
            // This is a placeholder for query plan logging
            _logger.LogDebug("Query plan logging requested for: {Query}", TruncateQuery(command.CommandText));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log query plan");
        }
    }

    private static int CountOccurrences(string text, string pattern)
    {
        return (text.Length - text.Replace(pattern, "").Length) / pattern.Length;
    }

    private static string TruncateQuery(string query)
    {
        const int maxLength = 500;
        return query.Length > maxLength ? query[..maxLength] + "..." : query;
    }
}

/// <summary>
/// Settings for query optimization
/// </summary>
public class QueryOptimizationSettings
{
    public bool EnableSlowQueryLogging { get; set; } = true;
    public bool EnablePerformanceLogging { get; set; } = false;
    public bool EnableQueryOptimization { get; set; } = true;
    public bool EnableQueryPlanLogging { get; set; } = true;
    public bool EnableQueryHints { get; set; } = false;
    public bool UseReadUncommittedForReports { get; set; } = false;
    
    public int SlowQueryThresholdMs { get; set; } = 1000;
    public int ComplexQueryThreshold { get; set; } = 10;
    public int ComplexQueryTimeoutSeconds { get; set; } = 300;
    
    public Dictionary<string, string> IndexHints { get; set; } = new();
}

/// <summary>
/// Database performance metrics collector
/// </summary>
public class DatabasePerformanceCollector
{
    private readonly ILogger<DatabasePerformanceCollector> _logger;
    private static readonly ConcurrentDictionary<string, QueryMetrics> _queryMetrics = new();
    
    public DatabasePerformanceCollector(ILogger<DatabasePerformanceCollector> logger)
    {
        _logger = logger;
    }

    public void RecordQuery(string query, TimeSpan duration, bool successful)
    {
        var queryHash = GetQueryHash(query);
        
        _queryMetrics.AddOrUpdate(queryHash, 
            new QueryMetrics { QueryPattern = GetQueryPattern(query) },
            (key, existing) =>
            {
                existing.ExecutionCount++;
                existing.TotalDuration += duration;
                existing.AverageDuration = TimeSpan.FromTicks(existing.TotalDuration.Ticks / existing.ExecutionCount);
                
                if (duration > existing.MaxDuration)
                    existing.MaxDuration = duration;
                
                if (duration < existing.MinDuration || existing.MinDuration == TimeSpan.Zero)
                    existing.MinDuration = duration;
                
                if (!successful)
                    existing.ErrorCount++;
                
                existing.LastExecuted = DateTime.UtcNow;
                
                return existing;
            });
    }

    public IEnumerable<QueryMetrics> GetTopSlowQueries(int count = 10)
    {
        return _queryMetrics.Values
            .OrderByDescending(m => m.AverageDuration)
            .Take(count);
    }

    public IEnumerable<QueryMetrics> GetMostFrequentQueries(int count = 10)
    {
        return _queryMetrics.Values
            .OrderByDescending(m => m.ExecutionCount)
            .Take(count);
    }

    public void ClearMetrics()
    {
        _queryMetrics.Clear();
        _logger.LogInformation("Query performance metrics cleared");
    }

    private static string GetQueryHash(string query)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(query));
        return Convert.ToBase64String(hash)[..16]; // First 16 characters
    }

    private static string GetQueryPattern(string query)
    {
        // Normalize query to pattern by removing specific values
        var pattern = query;
        
        // Replace string literals
        pattern = System.Text.RegularExpressions.Regex.Replace(pattern, @"'[^']*'", "'?'");
        
        // Replace numeric literals
        pattern = System.Text.RegularExpressions.Regex.Replace(pattern, @"\b\d+\b", "?");
        
        // Replace GUIDs
        pattern = System.Text.RegularExpressions.Regex.Replace(pattern, 
            @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", "?");
        
        return pattern.Length > 200 ? pattern[..200] + "..." : pattern;
    }
}

/// <summary>
/// Query performance metrics
/// </summary>
public class QueryMetrics
{
    public string QueryPattern { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public int ErrorCount { get; set; }
    public DateTime LastExecuted { get; set; }
    
    public double SuccessRate => ExecutionCount > 0 ? (double)(ExecutionCount - ErrorCount) / ExecutionCount * 100 : 0;
}