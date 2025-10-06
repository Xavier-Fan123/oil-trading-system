using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OilTrading.Infrastructure.Data;

/// <summary>
/// Validates database configuration and settings for optimal performance
/// </summary>
public class DatabaseConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseConfigurationValidator> _logger;

    public DatabaseConfigurationValidator(
        IConfiguration configuration,
        ILogger<DatabaseConfigurationValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Validates the database configuration for production readiness
    /// </summary>
    public async Task<DatabaseConfigValidationResult> ValidateConfigurationAsync()
    {
        var result = new DatabaseConfigValidationResult();

        try
        {
            _logger.LogInformation("Starting database configuration validation");

            // Validate connection strings
            result.ConnectionStringValid = ValidateConnectionStrings();
            
            // Validate Entity Framework settings
            result.EfConfigurationValid = ValidateEntityFrameworkConfiguration();
            
            // Validate performance settings
            result.PerformanceConfigValid = ValidatePerformanceConfiguration();
            
            // Validate security settings
            result.SecurityConfigValid = ValidateSecurityConfiguration();
            
            // Validate backup settings
            result.BackupConfigValid = ValidateBackupConfiguration();

            result.IsValid = result.ConnectionStringValid && result.EfConfigurationValid && 
                           result.PerformanceConfigValid && result.SecurityConfigValid &&
                           result.BackupConfigValid;

            _logger.LogInformation("Database configuration validation completed. Valid: {IsValid}", result.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database configuration validation");
            result.Errors.Add($"Configuration validation error: {ex.Message}");
        }

        return result;
    }

    private bool ValidateConnectionStrings()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("DefaultConnection string is missing");
                return false;
            }

            // Parse connection string to validate components
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            
            if (string.IsNullOrEmpty(builder.DataSource))
            {
                _logger.LogError("Database server is not specified in connection string");
                return false;
            }

            if (string.IsNullOrEmpty(builder.InitialCatalog))
            {
                _logger.LogError("Database name is not specified in connection string");
                return false;
            }

            // Check for production security requirements
            if (!builder.IntegratedSecurity && (string.IsNullOrEmpty(builder.UserID) || string.IsNullOrEmpty(builder.Password)))
            {
                _logger.LogWarning("Database authentication credentials may be missing");
            }

            // Check for encrypted connections in production
            if (!builder.Encrypt)
            {
                _logger.LogWarning("Database connection is not encrypted - consider enabling for production");
            }

            // Validate timeout settings
            if (builder.CommandTimeout < 30)
            {
                _logger.LogWarning("Command timeout is very low - may cause issues with complex operations");
            }

            _logger.LogInformation("Connection string validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating connection string");
            return false;
        }
    }

    private bool ValidateEntityFrameworkConfiguration()
    {
        try
        {
            // Check Entity Framework specific settings
            var efSection = _configuration.GetSection("EntityFramework");
            
            // Validate command timeout
            var commandTimeout = efSection.GetValue<int?>("CommandTimeout");
            if (commandTimeout.HasValue && commandTimeout.Value < 30)
            {
                _logger.LogWarning("EF Command timeout is set to {Timeout}s - may be too low for complex operations", commandTimeout.Value);
            }

            // Validate query tracking behavior
            var trackingBehavior = efSection.GetValue<string>("QueryTrackingBehavior");
            if (!string.IsNullOrEmpty(trackingBehavior) && trackingBehavior != "TrackAll")
            {
                _logger.LogInformation("Query tracking behavior is set to {Behavior}", trackingBehavior);
            }

            // Validate migration settings
            var autoMigrate = efSection.GetValue<bool>("AutoMigrate");
            if (autoMigrate)
            {
                _logger.LogWarning("Auto migration is enabled - ensure this is intended for production");
            }

            _logger.LogInformation("Entity Framework configuration validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Entity Framework configuration");
            return false;
        }
    }

    private bool ValidatePerformanceConfiguration()
    {
        try
        {
            var perfSection = _configuration.GetSection("Database:Performance");
            
            // Validate connection pool settings
            var maxPoolSize = perfSection.GetValue<int?>("MaxPoolSize");
            if (maxPoolSize.HasValue)
            {
                if (maxPoolSize.Value < 10)
                {
                    _logger.LogWarning("Connection pool size is very small: {PoolSize}", maxPoolSize.Value);
                }
                else if (maxPoolSize.Value > 1000)
                {
                    _logger.LogWarning("Connection pool size is very large: {PoolSize}", maxPoolSize.Value);
                }
            }

            // Validate query cache settings
            var queryCacheSize = perfSection.GetValue<int?>("QueryCacheSize");
            if (queryCacheSize.HasValue && queryCacheSize.Value < 100)
            {
                _logger.LogWarning("Query cache size may be too small for optimal performance");
            }

            // Validate batch size settings
            var batchSize = perfSection.GetValue<int?>("BatchSize");
            if (batchSize.HasValue)
            {
                if (batchSize.Value < 10)
                {
                    _logger.LogWarning("Batch size is very small and may impact performance");
                }
                else if (batchSize.Value > 10000)
                {
                    _logger.LogWarning("Batch size is very large and may cause memory issues");
                }
            }

            _logger.LogInformation("Performance configuration validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating performance configuration");
            return false;
        }
    }

    private bool ValidateSecurityConfiguration()
    {
        try
        {
            var securitySection = _configuration.GetSection("Database:Security");
            
            // Check encryption settings
            var encryptionEnabled = securitySection.GetValue<bool>("EncryptionEnabled");
            if (!encryptionEnabled)
            {
                _logger.LogWarning("Database encryption is not enabled - consider enabling for production");
            }

            // Check audit settings
            var auditEnabled = securitySection.GetValue<bool>("AuditEnabled");
            if (!auditEnabled)
            {
                _logger.LogWarning("Database auditing is not enabled - may be required for compliance");
            }

            // Check access control
            var accessControlEnabled = securitySection.GetValue<bool>("AccessControlEnabled");
            if (!accessControlEnabled)
            {
                _logger.LogWarning("Database access control is not enabled");
            }

            // Check for sensitive data protection
            var sensitiveDataLogging = securitySection.GetValue<bool>("EnableSensitiveDataLogging");
            if (sensitiveDataLogging)
            {
                _logger.LogWarning("Sensitive data logging is enabled - should be disabled in production");
            }

            _logger.LogInformation("Security configuration validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating security configuration");
            return false;
        }
    }

    private bool ValidateBackupConfiguration()
    {
        try
        {
            var backupSection = _configuration.GetSection("Database:Backup");
            
            // Check backup strategy
            var backupEnabled = backupSection.GetValue<bool>("Enabled");
            if (!backupEnabled)
            {
                _logger.LogWarning("Database backup is not enabled - highly recommended for production");
            }

            // Check backup frequency
            var backupFrequency = backupSection.GetValue<string>("Frequency");
            if (string.IsNullOrEmpty(backupFrequency))
            {
                _logger.LogWarning("Backup frequency is not configured");
            }

            // Check backup retention
            var retentionDays = backupSection.GetValue<int?>("RetentionDays");
            if (retentionDays.HasValue && retentionDays.Value < 7)
            {
                _logger.LogWarning("Backup retention period is very short: {Days} days", retentionDays.Value);
            }

            // Check backup location
            var backupPath = backupSection.GetValue<string>("BackupPath");
            if (string.IsNullOrEmpty(backupPath))
            {
                _logger.LogWarning("Backup path is not configured");
            }

            _logger.LogInformation("Backup configuration validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating backup configuration");
            return false;
        }
    }

    /// <summary>
    /// Generates recommended configuration for optimal performance
    /// </summary>
    public DatabaseConfigRecommendations GenerateRecommendations()
    {
        var recommendations = new DatabaseConfigRecommendations();

        try
        {
            // Connection string recommendations
            recommendations.ConnectionStringRecommendations.Add("Enable connection encryption with Encrypt=true");
            recommendations.ConnectionStringRecommendations.Add("Set command timeout to at least 60 seconds for complex operations");
            recommendations.ConnectionStringRecommendations.Add("Use connection pooling with appropriate pool size");

            // Performance recommendations
            recommendations.PerformanceRecommendations.Add("Set max pool size between 50-200 based on expected load");
            recommendations.PerformanceRecommendations.Add("Enable query result caching for frequently accessed data");
            recommendations.PerformanceRecommendations.Add("Configure batch size between 100-1000 for bulk operations");
            recommendations.PerformanceRecommendations.Add("Enable compiled query caching");

            // Security recommendations
            recommendations.SecurityRecommendations.Add("Enable database encryption at rest");
            recommendations.SecurityRecommendations.Add("Configure database auditing for compliance");
            recommendations.SecurityRecommendations.Add("Implement row-level security for sensitive data");
            recommendations.SecurityRecommendations.Add("Disable sensitive data logging in production");

            // Backup recommendations
            recommendations.BackupRecommendations.Add("Configure automated daily full backups");
            recommendations.BackupRecommendations.Add("Set backup retention to at least 30 days");
            recommendations.BackupRecommendations.Add("Store backups in geographically separate location");
            recommendations.BackupRecommendations.Add("Test backup restoration procedures regularly");

            // Monitoring recommendations
            recommendations.MonitoringRecommendations.Add("Enable query performance monitoring");
            recommendations.MonitoringRecommendations.Add("Set up alerts for long-running queries");
            recommendations.MonitoringRecommendations.Add("Monitor database size and growth trends");
            recommendations.MonitoringRecommendations.Add("Track connection pool utilization");

            _logger.LogInformation("Database configuration recommendations generated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating configuration recommendations");
        }

        return recommendations;
    }
}

/// <summary>
/// Result of database configuration validation
/// </summary>
public class DatabaseConfigValidationResult
{
    public bool IsValid { get; set; }
    public bool ConnectionStringValid { get; set; }
    public bool EfConfigurationValid { get; set; }
    public bool PerformanceConfigValid { get; set; }
    public bool SecurityConfigValid { get; set; }
    public bool BackupConfigValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public string GetSummary()
    {
        var status = IsValid ? "VALID" : "INVALID";
        var errorCount = Errors.Count;
        var warningCount = Warnings.Count;
        
        return $"Database Configuration: {status} | Errors: {errorCount} | Warnings: {warningCount}";
    }
}

/// <summary>
/// Database configuration recommendations
/// </summary>
public class DatabaseConfigRecommendations
{
    public List<string> ConnectionStringRecommendations { get; set; } = new();
    public List<string> PerformanceRecommendations { get; set; } = new();
    public List<string> SecurityRecommendations { get; set; } = new();
    public List<string> BackupRecommendations { get; set; } = new();
    public List<string> MonitoringRecommendations { get; set; } = new();

    public void PrintRecommendations(ILogger logger)
    {
        logger.LogInformation("=== Database Configuration Recommendations ===");
        
        logger.LogInformation("Connection String:");
        foreach (var rec in ConnectionStringRecommendations)
            logger.LogInformation("  • {Recommendation}", rec);

        logger.LogInformation("Performance:");
        foreach (var rec in PerformanceRecommendations)
            logger.LogInformation("  • {Recommendation}", rec);

        logger.LogInformation("Security:");
        foreach (var rec in SecurityRecommendations)
            logger.LogInformation("  • {Recommendation}", rec);

        logger.LogInformation("Backup:");
        foreach (var rec in BackupRecommendations)
            logger.LogInformation("  • {Recommendation}", rec);

        logger.LogInformation("Monitoring:");
        foreach (var rec in MonitoringRecommendations)
            logger.LogInformation("  • {Recommendation}", rec);
    }
}