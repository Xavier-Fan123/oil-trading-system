using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Services;

/// <summary>
/// Service for managing report scheduling with frequency and timezone support
/// </summary>
public class ReportScheduleService : IReportScheduleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportScheduleService> _logger;

    public ReportScheduleService(
        ApplicationDbContext context,
        ILogger<ReportScheduleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new report schedule
    /// </summary>
    public async Task<ReportSchedule> CreateAsync(
        Guid reportConfigId,
        string frequency,
        string? time = null,
        string? timezone = null,
        int? dayOfWeek = null,
        int? dayOfMonth = null)
    {
        _logger.LogInformation("Creating schedule for report: {ConfigId}, Frequency: {Frequency}", reportConfigId, frequency);

        // Validate report configuration exists
        var config = await _context.ReportConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == reportConfigId && !c.IsDeleted);

        if (config == null)
        {
            throw new InvalidOperationException($"Report configuration not found: {reportConfigId}");
        }

        var schedule = new ReportSchedule
        {
            Id = Guid.NewGuid(),
            ReportConfigId = reportConfigId,
            Frequency = frequency,
            Time = time ?? "00:00",
            Timezone = timezone ?? "UTC",
            DayOfWeek = dayOfWeek,
            DayOfMonth = dayOfMonth,
            IsEnabled = true,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow,
            NextRunDate = CalculateNextRunDate(frequency, time, timezone, dayOfWeek, dayOfMonth)
        };

        _context.ReportSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Schedule created: {ScheduleId}, NextRun: {NextRun}", schedule.Id, schedule.NextRunDate);
        return schedule;
    }

    /// <summary>
    /// Get a specific schedule by ID
    /// </summary>
    public async Task<ReportSchedule?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving schedule: {ScheduleId}", id);

        var schedule = await _context.ReportSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (schedule == null)
        {
            _logger.LogWarning("Schedule not found: {ScheduleId}", id);
        }

        return schedule;
    }

    /// <summary>
    /// Get all schedules for a report configuration
    /// </summary>
    public async Task<List<ReportSchedule>> GetByConfigAsync(Guid configId)
    {
        _logger.LogInformation("Retrieving schedules for config: {ConfigId}", configId);

        var schedules = await _context.ReportSchedules
            .Where(s => s.ReportConfigId == configId && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} schedules for config {ConfigId}", schedules.Count, configId);
        return schedules;
    }

    /// <summary>
    /// Update an existing schedule
    /// </summary>
    public async Task<ReportSchedule?> UpdateAsync(
        Guid id,
        string? frequency = null,
        string? time = null,
        string? timezone = null,
        int? dayOfWeek = null,
        int? dayOfMonth = null,
        bool? isEnabled = null)
    {
        _logger.LogInformation("Updating schedule: {ScheduleId}", id);

        var schedule = await _context.ReportSchedules
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (schedule == null)
        {
            _logger.LogWarning("Schedule not found for update: {ScheduleId}", id);
            return null;
        }

        if (!string.IsNullOrEmpty(frequency))
        {
            schedule.Frequency = frequency;
        }

        if (!string.IsNullOrEmpty(time))
            schedule.Time = time;
        if (!string.IsNullOrEmpty(timezone))
            schedule.Timezone = timezone;
        if (dayOfWeek.HasValue)
            schedule.DayOfWeek = dayOfWeek;
        if (dayOfMonth.HasValue)
            schedule.DayOfMonth = dayOfMonth;
        if (isEnabled.HasValue)
            schedule.IsEnabled = isEnabled.Value;

        // Recalculate next run date if frequency or timing changed
        if (!string.IsNullOrEmpty(frequency) || !string.IsNullOrEmpty(time) || !string.IsNullOrEmpty(timezone))
        {
            schedule.NextRunDate = CalculateNextRunDate(
                frequency ?? schedule.Frequency,
                time ?? schedule.Time,
                timezone ?? schedule.Timezone,
                dayOfWeek ?? schedule.DayOfWeek,
                dayOfMonth ?? schedule.DayOfMonth);
        }

        schedule.UpdatedDate = DateTime.UtcNow;

        _context.ReportSchedules.Update(schedule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Schedule updated: {ScheduleId}, NextRun: {NextRun}", schedule.Id, schedule.NextRunDate);
        return schedule;
    }

    /// <summary>
    /// Toggle schedule enabled/disabled status
    /// </summary>
    public async Task<bool> ToggleAsync(Guid id)
    {
        _logger.LogInformation("Toggling schedule: {ScheduleId}", id);

        var schedule = await _context.ReportSchedules
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (schedule == null)
        {
            _logger.LogWarning("Schedule not found for toggle: {ScheduleId}", id);
            return false;
        }

        schedule.IsEnabled = !schedule.IsEnabled;
        schedule.UpdatedDate = DateTime.UtcNow;

        _context.ReportSchedules.Update(schedule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Schedule toggled: {ScheduleId}, IsEnabled: {IsEnabled}", schedule.Id, schedule.IsEnabled);
        return true;
    }

    /// <summary>
    /// Update the next run date for a schedule
    /// </summary>
    public async Task<bool> UpdateNextRunDateAsync(Guid id, DateTime nextRunDate)
    {
        _logger.LogInformation("Updating next run date for schedule: {ScheduleId}, NextRun: {NextRun}", id, nextRunDate);

        var schedule = await _context.ReportSchedules
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (schedule == null)
        {
            _logger.LogWarning("Schedule not found for next run update: {ScheduleId}", id);
            return false;
        }

        schedule.LastRunDate = DateTime.UtcNow;
        schedule.NextRunDate = nextRunDate;
        schedule.UpdatedDate = DateTime.UtcNow;

        _context.ReportSchedules.Update(schedule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Next run date updated: {ScheduleId}, NextRun: {NextRun}", schedule.Id, schedule.NextRunDate);
        return true;
    }

    /// <summary>
    /// Soft delete a schedule
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Deleting schedule: {ScheduleId}", id);

        var schedule = await _context.ReportSchedules
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (schedule == null)
        {
            _logger.LogWarning("Schedule not found for deletion: {ScheduleId}", id);
            return false;
        }

        schedule.IsDeleted = true;
        schedule.UpdatedDate = DateTime.UtcNow;

        _context.ReportSchedules.Update(schedule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Schedule deleted: {ScheduleId}", id);
        return true;
    }

    /// <summary>
    /// Get all enabled schedules that are due to run (next run date <= now)
    /// </summary>
    public async Task<List<ReportSchedule>> GetDueSchedulesAsync()
    {
        _logger.LogInformation("Retrieving due schedules");

        var now = DateTime.UtcNow;
        var dueSchedules = await _context.ReportSchedules
            .Where(s => s.IsEnabled &&
                   !s.IsDeleted &&
                   s.NextRunDate.HasValue &&
                   s.NextRunDate.Value <= now)
            .Include(s => s.ReportConfig)
            .OrderBy(s => s.NextRunDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} due schedules", dueSchedules.Count);
        return dueSchedules;
    }

    /// <summary>
    /// Get upcoming schedules (will run within the specified hours)
    /// </summary>
    public async Task<List<ReportSchedule>> GetUpcomingAsync(int hoursAhead = 24)
    {
        _logger.LogInformation("Retrieving upcoming schedules for next {Hours} hours", hoursAhead);

        var now = DateTime.UtcNow;
        var future = now.AddHours(hoursAhead);

        var upcomingSchedules = await _context.ReportSchedules
            .Where(s => s.IsEnabled &&
                   !s.IsDeleted &&
                   s.NextRunDate.HasValue &&
                   s.NextRunDate.Value > now &&
                   s.NextRunDate.Value <= future)
            .Include(s => s.ReportConfig)
            .OrderBy(s => s.NextRunDate)
            .ToListAsync();

        _logger.LogInformation("Found {Count} upcoming schedules", upcomingSchedules.Count);
        return upcomingSchedules;
    }

    /// <summary>
    /// Calculate next run date based on frequency and timing configuration
    /// </summary>
    private DateTime CalculateNextRunDate(
        string frequency,
        string? time,
        string? timezone,
        int? dayOfWeek,
        int? dayOfMonth)
    {
        var now = DateTime.UtcNow;
        var scheduleTime = ParseTime(time ?? "00:00");

        return frequency.ToLower() switch
        {
            "once" => now.AddMinutes(1), // Run within the next minute
            "daily" => GetNextDailyRun(now, scheduleTime),
            "weekly" => GetNextWeeklyRun(now, scheduleTime, dayOfWeek ?? 0),
            "monthly" => GetNextMonthlyRun(now, scheduleTime, dayOfMonth ?? 1),
            "quarterly" => GetNextQuarterlyRun(now, scheduleTime, dayOfMonth ?? 1),
            "annually" => GetNextAnnualRun(now, scheduleTime, dayOfMonth ?? 1),
            _ => now.AddHours(1) // Default: next hour
        };
    }

    /// <summary>
    /// Parse time string in HH:mm format
    /// </summary>
    private TimeSpan ParseTime(string timeStr)
    {
        if (TimeSpan.TryParse(timeStr, out var ts))
            return ts;
        return TimeSpan.Zero;
    }

    private DateTime GetNextDailyRun(DateTime now, TimeSpan scheduleTime)
    {
        var nextRun = now.Date.Add(scheduleTime);
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);
        return nextRun;
    }

    private DateTime GetNextWeeklyRun(DateTime now, TimeSpan scheduleTime, int dayOfWeek)
    {
        var nextRun = now.Date.Add(scheduleTime);
        var daysUntilTarget = ((dayOfWeek - (int)nextRun.DayOfWeek) + 7) % 7;

        if (daysUntilTarget == 0 && nextRun <= now)
            daysUntilTarget = 7;

        nextRun = nextRun.AddDays(daysUntilTarget);
        return nextRun;
    }

    private DateTime GetNextMonthlyRun(DateTime now, TimeSpan scheduleTime, int dayOfMonth)
    {
        var nextRun = new DateTime(now.Year, now.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(now.Year, now.Month))).Add(scheduleTime);

        if (nextRun <= now)
        {
            var nextMonth = now.AddMonths(1);
            nextRun = new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month))).Add(scheduleTime);
        }

        return nextRun;
    }

    private DateTime GetNextQuarterlyRun(DateTime now, TimeSpan scheduleTime, int dayOfMonth)
    {
        var quarter = (now.Month - 1) / 3;
        var startMonth = (quarter * 3) + 1;
        var nextRun = new DateTime(now.Year, startMonth, Math.Min(dayOfMonth, DateTime.DaysInMonth(now.Year, startMonth))).Add(scheduleTime);

        if (nextRun <= now)
        {
            quarter = (quarter + 1) % 4;
            startMonth = (quarter * 3) + 1;
            var year = quarter == 0 ? now.Year + 1 : now.Year;
            nextRun = new DateTime(year, startMonth, Math.Min(dayOfMonth, DateTime.DaysInMonth(year, startMonth))).Add(scheduleTime);
        }

        return nextRun;
    }

    private DateTime GetNextAnnualRun(DateTime now, TimeSpan scheduleTime, int dayOfMonth)
    {
        var nextRun = new DateTime(now.Year, 1, Math.Min(dayOfMonth, DateTime.DaysInMonth(now.Year, 1))).Add(scheduleTime);

        if (nextRun <= now)
            nextRun = new DateTime(now.Year + 1, 1, Math.Min(dayOfMonth, DateTime.DaysInMonth(now.Year + 1, 1))).Add(scheduleTime);

        return nextRun;
    }
}

/// <summary>
/// Interface for report schedule service
/// </summary>
public interface IReportScheduleService
{
    Task<ReportSchedule> CreateAsync(
        Guid reportConfigId,
        string frequency,
        string? time = null,
        string? timezone = null,
        int? dayOfWeek = null,
        int? dayOfMonth = null);

    Task<ReportSchedule?> GetByIdAsync(Guid id);
    Task<List<ReportSchedule>> GetByConfigAsync(Guid configId);
    Task<ReportSchedule?> UpdateAsync(
        Guid id,
        string? frequency = null,
        string? time = null,
        string? timezone = null,
        int? dayOfWeek = null,
        int? dayOfMonth = null,
        bool? isEnabled = null);
    Task<bool> ToggleAsync(Guid id);
    Task<bool> UpdateNextRunDateAsync(Guid id, DateTime nextRunDate);
    Task<bool> DeleteAsync(Guid id);
    Task<List<ReportSchedule>> GetDueSchedulesAsync();
    Task<List<ReportSchedule>> GetUpcomingAsync(int hoursAhead = 24);
}
