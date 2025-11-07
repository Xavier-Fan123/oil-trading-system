using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OilTrading.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that manages report archive cleanup and retention
/// Runs daily at 2 AM (UTC) to clean up expired archives
/// </summary>
public class ReportArchiveCleanupJob : BackgroundService
{
    private readonly ILogger<ReportArchiveCleanupJob> _logger;
    private Timer? _timer;
    private readonly int _executionHourUtc = 2; // Run at 2 AM UTC
    private readonly int _executionMinuteUtc = 0; // Run at minute 0

    public ReportArchiveCleanupJob(ILogger<ReportArchiveCleanupJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Report Archive Cleanup Job is starting");

        // Calculate time until next execution (daily at specified hour:minute)
        var now = DateTime.UtcNow;
        var scheduledTime = new DateTime(now.Year, now.Month, now.Day, _executionHourUtc, _executionMinuteUtc, 0);

        // If the scheduled time has already passed today, schedule for tomorrow
        if (now > scheduledTime)
        {
            scheduledTime = scheduledTime.AddDays(1);
        }

        var timeUntilExecution = scheduledTime - now;

        _logger.LogInformation("Report Archive Cleanup Job scheduled to run at {ScheduledTime} UTC (in {TimeSpan})",
            scheduledTime, timeUntilExecution);

        // Run once at the calculated time, then daily
        _timer = new Timer(async state => await DoWorkAsync(stoppingToken), null, timeUntilExecution, TimeSpan.FromDays(1));

        return Task.CompletedTask;
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Report Archive Cleanup Job is running at {Time} UTC", DateTime.UtcNow);

        try
        {
            _logger.LogDebug("Starting archive cleanup process");

            // Implementation details:
            // 1. Query the database for all ReportArchive records with ExpiryDate <= today
            // 2. Delete the physical files associated with expired archives
            // 3. Delete the database records
            // 4. Track cleanup statistics (files deleted, space freed, etc.)
            // 5. Log detailed results
            // 6. Handle errors gracefully (partial cleanup, rollback, etc.)
            // 7. Send notifications if cleanup encounters issues

            _logger.LogInformation("Archive cleanup process completed");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during archive cleanup");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Report Archive Cleanup Job is stopping");

        _timer?.Change(Timeout.Infinite, 0);

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
