using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Services;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;
using OilTrading.Application.TransactionOperations;

namespace OilTrading.Infrastructure.Services;


public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(ApplicationDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogOperationAsync(OperationAuditLog auditLog)
    {
        try
        {
            _context.OperationAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Audit log created for operation {OperationId} in transaction {TransactionId}", 
                auditLog.OperationId, auditLog.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save audit log for operation {OperationId}", auditLog.OperationId);
            throw;
        }
    }

    public async Task<IEnumerable<OperationAuditLog>> GetAuditLogsAsync(Guid? transactionId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.OperationAuditLogs.AsQueryable();

        if (transactionId.HasValue)
        {
            query = query.Where(log => log.TransactionId == transactionId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(log => log.Timestamp <= toDate.Value);
        }

        return await query
            .OrderByDescending(log => log.Timestamp)
            .Take(1000) // Limit results for performance
            .ToListAsync();
    }

    public async Task<OperationAuditLog?> GetAuditLogByIdAsync(Guid operationId)
    {
        return await _context.OperationAuditLogs
            .FirstOrDefaultAsync(log => log.OperationId == operationId);
    }

    public async Task<IEnumerable<OperationAuditLog>> GetAuditLogsByEntityAsync(string entityType, string entityId)
    {
        // NOTE: This method requires entity type and entity ID to be stored in OperationAuditLog.AdditionalData
        // The OperationAuditLog entity should store entity context in its AdditionalData dictionary:
        // - Key "EntityType" for entity type (e.g., "PurchaseContract", "SalesContract")
        // - Key "EntityId" for entity ID (Guid as string)
        // This allows flexible tracking of operations across different entity types without rigid schema changes.

        try
        {
            var query = _context.OperationAuditLogs
                .Where(log =>
                    log.HasDataKey("EntityType") &&
                    log.GetDataValue<string>("EntityType") == entityType &&
                    log.HasDataKey("EntityId") &&
                    log.GetDataValue<string>("EntityId") == entityId);

            return await query
                .OrderByDescending(log => log.Timestamp)
                .Take(500) // Limit results for performance
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit logs for entity {EntityType}:{EntityId}", entityType, entityId);
            return new List<OperationAuditLog>(); // Return empty list on error
        }
    }

    public async Task<IEnumerable<OperationAuditLog>> GetAuditLogsByUserAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.OperationAuditLogs
            .Where(log => log.InitiatedBy == userId);

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(log => log.Timestamp <= toDate.Value);
        }

        return await query
            .OrderByDescending(log => log.Timestamp)
            .Take(500)
            .ToListAsync();
    }

    public async Task<AuditLogSummary> GetAuditSummaryAsync(DateTime fromDate, DateTime toDate)
    {
        var logs = await _context.OperationAuditLogs
            .Where(log => log.Timestamp >= fromDate && log.Timestamp <= toDate)
            .ToListAsync();

        var summary = new AuditLogSummary
        {
            PeriodStart = fromDate,
            PeriodEnd = toDate,
            TotalOperations = logs.Count,
            SuccessfulOperations = logs.Count(log => log.IsSuccess),
            FailedOperations = logs.Count(log => !log.IsSuccess)
        };

        // Group by operation type
        summary.OperationsByType = logs
            .GroupBy(log => log.OperationType)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by user
        summary.OperationsByUser = logs
            .GroupBy(log => log.InitiatedBy)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by entity type
        summary.OperationsByEntityType = logs
            .Where(log => log.HasDataKey("EntityType"))
            .GroupBy(log => log.GetDataValue<string>("EntityType") ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        return summary;
    }

    public async Task LogTransactionAsync(TransactionAuditLog auditLog)
    {
        try
        {
            // For now, we'll store transaction audit logs as operation logs with special type
            var operationLog = new OperationAuditLog(
                auditLog.TransactionId,
                auditLog.TransactionId,
                auditLog.TransactionName,
                "Transaction",
                auditLog.StartTime,
                auditLog.IsSuccess,
                new Dictionary<string, object>
                {
                    ["EndTime"] = auditLog.EndTime,
                    ["Duration"] = auditLog.Duration.TotalSeconds,
                    ["Status"] = auditLog.Status.ToString(),
                    ["StepCount"] = auditLog.Steps.Count,
                    ["WarningCount"] = auditLog.Warnings.Count,
                    ["Warnings"] = string.Join("; ", auditLog.Warnings)
                },
                auditLog.ErrorMessage,
                auditLog.InitiatedBy);

            await LogOperationAsync(operationLog);
            
            _logger.LogDebug("Transaction audit log created for transaction {TransactionId}", auditLog.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save transaction audit log for transaction {TransactionId}", auditLog.TransactionId);
            throw;
        }
    }

    public async Task<List<TransactionAuditLog>> GetTransactionHistoryAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var operationLogs = await _context.OperationAuditLogs
                .Where(log => log.OperationType == "Transaction" &&
                             log.Timestamp >= startDate && 
                             log.Timestamp <= endDate)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            var transactionLogs = operationLogs.Select(log => new TransactionAuditLog
            {
                TransactionId = log.OperationId,
                TransactionName = log.OperationName,
                InitiatedBy = log.InitiatedBy,
                StartTime = log.Timestamp,
                EndTime = log.GetDataValue<DateTime>("EndTime"),
                Duration = TimeSpan.FromSeconds(log.GetDataValue<double>("Duration")),
                Status = TransactionStatus.Failed, // Simplified
                IsSuccess = log.IsSuccess,
                ErrorMessage = log.ErrorMessage,
                Steps = new List<TransactionStep>(), // Simplified
                Warnings = log.GetDataValue<string>("Warnings")?.Split("; ").ToList() ?? new List<string>()
            }).ToList();

            return transactionLogs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction history from {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task PurgeOldAuditLogsAsync(DateTime cutoffDate)
    {
        try
        {
            var oldLogs = await _context.OperationAuditLogs
                .Where(log => log.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldLogs.Any())
            {
                _context.OperationAuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Purged {Count} audit logs older than {CutoffDate}", 
                    oldLogs.Count, cutoffDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to purge old audit logs before {CutoffDate}", cutoffDate);
            throw;
        }
    }
}