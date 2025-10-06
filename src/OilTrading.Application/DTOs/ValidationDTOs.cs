namespace OilTrading.Application.DTOs;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> MetaData { get; set; } = new();
    
    // Additional properties required by the validation system
    public string EntityType { get; set; } = string.Empty;
    public object? EntityId { get; set; }
    public int RulesExecuted { get; set; }
    public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan ValidationDuration { get; set; }
    
    public void AddError(string fieldName, string errorMessage, object? attemptedValue = null)
    {
        Errors.Add(new ValidationError 
        { 
            FieldName = fieldName, 
            ErrorMessage = errorMessage, 
            AttemptedValue = attemptedValue 
        });
        IsValid = false;
    }
    
    public void AddWarning(string fieldName, string warningMessage, object? attemptedValue = null)
    {
        Warnings.Add(new ValidationWarning 
        { 
            FieldName = fieldName, 
            WarningMessage = warningMessage, 
            AttemptedValue = attemptedValue 
        });
    }
}

public class ValidationError
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }
    public object? CurrentValue { get; set; }
    public object? ActualValue { get; set; }
    public object? ExpectedValue { get; set; }
    public string? ErrorCode { get; set; }
    public ValidationSeverity Severity { get; set; }
    public Dictionary<string, object> MetaData { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
}

public class ValidationWarning
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string WarningMessage { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }
    public object? CurrentValue { get; set; }
    public object? Value { get; set; }
    public string? WarningCode { get; set; }
    public Dictionary<string, object> MetaData { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
}

public enum ValidationSeverity
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}