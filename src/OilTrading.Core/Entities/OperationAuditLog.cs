using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

public class OperationAuditLog : BaseEntity
{
    private OperationAuditLog() { } // For EF Core

    public OperationAuditLog(
        Guid operationId,
        Guid? transactionId,
        string operationName,
        string operationType,
        DateTime timestamp,
        bool isSuccess,
        Dictionary<string, object> data,
        string? errorMessage = null,
        string initiatedBy = "System")
    {
        if (string.IsNullOrWhiteSpace(operationName))
            throw new DomainException("Operation name cannot be empty");

        if (string.IsNullOrWhiteSpace(operationType))
            throw new DomainException("Operation type cannot be empty");

        OperationId = operationId;
        TransactionId = transactionId;
        OperationName = operationName.Trim();
        OperationType = operationType.Trim();
        Timestamp = timestamp;
        IsSuccess = isSuccess;
        Data = data ?? new Dictionary<string, object>();
        ErrorMessage = errorMessage?.Trim();
        InitiatedBy = initiatedBy;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid OperationId { get; private set; }
    public Guid? TransactionId { get; private set; }
    public string OperationName { get; private set; } = string.Empty;
    public string OperationType { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public bool IsSuccess { get; private set; }
    public Dictionary<string, object> Data { get; private set; } = new();
    public string? ErrorMessage { get; private set; }
    public string InitiatedBy { get; private set; } = string.Empty;
    public new DateTime CreatedAt { get; private set; }

    // Business Methods
    public void AddAdditionalData(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Data key cannot be empty");

        Data[key] = value;
        SetUpdatedBy("System");
    }

    public T? GetDataValue<T>(string key)
    {
        if (Data.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;
            
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }
        
        return default(T);
    }

    public bool HasDataKey(string key)
    {
        return Data.ContainsKey(key);
    }

    public void MarkAsFailure(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new DomainException("Error message cannot be empty");

        IsSuccess = false;
        ErrorMessage = errorMessage.Trim();
        SetUpdatedBy("System");
    }

    public TimeSpan GetTimeSinceOperation()
    {
        return DateTime.UtcNow - Timestamp;
    }

    public string GetOperationSummary()
    {
        var status = IsSuccess ? "SUCCESS" : "FAILED";
        var entityType = GetDataValue<string>("EntityType") ?? "Unknown";
        var entityId = GetDataValue<string>("EntityId") ?? "Unknown";
        
        return $"{OperationType} on {entityType} ({entityId}) - {status}";
    }

    public bool IsRelatedToEntity(string entityType, string entityId)
    {
        var logEntityType = GetDataValue<string>("EntityType");
        var logEntityId = GetDataValue<string>("EntityId");
        
        return string.Equals(logEntityType, entityType, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(logEntityId, entityId, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsOlderThan(TimeSpan timeSpan)
    {
        return DateTime.UtcNow - CreatedAt > timeSpan;
    }

    public bool IsOlderThan(DateTime cutoffDate)
    {
        return CreatedAt < cutoffDate;
    }

    public Dictionary<string, object> GetSanitizedData()
    {
        var sanitizedData = new Dictionary<string, object>();
        
        foreach (var kvp in Data)
        {
            // Remove sensitive information
            if (IsSensitiveKey(kvp.Key))
            {
                sanitizedData[kvp.Key] = "***REDACTED***";
            }
            else
            {
                sanitizedData[kvp.Key] = kvp.Value;
            }
        }
        
        return sanitizedData;
    }

    private bool IsSensitiveKey(string key)
    {
        var sensitiveKeys = new[] 
        {
            "password", "secret", "token", "key", "credential",
            "bankaccount", "swift", "iban", "accountnumber"
        };
        
        return sensitiveKeys.Any(sk => key.ToLower().Contains(sk));
    }
}