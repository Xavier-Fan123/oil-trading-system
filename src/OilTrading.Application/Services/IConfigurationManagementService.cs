using System.ComponentModel.DataAnnotations;

namespace OilTrading.Application.Services;

/// <summary>
/// Configuration management service interface
/// </summary>
public interface IConfigurationManagementService
{
    // Configuration retrieval
    Task<T?> GetConfigurationAsync<T>(string key, string? environment = null) where T : class;
    Task<T> GetConfigurationAsync<T>(string key, T defaultValue, string? environment = null) where T : class;
    Task<string?> GetConfigurationValueAsync(string key, string? environment = null);
    Task<Dictionary<string, object>> GetConfigurationSectionAsync(string section, string? environment = null);
    
    // Configuration management
    Task<bool> SetConfigurationAsync<T>(string key, T value, string? environment = null) where T : class;
    Task<bool> SetConfigurationValueAsync(string key, string value, string? environment = null);
    Task<bool> DeleteConfigurationAsync(string key, string? environment = null);
    
    // Hot reload and versioning
    Task<bool> ReloadConfigurationAsync(string? environment = null);
    Task<ConfigurationVersion> CreateConfigurationVersionAsync(string environment, string comment);
    Task<List<ConfigurationVersion>> GetConfigurationVersionsAsync(string environment);
    Task<bool> RollbackToVersionAsync(string environment, Guid versionId);
    
    // Configuration validation
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(string environment);
    Task<ConfigurationValidationResult> ValidateConfigurationAsync<T>(string key, T value) where T : class;
    
    // Environment management
    Task<List<string>> GetEnvironmentsAsync();
    Task<bool> CreateEnvironmentAsync(string environment, string? templateEnvironment = null);
    Task<bool> DeleteEnvironmentAsync(string environment);
    
    // Configuration monitoring
    Task<ConfigurationHealth> GetConfigurationHealthAsync();
    Task<List<ConfigurationChange>> GetConfigurationChangesAsync(DateTime? since = null);
    
    // Export/Import
    Task<byte[]> ExportConfigurationAsync(string environment, ConfigurationExportFormat format);
    Task<ConfigurationImportResult> ImportConfigurationAsync(string environment, byte[] data, ConfigurationImportMode mode);
    
    // Events
    event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    event EventHandler<ConfigurationErrorEventArgs> ConfigurationError;
}

/// <summary>
/// Configuration item with metadata
/// </summary>
public class ConfigurationItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public ConfigurationType Type { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsReadOnly { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public string? LastModifiedBy { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration version for rollback support
/// </summary>
public class ConfigurationVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Environment { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public Dictionary<string, ConfigurationItem> Configuration { get; set; } = new();
    public string ConfigurationHash { get; set; } = string.Empty;
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public List<ConfigurationValidationError> Errors { get; set; } = new();
    public List<ConfigurationValidationWarning> Warnings { get; set; } = new();
    public Dictionary<string, object> ValidationMetadata { get; set; } = new();
}

/// <summary>
/// Configuration validation error
/// </summary>
public class ConfigurationValidationError
{
    public string Key { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public ConfigurationValidationType ValidationType { get; set; }
    public string? ExpectedValue { get; set; }
    public string? ActualValue { get; set; }
}

/// <summary>
/// Configuration validation warning
/// </summary>
public class ConfigurationValidationWarning
{
    public string Key { get; set; } = string.Empty;
    public string WarningMessage { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
}

/// <summary>
/// Configuration health status
/// </summary>
public class ConfigurationHealth
{
    public bool IsHealthy { get; set; }
    public DateTime LastCheck { get; set; } = DateTime.UtcNow;
    public List<ConfigurationHealthIssue> Issues { get; set; } = new();
    public Dictionary<string, ConfigurationEnvironmentHealth> EnvironmentHealth { get; set; } = new();
    public ConfigurationPerformanceMetrics Performance { get; set; } = new();
}

/// <summary>
/// Configuration health issue
/// </summary>
public class ConfigurationHealthIssue
{
    public string Environment { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public ConfigurationHealthIssueType IssueType { get; set; }
    public string Description { get; set; } = string.Empty;
    public ConfigurationHealthSeverity Severity { get; set; }
    public DateTime DetectedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Environment-specific health status
/// </summary>
public class ConfigurationEnvironmentHealth
{
    public string Environment { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime LastUpdate { get; set; }
    public int ConfigurationCount { get; set; }
    public List<string> MissingRequiredConfigurations { get; set; } = new();
    public List<string> InvalidConfigurations { get; set; } = new();
}

/// <summary>
/// Configuration performance metrics
/// </summary>
public class ConfigurationPerformanceMetrics
{
    public TimeSpan AverageRetrievalTime { get; set; }
    public int CacheHitRate { get; set; }
    public long TotalRequests { get; set; }
    public Dictionary<string, TimeSpan> SlowQueries { get; set; } = new();
}

/// <summary>
/// Configuration change tracking
/// </summary>
public class ConfigurationChange
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Environment { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public ConfigurationChangeType ChangeType { get; set; }
    public DateTime ChangeDate { get; set; } = DateTime.UtcNow;
    public string ChangedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? Source { get; set; }
}

/// <summary>
/// Configuration import result
/// </summary>
public class ConfigurationImportResult
{
    public bool IsSuccessful { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ConfigurationImportError> Errors { get; set; } = new();
    public List<string> ImportedKeys { get; set; } = new();
    public List<string> SkippedKeys { get; set; } = new();
}

/// <summary>
/// Configuration import error
/// </summary>
public class ConfigurationImportError
{
    public string Key { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? Value { get; set; }
}

/// <summary>
/// Configuration changed event arguments
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    public string Environment { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public ConfigurationChangeType ChangeType { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
}

/// <summary>
/// Configuration error event arguments
/// </summary>
public class ConfigurationErrorEventArgs : EventArgs
{
    public string Environment { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public Exception Exception { get; set; } = null!;
    public string ErrorMessage { get; set; } = string.Empty;
}

// Enums
public enum ConfigurationType
{
    String,
    Number,
    Boolean,
    Json,
    ConnectionString,
    Secret,
    Array,
    Object
}

public enum ConfigurationValidationType
{
    Required,
    Format,
    Range,
    Dependency,
    Security,
    Performance
}

public enum ConfigurationExportFormat
{
    Json,
    Yaml,
    Xml,
    Properties,
    Excel
}

public enum ConfigurationImportMode
{
    Override,
    Merge,
    Add
}

public enum ConfigurationChangeType
{
    Created,
    Updated,
    Deleted,
    Restored
}

public enum ConfigurationHealthIssueType
{
    MissingRequired,
    InvalidValue,
    ConnectionFailure,
    PerformanceIssue,
    SecurityIssue,
    VersionMismatch
}

public enum ConfigurationHealthSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Configuration validation attributes
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ConfigurationRequiredAttribute : ValidationAttribute
{
    public string? Environment { get; set; }
    
    public override bool IsValid(object? value)
    {
        return value != null && !string.IsNullOrWhiteSpace(value.ToString());
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class ConfigurationRangeAttribute : ValidationAttribute
{
    public double Minimum { get; }
    public double Maximum { get; }
    
    public ConfigurationRangeAttribute(double minimum, double maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }
    
    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        
        if (double.TryParse(value.ToString(), out var numericValue))
        {
            return numericValue >= Minimum && numericValue <= Maximum;
        }
        
        return false;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class ConfigurationFormatAttribute : ValidationAttribute
{
    public string Pattern { get; }
    
    public ConfigurationFormatAttribute(string pattern)
    {
        Pattern = pattern;
    }
    
    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        
        return System.Text.RegularExpressions.Regex.IsMatch(value.ToString() ?? "", Pattern);
    }
}

/// <summary>
/// Configuration model base class with validation
/// </summary>
public abstract class ConfigurationModelBase
{
    public virtual ConfigurationValidationResult Validate()
    {
        var result = new ConfigurationValidationResult { IsValid = true };
        var validationContext = new ValidationContext(this);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            result.IsValid = false;
            foreach (var validationResult in validationResults)
            {
                result.Errors.Add(new ConfigurationValidationError
                {
                    Key = string.Join(",", validationResult.MemberNames),
                    ErrorMessage = validationResult.ErrorMessage ?? "Validation failed",
                    ValidationType = ConfigurationValidationType.Format
                });
            }
        }
        
        return result;
    }
}

/// <summary>
/// Common configuration models
/// </summary>
public class DatabaseConfiguration : ConfigurationModelBase
{
    [ConfigurationRequired]
    public string ConnectionString { get; set; } = string.Empty;
    
    [ConfigurationRange(1, 100)]
    public int MaxConnections { get; set; } = 20;
    
    [ConfigurationRange(1, 300)]
    public int CommandTimeout { get; set; } = 30;
    
    public bool EnableRetry { get; set; } = true;
    
    [ConfigurationRange(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;
}

public class CacheConfiguration : ConfigurationModelBase
{
    [ConfigurationRequired]
    public string RedisConnectionString { get; set; } = string.Empty;
    
    [ConfigurationRange(1, 3600)]
    public int DefaultExpirationMinutes { get; set; } = 30;
    
    public bool EnableCompression { get; set; } = true;
    
    [ConfigurationRange(1, 100)]
    public int MaxConcurrentConnections { get; set; } = 10;
}

public class SecurityConfiguration : ConfigurationModelBase
{
    [ConfigurationRequired]
    public string JwtSecretKey { get; set; } = string.Empty;
    
    [ConfigurationRange(1, 1440)]
    public int JwtExpirationMinutes { get; set; } = 60;
    
    public bool RequireHttps { get; set; } = true;
    
    [ConfigurationRange(1, 60)]
    public int PasswordExpirationDays { get; set; } = 90;
    
    [ConfigurationRange(3, 20)]
    public int MinPasswordLength { get; set; } = 8;
}

public class ApiConfiguration : ConfigurationModelBase
{
    [ConfigurationRequired]
    [ConfigurationFormat(@"^https?://.*")]
    public string BaseUrl { get; set; } = string.Empty;
    
    [ConfigurationRange(1, 10000)]
    public int RequestTimeoutSeconds { get; set; } = 30;
    
    [ConfigurationRange(1, 1000)]
    public int MaxConcurrentRequests { get; set; } = 100;
    
    public bool EnableRequestLogging { get; set; } = true;
    
    [ConfigurationRange(1, 100)]
    public int RateLimitPerMinute { get; set; } = 60;
}