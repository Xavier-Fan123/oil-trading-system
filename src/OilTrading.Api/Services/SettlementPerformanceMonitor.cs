using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OilTrading.Api.Services;

/// <summary>
/// Performance monitoring utility for settlement operations
/// Tracks execution time, logs metrics, and identifies bottlenecks
/// Part of Phase 12: Monitoring & Performance
/// </summary>
public interface ISettlementPerformanceMonitor
{
    Task<T> MonitorAsync<T>(string operationName, Func<Task<T>> operation);
    Task MonitorAsync(string operationName, Func<Task> operation);
    void RecordMetric(string metricName, double value, string unit = "ms");
}

public class SettlementPerformanceMonitor : ISettlementPerformanceMonitor
{
    private readonly ILogger<SettlementPerformanceMonitor> _logger;
    private const double SlowQueryThresholdMs = 1000; // 1 second
    private const double CriticalQueryThresholdMs = 5000; // 5 seconds

    public SettlementPerformanceMonitor(ILogger<SettlementPerformanceMonitor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Monitors async operation with timing and logging
    /// </summary>
    public async Task<T> MonitorAsync<T>(string operationName, Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Settlement operation started: {OperationName} at {StartTime}", operationName, startTime);

            var result = await operation();

            stopwatch.Stop();

            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            LogPerformanceMetric(operationName, elapsedMs, success: true);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            LogPerformanceMetric(operationName, elapsedMs, success: false, exception: ex);
            throw;
        }
    }

    /// <summary>
    /// Monitors async operation without return value
    /// </summary>
    public async Task MonitorAsync(string operationName, Func<Task> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Settlement operation started: {OperationName} at {StartTime}", operationName, startTime);

            await operation();

            stopwatch.Stop();

            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            LogPerformanceMetric(operationName, elapsedMs, success: true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            LogPerformanceMetric(operationName, elapsedMs, success: false, exception: ex);
            throw;
        }
    }

    /// <summary>
    /// Records custom metric with unit
    /// </summary>
    public void RecordMetric(string metricName, double value, string unit = "ms")
    {
        _logger.LogInformation(
            "Settlement metric recorded: {MetricName} = {Value} {Unit}",
            metricName, value, unit);
    }

    private void LogPerformanceMetric(
        string operationName,
        double elapsedMs,
        bool success,
        Exception? exception = null)
    {
        var level = GetLogLevel(elapsedMs, success);

        if (success)
        {
            _logger.Log(
                level,
                "Settlement operation completed: {OperationName} completed in {ElapsedMs}ms",
                operationName,
                elapsedMs);
        }
        else
        {
            _logger.Log(
                LogLevel.Error,
                exception,
                "Settlement operation failed: {OperationName} failed after {ElapsedMs}ms",
                operationName,
                elapsedMs);
        }
    }

    private LogLevel GetLogLevel(double elapsedMs, bool success)
    {
        if (!success)
            return LogLevel.Error;

        if (elapsedMs >= CriticalQueryThresholdMs)
            return LogLevel.Warning;

        if (elapsedMs >= SlowQueryThresholdMs)
            return LogLevel.Information;

        return LogLevel.Debug;
    }
}

/// <summary>
/// Batch operation performance monitor for bulk settlement operations
/// </summary>
public interface IBatchSettlementPerformanceMonitor
{
    void StartBatch(string batchName, int itemCount);
    void LogItemCompletion(int processedCount);
    void EndBatch(bool success = true, Exception? exception = null);
    double GetAverageItemTime();
    double GetTotalTime();
}

public class BatchSettlementPerformanceMonitor : IBatchSettlementPerformanceMonitor
{
    private readonly ILogger<BatchSettlementPerformanceMonitor> _logger;
    private Stopwatch? _batchStopwatch;
    private string? _currentBatchName;
    private int _totalItems;
    private int _processedItems;
    private DateTime _batchStartTime;

    public BatchSettlementPerformanceMonitor(ILogger<BatchSettlementPerformanceMonitor> logger)
    {
        _logger = logger;
    }

    public void StartBatch(string batchName, int itemCount)
    {
        _currentBatchName = batchName;
        _totalItems = itemCount;
        _processedItems = 0;
        _batchStartTime = DateTime.UtcNow;
        _batchStopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Batch settlement operation started: {BatchName} with {ItemCount} items at {StartTime}",
            batchName,
            itemCount,
            _batchStartTime);
    }

    public void LogItemCompletion(int processedCount)
    {
        _processedItems = processedCount;
        var progress = (double)_processedItems / _totalItems * 100;

        _logger.LogInformation(
            "Batch progress: {BatchName} - {ProcessedCount}/{TotalCount} items ({Progress:F1}%)",
            _currentBatchName,
            _processedItems,
            _totalItems,
            progress);
    }

    public void EndBatch(bool success = true, Exception? exception = null)
    {
        _batchStopwatch?.Stop();

        var elapsedMs = _batchStopwatch?.Elapsed.TotalMilliseconds ?? 0;
        var averageMs = _totalItems > 0 ? elapsedMs / _totalItems : 0;
        var throughput = _totalItems > 0 ? (1000 / averageMs * _totalItems) : 0; // items per second

        if (success)
        {
            _logger.LogInformation(
                "Batch settlement operation completed: {BatchName} processed {ItemCount} items in {ElapsedMs}ms (avg {AverageMs:F2}ms per item, {Throughput:F0} items/sec)",
                _currentBatchName,
                _totalItems,
                elapsedMs,
                averageMs,
                throughput);
        }
        else
        {
            _logger.LogError(
                exception,
                "Batch settlement operation failed: {BatchName} failed after processing {ProcessedCount}/{TotalCount} items in {ElapsedMs}ms",
                _currentBatchName,
                _processedItems,
                _totalItems,
                elapsedMs);
        }
    }

    public double GetAverageItemTime()
    {
        return _totalItems > 0 ? (_batchStopwatch?.Elapsed.TotalMilliseconds ?? 0) / _totalItems : 0;
    }

    public double GetTotalTime()
    {
        return _batchStopwatch?.Elapsed.TotalMilliseconds ?? 0;
    }
}

/// <summary>
/// Settlement operation metrics collector for aggregate statistics
/// </summary>
public interface ISettlementMetricsCollector
{
    void RecordSettlementCreation(double durationMs);
    void RecordSettlementCalculation(double durationMs);
    void RecordSettlementApproval(double durationMs);
    void RecordSettlementFinalization(double durationMs);

    // Statistics
    double GetAverageCreationTime();
    double GetAverageCalculationTime();
    double GetAverageApprovalTime();
    double GetAverageFinalizationTime();
    int GetTotalOperations();
}

public class SettlementMetricsCollector : ISettlementMetricsCollector
{
    private readonly List<double> _creationTimes = new();
    private readonly List<double> _calculationTimes = new();
    private readonly List<double> _approvalTimes = new();
    private readonly List<double> _finalizationTimes = new();
    private readonly ILogger<SettlementMetricsCollector> _logger;
    private readonly object _lockObject = new();

    public SettlementMetricsCollector(ILogger<SettlementMetricsCollector> logger)
    {
        _logger = logger;
    }

    public void RecordSettlementCreation(double durationMs)
    {
        lock (_lockObject)
        {
            _creationTimes.Add(durationMs);
        }
    }

    public void RecordSettlementCalculation(double durationMs)
    {
        lock (_lockObject)
        {
            _calculationTimes.Add(durationMs);
        }
    }

    public void RecordSettlementApproval(double durationMs)
    {
        lock (_lockObject)
        {
            _approvalTimes.Add(durationMs);
        }
    }

    public void RecordSettlementFinalization(double durationMs)
    {
        lock (_lockObject)
        {
            _finalizationTimes.Add(durationMs);
        }
    }

    public double GetAverageCreationTime() => CalculateAverage(_creationTimes);
    public double GetAverageCalculationTime() => CalculateAverage(_calculationTimes);
    public double GetAverageApprovalTime() => CalculateAverage(_approvalTimes);
    public double GetAverageFinalizationTime() => CalculateAverage(_finalizationTimes);

    public int GetTotalOperations()
    {
        lock (_lockObject)
        {
            return _creationTimes.Count + _calculationTimes.Count + _approvalTimes.Count + _finalizationTimes.Count;
        }
    }

    private double CalculateAverage(List<double> times)
    {
        lock (_lockObject)
        {
            return times.Count > 0 ? times.Average() : 0;
        }
    }

    public void LogMetricsSummary()
    {
        _logger.LogInformation(
            "Settlement metrics summary: CreationAvg={CreationAvg:F2}ms, CalculationAvg={CalcAvg:F2}ms, ApprovalAvg={ApprovalAvg:F2}ms, FinalizationAvg={FinalAvg:F2}ms, Total={Total} operations",
            GetAverageCreationTime(),
            GetAverageCalculationTime(),
            GetAverageApprovalTime(),
            GetAverageFinalizationTime(),
            GetTotalOperations());
    }
}
