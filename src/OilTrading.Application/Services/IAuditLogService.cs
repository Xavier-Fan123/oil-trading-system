using OilTrading.Core.Entities;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Services;

public interface IAuditLogService
{
    Task LogOperationAsync(OperationAuditLog auditLog);
    Task LogTransactionAsync(TransactionAuditLog auditLog);
    Task<IEnumerable<OperationAuditLog>> GetAuditLogsAsync(Guid? transactionId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<OperationAuditLog?> GetAuditLogByIdAsync(Guid operationId);
    Task<IEnumerable<OperationAuditLog>> GetAuditLogsByEntityAsync(string entityType, string entityId);
    Task<IEnumerable<OperationAuditLog>> GetAuditLogsByUserAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<TransactionAuditLog>> GetTransactionHistoryAsync(DateTime startDate, DateTime endDate);
    Task<AuditLogSummary> GetAuditSummaryAsync(DateTime fromDate, DateTime toDate);
    Task PurgeOldAuditLogsAsync(DateTime cutoffDate);
}

public class AuditLogSummary
{
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public Dictionary<string, int> OperationsByType { get; set; } = new();
    public Dictionary<string, int> OperationsByUser { get; set; } = new();
    public Dictionary<string, int> OperationsByEntityType { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}