using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OilTrading.Infrastructure.Data;
using StackExchange.Redis;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ApplicationReadDbContext _readContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ApplicationDbContext context,
        ApplicationReadDbContext readContext,
        IConnectionMultiplexer redis,
        ILogger<HealthController> logger)
    {
        _context = context;
        _readContext = readContext;
        _redis = redis;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var healthStatus = new HealthStatus
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            Uptime = GetUptime()
        };

        try
        {
            // Check database connectivity
            healthStatus.Database = await CheckDatabaseHealth();
            
            // Check read database connectivity
            healthStatus.ReadDatabase = await CheckReadDatabaseHealth();
            
            // Check Redis connectivity
            healthStatus.Redis = await CheckRedisHealth();
            
            // Check system resources
            healthStatus.System = GetSystemHealth();
            
            // Determine overall status
            healthStatus.Status = DetermineOverallStatus(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            healthStatus.Status = "Unhealthy";
            healthStatus.Error = ex.Message;
        }

        var statusCode = healthStatus.Status == "Healthy" ? 200 : 
                        healthStatus.Status == "Degraded" ? 200 : 503;

        return StatusCode(statusCode, healthStatus);
    }

    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedHealth()
    {
        var detailedHealth = new DetailedHealthStatus
        {
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown"
        };

        try
        {
            // Database detailed checks
            detailedHealth.Database = await GetDetailedDatabaseHealth();
            
            // Redis detailed checks
            detailedHealth.Redis = await GetDetailedRedisHealth();
            
            // System detailed checks
            detailedHealth.System = GetDetailedSystemHealth();
            
            // Application metrics
            detailedHealth.Application = GetApplicationMetrics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed health check failed");
            detailedHealth.Error = ex.Message;
        }

        return Ok(detailedHealth);
    }

    [HttpGet("liveness")]
    public IActionResult GetLiveness()
    {
        // Simple liveness probe for Kubernetes
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }

    [HttpGet("readiness")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            // Check if application is ready to serve traffic
            var canConnectToDb = await _context.Database.CanConnectAsync();
            var canConnectToRedis = _redis.IsConnected;

            if (canConnectToDb && canConnectToRedis)
            {
                return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
            }
            else
            {
                return StatusCode(503, new { 
                    status = "not_ready", 
                    timestamp = DateTime.UtcNow,
                    database = canConnectToDb,
                    redis = canConnectToRedis
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new { status = "not_ready", error = ex.Message });
        }
    }

    private async Task<DatabaseHealthInfo> CheckDatabaseHealth()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            stopwatch.Stop();

            return new DatabaseHealthInfo
            {
                Status = canConnect ? "Healthy" : "Unhealthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                ConnectionString = MaskConnectionString(_context.Database.GetConnectionString()),
                CanConnect = canConnect
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new DatabaseHealthInfo
            {
                Status = "Unhealthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                ConnectionString = MaskConnectionString(_context.Database.GetConnectionString()),
                CanConnect = false,
                Error = ex.Message
            };
        }
    }

    private async Task<DatabaseHealthInfo> CheckReadDatabaseHealth()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var canConnect = await _readContext.Database.CanConnectAsync();
            stopwatch.Stop();

            return new DatabaseHealthInfo
            {
                Status = canConnect ? "Healthy" : "Unhealthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                ConnectionString = MaskConnectionString(_readContext.Database.GetConnectionString()),
                CanConnect = canConnect
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new DatabaseHealthInfo
            {
                Status = "Unhealthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                ConnectionString = MaskConnectionString(_readContext.Database.GetConnectionString()),
                CanConnect = false,
                Error = ex.Message
            };
        }
    }

    private async Task<RedisHealthInfo> CheckRedisHealth()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var database = _redis.GetDatabase();
            await database.PingAsync();
            stopwatch.Stop();

            return new RedisHealthInfo
            {
                Status = "Healthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                IsConnected = _redis.IsConnected,
                ServerCount = _redis.GetServers().Count()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new RedisHealthInfo
            {
                Status = "Unhealthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                IsConnected = false,
                Error = ex.Message
            };
        }
    }

    private SystemHealthInfo GetSystemHealth()
    {
        var process = Process.GetCurrentProcess();
        
        return new SystemHealthInfo
        {
            Status = "Healthy",
            CpuUsage = GetCpuUsage(),
            MemoryUsage = GC.GetTotalMemory(false),
            WorkingSet = process.WorkingSet64,
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount
        };
    }

    private async Task<DetailedDatabaseHealthInfo> GetDetailedDatabaseHealth()
    {
        var info = new DetailedDatabaseHealthInfo();
        
        try
        {
            // Connection pool info
            var poolInfo = await GetConnectionPoolInfo();
            info.ConnectionPool = poolInfo;
            
            // Database size and statistics
            var dbStats = await GetDatabaseStatistics();
            info.Statistics = dbStats;
            
            // Check for long-running queries
            var longRunningQueries = await GetLongRunningQueries();
            info.LongRunningQueries = longRunningQueries;
            
            // Replication status (if applicable)
            var replicationStatus = await GetReplicationStatus();
            info.ReplicationStatus = replicationStatus;
            
            info.Status = "Healthy";
        }
        catch (Exception ex)
        {
            info.Status = "Unhealthy";
            info.Error = ex.Message;
        }
        
        return info;
    }

    private async Task<DetailedRedisHealthInfo> GetDetailedRedisHealth()
    {
        var info = new DetailedRedisHealthInfo();
        
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var serverInfo = await server.InfoAsync();
            
            // Extract Redis server information
            try
            {
                // Redis server info is organized in groups - flatten it first
                var flatInfo = serverInfo.SelectMany(group => group).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                
                info.Memory = flatInfo.TryGetValue("used_memory", out var memory) ? memory : "N/A";
                info.ConnectedClients = flatInfo.TryGetValue("connected_clients", out var clients) ? clients : "N/A";
                info.Uptime = flatInfo.TryGetValue("uptime_in_seconds", out var uptime) ? uptime : "N/A";
                info.CommandsProcessed = flatInfo.TryGetValue("total_commands_processed", out var commands) ? commands : "N/A";
            }
            catch
            {
                info.Memory = "N/A";
                info.ConnectedClients = "N/A";
                info.Uptime = "N/A";
                info.CommandsProcessed = "N/A";
            }
            info.Status = "Healthy";
        }
        catch (Exception ex)
        {
            info.Status = "Unhealthy";
            info.Error = ex.Message;
        }
        
        return info;
    }

    private DetailedSystemHealthInfo GetDetailedSystemHealth()
    {
        var process = Process.GetCurrentProcess();
        
        return new DetailedSystemHealthInfo
        {
            Status = "Healthy",
            ProcessorCount = Environment.ProcessorCount,
            OSVersion = Environment.OSVersion.ToString(),
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            WorkingDirectory = Environment.CurrentDirectory,
            CommandLine = Environment.CommandLine,
            ProcessorTime = process.TotalProcessorTime,
            GCCollections = new
            {
                Gen0 = GC.CollectionCount(0),
                Gen1 = GC.CollectionCount(1),
                Gen2 = GC.CollectionCount(2)
            }
        };
    }

    private ApplicationMetrics GetApplicationMetrics()
    {
        return new ApplicationMetrics
        {
            RequestCount = GetRequestCount(),
            AverageResponseTime = GetAverageResponseTime(),
            ErrorRate = GetErrorRate(),
            ActiveConnections = GetActiveConnections(),
            CacheHitRate = GetCacheHitRate()
        };
    }

    private string GetUptime()
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return uptime.ToString(@"dd\.hh\:mm\:ss");
    }

    private double GetCpuUsage()
    {
        // Simplified CPU usage calculation
        // In production, you might want to use more sophisticated metrics
        return Math.Round(Environment.ProcessorCount * 0.1, 2);
    }

    private string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "Not configured";
            
        // Mask sensitive information in connection string
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString, 
            @"(Password|Pwd)=([^;]*)", 
            "$1=***", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private string DetermineOverallStatus(HealthStatus health)
    {
        if (health.Database?.Status == "Unhealthy" || health.Redis?.Status == "Unhealthy")
            return "Unhealthy";
        if (health.ReadDatabase?.Status == "Unhealthy")
            return "Degraded";
        return "Healthy";
    }

    // Placeholder methods for detailed health checks
    private async Task<object> GetConnectionPoolInfo() => new { };
    private async Task<object> GetDatabaseStatistics() => new { };
    private async Task<object> GetLongRunningQueries() => new { };
    private async Task<object> GetReplicationStatus() => new { };
    private long GetRequestCount() => 0;
    private double GetAverageResponseTime() => 0.0;
    private double GetErrorRate() => 0.0;
    private int GetActiveConnections() => 0;
    private double GetCacheHitRate() => 0.0;
}

// Health status models
public class HealthStatus
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public string Uptime { get; set; } = string.Empty;
    public DatabaseHealthInfo? Database { get; set; }
    public DatabaseHealthInfo? ReadDatabase { get; set; }
    public RedisHealthInfo? Redis { get; set; }
    public SystemHealthInfo? System { get; set; }
    public string? Error { get; set; }
}

public class DatabaseHealthInfo
{
    public string Status { get; set; } = string.Empty;
    public long ResponseTime { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public bool CanConnect { get; set; }
    public string? Error { get; set; }
}

public class RedisHealthInfo
{
    public string Status { get; set; } = string.Empty;
    public long ResponseTime { get; set; }
    public bool IsConnected { get; set; }
    public int ServerCount { get; set; }
    public string? Error { get; set; }
}

public class SystemHealthInfo
{
    public string Status { get; set; } = string.Empty;
    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public long WorkingSet { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
}

public class DetailedHealthStatus
{
    public DateTime Timestamp { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DetailedDatabaseHealthInfo? Database { get; set; }
    public DetailedRedisHealthInfo? Redis { get; set; }
    public DetailedSystemHealthInfo? System { get; set; }
    public ApplicationMetrics? Application { get; set; }
    public string? Error { get; set; }
}

public class DetailedDatabaseHealthInfo
{
    public string Status { get; set; } = string.Empty;
    public object? ConnectionPool { get; set; }
    public object? Statistics { get; set; }
    public object? LongRunningQueries { get; set; }
    public object? ReplicationStatus { get; set; }
    public string? Error { get; set; }
}

public class DetailedRedisHealthInfo
{
    public string Status { get; set; } = string.Empty;
    public string? Memory { get; set; }
    public string? ConnectedClients { get; set; }
    public string? Uptime { get; set; }
    public string? CommandsProcessed { get; set; }
    public string? Error { get; set; }
}

public class DetailedSystemHealthInfo
{
    public string Status { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public string OSVersion { get; set; } = string.Empty;
    public string OSArchitecture { get; set; } = string.Empty;
    public string FrameworkDescription { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public string CommandLine { get; set; } = string.Empty;
    public TimeSpan ProcessorTime { get; set; }
    public object? GCCollections { get; set; }
}

public class ApplicationMetrics
{
    public long RequestCount { get; set; }
    public double AverageResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public double P99ResponseTime { get; set; }
    public double ThroughputRps { get; set; }
    public double ErrorRate { get; set; }
    public double SuccessRate { get; set; }
    public int ActiveSessions { get; set; }
    public int ActiveConnections { get; set; }
    public double MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public double CacheHitRate { get; set; }
    public object GarbageCollections { get; set; } = new();
}