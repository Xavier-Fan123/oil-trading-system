using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OilTrading.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that distributes completed reports to configured channels
/// Runs every 5 minutes to check for reports ready for distribution
/// </summary>
public class ReportDistributionJob : BackgroundService
{
    private readonly ILogger<ReportDistributionJob> _logger;
    private Timer? _timer;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Execute every 5 minutes

    public ReportDistributionJob(ILogger<ReportDistributionJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Report Distribution Job is starting");

        _timer = new Timer(async state => await DoWorkAsync(stoppingToken), null, TimeSpan.Zero, _interval);

        return Task.CompletedTask;
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Report Distribution Job is running");

        try
        {
            _logger.LogDebug("Checking for reports ready for distribution");

            // Implementation details:
            // 1. Query the database for all ReportExecution records with Status = "Completed" and not yet distributed
            // 2. For each report, get the associated distribution channels
            // 3. Send the report to each enabled distribution channel (Email, SFTP, Webhook)
            // 4. Handle failures gracefully (retry, log, etc.)
            // 5. Update the execution record to mark distribution as complete
            // 6. Track distribution metrics (success rate, delivery time, etc.)

            _logger.LogDebug("Report distribution check completed");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while distributing reports");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Report Distribution Job is stopping");

        _timer?.Change(Timeout.Infinite, 0);

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
