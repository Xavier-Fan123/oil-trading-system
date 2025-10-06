using System.Transactions;

namespace OilTrading.Application.TransactionOperations;

public interface ITransactionOperation
{
    string OperationName { get; }
    int Order { get; set; }
    bool RequiresCompensation { get; }
    
    Task<OperationResult> ExecuteAsync(TransactionContext context);
    Task<OperationResult> CompensateAsync(TransactionContext context);
}

public class OperationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public class TransactionContext
{
    public Guid TransactionId { get; set; } = Guid.NewGuid();
    public string TransactionName { get; set; } = string.Empty;
    public string InitiatedBy { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public Dictionary<string, object> Data { get; set; } = new();
    public List<TransactionStep> Steps { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? FailureReason { get; set; }
    
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
    
    public void AddStep(TransactionStep step)
    {
        Steps.Add(step);
    }
    
    public void SetData(string key, object value)
    {
        Data[key] = value;
    }
    
    public T? GetData<T>(string key)
    {
        return Data.TryGetValue(key, out var value) ? (T)value : default;
    }

    public void AddStep(string operationName, TransactionStepStatus status, string? errorMessage = null)
    {
        Steps.Add(new TransactionStep
        {
            OperationName = operationName,
            Status = status,
            Timestamp = DateTime.UtcNow,
            ErrorMessage = errorMessage
        });
    }
}

public class TransactionResult
{
    public Guid TransactionId { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public TransactionStatus Status { get; set; }
    public List<OperationResult> OperationResults { get; set; } = new();
}

public enum TransactionStatus
{
    NotStarted = 0,
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
    Compensating = 6,
    Compensated = 7,
    CompensationFailed = 8
}

public class TransactionStep
{
    public string OperationName { get; set; } = string.Empty;
    public TransactionStepStatus Status { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public enum TransactionStepStatus
{
    Started = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Compensated = 5
}

public class CompensationContext
{
    public Guid TransactionId { get; set; }
    public string TransactionName { get; set; } = string.Empty;
    public string CompensationReason { get; set; } = string.Empty;
    public string InitiatedBy { get; set; } = string.Empty;
    public CompensationStrategy Strategy { get; set; } = CompensationStrategy.BestEffort;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Data { get; set; } = new();
    public Dictionary<string, object> OriginalTransactionData { get; set; } = new();
    public List<CompensationStep> CompensationSteps { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public CompensationStatus Status { get; set; }

    public void AddStep(string operationName, CompensationStepStatus status)
    {
        CompensationSteps.Add(new CompensationStep
        {
            OperationName = operationName,
            Status = status,
            Timestamp = DateTime.UtcNow
        });
    }

    public void UpdateStepStatus(string operationName, CompensationStepStatus status, string? message = null)
    {
        var step = CompensationSteps.FirstOrDefault(s => s.OperationName == operationName);
        if (step != null)
        {
            step.Status = status;
            step.Message = message;
            step.Timestamp = DateTime.UtcNow;
        }
    }

    public void IncrementRetryCount(string operationName)
    {
        var step = CompensationSteps.FirstOrDefault(s => s.OperationName == operationName);
        if (step != null)
        {
            step.RetryCount++;
        }
    }

    public CompensationSummary GetSummary()
    {
        var totalSteps = CompensationSteps.Count;
        var completedSteps = CompensationSteps.Count(s => s.Status == CompensationStepStatus.Completed);
        var successRate = totalSteps > 0 ? (double)completedSteps / totalSteps * 100 : 0;

        return new CompensationSummary
        {
            OverallStatus = Status,
            TotalSteps = totalSteps,
            CompletedSteps = completedSteps,
            FailedSteps = CompensationSteps.Count(s => s.Status == CompensationStepStatus.Failed),
            SuccessRate = successRate,
            CompensationDuration = DateTime.UtcNow - StartTime,
            Errors = Errors.ToList(),
            Warnings = Warnings.ToList()
        };
    }
}

public class CompensationStep
{
    public string OperationName { get; set; } = string.Empty;
    public CompensationStepStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Message { get; set; }
    public int RetryCount { get; set; }
}

public class CompensationSummary
{
    public CompensationStatus OverallStatus { get; set; }
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public int FailedSteps { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan CompensationDuration { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public enum CompensationStrategy
{
    BestEffort = 1,
    FailFast = 2,
    AllOrNothing = 3,
    ManualIntervention = 4
}

public enum CompensationStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    PartiallyCompleted = 3,
    Failed = 4
}

public enum CompensationStepStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Retrying = 4
}