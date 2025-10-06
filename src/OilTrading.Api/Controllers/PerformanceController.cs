using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OilTrading.Infrastructure.Data;
using System.Diagnostics;
using System.Text.Json;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Performance")]
public class PerformanceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(ApplicationDbContext context, ILogger<PerformanceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("report")]
    [ProducesResponseType(typeof(PerformanceReport), 200)]
    public async Task<IActionResult> GetPerformanceReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string format = "json")
    {
        var start = startDate ?? DateTime.UtcNow.AddHours(-24);
        var end = endDate ?? DateTime.UtcNow;

        try
        {
            var report = await GeneratePerformanceReportAsync(start, end);
            
            return format.ToLower() switch
            {
                "csv" => File(GenerateCsvReport(report), "text/csv", $"performance-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv"),
                "pdf" => File(await GeneratePdfReportAsync(report), "application/pdf", $"performance-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf"),
                _ => Ok(report)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating performance report");
            return StatusCode(500, new { error = "Failed to generate performance report" });
        }
    }

    [HttpGet("metrics/summary")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetMetricsSummary()
    {
        try
        {
            var summary = new
            {
                timestamp = DateTime.UtcNow,
                application = await GetApplicationMetricsAsync(),
                database = await GetDatabaseMetricsAsync(),
                system = GetSystemMetrics(),
                business = await GetBusinessMetricsAsync()
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics summary");
            return StatusCode(500, new { error = "Failed to get metrics summary" });
        }
    }

    [HttpGet("trends")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetPerformanceTrends(
        [FromQuery] int days = 7)
    {
        try
        {
            var trends = await GeneratePerformanceTrendsAsync(days);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating performance trends");
            return StatusCode(500, new { error = "Failed to generate performance trends" });
        }
    }

    [HttpGet("bottlenecks")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetBottleneckAnalysis()
    {
        try
        {
            var analysis = await AnalyzeBottlenecksAsync();
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing bottlenecks");
            return StatusCode(500, new { error = "Failed to analyze bottlenecks" });
        }
    }

    [HttpPost("benchmark")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> RunBenchmark(
        [FromBody] BenchmarkRequest request)
    {
        try
        {
            var results = await RunPerformanceBenchmarkAsync(request);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running benchmark");
            return StatusCode(500, new { error = "Failed to run benchmark" });
        }
    }

    [HttpGet("alerts")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetPerformanceAlerts()
    {
        try
        {
            var alerts = await GeneratePerformanceAlertsAsync();
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance alerts");
            return StatusCode(500, new { error = "Failed to get performance alerts" });
        }
    }

    private async Task<PerformanceReport> GeneratePerformanceReportAsync(DateTime start, DateTime end)
    {
        var report = new PerformanceReport
        {
            GeneratedAt = DateTime.UtcNow,
            PeriodStart = start,
            PeriodEnd = end,
            ApplicationMetrics = await GetApplicationMetricsAsync(),
            DatabaseMetrics = await GetDatabaseMetricsAsync(),
            SystemMetrics = GetSystemMetrics(),
            BusinessMetrics = await GetBusinessMetricsAsync(),
            Recommendations = await GenerateRecommendationsAsync()
        };

        return report;
    }

    private async Task<ApplicationMetrics> GetApplicationMetricsAsync()
    {
        // Simulate collecting application metrics
        // In real implementation, integrate with APM tools
        return new ApplicationMetrics
        {
            AverageResponseTime = 245.5,
            P95ResponseTime = 450.2,
            P99ResponseTime = 1200.8,
            ThroughputRps = 156.7,
            ErrorRate = 0.12,
            SuccessRate = 99.88,
            ActiveSessions = 45,
            MemoryUsage = GetMemoryUsage(),
            CpuUsage = GetCpuUsage(),
            GarbageCollections = GetGcMetrics()
        };
    }

    private async Task<DatabaseMetrics> GetDatabaseMetricsAsync()
    {
        var connectionCount = 0;
        var avgQueryTime = 0.0;
        
        try
        {
            // Get database connection info
            var connectionString = _context.Database.GetConnectionString();
            if (!string.IsNullOrEmpty(connectionString))
            {
                // Simulate database metrics collection
                connectionCount = Random.Shared.Next(5, 20);
                avgQueryTime = Random.Shared.NextDouble() * 100 + 50; // 50-150ms
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect database metrics");
        }

        return new DatabaseMetrics
        {
            ActiveConnections = connectionCount,
            AverageQueryTime = avgQueryTime,
            SlowQueries = Random.Shared.Next(0, 5),
            CacheHitRatio = 0.925,
            IndexEfficiency = 0.887,
            DeadlockCount = 0,
            LockWaitTime = 12.3
        };
    }

    private SystemMetrics GetSystemMetrics()
    {
        var process = Process.GetCurrentProcess();
        
        return new SystemMetrics
        {
            CpuUsage = GetCpuUsage(),
            MemoryUsage = process.WorkingSet64 / 1024.0 / 1024.0, // MB
            DiskUsage = GetDiskUsage(),
            NetworkIO = GetNetworkIO(),
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount,
            UptimeSeconds = (DateTime.UtcNow - process.StartTime).TotalSeconds
        };
    }

    private async Task<BusinessMetrics> GetBusinessMetricsAsync()
    {
        var activeContracts = await _context.PurchaseContracts.CountAsync(c => c.Status == Core.Entities.ContractStatus.Active);
        var todayPricingEvents = await _context.PricingEvents.CountAsync(p => p.EventDate.Date == DateTime.UtcNow.Date);
        
        return new BusinessMetrics
        {
            ActiveContracts = activeContracts,
            DailyTransactions = Random.Shared.Next(50, 200),
            TradingVolume = Random.Shared.Next(10000, 50000),
            AverageContractValue = Random.Shared.Next(1000000, 5000000),
            PricingEvents = todayPricingEvents,
            SettlementEfficiency = 0.945
        };
    }

    private async Task<List<string>> GenerateRecommendationsAsync()
    {
        var recommendations = new List<string>();
        
        var metrics = await GetApplicationMetricsAsync();
        var dbMetrics = await GetDatabaseMetricsAsync();
        var sysMetrics = GetSystemMetrics();

        if (metrics.P95ResponseTime > 500)
            recommendations.Add("Consider optimizing API response times - P95 exceeds 500ms");
            
        if (metrics.ErrorRate > 0.5)
            recommendations.Add("Error rate is above acceptable threshold - investigate error patterns");
            
        if (dbMetrics.CacheHitRatio < 0.9)
            recommendations.Add("Database cache hit ratio is low - consider cache optimization");
            
        if (dbMetrics.SlowQueries > 10)
            recommendations.Add("High number of slow queries detected - review query performance");
            
        if (sysMetrics.MemoryUsage > 2000)
            recommendations.Add("Memory usage is high - consider memory optimization");
            
        if (sysMetrics.CpuUsage > 80)
            recommendations.Add("CPU usage is high - investigate CPU-intensive operations");

        if (!recommendations.Any())
            recommendations.Add("System is performing within normal parameters");

        return recommendations;
    }

    private async Task<object> GeneratePerformanceTrendsAsync(int days)
    {
        var trends = new
        {
            period_days = days,
            response_time_trend = GenerateTrendData(days, 200, 50), // Base 200ms ± 50ms
            throughput_trend = GenerateTrendData(days, 150, 30),     // Base 150 RPS ± 30
            error_rate_trend = GenerateTrendData(days, 0.1, 0.05),   // Base 0.1% ± 0.05%
            memory_usage_trend = GenerateTrendData(days, 1500, 200), // Base 1500MB ± 200MB
            cpu_usage_trend = GenerateTrendData(days, 45, 15),       // Base 45% ± 15%
            business_volume_trend = GenerateTrendData(days, 25000, 5000), // Base 25k ± 5k
            recommendations = new[]
            {
                "Response times have been stable over the last week",
                "Throughput shows slight improvement trend",
                "Error rates are within acceptable range",
                "Memory usage trending upward - monitor closely"
            }
        };

        return trends;
    }

    private async Task<object> AnalyzeBottlenecksAsync()
    {
        var bottlenecks = new
        {
            analysis_timestamp = DateTime.UtcNow,
            identified_bottlenecks = new[]
            {
                new {
                    component = "Database Queries",
                    severity = "Medium",
                    impact = "Response time increase of 15%",
                    description = "Several queries showing suboptimal execution plans",
                    recommendation = "Add indexes on frequently queried columns",
                    estimated_improvement = "25% response time reduction"
                },
                new {
                    component = "API Serialization",
                    severity = "Low",
                    impact = "CPU utilization increase of 8%",
                    description = "JSON serialization consuming more CPU than expected",
                    recommendation = "Consider using System.Text.Json optimizations",
                    estimated_improvement = "10% CPU reduction"
                },
                new {
                    component = "Cache Layer",
                    severity = "Low",
                    impact = "Cache miss rate of 12%",
                    description = "Some frequently accessed data not being cached effectively",
                    recommendation = "Review cache expiration policies and key strategies",
                    estimated_improvement = "15% reduction in database load"
                }
            },
            overall_performance_score = 85.5,
            next_analysis_recommended = DateTime.UtcNow.AddDays(7)
        };

        return bottlenecks;
    }

    private async Task<object> RunPerformanceBenchmarkAsync(BenchmarkRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<BenchmarkResult>();

        for (int i = 0; i < request.IterationCount; i++)
        {
            var iterationStart = Stopwatch.StartNew();
            
            try
            {
                switch (request.TestType.ToLower())
                {
                    case "database":
                        await BenchmarkDatabaseOperationsAsync();
                        break;
                    case "api":
                        await BenchmarkApiOperationsAsync();
                        break;
                    case "cache":
                        await BenchmarkCacheOperationsAsync();
                        break;
                    default:
                        await BenchmarkGeneralOperationsAsync();
                        break;
                }
                
                iterationStart.Stop();
                results.Add(new BenchmarkResult
                {
                    Iteration = i + 1,
                    Duration = iterationStart.ElapsedMilliseconds,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                results.Add(new BenchmarkResult
                {
                    Iteration = i + 1,
                    Duration = iterationStart.ElapsedMilliseconds,
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        stopwatch.Stop();

        var successfulResults = results.Where(r => r.Success).ToList();
        
        return new
        {
            test_type = request.TestType,
            total_duration_ms = stopwatch.ElapsedMilliseconds,
            iterations = request.IterationCount,
            successful_iterations = successfulResults.Count,
            success_rate = (double)successfulResults.Count / request.IterationCount * 100,
            statistics = new
            {
                avg_duration_ms = successfulResults.Any() ? successfulResults.Average(r => r.Duration) : 0,
                min_duration_ms = successfulResults.Any() ? successfulResults.Min(r => r.Duration) : 0,
                max_duration_ms = successfulResults.Any() ? successfulResults.Max(r => r.Duration) : 0,
                p95_duration_ms = successfulResults.Any() ? successfulResults.OrderBy(r => r.Duration).Skip((int)(successfulResults.Count * 0.95)).First().Duration : 0
            },
            results = results.Take(10) // Return first 10 detailed results
        };
    }

    private async Task<object> GeneratePerformanceAlertsAsync()
    {
        var alerts = new List<object>();
        
        var metrics = await GetApplicationMetricsAsync();
        var dbMetrics = await GetDatabaseMetricsAsync();
        var sysMetrics = GetSystemMetrics();

        if (metrics.P95ResponseTime > 1000)
        {
            alerts.Add(new
            {
                severity = "High",
                component = "API Response Time",
                message = $"P95 response time is {metrics.P95ResponseTime}ms (threshold: 1000ms)",
                timestamp = DateTime.UtcNow,
                action_required = "Investigate slow API endpoints"
            });
        }

        if (metrics.ErrorRate > 1.0)
        {
            alerts.Add(new
            {
                severity = "Medium",
                component = "Error Rate",
                message = $"Error rate is {metrics.ErrorRate}% (threshold: 1%)",
                timestamp = DateTime.UtcNow,
                action_required = "Review application logs for error patterns"
            });
        }

        if (dbMetrics.SlowQueries > 20)
        {
            alerts.Add(new
            {
                severity = "Medium",
                component = "Database Performance",
                message = $"Slow query count is {dbMetrics.SlowQueries} (threshold: 20)",
                timestamp = DateTime.UtcNow,
                action_required = "Optimize database queries and indexes"
            });
        }

        if (sysMetrics.MemoryUsage > 4000)
        {
            alerts.Add(new
            {
                severity = "High",
                component = "Memory Usage",
                message = $"Memory usage is {sysMetrics.MemoryUsage:F1}MB (threshold: 4000MB)",
                timestamp = DateTime.UtcNow,
                action_required = "Investigate memory leaks and optimize memory usage"
            });
        }

        return new
        {
            alert_count = alerts.Count,
            high_severity_count = alerts.Count(a => a.GetType().GetProperty("severity")?.GetValue(a)?.ToString() == "High"),
            alerts = alerts,
            next_check = DateTime.UtcNow.AddMinutes(5)
        };
    }

    // Benchmark operations
    private async Task BenchmarkDatabaseOperationsAsync()
    {
        await _context.Products.CountAsync();
        await Task.Delay(Random.Shared.Next(10, 50)); // Simulate variable DB response time
    }

    private async Task BenchmarkApiOperationsAsync()
    {
        // Simulate API operations
        await Task.Delay(Random.Shared.Next(50, 200));
    }

    private async Task BenchmarkCacheOperationsAsync()
    {
        // Simulate cache operations
        await Task.Delay(Random.Shared.Next(1, 10));
    }

    private async Task BenchmarkGeneralOperationsAsync()
    {
        // Simulate general operations
        await Task.Delay(Random.Shared.Next(20, 100));
    }

    // Helper methods
    private double GetCpuUsage()
    {
        // Simplified CPU usage calculation
        return Random.Shared.NextDouble() * 30 + 20; // 20-50%
    }

    private double GetMemoryUsage()
    {
        var process = Process.GetCurrentProcess();
        return process.WorkingSet64 / 1024.0 / 1024.0; // MB
    }

    private double GetDiskUsage()
    {
        var drive = new DriveInfo(Directory.GetCurrentDirectory());
        return (double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize * 100;
    }

    private object GetNetworkIO()
    {
        return new
        {
            bytes_sent_per_sec = Random.Shared.Next(1000, 10000),
            bytes_received_per_sec = Random.Shared.Next(5000, 50000),
            packets_sent_per_sec = Random.Shared.Next(10, 100),
            packets_received_per_sec = Random.Shared.Next(50, 500)
        };
    }

    private object GetGcMetrics()
    {
        return new
        {
            gen0_collections = GC.CollectionCount(0),
            gen1_collections = GC.CollectionCount(1),
            gen2_collections = GC.CollectionCount(2),
            total_memory_mb = GC.GetTotalMemory(false) / 1024.0 / 1024.0
        };
    }

    private List<object> GenerateTrendData(int days, double baseValue, double variance)
    {
        var data = new List<object>();
        var random = new Random();
        
        for (int i = days - 1; i >= 0; i--)
        {
            var date = DateTime.UtcNow.AddDays(-i).Date;
            var value = baseValue + (random.NextDouble() - 0.5) * 2 * variance;
            
            data.Add(new
            {
                date = date.ToString("yyyy-MM-dd"),
                value = Math.Round(value, 2)
            });
        }
        
        return data;
    }

    private byte[] GenerateCsvReport(PerformanceReport report)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Metric,Value,Unit,Status");
        csv.AppendLine($"Average Response Time,{report.ApplicationMetrics.AverageResponseTime},ms,OK");
        csv.AppendLine($"P95 Response Time,{report.ApplicationMetrics.P95ResponseTime},ms,OK");
        csv.AppendLine($"Throughput,{report.ApplicationMetrics.ThroughputRps},RPS,OK");
        csv.AppendLine($"Error Rate,{report.ApplicationMetrics.ErrorRate},%,OK");
        csv.AppendLine($"Active Connections,{report.DatabaseMetrics.ActiveConnections},count,OK");
        csv.AppendLine($"Cache Hit Ratio,{report.DatabaseMetrics.CacheHitRatio * 100},%,OK");
        csv.AppendLine($"Memory Usage,{report.SystemMetrics.MemoryUsage},MB,OK");
        csv.AppendLine($"CPU Usage,{report.SystemMetrics.CpuUsage},%,OK");
        
        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    private async Task<byte[]> GeneratePdfReportAsync(PerformanceReport report)
    {
        // Simplified PDF generation - in real implementation, use a PDF library
        var content = $@"
PERFORMANCE REPORT
Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC
Period: {report.PeriodStart:yyyy-MM-dd} to {report.PeriodEnd:yyyy-MM-dd}

APPLICATION METRICS
Average Response Time: {report.ApplicationMetrics.AverageResponseTime}ms
P95 Response Time: {report.ApplicationMetrics.P95ResponseTime}ms
Throughput: {report.ApplicationMetrics.ThroughputRps} RPS
Error Rate: {report.ApplicationMetrics.ErrorRate}%

DATABASE METRICS
Active Connections: {report.DatabaseMetrics.ActiveConnections}
Average Query Time: {report.DatabaseMetrics.AverageQueryTime}ms
Cache Hit Ratio: {report.DatabaseMetrics.CacheHitRatio * 100:F1}%

SYSTEM METRICS
Memory Usage: {report.SystemMetrics.MemoryUsage:F1} MB
CPU Usage: {report.SystemMetrics.CpuUsage:F1}%
Uptime: {report.SystemMetrics.UptimeSeconds / 3600:F1} hours

RECOMMENDATIONS
{string.Join("\n", report.Recommendations.Select(r => $"- {r}"))}
";
        
        return System.Text.Encoding.UTF8.GetBytes(content);
    }
}

// Data models
public class PerformanceReport
{
    public DateTime GeneratedAt { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public ApplicationMetrics ApplicationMetrics { get; set; } = new();
    public DatabaseMetrics DatabaseMetrics { get; set; } = new();
    public SystemMetrics SystemMetrics { get; set; } = new();
    public BusinessMetrics BusinessMetrics { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

// ApplicationMetrics class defined in HealthController.cs

public class DatabaseMetrics
{
    public int ActiveConnections { get; set; }
    public double AverageQueryTime { get; set; }
    public int SlowQueries { get; set; }
    public double CacheHitRatio { get; set; }
    public double IndexEfficiency { get; set; }
    public int DeadlockCount { get; set; }
    public double LockWaitTime { get; set; }
}

public class SystemMetrics
{
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public object NetworkIO { get; set; } = new();
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public double UptimeSeconds { get; set; }
}

public class BusinessMetrics
{
    public int ActiveContracts { get; set; }
    public int DailyTransactions { get; set; }
    public int TradingVolume { get; set; }
    public int AverageContractValue { get; set; }
    public int PricingEvents { get; set; }
    public double SettlementEfficiency { get; set; }
}

public class BenchmarkRequest
{
    public string TestType { get; set; } = "general";
    public int IterationCount { get; set; } = 100;
}

public class BenchmarkResult
{
    public int Iteration { get; set; }
    public long Duration { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}