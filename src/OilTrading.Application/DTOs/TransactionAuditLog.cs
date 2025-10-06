using OilTrading.Application.TransactionOperations;

namespace OilTrading.Application.DTOs;

// TransactionAuditLog for transaction-level auditing (different from operation-level)
public class TransactionAuditLog
{
    public Guid TransactionId { get; set; }
    public string TransactionName { get; set; } = string.Empty;
    public string InitiatedBy { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public TransactionStatus Status { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<TransactionStep> Steps { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}