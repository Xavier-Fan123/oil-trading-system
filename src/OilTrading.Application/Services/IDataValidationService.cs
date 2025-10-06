using System.Linq.Expressions;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Services;

/// <summary>
/// Data validation service interface
/// </summary>
public interface IDataValidationService
{
    // Rule management
    Task<Guid> CreateValidationRuleAsync(ValidationRuleDefinition definition);
    Task<bool> UpdateValidationRuleAsync(Guid ruleId, ValidationRuleDefinition definition);
    Task<bool> DeleteValidationRuleAsync(Guid ruleId);
    Task<List<ValidationRule>> GetValidationRulesAsync(string? entityType = null);
    Task<ValidationRule?> GetValidationRuleAsync(Guid ruleId);
    
    // Validation execution
    Task<ValidationResult> ValidateEntityAsync<T>(T entity) where T : class;
    Task<ValidationResult> ValidateEntityAsync<T>(T entity, string? ruleGroup) where T : class;
    Task<List<ValidationResult>> ValidateBatchAsync<T>(IEnumerable<T> entities) where T : class;
    Task<ValidationResult> ValidateFieldAsync<T>(T entity, string fieldName, object value) where T : class;
    
    // Data quality assessment
    Task<DataQualityReport> AssessDataQualityAsync<T>(IEnumerable<T> entities) where T : class;
    Task<DataQualityReport> AssessDataQualityAsync(string entityType, DateTime? since = null);
    Task<DataQualityTrend> GetDataQualityTrendAsync(string entityType, TimeSpan period);
    
    // Data repair and cleansing
    Task<DataRepairResult> RepairDataAsync<T>(T entity, DataRepairOptions? options = null) where T : class;
    Task<List<DataRepairResult>> RepairBatchAsync<T>(IEnumerable<T> entities, DataRepairOptions? options = null) where T : class;
    Task<DataCleansingReport> PerformDataCleansingAsync(string entityType, DataCleansingOptions options);
    
    // Anomaly detection
    Task<List<DataAnomaly>> DetectAnomaliesAsync<T>(IEnumerable<T> entities) where T : class;
    Task<DataAnomalyReport> GetAnomalyReportAsync(string entityType, DateTime? since = null);
    Task<bool> MarkAnomalyAsReviewedAsync(Guid anomalyId, string reviewedBy, string? comments = null);
    
    // Configuration and monitoring
    Task<bool> EnableValidationRuleAsync(Guid ruleId);
    Task<bool> DisableValidationRuleAsync(Guid ruleId);
    Task<ValidationStatistics> GetValidationStatisticsAsync(TimeSpan? period = null);
    Task<List<ValidationPerformanceMetric>> GetValidationPerformanceAsync();
}

/// <summary>
/// Validation rule definition
/// </summary>
public class ValidationRuleDefinition
{
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public ValidationRuleType RuleType { get; set; }
    public ValidationSeverity Severity { get; set; }
    public string RuleExpression { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? RuleGroup { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 100;
    public string? ErrorMessage { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Validation rule with metadata
/// </summary>
public class ValidationRule : ValidationRuleDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string? LastModifiedBy { get; set; }
    public ValidationRuleStatistics Statistics { get; set; } = new();
    public int Version { get; set; } = 1;
}

// Note: ValidationResult, ValidationError, and ValidationWarning are defined in ValidationDTOs.cs

/// <summary>
/// Validation rule statistics
/// </summary>
public class ValidationRuleStatistics
{
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public DateTime LastExecuted { get; set; }
    public double SuccessRate => ExecutionCount > 0 ? (double)SuccessCount / ExecutionCount * 100 : 0;
}


/// <summary>
/// Data quality report
/// </summary>
public class DataQualityReport
{
    public string EntityType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public double QualityScore { get; set; }
    public Dictionary<string, DataQualityMetric> FieldMetrics { get; set; } = new();
    public List<DataQualityIssue> QualityIssues { get; set; } = new();
    public DataQualityDimensions Dimensions { get; set; } = new();
}

/// <summary>
/// Data quality metric for a field
/// </summary>
public class DataQualityMetric
{
    public string FieldName { get; set; } = string.Empty;
    public int TotalValues { get; set; }
    public int ValidValues { get; set; }
    public int NullValues { get; set; }
    public int EmptyValues { get; set; }
    public int DuplicateValues { get; set; }
    public double Completeness { get; set; }
    public double Validity { get; set; }
    public double Uniqueness { get; set; }
    public Dictionary<string, int> ValueDistribution { get; set; } = new();
}

/// <summary>
/// Data quality dimensions assessment
/// </summary>
public class DataQualityDimensions
{
    public double Completeness { get; set; }  // Percentage of non-null values
    public double Validity { get; set; }      // Percentage of values that pass validation
    public double Uniqueness { get; set; }   // Percentage of unique values where uniqueness is expected
    public double Consistency { get; set; }  // Consistency across related fields/entities
    public double Accuracy { get; set; }     // Accuracy based on reference data
    public double Timeliness { get; set; }   // Freshness of the data
}

/// <summary>
/// Data quality issue
/// </summary>
public class DataQualityIssue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public object? EntityId { get; set; }
    public ValidationSeverity Severity { get; set; }
    public int AffectedRecords { get; set; }
    public string? SuggestedFix { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Data quality trend
/// </summary>
public class DataQualityTrend
{
    public string EntityType { get; set; } = string.Empty;
    public TimeSpan Period { get; set; }
    public List<DataQualitySnapshot> Snapshots { get; set; } = new();
    public DataQualityTrendAnalysis Analysis { get; set; } = new();
}

/// <summary>
/// Data quality snapshot at a point in time
/// </summary>
public class DataQualitySnapshot
{
    public DateTime Timestamp { get; set; }
    public double QualityScore { get; set; }
    public int TotalRecords { get; set; }
    public int IssueCount { get; set; }
    public Dictionary<string, double> DimensionScores { get; set; } = new();
}

/// <summary>
/// Data quality trend analysis
/// </summary>
public class DataQualityTrendAnalysis
{
    public DataQualityTrendDirection Direction { get; set; }
    public double TrendSlope { get; set; }
    public double VariationCoefficient { get; set; }
    public List<string> KeyInsights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Data repair result
/// </summary>
public class DataRepairResult
{
    public bool IsRepaired { get; set; }
    public object? OriginalEntity { get; set; }
    public object? RepairedEntity { get; set; }
    public List<DataRepairAction> RepairActions { get; set; } = new();
    public List<string> FieldsRepaired { get; set; } = new();
    public List<string> UnrepairableIssues { get; set; } = new();
}

/// <summary>
/// Data repair action
/// </summary>
public class DataRepairAction
{
    public string FieldName { get; set; } = string.Empty;
    public DataRepairActionType ActionType { get; set; }
    public object? OriginalValue { get; set; }
    public object? RepairedValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

/// <summary>
/// Data repair options
/// </summary>
public class DataRepairOptions
{
    public bool AutoRepair { get; set; } = true;
    public double MinConfidenceThreshold { get; set; } = 0.8;
    public List<string> ExcludeFields { get; set; } = new();
    public Dictionary<string, object> RepairStrategies { get; set; } = new();
    public bool CreateBackup { get; set; } = true;
}

/// <summary>
/// Data cleansing options
/// </summary>
public class DataCleansingOptions
{
    public bool RemoveDuplicates { get; set; } = true;
    public bool StandardizeFormats { get; set; } = true;
    public bool FillMissingValues { get; set; } = true;
    public bool CorrectTypos { get; set; } = true;
    public bool ValidateConstraints { get; set; } = true;
    public DateTime? CutoffDate { get; set; }
    public List<string> TargetFields { get; set; } = new();
}

/// <summary>
/// Data cleansing report
/// </summary>
public class DataCleansingReport
{
    public string EntityType { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int TotalRecords { get; set; }
    public int RecordsProcessed { get; set; }
    public int RecordsCleansed { get; set; }
    public int RecordsRemoved { get; set; }
    public List<DataCleansingAction> Actions { get; set; } = new();
    public Dictionary<string, int> IssuesFixed { get; set; } = new();
}

/// <summary>
/// Data cleansing action
/// </summary>
public class DataCleansingAction
{
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AffectedRecords { get; set; }
    public List<string> Details { get; set; } = new();
}

/// <summary>
/// Data anomaly
/// </summary>
public class DataAnomaly
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public object? EntityId { get; set; }
    public DataAnomalyType AnomalyType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public object? AnomalousValue { get; set; }
    public object? ExpectedValue { get; set; }
    public double AnomalyScore { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DataAnomalyStatus Status { get; set; } = DataAnomalyStatus.Detected;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComments { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Data anomaly report
/// </summary>
public class DataAnomalyReport
{
    public string EntityType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TotalAnomalies { get; set; }
    public int NewAnomalies { get; set; }
    public int ResolvedAnomalies { get; set; }
    public Dictionary<DataAnomalyType, int> AnomaliesByType { get; set; } = new();
    public Dictionary<string, int> AnomaliesByField { get; set; } = new();
    public List<DataAnomaly> HighSeverityAnomalies { get; set; } = new();
}

/// <summary>
/// Validation statistics
/// </summary>
public class ValidationStatistics
{
    public TimeSpan Period { get; set; }
    public int TotalValidations { get; set; }
    public int SuccessfulValidations { get; set; }
    public int FailedValidations { get; set; }
    public TimeSpan AverageValidationTime { get; set; }
    public Dictionary<string, int> ValidationsByEntityType { get; set; } = new();
    public Dictionary<ValidationSeverity, int> ErrorsBySeverity { get; set; } = new();
    public List<ValidationRulePerformance> TopFailingRules { get; set; } = new();
}

/// <summary>
/// Validation rule performance
/// </summary>
public class ValidationRulePerformance
{
    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public int FailureCount { get; set; }
    public double FailureRate { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
}

/// <summary>
/// Validation performance metric
/// </summary>
public class ValidationPerformanceMetric
{
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();
}

// Enums
public enum ValidationRuleType
{
    Required,
    Format,
    Range,
    Length,
    Custom,
    BusinessRule,
    Reference,
    Uniqueness,
    Consistency,
    Completeness
}


public enum DataQualityTrendDirection
{
    Improving,
    Stable,
    Declining,
    Volatile
}

public enum DataRepairActionType
{
    Fill,
    Correct,
    Standardize,
    Remove,
    Replace,
    Transform
}

public enum DataAnomalyType
{
    Outlier,
    Duplicate,
    Missing,
    Inconsistent,
    Invalid,
    Unexpected,
    Temporal,
    Pattern
}

public enum DataAnomalyStatus
{
    Detected,
    UnderReview,
    Confirmed,
    FalsePositive,
    Resolved,
    Ignored
}

/// <summary>
/// Business rule expression builder
/// </summary>
public class BusinessRuleBuilder<T> where T : class
{
    private readonly List<Expression<Func<T, bool>>> _conditions = new();
    private readonly List<string> _errorMessages = new();

    public BusinessRuleBuilder<T> When(Expression<Func<T, bool>> condition)
    {
        _conditions.Add(condition);
        return this;
    }

    public BusinessRuleBuilder<T> WithMessage(string errorMessage)
    {
        _errorMessages.Add(errorMessage);
        return this;
    }

    public ValidationRuleDefinition Build(string ruleName, string description)
    {
        // Convert expressions to rule definition
        // This is a simplified implementation
        return new ValidationRuleDefinition
        {
            RuleName = ruleName,
            Description = description,
            EntityType = typeof(T).Name,
            RuleType = ValidationRuleType.BusinessRule,
            RuleExpression = string.Join(" AND ", _conditions.Select(c => c.ToString())),
            ErrorMessage = string.Join("; ", _errorMessages)
        };
    }
}

/// <summary>
/// Common validation rules
/// </summary>
public static class CommonValidationRules
{
    public static ValidationRuleDefinition Required(string fieldName, string? errorMessage = null)
    {
        return new ValidationRuleDefinition
        {
            RuleName = $"{fieldName}_Required",
            Description = $"{fieldName} is required",
            FieldName = fieldName,
            RuleType = ValidationRuleType.Required,
            Severity = ValidationSeverity.Error,
            RuleExpression = $"{fieldName} != null && {fieldName} != ''",
            ErrorMessage = errorMessage ?? $"{fieldName} is required"
        };
    }

    public static ValidationRuleDefinition Range(string fieldName, double min, double max, string? errorMessage = null)
    {
        return new ValidationRuleDefinition
        {
            RuleName = $"{fieldName}_Range",
            Description = $"{fieldName} must be between {min} and {max}",
            FieldName = fieldName,
            RuleType = ValidationRuleType.Range,
            Severity = ValidationSeverity.Error,
            RuleExpression = $"{fieldName} >= {min} && {fieldName} <= {max}",
            Parameters = new Dictionary<string, object> { ["min"] = min, ["max"] = max },
            ErrorMessage = errorMessage ?? $"{fieldName} must be between {min} and {max}"
        };
    }

    public static ValidationRuleDefinition Email(string fieldName, string? errorMessage = null)
    {
        return new ValidationRuleDefinition
        {
            RuleName = $"{fieldName}_Email",
            Description = $"{fieldName} must be a valid email address",
            FieldName = fieldName,
            RuleType = ValidationRuleType.Format,
            Severity = ValidationSeverity.Error,
            RuleExpression = @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            ErrorMessage = errorMessage ?? $"{fieldName} must be a valid email address"
        };
    }

    public static ValidationRuleDefinition Unique(string fieldName, string? errorMessage = null)
    {
        return new ValidationRuleDefinition
        {
            RuleName = $"{fieldName}_Unique",
            Description = $"{fieldName} must be unique",
            FieldName = fieldName,
            RuleType = ValidationRuleType.Uniqueness,
            Severity = ValidationSeverity.Error,
            ErrorMessage = errorMessage ?? $"{fieldName} must be unique"
        };
    }
}