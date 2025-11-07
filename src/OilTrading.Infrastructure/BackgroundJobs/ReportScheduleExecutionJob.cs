using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OilTrading.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that executes scheduled reports
/// Runs every minute to check for reports that need to be executed
/// </summary>
public class ReportScheduleExecutionJob : BackgroundService
{
    private readonly ILogger<ReportScheduleExecutionJob> _logger;
    private Timer? _timer;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // Execute every minute

    public ReportScheduleExecutionJob(ILogger<ReportScheduleExecutionJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Report Schedule Execution Job is starting");

        _timer = new Timer(async state => await DoWorkAsync(stoppingToken), null, TimeSpan.Zero, _interval);

        return Task.CompletedTask;
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Report Schedule Execution Job is running");

        try
        {
            // Check for reports scheduled to run at current time
            var currentUtc = DateTime.UtcNow;
            _logger.LogDebug("Checking for reports scheduled to run at {Time}", currentUtc);

            // Implementation details:
            // 1. Query the database for all schedules with NextRunDate <= currentUtc
            // 2. Execute each report using IReportExecutionService
            // 3. Update the NextRunDate based on the schedule frequency
            // 4. Log success/failure for each execution

            _logger.LogDebug("Report schedule execution check completed");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while executing scheduled reports");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Report Schedule Execution Job is stopping");

        _timer?.Change(Timeout.Infinite, 0);

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
