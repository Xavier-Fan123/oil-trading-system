namespace OilTrading.Application.Services;

/// <summary>
/// Enhanced context for transaction compensation operations
/// </summary>
public class CompensationContext
{
    public Guid TransactionId { get; set; }
    public string TransactionName { get; set; } = string.Empty;
    public DateTime CompensationStartTime { get; set; } = DateTime.UtcNow;
    public string CompensationReason { get; set; } = string.Empty;
    public string InitiatedBy { get; set; } = string.Empty;
    
    // Original transaction data for compensation reference
    public Dictionary<string, object> OriginalTransactionData { get; set; } = new();
    
    // Compensation-specific data
    public Dictionary<string, object> CompensationData { get; set; } = new();
    
    // Track compensation operations
    public List<CompensationStep> CompensationSteps { get; set; } = new();
    
    // Execution context
    public CompensationStrategy Strategy { get; set; } = CompensationStrategy.BestEffort;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    
    // State tracking
    public CompensationStatus Status { get; set; } = CompensationStatus.NotStarted;
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();

    public void AddStep(string operationName, CompensationStepStatus status, string? details = null, Exception? exception = null)
    {
        CompensationSteps.Add(new CompensationStep
        {
            OperationName = operationName,
            Status = status,
            Timestamp = DateTime.UtcNow,
            Details = details,
            Exception = exception?.ToString(),
            RetryCount = 0
        });
    }

    public void UpdateStepStatus(string operationName, CompensationStepStatus status, string? details = null)
    {
        var step = CompensationSteps.LastOrDefault(s => s.OperationName == operationName);
        if (step != null)
        {
            step.Status = status;
            step.Details = details;
            step.LastUpdated = DateTime.UtcNow;
        }
    }

    public void IncrementRetryCount(string operationName)
    {
        var step = CompensationSteps.LastOrDefault(s => s.OperationName == operationName);
        if (step != null)
        {
            step.RetryCount++;
            step.LastUpdated = DateTime.UtcNow;
        }
    }

    public bool ShouldRetry(string operationName)
    {
        var step = CompensationSteps.LastOrDefault(s => s.OperationName == operationName);
        return step?.RetryCount < MaxRetryAttempts;
    }

    public CompensationSummary GetSummary()
    {
        var totalSteps = CompensationSteps.Count;
        var successfulSteps = CompensationSteps.Count(s => s.Status == CompensationStepStatus.Completed);
        var failedSteps = CompensationSteps.Count(s => s.Status == CompensationStepStatus.Failed);
        var skippedSteps = CompensationSteps.Count(s => s.Status == CompensationStepStatus.Skipped);

        return new CompensationSummary
        {
            TransactionId = TransactionId,
            TotalSteps = totalSteps,
            SuccessfulSteps = successfulSteps,
            FailedSteps = failedSteps,
            SkippedSteps = skippedSteps,
            CompensationDuration = DateTime.UtcNow - CompensationStartTime,
            OverallStatus = Status,
            SuccessRate = totalSteps > 0 ? (double)successfulSteps / totalSteps * 100 : 0,
            Warnings = Warnings.ToList(),
            Errors = Errors.ToList()
        };
    }
}

public class CompensationStep
{
    public string OperationName { get; set; } = string.Empty;
    public CompensationStepStatus Status { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string? Details { get; set; }
    public string? Exception { get; set; }
    public int RetryCount { get; set; }
    public TimeSpan Duration => LastUpdated - Timestamp;
}

public class CompensationSummary
{
    public Guid TransactionId { get; set; }
    public int TotalSteps { get; set; }
    public int SuccessfulSteps { get; set; }
    public int FailedSteps { get; set; }
    public int SkippedSteps { get; set; }
    public TimeSpan CompensationDuration { get; set; }
    public CompensationStatus OverallStatus { get; set; }
    public double SuccessRate { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public enum CompensationStrategy
{
    /// <summary>
    /// Continue compensation even if some operations fail
    /// </summary>
    BestEffort = 1,
    
    /// <summary>
    /// Stop compensation on first failure
    /// </summary>
    FailFast = 2,
    
    /// <summary>
    /// All compensation operations must succeed
    /// </summary>
    AllOrNothing = 3,
    
    /// <summary>
    /// Use manual intervention for critical failures
    /// </summary>
    ManualIntervention = 4
}

public enum CompensationStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
    PartiallyCompleted = 4,
    Failed = 5,
    ManualInterventionRequired = 6
}

public enum CompensationStepStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Retrying = 5,
    Skipped = 6,
    ManualInterventionRequired = 7
}